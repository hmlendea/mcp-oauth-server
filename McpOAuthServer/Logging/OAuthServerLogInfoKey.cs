using NuciLog.Core;

namespace McpOAuthServer.Logging
{
    public sealed class OAuthServerLogInfoKey : LogInfoKey
    {
        private OAuthServerLogInfoKey(string name) : base(name)
        {
        }

        public static LogInfoKey ClientId => new OAuthServerLogInfoKey(nameof(ClientId));

        public static LogInfoKey UserId => new OAuthServerLogInfoKey(nameof(UserId));

        public static LogInfoKey GrantType => new OAuthServerLogInfoKey(nameof(GrantType));

        public static LogInfoKey Scope => new OAuthServerLogInfoKey(nameof(Scope));

        public static LogInfoKey Audience => new OAuthServerLogInfoKey(nameof(Audience));

        public static LogInfoKey Error => new OAuthServerLogInfoKey(nameof(Error));

        public static LogInfoKey Count => new OAuthServerLogInfoKey(nameof(Count));
    }
}
