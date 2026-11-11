using Microsoft.AspNetCore.Mvc;

namespace McpOAuthServer.Api.Requests
{
    public sealed class AuthoriseDecisionRequest
    {
        [FromForm(Name = "response_type")]
        public string ResponseType { get; set; } = string.Empty;

        [FromForm(Name = "client_id")]
        public string ClientId { get; set; } = string.Empty;

        [FromForm(Name = "redirect_uri")]
        public string RedirectUri { get; set; } = string.Empty;

        [FromForm(Name = "scope")]
        public string Scope { get; set; } = string.Empty;

        [FromForm(Name = "state")]
        public string State { get; set; } = string.Empty;

        [FromForm(Name = "code_challenge")]
        public string CodeChallenge { get; set; } = string.Empty;

        [FromForm(Name = "code_challenge_method")]
        public string CodeChallengeMethod { get; set; } = string.Empty;

        [FromForm(Name = "resource")]
        public string Resource { get; set; } = string.Empty;

        [FromForm(Name = "nonce")]
        public string Nonce { get; set; } = string.Empty;

        [FromForm(Name = "username")]
        public string Username { get; set; } = string.Empty;

        [FromForm(Name = "password")]
        public string Password { get; set; } = string.Empty;

        [FromForm(Name = "decision")]
        public string Decision { get; set; } = string.Empty;
    }
}