using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Tokens
{
    public interface ITokenService
    {
        IssuedTokens IssueAccessTokens(TokenIssueRequest request);

        TokenIntrospection Introspect(string token);
    }
}