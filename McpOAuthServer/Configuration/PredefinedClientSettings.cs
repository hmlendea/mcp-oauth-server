using System.Collections.Generic;

namespace McpOAuthServer.Configuration
{
    public sealed class PredefinedClientSettings
    {
        public string ClientId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public IEnumerable<string> RedirectUris { get; set; } = [];

        public IEnumerable<string> AllowedScopes { get; set; } = [];

        public bool IsPublic { get; set; }

        public string SecretHash { get; set; } = string.Empty;
    }
}