using System.Collections.Generic;

using NuciDAL.DataObjects;

namespace McpOAuthServer.DataAccess.DataObjects
{
    public sealed class OAuthClientDataObject : EntityBase
    {
        public string Name { get; set; }

        public IEnumerable<string> RedirectUris { get; set; }

        public IEnumerable<string> AllowedScopes { get; set; }

        public bool IsPublic { get; set; }

        public string SecretHash { get; set; }

        public string CreatedTimestamp { get; set; }
    }
}
