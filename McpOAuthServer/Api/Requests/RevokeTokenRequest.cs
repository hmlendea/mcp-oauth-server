using Microsoft.AspNetCore.Mvc;

namespace McpOAuthServer.Api.Requests
{
    public sealed class RevokeTokenRequest
    {
        [FromForm(Name = "token")]
        public string Token { get; set; } = string.Empty;

        [FromForm(Name = "token_type_hint")]
        public string TokenTypeHint { get; set; } = string.Empty;

        [FromForm(Name = "client_id")]
        public string ClientId { get; set; } = string.Empty;

        [FromForm(Name = "client_secret")]
        public string ClientSecret { get; set; } = string.Empty;
    }
}