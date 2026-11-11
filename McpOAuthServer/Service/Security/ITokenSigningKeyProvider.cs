using Microsoft.IdentityModel.Tokens;

namespace McpOAuthServer.Service.Security
{
    public interface ITokenSigningKeyProvider
    {
        SigningCredentials SigningCredentials { get; }

        SecurityKey SecurityKey { get; }

        string KeyId { get; }

        string Modulus { get; }

        string Exponent { get; }
    }
}