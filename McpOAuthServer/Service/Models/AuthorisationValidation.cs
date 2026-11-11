using System.Collections.Generic;

namespace McpOAuthServer.Service.Models
{
    public sealed class AuthorisationValidation
    {
        public bool IsValid { get; set; }

        public string Error { get; set; } = string.Empty;

        public string ErrorDescription { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public IEnumerable<string> ScopeValues { get; set; } = [];
    }
}
