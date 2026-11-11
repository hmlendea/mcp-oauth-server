using System.Collections.Generic;

using System.Text.Json.Serialization;

namespace McpOAuthServer.Api.Responses
{
    public sealed class GetDiscoveryResponse
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonPropertyName("authorization_endpoint")]
        public string AuthorisationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("registration_endpoint")]
        public string RegistrationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("revocation_endpoint")]
        public string RevocationEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("jwks_uri")]
        public string JsonWebKeySetUri { get; set; } = string.Empty;

        [JsonPropertyName("response_types_supported")]
        public IEnumerable<string> ResponseTypesSupported { get; set; } = [];

        [JsonPropertyName("response_modes_supported")]
        public IEnumerable<string> ResponseModesSupported { get; set; } = [];

        [JsonPropertyName("grant_types_supported")]
        public IEnumerable<string> GrantTypesSupported { get; set; } = [];

        [JsonPropertyName("code_challenge_methods_supported")]
        public IEnumerable<string> CodeChallengeMethodsSupported { get; set; } = [];

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public IEnumerable<string> TokenEndpointAuthenticationMethodsSupported { get; set; } = [];

        [JsonPropertyName("scopes_supported")]
        public IEnumerable<string> ScopesSupported { get; set; } = [];

        [JsonPropertyName("subject_types_supported")]
        public IEnumerable<string> SubjectTypesSupported { get; set; } = [];

        [JsonPropertyName("id_token_signing_alg_values_supported")]
        public IEnumerable<string> IdTokenSigningAlgorithmsSupported { get; set; } = [];

        [JsonPropertyName("claims_supported")]
        public IEnumerable<string> ClaimsSupported { get; set; } = [];

        [JsonPropertyName("authorization_response_iss_parameter_supported")]
        public bool IsAuthorisationResponseIssuerParameterSupported { get; set; }
    }
}