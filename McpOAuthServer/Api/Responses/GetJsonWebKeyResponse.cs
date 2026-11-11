using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetJsonWebKeyResponse
    {
        [JsonPropertyName("kty")]
        public string KeyType { get; set; } = "RSA";

        [JsonPropertyName("use")]
        public string Use { get; set; } = "sig";

        [JsonPropertyName("alg")]
        public string Algorithm { get; set; } = "RS256";

        [JsonPropertyName("kid")]
        public string KeyId { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string Modulus { get; set; } = string.Empty;

        [JsonPropertyName("e")]
        public string Exponent { get; set; } = string.Empty;
    }
}