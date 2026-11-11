using System.Collections.Generic;

namespace McpOAuthServer.Api.Models
{
    public sealed class AuthorisationPage
    {
        public string ClientName { get; set; } = string.Empty;

        public IEnumerable<string> Scopes { get; set; } = [];

        public string ResponseType { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string CodeChallenge { get; set; } = string.Empty;

        public string CodeChallengeMethod { get; set; } = string.Empty;

        public string Resource { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}