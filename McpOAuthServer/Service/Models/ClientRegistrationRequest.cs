using System.Collections.Generic;

namespace McpOAuthServer.Service.Models
{
    public sealed class ClientRegistrationRequest
    {
        public string Name { get; set; } = string.Empty;

        public IEnumerable<string> RedirectUris { get; set; } = [];

        public IEnumerable<string> GrantTypes { get; set; } = [];

        public IEnumerable<string> Scopes { get; set; } = [];

        public string TokenEndpointAuthenticationMethod { get; set; } = string.Empty;
    }
}
