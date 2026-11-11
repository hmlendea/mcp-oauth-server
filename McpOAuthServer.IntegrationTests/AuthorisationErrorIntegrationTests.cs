using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;

using NUnit.Framework;

namespace McpOAuthServer.IntegrationTests
{
    [TestFixture]
    public sealed class AuthorisationErrorIntegrationTests
    {
        private static string RedirectUri => "https://chatgpt.com/connector_platform_oauth_redirect";

        private string temporaryDirectory = string.Empty;
        private IntegrationTestApplicationFactory? applicationFactory;
        private HttpClient? client;

        private HttpClient Client
        {
            get
            {
                if (client is null)
                {
                    throw new InvalidOperationException("The client field must be initialised by SetUp.");
                }

                return client;
            }
        }

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(Path.GetTempPath(), $"mcp-oauth-server-{Guid.NewGuid():N}");
            applicationFactory = new(temporaryDirectory);
            client = applicationFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new("https://127.0.0.1"),
                AllowAutoRedirect = false
            });
        }

        [TearDown]
        public void TearDown()
        {
            client?.Dispose();
            applicationFactory?.Dispose();

            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [TestCase("plain")]
        [TestCase("S512")]
        public async Task GivenAnUnsupportedChallengeMethod_WhenAuthorising_ThenTheRequestIsRejected(
            string codeChallengeMethod)
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");

            using FormUrlEncodedContent content = new(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = RedirectUri,
                ["scope"] = "memories",
                ["state"] = "state-2048",
                ["code_challenge"] = OAuthTestClient.CreateCodeChallenge(OAuthTestClient.CreateCodeVerifier()),
                ["code_challenge_method"] = codeChallengeMethod,
                ["resource"] = IntegrationTestApplicationFactory.ApiResource
            });
            using HttpResponseMessage response = await Client.PostAsync("/authorize", content);

            string location = string.Empty;

            if (response.Headers.Location is not null)
            {
                location = response.Headers.Location.ToString();
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
                Assert.That(location, Does.Contain("error=invalid_request"));
            }
        }

        [Test]
        public async Task GivenAMismatchedCodeVerifier_WhenExchangingTheCode_ThenTheRequestIsRejected()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");
            string codeVerifier = OAuthTestClient.CreateCodeVerifier();

            using HttpResponseMessage authorisationResponse = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                OAuthTestClient.CreateCodeChallenge(codeVerifier),
                "state-1024",
                IntegrationTestApplicationFactory.ApiResource);

            string code = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "code");

            using HttpResponseMessage tokenResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                OAuthTestClient.CreateCodeVerifier(),
                IntegrationTestApplicationFactory.ApiResource);
            string body = await tokenResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("invalid_grant"));
            }
        }

        [Test]
        public async Task GivenAnUnregisteredRedirectUri_WhenAuthorising_ThenTheRequestIsRejected()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");

            using FormUrlEncodedContent content = new(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = "https://attacker.example/callback",
                ["scope"] = "memories",
                ["code_challenge"] = OAuthTestClient.CreateCodeChallenge(OAuthTestClient.CreateCodeVerifier()),
                ["code_challenge_method"] = "S256"
            });
            using HttpResponseMessage response = await Client.PostAsync("/authorize", content);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("invalid_request"));
            }
        }

        [Test]
        public async Task GivenAnUnrecognisedResource_WhenAuthorising_ThenTheRequestIsRejected()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");

            using HttpResponseMessage response = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                OAuthTestClient.CreateCodeChallenge(OAuthTestClient.CreateCodeVerifier()),
                "state-48",
                "https://unexpected.example/mcp");

            string location = string.Empty;

            if (response.Headers.Location is not null)
            {
                location = response.Headers.Location.ToString();
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(location, Does.Contain("error=invalid_target"));
                Assert.That(location, Does.Contain("iss=https%3A%2F%2Fauth.example.com"));
            }
        }

        [Test]
        public async Task GivenInvalidCredentials_WhenAuthenticating_ThenTheFormIsRepresented()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");

            using FormUrlEncodedContent content = new(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = RedirectUri,
                ["scope"] = "memories",
                ["code_challenge"] = OAuthTestClient.CreateCodeChallenge(OAuthTestClient.CreateCodeVerifier()),
                ["code_challenge_method"] = "S256",
                ["resource"] = IntegrationTestApplicationFactory.ApiResource,
                ["username"] = IntegrationTestApplicationFactory.ApiUsername,
                ["password"] = "a-wrong-passphrase",
                ["decision"] = "approve"
            });
            using HttpResponseMessage response = await Client.PostAsync("/authorize", content);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("The supplied credentials are not valid."));
                Assert.That(body, Does.Not.Contain("a-wrong-passphrase"));
            }
        }

        [Test]
        public async Task GivenADeniedRequest_WhenReturning_ThenTheAccessDeniedErrorIsReturned()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");

            using FormUrlEncodedContent content = new(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = RedirectUri,
                ["scope"] = "memories",
                ["code_challenge"] = OAuthTestClient.CreateCodeChallenge(OAuthTestClient.CreateCodeVerifier()),
                ["code_challenge_method"] = "S256",
                ["resource"] = IntegrationTestApplicationFactory.ApiResource,
                ["username"] = IntegrationTestApplicationFactory.ApiUsername,
                ["password"] = IntegrationTestApplicationFactory.ApiPassword,
                ["decision"] = "deny"
            });
            using HttpResponseMessage response = await Client.PostAsync("/authorize", content);

            string location = string.Empty;

            if (response.Headers.Location is not null)
            {
                location = response.Headers.Location.ToString();
            }

            Assert.That(location, Does.Contain("error=access_denied"));
        }

        [TestCase("insecure.example.com/callback")]
        [TestCase("http://insecure.example.com/callback")]
        public async Task GivenAnInsecureRedirectUri_WhenRegistering_ThenTheClientIsRejected(
            string redirectUri)
        {
            string payload = "{" +
                "\"client_name\":\"ChatGPT\"," +
                $"\"redirect_uris\":[\"{redirectUri}\"]," +
                "\"token_endpoint_auth_method\":\"none\"}";

            using StringContent content = new(payload, System.Text.Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await Client.PostAsync("/register", content);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("invalid_redirect_uri"));
            }
        }

        [Test]
        public async Task GivenAReusedAuthorisationCode_WhenExchanging_ThenTheRequestIsRejected()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");
            string codeVerifier = OAuthTestClient.CreateCodeVerifier();

            using HttpResponseMessage authorisationResponse = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                OAuthTestClient.CreateCodeChallenge(codeVerifier),
                "state-16",
                IntegrationTestApplicationFactory.ApiResource);

            string code = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "code");

            using HttpResponseMessage firstResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                codeVerifier,
                IntegrationTestApplicationFactory.ApiResource);
            using HttpResponseMessage secondResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                codeVerifier,
                IntegrationTestApplicationFactory.ApiResource);
            string body = await secondResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(secondResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("invalid_grant"));
            }
        }
    }
}