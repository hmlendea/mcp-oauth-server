using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetRegisteredClientResponse
    {
        [JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;

        [JsonPropertyName("client_name")]
        public string ClientName { get; set; } = string.Empty;

        [JsonPropertyName("client_id_issued_at")]
        public long ClientIdIssuedAt { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("token_endpoint_auth_method")]
        public string TokenEndpointAuthenticationMethod { get; set; } = "none";

        [JsonPropertyName("grant_types")]
        public string[] GrantTypes { get; set; } = ["authorization_code", "refresh_token"];
    }
}