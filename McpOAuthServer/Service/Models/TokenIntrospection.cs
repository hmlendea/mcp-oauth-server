using System;

namespace McpOAuthServer.Service.Models
{
    public sealed class TokenIntrospection
    {
        public bool IsActive { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public DateTimeOffset IssuedAtDateTime { get; set; }

        public DateTimeOffset ExpiryDateTime { get; set; }
    }
}
