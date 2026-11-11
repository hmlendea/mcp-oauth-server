namespace McpOAuthServer.Service.Models
{
    public sealed class ClientAuthentication
    {
        public bool IsAuthenticated { get; set; }

        public string ClientId { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        public OAuthClient Client { get; set; }
    }
}
