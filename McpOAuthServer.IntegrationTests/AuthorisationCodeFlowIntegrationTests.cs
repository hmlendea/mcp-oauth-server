using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Testing;

using NUnit.Framework;

namespace McpOAuthServer.IntegrationTests
{
    [TestFixture]
    public sealed class AuthorisationCodeFlowIntegrationTests
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

        [Test]
        public async Task GivenValidCredentials_WhenCompletingTheFlow_ThenAnAccessTokenIsIssuedForTheResource()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories openid email");
            string codeVerifier = OAuthTestClient.CreateCodeVerifier();
            string codeChallenge = OAuthTestClient.CreateCodeChallenge(codeVerifier);

            using HttpResponseMessage authorisationResponse = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                codeChallenge,
                "state-613",
                IntegrationTestApplicationFactory.ApiResource);

            string code = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "code");
            string issuer = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "iss");

            using HttpResponseMessage tokenResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                codeVerifier,
                IntegrationTestApplicationFactory.ApiResource);

            string body = await tokenResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issuer, Is.EqualTo(IntegrationTestApplicationFactory.ApiIssuer));
                Assert.That(tokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"token_type\":\"Bearer\""));
                Assert.That(body, Does.Contain("\"id_token\":"));
                Assert.That(body, Does.Contain("\"refresh_token\":"));
                Assert.That(body, Does.Contain("\"scope\":\"email memories openid\""));
            }
        }

        [Test]
        public async Task GivenAnIssuedToken_WhenReadingUserInfo_ThenProfileClaimsAreReturned()
        {
            string accessToken = await OAuthTestClient.GetAccessTokenAsync(
                Client,
                RedirectUri,
                IntegrationTestApplicationFactory.ApiResource);

            using HttpRequestMessage request = new(HttpMethod.Get, "/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using HttpResponseMessage response = await Client.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"email\":\"solaire@astora.example\""));
                Assert.That(body, Does.Contain("\"email_verified\":true"));
                Assert.That(body, Does.Contain("\"preferred_username\":\"solaire_of_astora\""));
                Assert.That(body, Does.Contain("\"sub\":"));
            }
        }

        [Test]
        public async Task GivenIssuedTokens_WhenRefreshing_ThenANewAccessTokenIsIssued()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");
            string codeVerifier = OAuthTestClient.CreateCodeVerifier();

            using HttpResponseMessage authorisationResponse = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                OAuthTestClient.CreateCodeChallenge(codeVerifier),
                "state-42",
                IntegrationTestApplicationFactory.ApiResource);

            string code = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "code");

            using HttpResponseMessage tokenResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                codeVerifier,
                IntegrationTestApplicationFactory.ApiResource);

            using JsonDocument document = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
            string refreshToken = document.RootElement.GetProperty("refresh_token").GetString();

            using HttpResponseMessage refreshResponse = await OAuthTestClient.RefreshAsync(
                Client,
                clientId,
                refreshToken);
            string body = await refreshResponse.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refreshResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"access_token\":"));
                Assert.That(body, Does.Contain("\"refresh_token\":"));
            }
        }

        [Test]
        public async Task GivenAnIssuedToken_WhenIntrospecting_ThenItIsReportedAsActive()
        {
            string accessToken = await OAuthTestClient.GetAccessTokenAsync(
                Client,
                RedirectUri,
                IntegrationTestApplicationFactory.ApiResource);

            using FormUrlEncodedContent content = new(new System.Collections.Generic.Dictionary<string, string>
            {
                ["token"] = accessToken
            });
            using HttpResponseMessage response = await Client.PostAsync("/introspect", content);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"active\":true"));
                Assert.That(body, Does.Contain("\"aud\":\"https://memory.example.com/mcp\""));
            }
        }

        [Test]
        public async Task GivenAnUnknownToken_WhenIntrospecting_ThenItIsReportedAsInactive()
        {
            using FormUrlEncodedContent content = new(new System.Collections.Generic.Dictionary<string, string>
            {
                ["token"] = "not-a-real-token"
            });
            using HttpResponseMessage response = await Client.PostAsync("/introspect", content);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"active\":false"));
            }
        }

        [Test]
        public async Task GivenAnIssuedRefreshToken_WhenRevoking_ThenTheResponseIsEmpty()
        {
            string clientId = await OAuthTestClient.RegisterClientAsync(Client, RedirectUri, "memories");
            string codeVerifier = OAuthTestClient.CreateCodeVerifier();

            using HttpResponseMessage authorisationResponse = await OAuthTestClient.AuthoriseAsync(
                Client,
                clientId,
                RedirectUri,
                OAuthTestClient.CreateCodeChallenge(codeVerifier),
                "state-873",
                IntegrationTestApplicationFactory.ApiResource);

            string code = OAuthTestClient.ExtractQueryParameter(authorisationResponse, "code");

            using HttpResponseMessage tokenResponse = await OAuthTestClient.ExchangeCodeAsync(
                Client,
                clientId,
                RedirectUri,
                code,
                codeVerifier,
                IntegrationTestApplicationFactory.ApiResource);

            using JsonDocument document = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
            string refreshToken = document.RootElement.GetProperty("refresh_token").GetString();

            using FormUrlEncodedContent content = new(new System.Collections.Generic.Dictionary<string, string>
            {
                ["token"] = refreshToken,
                ["client_id"] = clientId
            });
            using HttpResponseMessage response = await Client.PostAsync("/revoke", content);

            using HttpResponseMessage refreshResponse = await OAuthTestClient.RefreshAsync(
                Client,
                clientId,
                refreshToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(refreshResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }
    }
}