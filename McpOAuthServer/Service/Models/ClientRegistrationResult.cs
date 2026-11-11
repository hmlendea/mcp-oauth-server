namespace McpOAuthServer.Service.Models
{
    public sealed class ClientRegistrationResult
    {
        public bool IsSuccessful { get; set; }

        public string ErrorDescription { get; set; } = string.Empty;

        public RegisteredClient Client { get; set; }
    }
}