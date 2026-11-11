using Microsoft.AspNetCore.Mvc;

namespace McpOAuthServer.Api.Requests
{
    public sealed class AuthoriseRequest
    {
        [FromQuery(Name = "response_type")]
        public string ResponseType { get; set; } = string.Empty;

        [FromQuery(Name = "client_id")]
        public string ClientId { get; set; } = string.Empty;

        [FromQuery(Name = "redirect_uri")]
        public string RedirectUri { get; set; } = string.Empty;

        [FromQuery(Name = "scope")]
        public string Scope { get; set; } = string.Empty;

        [FromQuery(Name = "state")]
        public string State { get; set; } = string.Empty;

        [FromQuery(Name = "code_challenge")]
        public string CodeChallenge { get; set; } = string.Empty;

        [FromQuery(Name = "code_challenge_method")]
        public string CodeChallengeMethod { get; set; } = string.Empty;

        [FromQuery(Name = "resource")]
        public string Resource { get; set; } = string.Empty;

        [FromQuery(Name = "nonce")]
        public string Nonce { get; set; } = string.Empty;
    }
}