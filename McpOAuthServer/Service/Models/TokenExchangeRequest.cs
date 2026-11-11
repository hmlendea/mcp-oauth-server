namespace McpOAuthServer.Service.Models
{
    public sealed class TokenExchangeRequest
    {
        public string GrantType { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string RedirectUri { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string CodeVerifier { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string Resource { get; set; } = string.Empty;
    }
}
