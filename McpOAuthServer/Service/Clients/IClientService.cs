using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Clients
{
    public interface IClientService
    {
        OAuthClient Get(string clientId);

        OAuthClient TryGet(string clientId);

        ClientRegistrationResult Register(ClientRegistrationRequest request);

        ClientAuthentication Authenticate(string clientId, string clientSecret);

        bool IsRedirectUriAllowed(OAuthClient client, string redirectUri);

        void SeedClients();
    }
}