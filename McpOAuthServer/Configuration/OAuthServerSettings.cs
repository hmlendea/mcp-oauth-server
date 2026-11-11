using System.Collections.Generic;

namespace McpOAuthServer.Configuration
{
    public sealed class OAuthServerSettings
    {
        public string Issuer { get; set; } = string.Empty;

        public string SigningKeyPath { get; set; } = "Data/signing-key.pem";

        public IEnumerable<string> SupportedScopes { get; set; } = [];

        public IEnumerable<string> AllowedResources { get; set; } = [];

        public IEnumerable<string> AllowedRedirectUriPrefixes { get; set; } = [];

        public int AuthorisationCodeLifetimeSeconds { get; set; } = 60;

        public int AccessTokenLifetimeSeconds { get; set; } = 3600;

        public int IdTokenLifetimeSeconds { get; set; } = 3600;

        public int RefreshTokenLifetimeSeconds { get; set; } = 2592000;

        public int PasswordHashIterations { get; set; } = 210000;

        public IEnumerable<PredefinedClientSettings> PredefinedClients { get; set; } = [];

        public IEnumerable<SeedUserSettings> SeedUsers { get; set; } = [];
    }
}
