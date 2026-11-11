using NUnit.Framework;

using McpOAuthServer.Service.Security;

namespace McpOAuthServer.UnitTests.Service.Security
{
    [TestFixture]
    public sealed class ProofKeyForCodeExchangeTests
    {
        private static string CodeVerifier => "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

        private static string CodeChallenge => "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        [Test]
        public void GivenAMatchingVerifier_WhenVerifying_ThenItIsAccepted()
            => Assert.That(ProofKeyForCodeExchange.Verify(CodeVerifier, CodeChallenge), Is.True);

        [TestCase("a-different-verifier")]
        [TestCase("")]
        public void GivenAMismatchedVerifier_WhenVerifying_ThenItIsRejected(string codeVerifier)
            => Assert.That(ProofKeyForCodeExchange.Verify(codeVerifier, CodeChallenge), Is.False);

        [TestCase("")]
        [TestCase("not-a-challenge")]
        public void GivenAnInvalidChallenge_WhenVerifying_ThenItIsRejected(string codeChallenge)
            => Assert.That(ProofKeyForCodeExchange.Verify(CodeVerifier, codeChallenge), Is.False);

        [TestCase("S256", true)]
        [TestCase("s256", false)]
        [TestCase("plain", false)]
        [TestCase("", false)]
        public void GivenAChallengeMethod_WhenCheckingSupport_ThenOnlyS256IsAccepted(
            string codeChallengeMethod,
            bool expected)
            => Assert.That(
                ProofKeyForCodeExchange.IsSupportedChallengeMethod(codeChallengeMethod),
                Is.EqualTo(expected));
    }
}