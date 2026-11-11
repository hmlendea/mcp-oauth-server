using System;
using System.Collections.Generic;

using NUnit.Framework;

using McpOAuthServer.Configuration;

namespace McpOAuthServer.UnitTests.Configuration
{
    [TestFixture]
    public sealed class OAuthServerSettingsValidatorTests
    {
        private static readonly string[] expectedScopes = ["memories"];

        [Test]
        public void GivenValidSettings_WhenValidating_ThenNoExceptionIsThrown()
            => Assert.That(() => OAuthServerSettingsValidator.Validate(BuildSettings()), Throws.Nothing);

        [TestCase("http://auth.example.com")]
        [TestCase("https://auth.example.com/path")]
        [TestCase("not-a-url")]
        [TestCase("")]
        public void GivenAnInvalidIssuer_WhenValidating_ThenItIsRejected(string issuer)
        {
            OAuthServerSettings settings = BuildSettings();
            settings.Issuer = issuer;

            Assert.That(() => OAuthServerSettingsValidator.Validate(settings), Throws.TypeOf<InvalidOperationException>());
        }

        [TestCase(0)]
        [TestCase(-30)]
        public void GivenANonPositiveLifetime_WhenValidating_ThenItIsRejected(int lifetime)
        {
            OAuthServerSettings settings = BuildSettings();
            settings.AccessTokenLifetimeSeconds = lifetime;

            Assert.That(() => OAuthServerSettingsValidator.Validate(settings), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void GivenNoSupportedScopes_WhenValidating_ThenItIsRejected()
        {
            OAuthServerSettings settings = BuildSettings();
            settings.SupportedScopes = [];

            Assert.That(() => OAuthServerSettingsValidator.Validate(settings), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void GivenNoAllowedResources_WhenValidating_ThenItIsRejected()
        {
            OAuthServerSettings settings = BuildSettings();
            settings.AllowedResources = [];

            Assert.That(() => OAuthServerSettingsValidator.Validate(settings), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void GivenARequestedScopeSubset_WhenNormalising_ThenOnlySupportedScopesAreRetained()
            => Assert.That(
                OAuthServerSettingsValidator.NormaliseScopes(
                    ["memories", "files:read", "memories"],
                    ["memories", "openid"]),
                Is.EqualTo(expectedScopes));

        [Test]
        public void GivenNoOverlappingScopes_WhenNormalising_ThenTheResultIsEmpty()
            => Assert.That(
                OAuthServerSettingsValidator.NormaliseScopes(["files:read"], ["memories"]),
                Is.Empty);

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SigningKeyPath = "Data/signing-key.pem",
                SupportedScopes = ["memories", "openid", "email"],
                AllowedResources = ["https://memory.example.com/mcp"],
                AllowedRedirectUriPrefixes = ["https://chatgpt.com/"],
                PasswordHashIterations = 1000
            };
    }
}