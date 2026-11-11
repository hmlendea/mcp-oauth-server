using System;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace McpOAuthServer.Service.Security
{
    public static class SecureTokenGenerator
    {
        private static int TokenSize => 32;

        private static int ClientIdSize => 16;

        public static string GenerateToken()
            => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSize));

        public static string GenerateClientId()
            => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(ClientIdSize));
    }
}
