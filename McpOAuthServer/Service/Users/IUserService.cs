using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Users
{
    public interface IUserService
    {
        OAuthUser Get(string userId);

        OAuthUser TryGetByUsername(string username);

        UserAuthentication Authenticate(string username, string password);

        void SeedUsers();
    }
}