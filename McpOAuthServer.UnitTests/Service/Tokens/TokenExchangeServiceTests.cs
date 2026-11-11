using System.Collections.Generic;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using McpOAuthServer.Configuration;
using McpOAuthServer.Service.AuthorisationCodes;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.RefreshTokens;
using McpOAuthServer.Service.Tokens;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer.UnitTests.Service.Tokens
{
    [TestFixture]
    public sealed class TokenExchangeServiceTests
    {
        private Mock<IClientService> clientService = null!;

        private Mock<IAuthorisationCodeService> codeService = null!;

        private Mock<IRefreshTokenService> refreshTokenService = null!;

        private Mock<IUserService> userService = null!;

        private Mock<ITokenService> tokenService = null!;

        private Mock<ILogger> logger = null!;

        private TokenExchangeService service = null!;

        [SetUp]
        public void SetUp()
        {
            clientService = new();
            codeService = new();
            refreshTokenService = new();
            userService = new();
            tokenService = new();
            logger = new();
            service = new(
                BuildSettings(),
                clientService.Object,
                codeService.Object,
                refreshTokenService.Object,
                userService.Object,
                tokenService.Object,
                logger.Object);
            clientService.Setup(client => client.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new ClientAuthentication
                {
                    IsAuthenticated = true,
                    ClientId = "client-613",
                    Client = new() { Id = "client-613", IsPublic = true }
                });
            userService.Setup(users => users.Get("user-613")).Returns(new OAuthUser
            {
                Id = "user-613",
                Username = "solaire_of_astora",
                Email = "solaire@astora.example"
            });
            tokenService.Setup(tokens => tokens.IssueAccessTokens(It.IsAny<TokenIssueRequest>()))
                .Returns(new IssuedTokens { AccessToken = "access-token" });
        }

        [Test]
        public void GivenAValidAuthorisationCode_WhenExchanging_ThenTokensAreReturned()
        {
            codeService.Setup(codes => codes.Consume("valid-code")).Returns(BuildAuthorisationCode());
            TokenExchangeRequest request = BuildRequest("authorization_code");
            request.Code = "valid-code";
            request.CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

            TokenExchangeResult result = service.Exchange(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(result.Tokens.AccessToken, Is.EqualTo("access-token"));
            }
        }

        [Test]
        public void GivenInvalidClientCredentials_WhenExchanging_ThenTheRequestFails()
        {
            const string expectedError = "invalid_client";
            clientService.Setup(client => client.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new ClientAuthentication { IsAuthenticated = false, Error = expectedError });

            TokenExchangeResult result = service.Exchange(BuildRequest("authorization_code"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Error, Is.EqualTo(expectedError));
            }
        }

        [Test]
        public void GivenAnUnknownAuthorisationCode_WhenExchanging_ThenInvalidGrantIsReturned()
        {
            codeService.Setup(codes => codes.Consume(It.IsAny<string>()))
                .Throws(new KeyNotFoundException());

            TokenExchangeResult result = service.Exchange(BuildRequest("authorization_code"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Error, Is.EqualTo("invalid_grant"));
            }
        }

        [Test]
        public void GivenAnExpiredAuthorisationCode_WhenExchanging_ThenInvalidGrantIsReturned()
        {
            AuthorisationCode authorisationCode = BuildAuthorisationCode();
            authorisationCode.ExpiryDateTime = System.DateTimeOffset.UtcNow.AddMinutes(-1);
            codeService.Setup(codes => codes.Consume(It.IsAny<string>())).Returns(authorisationCode);
            TokenExchangeRequest request = BuildRequest("authorization_code");
            request.CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

            TokenExchangeResult result = service.Exchange(request);

            Assert.That(result.Error, Is.EqualTo("invalid_grant"));
        }

        [Test]
        public void GivenAMismatchedCodeVerifier_WhenExchanging_ThenInvalidGrantIsReturned()
        {
            codeService.Setup(codes => codes.Consume(It.IsAny<string>())).Returns(BuildAuthorisationCode());
            TokenExchangeRequest request = BuildRequest("authorization_code");
            request.CodeVerifier = "a-different-verifier";

            TokenExchangeResult result = service.Exchange(request);

            Assert.That(result.Error, Is.EqualTo("invalid_grant"));
        }

        [Test]
        public void GivenAValidRefreshToken_WhenExchanging_ThenNewTokensAreReturned()
        {
            refreshTokenService.Setup(tokens => tokens.Consume("valid-refresh-token")).Returns(new RefreshToken
            {
                ClientId = "client-613",
                UserId = "user-613",
                Scope = "memories openid",
                Audience = "https://memory.example.com/mcp",
                ExpiryDateTime = System.DateTimeOffset.UtcNow.AddDays(1)
            });

            TokenExchangeResult result = service.Exchange(BuildRequest("refresh_token"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.True);
                Assert.That(result.Tokens.AccessToken, Is.EqualTo("access-token"));
            }
        }

        [TestCase("files:read")]
        [TestCase("memories files:read")]
        public void GivenScopesBeyondTheOriginalGrant_WhenRefreshing_ThenInvalidScopeIsReturned(string scope)
        {
            refreshTokenService.Setup(tokens => tokens.Consume(It.IsAny<string>())).Returns(new RefreshToken
            {
                ClientId = "client-613",
                UserId = "user-613",
                Scope = "memories",
                Audience = "https://memory.example.com/mcp",
                ExpiryDateTime = System.DateTimeOffset.UtcNow.AddDays(1)
            });
            TokenExchangeRequest request = BuildRequest("refresh_token");
            request.Scope = scope;

            TokenExchangeResult result = service.Exchange(request);

            Assert.That(result.Error, Is.EqualTo("invalid_scope"));
        }

        [TestCase("client_credentials")]
        [TestCase("password")]
        [TestCase("")]
        public void GivenAnUnsupportedGrantType_WhenExchanging_ThenTheRequestFails(string grantType)
        {
            TokenExchangeResult result = service.Exchange(BuildRequest(grantType));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.IsSuccessful, Is.False);
                Assert.That(result.Error, Is.EqualTo("unsupported_grant_type"));
            }
        }

        private static AuthorisationCode BuildAuthorisationCode()
            => new()
            {
                Id = "valid-code",
                ClientId = "client-613",
                UserId = "user-613",
                RedirectUri = "https://chatgpt.com/connector_platform_oauth_redirect",
                Scope = "memories openid",
                Audience = "https://memory.example.com/mcp",
                CodeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                ExpiryDateTime = System.DateTimeOffset.UtcNow.AddMinutes(1)
            };

        private static TokenExchangeRequest BuildRequest(string grantType)
            => new()
            {
                GrantType = grantType,
                ClientId = "client-613",
                RedirectUri = "https://chatgpt.com/connector_platform_oauth_redirect",
                Code = "valid-code",
                CodeVerifier = "a-verifier",
                RefreshToken = "valid-refresh-token"
            };

        private static OAuthServerSettings BuildSettings()
            => new()
            {
                Issuer = "https://auth.example.com",
                SupportedScopes = ["memories", "openid", "email"],
                AllowedResources = ["https://memory.example.com/mcp"]
            };
    }
}