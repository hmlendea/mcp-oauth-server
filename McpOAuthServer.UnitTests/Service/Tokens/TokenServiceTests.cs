using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using McpOAuthServer.Configuration;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.RefreshTokens;
using McpOAuthServer.Service.Security;
using McpOAuthServer.Service.Tokens;

namespace McpOAuthServer.UnitTests.Service.Tokens
{
    [TestFixture]
    public sealed class TokenServiceTests
    {
        private static readonly string[] expectedResourceAudience = ["https://memory.example.com/mcp"];

        private static readonly string[] expectedClientAudience = ["client-613"];

        private Mock<IRefreshTokenService> refreshTokenService = null!;

        private Mock<ILogger> logger = null!;

        private TokenSigningKeyProvider keyProvider = null!;

        private string temporaryDirectory = string.Empty;

        private TokenService service = null!;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), $"mcp-oauth-server-{Guid.NewGuid()}");
            refreshTokenService = new();
            logger = new();
            keyProvider = new(Path.Combine(temporaryDirectory, "signing-key.pem"));
            service = new(BuildSettings(), keyProvider, refreshTokenService.Object, logger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            keyProvider.Dispose();

            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void GivenARequestForASingleScope_WhenIssuingTokens_ThenTheAudienceAndScopeAreEmbedded()
        {
            IssuedTokens tokens = service.IssueAccessTokens(BuildRequest("memories", includeRefreshToken: false));

            JsonWebToken accessToken = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(accessToken.Issuer, Is.EqualTo("https://auth.example.com"));
                Assert.That(accessToken.Audiences, Is.EqualTo(expectedResourceAudience));
                Assert.That(accessToken.GetClaim("scope").Value, Is.EqualTo("memories"));
                Assert.That(accessToken.GetClaim("client_id").Value, Is.EqualTo("client-613"));
                Assert.That(accessToken.GetClaim("sub").Value, Is.EqualTo("user-613"));
                Assert.That(tokens.IdToken, Is.Empty);
                Assert.That(tokens.RefreshToken, Is.Empty);
            }
        }

        [Test]
        public void GivenAnOpenIdScope_WhenIssuingTokens_ThenAnIdTokenIsIncluded()
        {
            IssuedTokens tokens = service.IssueAccessTokens(BuildRequest("memories openid email", includeRefreshToken: false));

            JsonWebToken idToken = new JsonWebTokenHandler().ReadJsonWebToken(tokens.IdToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(idToken.Audiences, Is.EqualTo(expectedClientAudience));
                Assert.That(idToken.GetClaim("email").Value, Is.EqualTo("solaire@astora.example"));
                Assert.That(idToken.GetClaim("email_verified").Value, Is.EqualTo("true"));
                Assert.That(idToken.GetClaim("nonce").Value, Is.EqualTo("nonce-42"));
                Assert.That(idToken.GetClaim("name").Value, Is.EqualTo("Solaire of Astora"));
            }
        }

        [Test]
        public void GivenARefreshTokenRequest_WhenIssuingTokens_ThenARefreshTokenIsPersisted()
        {
            refreshTokenService.Setup(service => service.Create(It.IsAny<RefreshToken>()))
                .Returns("refresh-token");

            IssuedTokens tokens = service.IssueAccessTokens(BuildRequest("memories", includeRefreshToken: true));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokens.RefreshToken, Is.EqualTo("refresh-token"));
                Assert.That(tokens.ExpiresInSeconds, Is.EqualTo(3600));
            }

            refreshTokenService.Verify(service => service.Create(It.Is<RefreshToken>(stored =>
                stored.ClientId == "client-613" &&
                stored.UserId == "user-613" &&
                stored.Audience == "https://memory.example.com/mcp" &&
                stored.Nonce == "nonce-42")), Times.Once);
        }

        [Test]
        public void GivenAnIssuedToken_WhenIntrospecting_ThenItsIdentityIsReported()
        {
            IssuedTokens tokens = service.IssueAccessTokens(BuildRequest("memories", includeRefreshToken: false));

            TokenIntrospection introspection = service.Introspect(tokens.AccessToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(introspection.IsActive, Is.True);
                Assert.That(introspection.ClientId, Is.EqualTo("client-613"));
                Assert.That(introspection.UserId, Is.EqualTo("user-613"));
                Assert.That(introspection.Scope, Is.EqualTo("memories"));
            }
        }

        [TestCase("not-a-token")]
        [TestCase("")]
        [TestCase("eyJhbGciOiJub25lIn0.eyJzdWIiOiJ1c2VyLTYxMyJ9.")]
        public void GivenAnUnverifiableToken_WhenIntrospecting_ThenItIsInactive(string token)
            => Assert.That(service.Introspect(token).IsActive, Is.False);

        [Test]
        public void GivenATokenSignedByAnotherKey_WhenIntrospecting_ThenItIsInactive()
        {
            using RSA otherKey = RSA.Create(2048);
            string forgedToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "https://auth.example.com",
                Audience = "https://memory.example.com/mcp",
                Claims = new Dictionary<string, object> { ["scope"] = "memories" },
                SigningCredentials = new(new RsaSecurityKey(otherKey), SecurityAlgorithms.RsaSha256),
                Expires = DateTime.UtcNow.AddMinutes(5)
            });

            Assert.That(service.Introspect(forgedToken).IsActive, Is.False);
        }

        [Test]
        public void GivenATokenIssuedByAnotherIssuer_WhenIntrospecting_ThenItIsInactive()
        {
            string foreignToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "https://other.example.com",
                Audience = "https://memory.example.com/mcp",
                Claims = new Dictionary<string, object> { ["scope"] = "memories" },
                SigningCredentials = keyProvider.SigningCredentials,
                Expires = DateTime.UtcNow.AddMinutes(5)
            });

            Assert.That(service.Introspect(foreignToken).IsActive, Is.False);
        }

        [Test]
        public void GivenAnExpiredToken_WhenIntrospecting_ThenItIsInactive()
        {
            string expiredToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "https://auth.example.com",
                Audience = "https://memory.example.com/mcp",
                Claims = new Dictionary<string, object> { ["scope"] = "memories" },
                SigningCredentials = keyProvider.SigningCredentials,
                IssuedAt = DateTime.UtcNow.AddHours(-2),
                NotBefore = DateTime.UtcNow.AddHours(-2),
                Expires = DateTime.UtcNow.AddMinutes(-10)
            });

            Assert.That(service.Introspect(expiredToken).IsActive, Is.False);
        }

        [Test]
        public void GivenATokenWithAnotherType_WhenIntrospecting_ThenItIsInactive()
        {
            string idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "https://auth.example.com",
                Audience = "client-613",
                Claims = new Dictionary<string, object> { ["scope"] = "memories" },
                SigningCredentials = keyProvider.SigningCredentials,
                Expires = DateTime.UtcNow.AddMinutes(5)
            });

            Assert.That(service.Introspect(idToken).IsActive, Is.False);
        }

        private static TokenIssueRequest BuildRequest(string scope, bool includeRefreshToken)
            => new()
            {
                ClientId = "client-613",
                UserId = "user-613",
                Scope = scope,
                Audience = "https://memory.example.com/mcp",
                Nonce = "nonce-42",
                Email = "solaire@astora.example",
                DisplayName = "Solaire of Astora",
                IsEmailVerified = true,
                IncludeRefreshToken = includeRefreshToken
            };

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SupportedScopes = ["memories", "openid", "email"],
                AllowedResources = ["https://memory.example.com/mcp"],
                AccessTokenLifetimeSeconds = 3600,
                IdTokenLifetimeSeconds = 3600,
                RefreshTokenLifetimeSeconds = 2592000
            };
    }
}