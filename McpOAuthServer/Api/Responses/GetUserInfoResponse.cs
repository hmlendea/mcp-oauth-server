using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetUserInfoResponse
    {
        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool IsEmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("preferred_username")]
        public string PreferredUsername { get; set; } = string.Empty;
    }
}