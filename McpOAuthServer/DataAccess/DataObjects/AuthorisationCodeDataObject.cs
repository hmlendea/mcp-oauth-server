using NuciDAL.DataObjects;

namespace McpOAuthServer.DataAccess.DataObjects
{
    public sealed class AuthorisationCodeDataObject : EntityBase
    {
        public string ClientId { get; set; }

        public string UserId { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }

        public string Audience { get; set; }

        public string CodeChallenge { get; set; }

        public string Nonce { get; set; }

        public string AuthenticationTimestamp { get; set; }

        public string ExpiryTimestamp { get; set; }
    }
}
