using System.Collections.Generic;
using System.IO;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using McpOAuthServer.Service.Security;

namespace McpOAuthServer.IntegrationTests
{
    public sealed class IntegrationTestApplicationFactory(
        string dataDirectory) : WebApplicationFactory<Program>
    {
        public string DataDirectory { get; } = dataDirectory;

        public static string ApiIssuer => "https://auth.example.com";

        public static string ApiResource => "https://memory.example.com/mcp";

        public static string ApiUsername => "solaire_of_astora";

        public static string ApiPassword => "0bvious-Passphrase";

        public static int ApiPasswordHashIterations => 1000;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            SecretHasher secretHasher = new(ApiPasswordHashIterations);

            builder.ConfigureServices(services => services
                .AddSingleton<IStartupFilter, TestServerRemoteIpStartupFilter>());

            builder.ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["OAuthServerSettings:Issuer"] = ApiIssuer,
                    ["OAuthServerSettings:SigningKeyPath"] = Path.Combine(DataDirectory, "signing-key.pem"),
                    ["OAuthServerSettings:SupportedScopes:0"] = "memories",
                    ["OAuthServerSettings:SupportedScopes:1"] = "openid",
                    ["OAuthServerSettings:SupportedScopes:2"] = "email",
                    ["OAuthServerSettings:AllowedResources:0"] = ApiResource,
                    ["OAuthServerSettings:AllowedRedirectUriPrefixes:0"] = "https://chatgpt.com/",
                    ["OAuthServerSettings:PasswordHashIterations"] = ApiPasswordHashIterations.ToString(),
                    ["OAuthServerSettings:SeedUsers:0:Username"] = ApiUsername,
                    ["OAuthServerSettings:SeedUsers:0:PasswordHash"] = secretHasher.HashSecret(ApiPassword),
                    ["OAuthServerSettings:SeedUsers:0:Email"] = "solaire@astora.example",
                    ["OAuthServerSettings:SeedUsers:0:DisplayName"] = "Solaire of Astora",
                    ["OAuthServerSettings:SeedUsers:0:IsEmailVerified"] = "true",
                    ["DataStoreSettings:AuthorisationCodesStorePath"] = Path.Combine(DataDirectory, "codes.json"),
                    ["DataStoreSettings:ClientsStorePath"] = Path.Combine(DataDirectory, "clients.json"),
                    ["DataStoreSettings:RefreshTokensStorePath"] = Path.Combine(DataDirectory, "tokens.json"),
                    ["DataStoreSettings:UsersStorePath"] = Path.Combine(DataDirectory, "users.json"),
                    ["NuciLoggerSettings:IsFileOutputEnabled"] = "false"
                }));
        }
    }
}