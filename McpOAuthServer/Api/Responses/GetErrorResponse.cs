using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;
    }
}