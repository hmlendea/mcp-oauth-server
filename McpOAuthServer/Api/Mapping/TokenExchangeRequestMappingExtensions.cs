using McpOAuthServer.Api.Requests;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Api.Mapping
{
    internal static class TokenExchangeRequestMappingExtensions
    {
        internal static TokenExchangeRequest ToServiceModel(this ExchangeTokenRequest request) => new()
        {
            GrantType = request.GrantType,
            Code = request.Code,
            RedirectUri = request.RedirectUri,
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            CodeVerifier = request.CodeVerifier,
            RefreshToken = request.RefreshToken,
            Scope = request.Scope,
            Resource = request.Resource
        };
    }
}