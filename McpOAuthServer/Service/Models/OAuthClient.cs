using System;
using System.Collections.Generic;

namespace McpOAuthServer.Service.Models
{
    public sealed class OAuthClient
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public IEnumerable<string> RedirectUris { get; set; } = [];

        public IEnumerable<string> AllowedScopes { get; set; } = [];

        public bool IsPublic { get; set; }

        public string SecretHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedDateTime { get; set; }
    }
}
