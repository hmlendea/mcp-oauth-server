using System;

namespace McpOAuthServer.Service.Models
{
    public sealed class AuthorisationCode
    {
        public string Id { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string CodeChallenge { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public DateTimeOffset AuthenticationDateTime { get; set; }

        public DateTimeOffset ExpiryDateTime { get; set; }
    }
}
