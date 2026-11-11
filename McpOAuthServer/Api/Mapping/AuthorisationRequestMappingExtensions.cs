using System.Collections.Generic;

using McpOAuthServer.Api.Requests;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Api.Mapping
{
    internal static class AuthorisationRequestMappingExtensions
    {
        private static string ApprovedDecision => "approve";

        internal static AuthorisationGrant ToServiceModel(this AuthoriseRequest request) => new()
        {
            ResponseType = request.ResponseType,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Resource = request.Resource,
            Nonce = request.Nonce,
            IsApproved = true
        };

        internal static AuthorisationGrant ToServiceModel(this AuthoriseDecisionRequest request) => new()
        {
            ResponseType = request.ResponseType,
            ClientId = request.ClientId,
            RedirectUri = request.RedirectUri,
            Scope = request.Scope,
            State = request.State,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            Resource = request.Resource,
            Nonce = request.Nonce,
            Username = request.Username,
            Password = request.Password,
            IsApproved = string.Equals(request.Decision, ApprovedDecision, System.StringComparison.Ordinal)
        };
    }
}