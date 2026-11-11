using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace McpOAuthServer.Service.Security
{
    public static class ProofKeyForCodeExchange
    {
        private static string ChallengeMethodName => "S256";

        public static bool IsSupportedChallengeMethod(string codeChallengeMethod)
            => string.Equals(codeChallengeMethod, ChallengeMethodName, StringComparison.Ordinal);

        public static bool Verify(string codeVerifier, string codeChallenge)
        {
            if (string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(codeChallenge))
            {
                return false;
            }

            byte[] digest = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            string computedChallenge = Base64UrlEncoder.Encode(digest);

            return string.Equals(computedChallenge, codeChallenge, StringComparison.Ordinal);
        }
    }
}
