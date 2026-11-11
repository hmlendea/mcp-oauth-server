using System;
using System.IO;
using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

namespace McpOAuthServer.Service.Security
{
    public sealed class TokenSigningKeyProvider : ITokenSigningKeyProvider, IDisposable
    {
        private static int KeySizeInBits => 2048;

        public TokenSigningKeyProvider(string keyPath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPath);

            string fullPath = Path.GetFullPath(keyPath);
            CreateKeyIfMissing(fullPath);

            rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(fullPath));

            RSAParameters parameters = rsa.ExportParameters(false);

            Modulus = Base64UrlEncoder.Encode(parameters.Modulus);
            Exponent = Base64UrlEncoder.Encode(parameters.Exponent);
            KeyId = ComputeKeyId(parameters.Modulus);

            SecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = KeyId
            };
            SigningCredentials = new(SecurityKey, SecurityAlgorithms.RsaSha256);
        }

        public SigningCredentials SigningCredentials { get; }

        public SecurityKey SecurityKey { get; }

        public string KeyId { get; }

        public string Modulus { get; }

        public string Exponent { get; }

        public void Dispose() => rsa.Dispose();

        private static void CreateKeyIfMissing(string path)
        {
            string directory = Path.GetDirectoryName(path);

            if (directory is null)
            {
                directory = string.Empty;
            }

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(path))
            {
                return;
            }

            using RSA generatedKey = RSA.Create(KeySizeInBits);
            File.WriteAllText(path, generatedKey.ExportPkcs8PrivateKeyPem());

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }

        private static string ComputeKeyId(byte[] modulus)
            => Base64UrlEncoder.Encode(SHA256.HashData(modulus));

        private readonly RSA rsa;
    }
}