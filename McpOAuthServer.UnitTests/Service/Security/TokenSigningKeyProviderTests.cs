using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using McpOAuthServer.Service.Security;

namespace McpOAuthServer.UnitTests.Service.Security
{
    [TestFixture]
    public sealed class TokenSigningKeyProviderTests
    {
        private string temporaryDirectory = string.Empty;

        [SetUp]
        public void SetUp()
            => temporaryDirectory = Path.Combine(Path.GetTempPath(), $"mcp-oauth-server-{Guid.NewGuid():N}");

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void GivenAnAbsentKey_WhenProvidingASigningKey_ThenAPrivateKeyPemIsCreated()
        {
            string keyPath = Path.Combine(temporaryDirectory, "nested", "signing-key.pem");

            using TokenSigningKeyProvider provider = new(keyPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(keyPath), Is.True);
                Assert.That(File.ReadAllText(keyPath), Does.Contain("BEGIN PRIVATE KEY"));
                Assert.That(provider.Modulus, Is.Not.Empty);
                Assert.That(provider.Exponent, Is.Not.Empty);
                Assert.That(provider.KeyId, Is.Not.Empty);
            }
        }

        [Test]
        public void GivenAnExistingKey_WhenProvidingASigningKey_ThenTheSameKeyIsReused()
        {
            string keyPath = Path.Combine(temporaryDirectory, "signing-key.pem");

            using TokenSigningKeyProvider first = new(keyPath);
            using TokenSigningKeyProvider second = new(keyPath);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(second.Modulus, Is.EqualTo(first.Modulus));
                Assert.That(second.KeyId, Is.EqualTo(first.KeyId));
            }
        }

        [Test]
        public void GivenASigningKey_WhenReadingCredentials_ThenRsaSha256IsUsed()
        {
            using TokenSigningKeyProvider provider = new(
                Path.Combine(temporaryDirectory, "signing-key.pem"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(provider.SigningCredentials.Algorithm, Is.EqualTo("RS256"));
                Assert.That(provider.SigningCredentials.Key.KeyId, Is.EqualTo(provider.KeyId));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void GivenABlankPath_WhenProvidingASigningKey_ThenItIsRejected(string keyPath)
            => Assert.That(() => new TokenSigningKeyProvider(keyPath), Throws.TypeOf<ArgumentException>());
    }
}