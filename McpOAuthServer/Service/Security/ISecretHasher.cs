namespace McpOAuthServer.Service.Security
{
    public interface ISecretHasher
    {
        string HashSecret(string secret);

        bool VerifySecret(string secret, string secretHash);
    }
}