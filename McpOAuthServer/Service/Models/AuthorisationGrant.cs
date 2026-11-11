using System;

namespace McpOAuthServer.Service.Models
{
    public sealed class AuthorisationGrant
    {
        public string ResponseType { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string CodeChallenge { get; set; } = string.Empty;

        public string CodeChallengeMethod { get; set; } = string.Empty;

        public string Resource { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool IsApproved { get; set; }
    }
}
