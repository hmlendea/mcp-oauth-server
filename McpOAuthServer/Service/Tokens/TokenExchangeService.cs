using System;
using System.Collections.Generic;
using System.Linq;

using NuciLog.Core;

using McpOAuthServer.Configuration;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.AuthorisationCodes;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.RefreshTokens;
using McpOAuthServer.Service.Security;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer.Service.Tokens
{
    public sealed class TokenExchangeService(
        OAuthServerSettings settings,
        IClientService clientService,
        IAuthorisationCodeService authorisationCodeService,
        IRefreshTokenService refreshTokenService,
        IUserService userService,
        ITokenService tokenService,
        ILogger logger) : ITokenExchangeService
    {
        private static string AuthorisationCodeGrantType => "authorization_code";

        private static string RefreshTokenGrantType => "refresh_token";

        public TokenExchangeResult Exchange(TokenExchangeRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            logger.Info(
                OAuthServerOperation.ExchangeAuthorisationCode,
                OperationStatus.Started,
                new LogInfo(OAuthServerLogInfoKey.GrantType, request.GrantType),
                new LogInfo(OAuthServerLogInfoKey.ClientId, request.ClientId));

            if (string.Equals(request.GrantType, AuthorisationCodeGrantType, StringComparison.Ordinal))
            {
                return ExchangeAuthorisationCode(request);
            }

            if (string.Equals(request.GrantType, RefreshTokenGrantType, StringComparison.Ordinal))
            {
                return ExchangeRefreshToken(request);
            }

            return Failure("unsupported_grant_type", "The requested grant type is not supported.");
        }

        private TokenExchangeResult ExchangeAuthorisationCode(TokenExchangeRequest request)
        {
            ClientAuthentication authentication = AuthenticateClient(request);

            if (!authentication.IsAuthenticated)
            {
                return Failure("invalid_client", "The client credentials are invalid.");
            }

            AuthorisationCode authorisationCode;

            try
            {
                authorisationCode = authorisationCodeService.Consume(request.Code);
            }
            catch (KeyNotFoundException)
            {
                return Failure("invalid_grant", "The authorisation code is unknown or was already used.");
            }

            if (!string.Equals(authorisationCode.ClientId, authentication.ClientId, StringComparison.Ordinal))
            {
                return Failure("invalid_grant", "The authorisation code belongs to another client.");
            }

            if (authorisationCode.ExpiryDateTime <= DateTimeOffset.UtcNow)
            {
                return Failure("invalid_grant", "The authorisation code has expired.");
            }

            if (!string.Equals(authorisationCode.RedirectUri, request.RedirectUri, StringComparison.Ordinal))
            {
                return Failure("invalid_grant", "The redirect URI does not match the authorisation request.");
            }

            if (!ProofKeyForCodeExchange.Verify(request.CodeVerifier, authorisationCode.CodeChallenge))
            {
                return Failure("invalid_grant", "The code verifier does not match the code challenge.");
            }

            OAuthUser user;

            try
            {
                user = userService.Get(authorisationCode.UserId);
            }
            catch (NuciDAL.Repositories.EntityNotFoundException)
            {
                return Failure("invalid_grant", "The authorising account is no longer available.");
            }

            IssuedTokens tokens = tokenService.IssueAccessTokens(new()
            {
                ClientId = authentication.ClientId,
                UserId = user.Id,
                Scope = authorisationCode.Scope,
                Audience = authorisationCode.Audience,
                Nonce = authorisationCode.Nonce,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsEmailVerified = user.IsEmailVerified,
                IncludeRefreshToken = true
            });

            return Success(tokens);
        }

        private TokenExchangeResult ExchangeRefreshToken(TokenExchangeRequest request)
        {
            ClientAuthentication authentication = AuthenticateClient(request);

            if (!authentication.IsAuthenticated)
            {
                return Failure("invalid_client", "The client credentials are invalid.");
            }

            RefreshToken refreshToken;

            try
            {
                refreshToken = refreshTokenService.Consume(request.RefreshToken);
            }
            catch (KeyNotFoundException)
            {
                return Failure("invalid_grant", "The refresh token is unknown or was already used.");
            }

            if (!string.Equals(refreshToken.ClientId, authentication.ClientId, StringComparison.Ordinal))
            {
                return Failure("invalid_grant", "The refresh token belongs to another client.");
            }

            if (refreshToken.ExpiryDateTime <= DateTimeOffset.UtcNow)
            {
                return Failure("invalid_grant", "The refresh token has expired.");
            }

            OAuthUser user;

            try
            {
                user = userService.Get(refreshToken.UserId);
            }
            catch (NuciDAL.Repositories.EntityNotFoundException)
            {
                return Failure("invalid_grant", "The authorising account is no longer available.");
            }

            List<string> requestedScopes = [.. request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
            List<string> grantedScopes = [.. OAuthServerSettingsValidator.NormaliseScopes(
                refreshToken.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                settings.SupportedScopes)];

            if (requestedScopes.Count > 0 &&
                requestedScopes.Any(scope => !grantedScopes.Contains(scope, StringComparer.Ordinal)))
            {
                return Failure("invalid_scope", "The requested scopes exceed the original grant.");
            }

            IssuedTokens tokens = tokenService.IssueAccessTokens(new()
            {
                ClientId = authentication.ClientId,
                UserId = user.Id,
                Scope = string.Join(' ', grantedScopes),
                Audience = refreshToken.Audience,
                Nonce = refreshToken.Nonce,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsEmailVerified = user.IsEmailVerified,
                IncludeRefreshToken = true
            });

            return Success(tokens);
        }

        private ClientAuthentication AuthenticateClient(TokenExchangeRequest request)
            => clientService.Authenticate(request.ClientId, request.ClientSecret);

        private static TokenExchangeResult Success(IssuedTokens tokens)
            => new()
            {
                IsSuccessful = true,
                Tokens = tokens
            };

        private static TokenExchangeResult Failure(string error, string description)
            => new()
            {
                IsSuccessful = false,
                Error = error,
                ErrorDescription = description
            };
    }
}
