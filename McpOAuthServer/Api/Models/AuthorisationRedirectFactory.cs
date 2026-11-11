using System.Collections.Generic;

namespace McpOAuthServer.Api.Models
{
    public static class AuthorisationRedirectFactory
    {
        private static string CodeParameterName => "code";

        private static string ErrorParameterName => "error";

        private static string ErrorDescriptionParameterName => "error_description";

        public static AuthorisationRedirect CreateCodeRedirect(
            string redirectUri,
            string code,
            string state,
            string issuer)
            => new()
            {
                RedirectUri = redirectUri,
                State = state,
                Issuer = issuer,
                Parameters = [new(CodeParameterName, code)]
            };

        public static AuthorisationRedirect CreateErrorRedirect(
            string redirectUri,
            string error,
            string errorDescription,
            string state,
            string issuer)
            => new()
            {
                RedirectUri = redirectUri,
                State = state,
                Issuer = issuer,
                Parameters =
                [
                    new(ErrorParameterName, error),
                    new(ErrorDescriptionParameterName, errorDescription)
                ]
            };
    }
}