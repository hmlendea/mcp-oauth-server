namespace McpOAuthServer.Configuration
{
    public sealed class DataStoreSettings
    {
        public string AuthorisationCodesStorePath { get; set; } = "Data/authorisation-codes.json";

        public string ClientsStorePath { get; set; } = "Data/clients.json";

        public string RefreshTokensStorePath { get; set; } = "Data/refresh-tokens.json";

        public string UsersStorePath { get; set; } = "Data/users.json";
    }
}
