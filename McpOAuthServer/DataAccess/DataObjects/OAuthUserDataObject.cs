using NuciDAL.DataObjects;

namespace McpOAuthServer.DataAccess.DataObjects
{
    public sealed class OAuthUserDataObject : EntityBase
    {
        public string Username { get; set; }

        public string PasswordHash { get; set; }

        public string Email { get; set; }

        public string DisplayName { get; set; }

        public bool IsEmailVerified { get; set; }
    }
}
