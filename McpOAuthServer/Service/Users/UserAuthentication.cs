using McpOAuthServer.Service.Models;

namespace McpOAuthServer.Service.Users
{
    public sealed class UserAuthentication
    {
        public bool IsAuthenticated { get; set; }

        public OAuthUser User { get; set; }
    }
}