using NUnit.Framework;

using McpOAuthServer.Service.Security;

namespace McpOAuthServer.UnitTests.Service.Security
{
    [TestFixture]
    public sealed class SecretHasherTests
    {
        private static string Password => "0bvious-Passphrase";

        [Test]
        public void GivenAPassword_WhenHashing_ThenVerificationSucceeds()
        {
            SecretHasher hasher = new(1000);

            string hash = hasher.HashSecret(Password);

            Assert.That(hasher.VerifySecret(Password, hash), Is.True);
        }

        [TestCase("a-different-password")]
        [TestCase("0bvious-Passphras")]
        [TestCase("")]
        public void GivenAHash_WhenVerifyingAnotherValue_ThenVerificationFails(string candidate)
        {
            SecretHasher hasher = new(1000);

            string hash = hasher.HashSecret(Password);

            Assert.That(hasher.VerifySecret(candidate, hash), Is.False);
        }

        [TestCase("")]
        [TestCase("not-a-hash")]
        [TestCase("pbkdf2-sha256$abc$def$ghi")]
        [TestCase("scrypt$1000$c2FsdA==$aGFzaA==")]
        public void GivenAnInvalidHash_WhenVerifying_ThenVerificationFails(string hash)
        {
            SecretHasher hasher = new(1000);

            Assert.That(hasher.VerifySecret(Password, hash), Is.False);
        }

        [Test]
        public void GivenTheSamePassword_WhenHashingTwice_ThenTheHashesDiffer()
        {
            SecretHasher hasher = new(1000);

            Assert.That(
                hasher.HashSecret(Password),
                Is.Not.EqualTo(hasher.HashSecret(Password)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GivenAnInvalidIterationCount_WhenConstructing_ThenItIsRejected(int iterations)
            => Assert.That(() => new SecretHasher(iterations), Throws.TypeOf<System.ArgumentOutOfRangeException>());
    }
}