using NuciLog.Core;

namespace McpOAuthServer.Logging
{
    public sealed class OAuthServerOperation : Operation
    {
        private OAuthServerOperation(string name) : base(name)
        {
        }

        public static Operation RegisterClient => new OAuthServerOperation(nameof(RegisterClient));

        public static Operation AuthenticateClient => new OAuthServerOperation(nameof(AuthenticateClient));

        public static Operation AuthenticateUser => new OAuthServerOperation(nameof(AuthenticateUser));

        public static Operation SeedUsers => new OAuthServerOperation(nameof(SeedUsers));

        public static Operation ValidateAuthorisation => new OAuthServerOperation(nameof(ValidateAuthorisation));

        public static Operation CreateAuthorisationCode => new OAuthServerOperation(nameof(CreateAuthorisationCode));

        public static Operation ExchangeAuthorisationCode => new OAuthServerOperation(nameof(ExchangeAuthorisationCode));

        public static Operation ExchangeRefreshToken => new OAuthServerOperation(nameof(ExchangeRefreshToken));

        public static Operation RevokeRefreshToken => new OAuthServerOperation(nameof(RevokeRefreshToken));

        public static Operation InspectAccessToken => new OAuthServerOperation(nameof(InspectAccessToken));
    }
}
