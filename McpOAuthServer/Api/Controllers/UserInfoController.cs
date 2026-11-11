using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

using Microsoft.IdentityModel.JsonWebTokens;

using McpOAuthServer.Api.Responses;
using McpOAuthServer.Configuration;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer.Api.Controllers
{
    [Route("userinfo")]
    [ApiController]
    public sealed class UserInfoController(
        OAuthServerSettings settings,
        ITokenSigningKeyProvider keyProvider,
        IUserService userService) : ControllerBase
    {
        private static string BearerPrefix => "Bearer ";

        private static string SubjectClaimName => "sub";

        [HttpGet]
        public ActionResult Get(
            [FromHeader(Name = "Authorization")] string authorisation)
        {
            string token = ExtractBearerToken(authorisation);

            if (string.IsNullOrWhiteSpace(token))
            {
                return Challenge();
            }

            AccessTokenIdentity identity = ValidateToken(token);

            if (identity is null)
            {
                return Challenge();
            }

            OAuthUser user;

            try
            {
                user = userService.Get(identity.UserId);
            }
            catch (NuciDAL.Repositories.EntityNotFoundException)
            {
                return Challenge();
            }

            return new JsonResult(new GetUserInfoResponse
            {
                Subject = user.Id,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Name = user.DisplayName,
                PreferredUsername = user.Username
            })
            {
                StatusCode = 200
            };
        }

        private AccessTokenIdentity ValidateToken(string token)
        {
            JsonWebTokenHandler handler = new();
            Microsoft.IdentityModel.Tokens.TokenValidationParameters parameters = new()
            {
                ValidIssuer = settings.Issuer,
                IssuerSigningKey = keyProvider.SecurityKey,
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidTypes = ["at+jwt"]
            };

            try
            {
                Microsoft.IdentityModel.Tokens.TokenValidationResult result = handler
                    .ValidateTokenAsync(token, parameters)
                    .GetAwaiter()
                    .GetResult();

                if (!result.IsValid)
                {
                    return null;
                }

                return new()
                {
                    UserId = GetClaimValue(
                    result.ClaimsIdentity,
                    SubjectClaimName),
                    Scope = GetClaimValue(
                        result.ClaimsIdentity,
                        "scope")
                };
            }
            catch (Microsoft.IdentityModel.Tokens.SecurityTokenException)
            {
                return null;
            }
        }

        private static string GetClaimValue(
            System.Security.Claims.ClaimsIdentity identity,
            string claimType)
        {
            System.Security.Claims.Claim claim = identity.FindFirst(claimType);

            if (claim is null)
            {
                return string.Empty;
            }

            return claim.Value;
        }

        private static string ExtractBearerToken(string authorisation)
        {
            if (string.IsNullOrWhiteSpace(authorisation) ||
                !authorisation.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return authorisation[BearerPrefix.Length..].Trim();
        }

        private static IEnumerable<string> EmptyScopes => [];
    }
}
