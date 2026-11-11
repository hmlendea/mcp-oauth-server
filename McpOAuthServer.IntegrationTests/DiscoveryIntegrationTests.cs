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
    public sealed class DiscoveryIntegrationTests
    {
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

        [TestCase("/.well-known/oauth-authorization-server")]
        [TestCase("/.well-known/openid-configuration")]
        public async Task GivenADiscoveryPath_WhenReadingMetadata_ThenRequiredOAuthFieldsArePublished(
            string path)
        {
            using HttpResponseMessage response = await Client.GetAsync(path);
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"issuer\":\"https://auth.example.com\""));
                Assert.That(body, Does.Contain("\"authorization_endpoint\":\"https://auth.example.com/authorize\""));
                Assert.That(body, Does.Contain("\"token_endpoint\":\"https://auth.example.com/token\""));
                Assert.That(body, Does.Contain("\"registration_endpoint\":\"https://auth.example.com/register\""));
                Assert.That(body, Does.Contain("\"code_challenge_methods_supported\":[\"S256\"]"));
                Assert.That(body, Does.Contain("\"authorization_response_iss_parameter_supported\":true"));
                Assert.That(body, Does.Contain("memories"));
            }
        }

        [Test]
        public async Task GivenAPublicJwksEndpoint_WhenReadingKeys_ThenAnRsaSigningKeyIsPublished()
        {
            using HttpResponseMessage response = await Client.GetAsync("/jwks");
            string body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("\"kty\":\"RSA\""));
                Assert.That(body, Does.Contain("\"alg\":\"RS256\""));
                Assert.That(body, Does.Contain("\"kid\":"));
                Assert.That(body, Does.Not.Contain("\"d\":"));
            }
        }

        [Test]
        public async Task GivenAConfiguredServer_WhenStarting_ThenDataStoresAndSigningKeyAreInitialised()
        {
            using HttpResponseMessage response = await Client.GetAsync("/.well-known/oauth-authorization-server");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(File.Exists(Path.Combine(temporaryDirectory, "signing-key.pem")), Is.True);
                Assert.That(File.Exists(Path.Combine(temporaryDirectory, "clients.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(temporaryDirectory, "codes.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(temporaryDirectory, "tokens.json")), Is.True);
                Assert.That(File.Exists(Path.Combine(temporaryDirectory, "users.json")), Is.True);
            }
        }
    }
}