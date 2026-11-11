namespace McpOAuthServer.Service.Models
{
    public sealed class RegisteredClient
    {
        public string ClientId { get; set; } = string.Empty;

        public string ClientSecret { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsPublic { get; set; }

        public string Scope { get; set; } = string.Empty;
    }
}
