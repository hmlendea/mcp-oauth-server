using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.RefreshTokens
{
    public interface IRefreshTokenService
    {
        string Create(RefreshToken refreshToken);

        RefreshToken Consume(string token);

        bool Revoke(string token);
    }
}