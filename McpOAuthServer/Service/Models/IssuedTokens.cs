namespace McpOAuthServer.Service.Models
{
    public sealed class IssuedTokens
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public int ExpiresInSeconds { get; set; }

        public string Scope { get; set; } = string.Empty;

        public string IdToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}
