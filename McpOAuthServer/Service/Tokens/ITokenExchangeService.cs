using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Tokens
{
    public interface ITokenExchangeService
    {
        TokenExchangeResult Exchange(TokenExchangeRequest request);
    }
}
