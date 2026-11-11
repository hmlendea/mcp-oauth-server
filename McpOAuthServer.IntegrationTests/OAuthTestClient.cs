using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

using NUnit.Framework;

namespace McpOAuthServer.IntegrationTests
{
    public static class OAuthTestClient
    {
        public static string ResourceParameterName => "resource";

        public static string CreateCodeVerifier()
            => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        public static string CreateCodeChallenge(string codeVerifier)
            => Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

        public static async Task<string> RegisterClientAsync(
            HttpClient client,
            string redirectUri,
            string scope)
        {
            string payload = "{" +
                "\"client_name\":\"ChatGPT\"," +
                $"\"redirect_uris\":[\"{redirectUri}\"]," +
                "\"grant_types\":[\"authorization_code\",\"refresh_token\"]," +
                $"\"scope\":[\"{string.Join("\",\"", scope.Split(' '))}\"]," +
                "\"token_endpoint_auth_method\":\"none\"}";

            using StringContent content = new(payload, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync("/register", content);
            string body = await response.Content.ReadAsStringAsync();

            Assert.That(response.IsSuccessStatusCode, Is.True, body);

            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);

            return document.RootElement.GetProperty("client_id").GetString();
        }

        public static async Task<HttpResponseMessage> AuthoriseAsync(
            HttpClient client,
            string clientId,
            string redirectUri,
            string codeChallenge,
            string state,
            string resource)
        {
            Dictionary<string, string> payload = new()
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["scope"] = "memories openid email",
                ["state"] = state,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                [ResourceParameterName] = resource,
                ["username"] = IntegrationTestApplicationFactory.ApiUsername,
                ["password"] = IntegrationTestApplicationFactory.ApiPassword,
                ["decision"] = "approve"
            };

            using FormUrlEncodedContent content = new(payload);

            return await client.PostAsync("/authorize", content);
        }
        public static async Task<HttpResponseMessage> ExchangeCodeAsync(
            HttpClient client,
            string clientId,
            string redirectUri,
            string code,
            string codeVerifier,
            string resource)
        {
            Dictionary<string, string> payload = new()
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["redirect_uri"] = redirectUri,
                ["code"] = code,
                ["code_verifier"] = codeVerifier,
                [ResourceParameterName] = resource
            };

            using FormUrlEncodedContent content = new(payload);

            return await client.PostAsync("/token", content);
        }

        public static async Task<HttpResponseMessage> RefreshAsync(
            HttpClient client,
            string clientId,
            string refreshToken)
        {
            Dictionary<string, string> payload = new()
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["refresh_token"] = refreshToken
            };

            using FormUrlEncodedContent content = new(payload);

            return await client.PostAsync("/token", content);
        }

        public static async Task<string> GetAccessTokenAsync(
            HttpClient client,
            string redirectUri,
            string resource)
        {
            string clientId = await RegisterClientAsync(client, redirectUri, "memories openid email");
            string codeVerifier = CreateCodeVerifier();
            string codeChallenge = CreateCodeChallenge(codeVerifier);

            using HttpResponseMessage authorisationResponse = await AuthoriseAsync(
                client,
                clientId,
                redirectUri,
                codeChallenge,
                "test-state",
                resource);

            string code = ExtractQueryParameter(authorisationResponse, "code");

            using HttpResponseMessage tokenResponse = await ExchangeCodeAsync(
                client,
                clientId,
                redirectUri,
                code,
                codeVerifier,
                resource);

            string body = await tokenResponse.Content.ReadAsStringAsync();

            Assert.That(tokenResponse.IsSuccessStatusCode, Is.True, body);

            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);

            return document.RootElement.GetProperty("access_token").GetString();
        }

        public static string ExtractQueryParameter(HttpResponseMessage response, string name)
        {
            Uri? location = response.Headers.Location;

            Assert.That(
                location,
                Is.Not.Null,
                $"The authorisation response must redirect. Status: {response.StatusCode}; Body: {response.Content.ReadAsStringAsync().GetAwaiter().GetResult()}");

            foreach (string pair in location.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] segments = pair.Split('=', 2);

                if (string.Equals(segments[0], name, StringComparison.Ordinal))
                {
                    return Uri.UnescapeDataString(segments[1]);
                }
            }

            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "The {0} parameter is absent.", name));
        }
    }
}