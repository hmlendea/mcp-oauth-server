using McpOAuthServer.Api.Requests;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Api.Mapping
{
    internal static class ClientRegistrationRequestMappingExtensions
    {
        internal static ClientRegistrationRequest ToServiceModel(this RegisterClientRequest request) => new()
        {
            Name = request.ClientName,
            RedirectUris = request.RedirectUris,
            GrantTypes = request.GrantTypes,
            Scopes = request.Scope,
            TokenEndpointAuthenticationMethod = request.TokenEndpointAuthMethod
        };
    }
}