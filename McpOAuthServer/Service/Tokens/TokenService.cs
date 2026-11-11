using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NuciLog.Core;

using McpOAuthServer.Configuration;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.RefreshTokens;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.Tokens
{
    public sealed class TokenService(
        OAuthServerSettings settings,
        ITokenSigningKeyProvider keyProvider,
        IRefreshTokenService refreshTokenService,
        ILogger logger) : ITokenService
    {
        private static string ScopeClaimName => "scope";

        private static string EmailClaimName => "email";

        private static string EmailVerifiedClaimName => "email_verified";

        private static string NameClaimName => "name";

        private static string SubjectClaimName => "sub";

        public IssuedTokens IssueAccessTokens(TokenIssueRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
            DateTimeOffset accessExpiry = issuedAt.AddSeconds(settings.AccessTokenLifetimeSeconds);
            string accessToken = CreateAccessToken(request, issuedAt, accessExpiry);
            string idToken = string.Empty;

            if (ContainsScope(request.Scope, "openid"))
            {
                idToken = CreateIdToken(request, issuedAt);
            }

            string refreshToken = string.Empty;

            if (request.IncludeRefreshToken)
            {
                refreshToken = refreshTokenService.Create(new()
                {
                    ClientId = request.ClientId,
                    UserId = request.UserId,
                    Scope = request.Scope,
                    Audience = request.Audience,
                    Nonce = request.Nonce,
                    ExpiryDateTime = issuedAt.AddSeconds(settings.RefreshTokenLifetimeSeconds)
                });
            }

            logger.Info(
                OAuthServerOperation.ExchangeAuthorisationCode,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.ClientId, request.ClientId),
                new LogInfo(OAuthServerLogInfoKey.UserId, request.UserId),
                new LogInfo(OAuthServerLogInfoKey.Scope, request.Scope));

            return new()
            {
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresInSeconds = settings.AccessTokenLifetimeSeconds,
                Scope = request.Scope,
                IdToken = idToken,
                RefreshToken = refreshToken
            };
        }

        public TokenIntrospection Introspect(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new() { IsActive = false };
            }

            JsonWebTokenHandler handler = new();
            TokenValidationParameters parameters = new()
            {
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.AllowedResources.First(),
                IssuerSigningKey = keyProvider.SecurityKey,
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidTypes = ["at+jwt"]
            };

            try
            {
                TokenValidationResult result = handler
                    .ValidateTokenAsync(token, parameters)
                    .GetAwaiter()
                    .GetResult();

                if (!result.IsValid)
                {
                    return new() { IsActive = false };
                }

                return CreateIntrospection(result.ClaimsIdentity);
            }
            catch (SecurityTokenException)
            {
                return new() { IsActive = false };
            }
        }

        private static TokenIntrospection CreateIntrospection(ClaimsIdentity identity)
        {
            string expiryClaim = GetClaimValue(identity, "exp");
            string issuedAtClaim = GetClaimValue(identity, "iat");

            return new()
            {
                IsActive = true,
                Scope = GetClaimValue(identity, ScopeClaimName),
                ClientId = GetClaimValue(identity, "client_id"),
                UserId = GetClaimValue(identity, SubjectClaimName),
                Audience = GetClaimValue(identity, "aud"),
                IssuedAtDateTime = DateTimeOffset.FromUnixTimeSeconds(ParseLong(issuedAtClaim)),
                ExpiryDateTime = DateTimeOffset.FromUnixTimeSeconds(ParseLong(expiryClaim))
            };
        }

        private static string GetClaimValue(ClaimsIdentity identity, string claimType)
        {
            Claim claim = identity.FindFirst(claimType);

            if (claim is null)
            {
                return string.Empty;
            }

            return claim.Value;
        }

        private static long ParseLong(string value)
        {
            if (!long.TryParse(value, out long parsed))
            {
                return 0;
            }

            return parsed;
        }

        private string CreateAccessToken(
            TokenIssueRequest request,
            DateTimeOffset issuedAt,
            DateTimeOffset expiry)
        {
            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = settings.Issuer,
                Audience = request.Audience,
                IssuedAt = issuedAt.UtcDateTime,
                NotBefore = issuedAt.UtcDateTime,
                Expires = expiry.UtcDateTime,
                TokenType = "at+jwt",
                SigningCredentials = keyProvider.SigningCredentials,
                Claims = new Dictionary<string, object>()
            };

            PopulateClaims(descriptor, request, includeProfile: false);

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private string CreateIdToken(TokenIssueRequest request, DateTimeOffset issuedAt)
        {
            SecurityTokenDescriptor descriptor = new()
            {
                Issuer = settings.Issuer,
                Audience = request.ClientId,
                IssuedAt = issuedAt.UtcDateTime,
                NotBefore = issuedAt.UtcDateTime,
                Expires = issuedAt.AddSeconds(settings.IdTokenLifetimeSeconds).UtcDateTime,
                SigningCredentials = keyProvider.SigningCredentials,
                Claims = new Dictionary<string, object>()
            };

            PopulateClaims(descriptor, request, includeProfile: true);

            if (!string.IsNullOrEmpty(request.Nonce))
            {
                descriptor.Claims.Add("nonce", request.Nonce);
            }

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private static void PopulateClaims(
            SecurityTokenDescriptor descriptor,
            TokenIssueRequest request,
            bool includeProfile)
        {
            descriptor.Claims.Add(SubjectClaimName, request.UserId);
            descriptor.Claims.Add("client_id", request.ClientId);
            descriptor.Claims.Add(ScopeClaimName, request.Scope);

            if (!includeProfile)
            {
                return;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                descriptor.Claims.Add(EmailClaimName, request.Email);
                descriptor.Claims.Add(EmailVerifiedClaimName, request.IsEmailVerified);
            }

            if (!string.IsNullOrEmpty(request.DisplayName))
            {
                descriptor.Claims.Add(NameClaimName, request.DisplayName);
            }
        }

        private static bool ContainsScope(string scope, string expectedScope)
            => !string.IsNullOrEmpty(scope) && scope
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(value => string.Equals(value, expectedScope, StringComparison.Ordinal));
    }
}