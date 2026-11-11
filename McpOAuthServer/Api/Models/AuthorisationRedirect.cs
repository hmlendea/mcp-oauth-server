using System.Collections.Generic;

namespace McpOAuthServer.Api.Models
{
    public sealed class AuthorisationRedirect
    {
        public string RedirectUri { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;

        public IEnumerable<KeyValuePair<string, string>> Parameters { get; set; } = [];
    }
}