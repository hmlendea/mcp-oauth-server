namespace McpOAuthServer.Service.Models
{
    public sealed class TokenExchangeResult
    {
        public bool IsSuccessful { get; set; }

        public string Error { get; set; } = string.Empty;

        public string ErrorDescription { get; set; } = string.Empty;

        public IssuedTokens Tokens { get; set; }
    }
}
