using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace McpOAuthServer.Service.Security
{
    public sealed class SecretHasher : ISecretHasher
    {
        private static string AlgorithmName => "pbkdf2-sha256";

        private static int SaltSize => 16;

        private static int HashSize => 32;

        public static int DefaultIterations => 210000;

        public SecretHasher(int iterations)
        {
            if (iterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations), "The iteration count must be positive.");
            }

            this.iterations = iterations;
        }

        public string HashSecret(string secret)
        {
            ArgumentNullException.ThrowIfNull(secret);

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Derive(secret, salt, iterations);

            return string.Join(
                '$',
                AlgorithmName,
                iterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
        }

        public bool VerifySecret(string secret, string secretHash)
        {
            if (secret is null || string.IsNullOrWhiteSpace(secretHash))
            {
                return false;
            }

            string[] segments = secretHash.Split('$');

            if (segments.Length != 4 || !string.Equals(segments[0], AlgorithmName, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int storedIterations) ||
                storedIterations <= 0)
            {
                return false;
            }

            byte[] salt = DecodeBase64(segments[2]);
            byte[] expectedHash = DecodeBase64(segments[3]);

            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            byte[] actualHash = Derive(secret, salt, storedIterations);

            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }

        private static byte[] DecodeBase64(string value)
        {
            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                return [];
            }
        }

        private static byte[] Derive(string secret, byte[] salt, int iterationCount)
            => Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(secret),
                salt,
                iterationCount,
                HashAlgorithmName.SHA256,
                HashSize);

        private readonly int iterations;
    }
}