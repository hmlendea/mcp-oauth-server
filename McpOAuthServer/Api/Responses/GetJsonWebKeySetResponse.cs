using System.Collections.Generic;

using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetJsonWebKeySetResponse
    {
        [JsonPropertyName("keys")]
        public IEnumerable<GetJsonWebKeyResponse> Keys { get; set; } = [];
    }
}