using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using McpOAuthServer.Api.Responses;
using McpOAuthServer.Configuration;

namespace McpOAuthServer.Api.Controllers
{
    [Route(".well-known")]
    [ApiController]
    public sealed class DiscoveryController(
        OAuthServerSettings settings) : ControllerBase
    {
        private static IEnumerable<string> ResponseTypesSupported => ["code"];

        private static IEnumerable<string> ResponseModesSupported => ["query"];

        private static IEnumerable<string> GrantTypesSupported => ["authorization_code", "refresh_token"];

        private static IEnumerable<string> CodeChallengeMethodsSupported => ["S256"];

        private static IEnumerable<string> TokenEndpointAuthenticationMethodsSupported
            => ["none", "client_secret_post", "client_secret_basic"];

        private static IEnumerable<string> SubjectTypesSupported => ["public"];

        private static IEnumerable<string> ClaimsSupported
            => ["sub", "email", "email_verified", "name", "preferred_username", "scope"];

        [HttpGet("oauth-authorization-server")]
        public ActionResult Get() => new JsonResult(CreateDiscovery()) { StatusCode = 200 };

        [HttpGet("openid-configuration")]
        public ActionResult GetOpenIdConfiguration() => new JsonResult(CreateDiscovery()) { StatusCode = 200 };

        private GetDiscoveryResponse CreateDiscovery()
            => new()
            {
                Issuer = settings.Issuer,
                AuthorisationEndpoint = $"{settings.Issuer}/authorize",
                TokenEndpoint = $"{settings.Issuer}/token",
                RegistrationEndpoint = $"{settings.Issuer}/register",
                RevocationEndpoint = $"{settings.Issuer}/revoke",
                UserInfoEndpoint = $"{settings.Issuer}/userinfo",
                JsonWebKeySetUri = $"{settings.Issuer}/jwks",
                ResponseTypesSupported = ResponseTypesSupported,
                ResponseModesSupported = ResponseModesSupported,
                GrantTypesSupported = GrantTypesSupported,
                CodeChallengeMethodsSupported = CodeChallengeMethodsSupported,
                TokenEndpointAuthenticationMethodsSupported = TokenEndpointAuthenticationMethodsSupported,
                ScopesSupported = settings.SupportedScopes,
                SubjectTypesSupported = SubjectTypesSupported,
                IdTokenSigningAlgorithmsSupported = ["RS256"],
                ClaimsSupported = ClaimsSupported,
                IsAuthorisationResponseIssuerParameterSupported = true
            };
    }
}