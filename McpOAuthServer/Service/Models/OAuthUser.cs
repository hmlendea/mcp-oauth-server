namespace McpOAuthServer.Service.Models
{
    public sealed class OAuthUser
    {
        public string Id { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public bool IsEmailVerified { get; set; }
    }
}
