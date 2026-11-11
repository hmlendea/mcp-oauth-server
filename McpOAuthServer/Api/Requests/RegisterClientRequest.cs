using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Requests
{
    public sealed class RegisterClientRequest
    {
        [JsonPropertyName("client_name")]
        public string ClientName { get; set; } = string.Empty;

        [JsonPropertyName("redirect_uris")]
        public string[] RedirectUris { get; set; } = [];

        [JsonPropertyName("grant_types")]
        public string[] GrantTypes { get; set; } = [];

        [JsonPropertyName("scope")]
        public string[] Scope { get; set; } = [];

        [JsonPropertyName("token_endpoint_auth_method")]
        public string TokenEndpointAuthMethod { get; set; } = string.Empty;
    }
}