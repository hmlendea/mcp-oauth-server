using Moq;

using NuciDAL.DataObjects;
using NuciDAL.Repositories;

using NuciLog.Core;

using NUnit.Framework;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Authorisation;
using McpOAuthServer.Service.AuthorisationCodes;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;

namespace McpOAuthServer.UnitTests.Service.Authorisation
{
    [TestFixture]
    public sealed class AuthorisationServiceTests
    {
        private Mock<IClientService> clientService = null!;

        private Mock<IAuthorisationCodeService> codeService = null!;

        private AuthorisationService service = null!;

        [SetUp]
        public void SetUp()
        {
            clientService = new();
            codeService = new();
            service = new(BuildSettings(), clientService.Object, codeService.Object);
            clientService.Setup(client => client.TryGet("client-613"))
                .Returns(BuildClient());
            clientService.Setup(client => client.IsRedirectUriAllowed(It.IsAny<OAuthClient>(), It.IsAny<string>()))
                .Returns(true);
        }

        [Test]
        public void GivenAValidRequest_WhenValidating_ThenTheScopesAndAudienceAreGranted()
        {
            AuthorisationGrant request = BuildRequest();

            AuthorisationValidation validation = service.Validate(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.True);
                Assert.That(validation.Scope, Is.EqualTo("memories openid"));
                Assert.That(validation.Audience, Is.EqualTo("https://memory.example.com/mcp"));
                Assert.That(validation.RedirectUri, Is.EqualTo("https://chatgpt.com/connector_platform_oauth_redirect"));
            }
        }

        [TestCase("token")]
        [TestCase("id_token")]
        public void GivenAnUnsupportedResponseType_WhenValidating_ThenItIsRejected(string responseType)
        {
            AuthorisationGrant request = BuildRequest();
            request.ResponseType = responseType;

            AuthorisationValidation validation = service.Validate(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.Error, Is.EqualTo("unsupported_response_type"));
            }
        }

        [Test]
        public void GivenAnUnknownClient_WhenValidating_ThenItIsRejected()
        {
            clientService.Setup(client => client.TryGet(It.IsAny<string>())).Returns((OAuthClient?)null!);
            AuthorisationGrant request = BuildRequest();

            AuthorisationValidation validation = service.Validate(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.Error, Is.EqualTo("invalid_request"));
            }
        }

        [Test]
        public void GivenAnUnregisteredRedirectUri_WhenValidating_ThenNoRedirectUriIsEchoed()
        {
            clientService.Setup(client => client.IsRedirectUriAllowed(It.IsAny<OAuthClient>(), It.IsAny<string>()))
                .Returns(false);

            AuthorisationValidation validation = service.Validate(BuildRequest());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.RedirectUri, Is.Empty);
            }
        }

        [TestCase("plain")]
        [TestCase("")]
        [TestCase("S512")]
        public void GivenAnUnsupportedChallengeMethod_WhenValidating_ThenItIsRejected(string method)
        {
            AuthorisationGrant request = BuildRequest();
            request.CodeChallengeMethod = method;

            AuthorisationValidation validation = service.Validate(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.Error, Is.EqualTo("invalid_request"));
            }
        }

        [TestCase("files:read openid", "openid")]
        [TestCase("openid", "openid")]
        [TestCase("files:read", "")]
        public void GivenRequestedScopes_WhenValidating_ThenOnlyPermittedScopesAreGranted(
            string scope,
            string expectedScope)
        {
            AuthorisationGrant request = BuildRequest();
            request.Scope = scope;

            AuthorisationValidation validation = service.Validate(request);

            Assert.That(validation.Scope, Is.EqualTo(expectedScope));
        }

        [Test]
        public void GivenAnUnrecognisedResource_WhenValidating_ThenItIsRejected()
        {
            AuthorisationGrant request = BuildRequest();
            request.Resource = "https://unexpected.example/mcp";

            AuthorisationValidation validation = service.Validate(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(validation.IsValid, Is.False);
                Assert.That(validation.Error, Is.EqualTo("invalid_target"));
            }
        }

        [Test]
        public void GivenAValidRequest_WhenCreatingACode_ThenTheAuthorisationCodeIsPersisted()
        {
            AuthorisationGrant request = BuildRequest();
            AuthorisationValidation validation = service.Validate(request);
            codeService.Setup(code => code.Create(It.IsAny<AuthorisationCode>())).Returns("generated-code");

            string code = service.CreateCode(request, validation, "user-613");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(code, Is.EqualTo("generated-code"));
            }

            codeService.Verify(code => code.Create(It.Is<AuthorisationCode>(stored =>
                stored.ClientId == "client-613" &&
                stored.UserId == "user-613" &&
                stored.Scope == "memories openid" &&
                stored.Audience == "https://memory.example.com/mcp" &&
                stored.CodeChallenge == "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" &&
                stored.Nonce == "nonce-42")), Times.Once);
        }

        private static AuthorisationGrant BuildRequest()
            => new()
            {
                ResponseType = "code",
                ClientId = "client-613",
                RedirectUri = "https://chatgpt.com/connector_platform_oauth_redirect",
                Scope = "openid memories files:read",
                State = "state-48",
                CodeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                CodeChallengeMethod = "S256",
                Resource = "https://memory.example.com/mcp",
                Nonce = "nonce-42"
            };

        private static OAuthClient BuildClient()
            => new()
            {
                Id = "client-613",
                Name = "ChatGPT",
                RedirectUris = ["https://chatgpt.com/connector_platform_oauth_redirect"],
                AllowedScopes = ["memories", "openid"],
                IsPublic = true
            };

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SupportedScopes = ["memories", "openid", "email"],
                AllowedResources = ["https://memory.example.com/mcp"],
                AllowedRedirectUriPrefixes = ["https://chatgpt.com/"],
                AuthorisationCodeLifetimeSeconds = 60
            };
    }
}