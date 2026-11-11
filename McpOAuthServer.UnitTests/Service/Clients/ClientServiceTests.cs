using System.Collections.Generic;

using Moq;

using NuciDAL.DataObjects;
using NuciDAL.Repositories;

using NuciLog.Core;

using NUnit.Framework;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.UnitTests.Service.Clients
{
    [TestFixture]
    public sealed class ClientServiceTests
    {
        private Mock<IFileRepository<OAuthClientDataObject>> repository = null!;

        private Mock<ISecretHasher> secretHasher = null!;

        private Mock<ILogger> logger = null!;

        private ClientService service = null!;

        [SetUp]
        public void SetUp()
        {
            repository = new();
            secretHasher = new();
            logger = new();
            service = new(repository.Object, BuildSettings(), secretHasher.Object, logger.Object);
        }

        [Test]
        public void GivenAPublicClientRequest_WhenRegistering_ThenNoSecretIsIssued()
        {
            ClientRegistrationRequest request = BuildRegistrationRequest();
            request.TokenEndpointAuthenticationMethod = "none";

            ClientRegistrationResult result = service.Register(request);
            RegisteredClient client = result.Client;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(client.IsPublic, Is.True);
                Assert.That(client.ClientSecret, Is.Empty);
                Assert.That(client.ClientId, Is.Not.Empty);
                Assert.That(client.Scope, Is.EqualTo("memories"));
            }

            repository.Verify(repository => repository.SaveChanges(), Times.Once);
            secretHasher.Verify(hasher => hasher.HashSecret(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GivenAConfidentialClientRequest_WhenRegistering_ThenASecretIsHashed()
        {
            ClientRegistrationRequest request = BuildRegistrationRequest();
            request.TokenEndpointAuthenticationMethod = "client_secret_post";
            secretHasher.Setup(hasher => hasher.HashSecret(It.IsAny<string>()))
                .Returns("hashed-secret");

            ClientRegistrationResult result = service.Register(request);
            RegisteredClient client = result.Client;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(client.IsPublic, Is.False);
                Assert.That(client.ClientSecret, Is.Not.Empty);
                Assert.That(client.ClientId, Is.Not.Empty);
            }

            secretHasher.Verify(hasher => hasher.HashSecret(client.ClientSecret), Times.Once);
        }

        [TestCase("http://insecure.example.com/callback")]
        [TestCase("https://insecure.example.com/callback")]
        [TestCase("relative/callback")]
        [TestCase("")]
        public void GivenAnUnpermittedRedirectUri_WhenRegistering_ThenRegistrationIsRejected(string redirectUri)
        {
            ClientRegistrationRequest request = BuildRegistrationRequest();
            request.RedirectUris = [redirectUri];

            ClientRegistrationResult result = service.Register(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.ErrorDescription, Is.Not.Empty);
            }

            repository.Verify(repository => repository.Add(It.IsAny<OAuthClientDataObject>()), Times.Never);
        }

        [TestCase("http://127.0.0.1:5081/callback", true)]
        [TestCase("http://localhost:5081/callback", true)]
        [TestCase("https://chatgpt.com/connector_platform_oauth_redirect", true)]
        public void GivenARegisteredRedirectUri_WhenCheckingAllowance_ThenItIsAccepted(
            string redirectUri,
            bool expected)
        {
            OAuthClient client = new()
            {
                Id = "client-42",
                RedirectUris = [redirectUri],
                AllowedScopes = ["memories"]
            };

            Assert.That(service.IsRedirectUriAllowed(client, redirectUri), Is.EqualTo(expected));
        }

        [TestCase("https://attacker.example/callback", "https://memory.example.com/callback")]
        [TestCase("https://memory.example.com/other", "https://memory.example.com/callback")]
        public void GivenAnUnregisteredRedirectUri_WhenCheckingAllowance_ThenItIsRejected(
            string redirectUri,
            string registeredRedirectUri)
        {
            OAuthClient client = new()
            {
                Id = "client-42",
                RedirectUris = [registeredRedirectUri],
                AllowedScopes = ["memories"]
            };

            Assert.That(service.IsRedirectUriAllowed(client, redirectUri), Is.False);
        }

        [TestCase("good-secret", true)]
        [TestCase("bad-secret", false)]
        [TestCase("", false)]
        public void GivenAClientSecret_WhenAuthenticating_ThenOnlyMatchingSecretsAreAccepted(
            string candidateSecret,
            bool expected)
        {
            repository.Setup(repository => repository.TryGet("client-613"))
                .Returns(BuildClientDataObject(isPublic: false));
            secretHasher.Setup(hasher => hasher.VerifySecret(It.IsAny<string>(), "stored-hash"))
                .Returns((string value, string _) => value == "good-secret");

            ClientAuthentication authentication = service.Authenticate("client-613", candidateSecret);

            Assert.That(authentication.IsAuthenticated, Is.EqualTo(expected));
        }

        [Test]
        public void GivenAPublicClient_WhenAuthenticatingWithoutSecret_ThenItIsAccepted()
        {
            repository.Setup(repository => repository.TryGet("client-613"))
                .Returns(BuildClientDataObject(isPublic: true));

            ClientAuthentication authentication = service.Authenticate("client-613", string.Empty);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authentication.IsAuthenticated, Is.True);
                Assert.That(authentication.ClientId, Is.EqualTo("client-613"));
            }

            secretHasher.Verify(hasher => hasher.VerifySecret(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GivenAnUnknownClient_WhenAuthenticating_ThenInvalidClientIsReported()
        {
            repository.Setup(repository => repository.TryGet(It.IsAny<string>()))
                .Returns((OAuthClientDataObject?)null!);

            ClientAuthentication authentication = service.Authenticate("client-999", "secret");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(authentication.IsAuthenticated, Is.False);
                Assert.That(authentication.Error, Is.EqualTo("invalid_client"));
            }
        }

        private static OAuthClientDataObject BuildClientDataObject(bool isPublic)
            => new()
            {
                Id = "client-613",
                Name = "ChatGPT",
                RedirectUris = ["https://chatgpt.com/connector_platform_oauth_redirect"],
                AllowedScopes = ["memories"],
                IsPublic = isPublic,
                SecretHash = "stored-hash",
                CreatedTimestamp = "2026-01-01T10:30:00.0000000+00:00"
            };

        private static ClientRegistrationRequest BuildRegistrationRequest()
            => new()
            {
                Name = "ChatGPT",
                RedirectUris = ["https://chatgpt.com/connector_platform_oauth_redirect"],
                GrantTypes = ["authorization_code"],
                Scopes = ["memories", "files:read"]
            };

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SupportedScopes = ["memories", "openid", "email"],
                AllowedResources = ["https://memory.example.com/mcp"],
                AllowedRedirectUriPrefixes = ["https://chatgpt.com/"],
                PasswordHashIterations = 1000
            };
    }
}