namespace McpOAuthServer.Service.Models
{
    public sealed class TokenIssueRequest
    {
        public string ClientId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string Scope { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string Nonce { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }

        public bool IncludeRefreshToken { get; set; }
    }
}