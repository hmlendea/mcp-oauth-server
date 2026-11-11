using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciDAL.DependencyInjection;
using NuciDAL.Repositories;

using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Service.Authorisation;
using McpOAuthServer.Service.AuthorisationCodes;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.RefreshTokens;
using McpOAuthServer.Service.Security;
using McpOAuthServer.Service.Tokens;
using McpOAuthServer.Service.Users;

namespace McpOAuthServer
{
    public static class ServiceCollectionExtensions
    {
        private static string authorisationCodesStorePath = string.Empty;

        private static string clientsStorePath = string.Empty;

        private static string refreshTokensStorePath = string.Empty;

        private static string usersStorePath = string.Empty;

        public static IServiceCollection AddConfigurations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            OAuthServerSettings oAuthServerSettings = new();
            DataStoreSettings dataStoreSettings = new();
            NuciLoggerSettings loggingSettings = new();

            configuration.Bind(nameof(OAuthServerSettings), oAuthServerSettings);
            configuration.Bind(nameof(DataStoreSettings), dataStoreSettings);
            configuration.Bind(nameof(NuciLoggerSettings), loggingSettings);

            OAuthServerSettingsValidator.Validate(oAuthServerSettings);

            authorisationCodesStorePath = dataStoreSettings.AuthorisationCodesStorePath;
            clientsStorePath = dataStoreSettings.ClientsStorePath;
            refreshTokensStorePath = dataStoreSettings.RefreshTokensStorePath;
            usersStorePath = dataStoreSettings.UsersStorePath;

            services.AddSingleton(oAuthServerSettings);
            services.AddSingleton(dataStoreSettings);
            services.AddSingleton(loggingSettings);

            return services;
        }

        public static IServiceCollection AddCustomServices(this IServiceCollection services) => services
            .AddSingleton<ISecretHasher>(serviceProvider => new SecretHasher(
                serviceProvider.GetRequiredService<OAuthServerSettings>().PasswordHashIterations))
            .AddSingleton<ITokenSigningKeyProvider>(serviceProvider => new TokenSigningKeyProvider(
                serviceProvider.GetRequiredService<OAuthServerSettings>().SigningKeyPath))
            .AddSingleton<IClientService, ClientService>()
            .AddSingleton<IUserService, UserService>()
            .AddSingleton<IAuthorisationCodeService, AuthorisationCodeService>()
            .AddSingleton<IRefreshTokenService, RefreshTokenService>()
            .AddSingleton<IAuthorisationService, AuthorisationService>()
            .AddSingleton<ITokenService, TokenService>()
            .AddSingleton<ITokenExchangeService, TokenExchangeService>()
            .AddJsonRepository<OAuthClientDataObject>(() =>
                clientsStorePath)
            .AddJsonRepository<OAuthUserDataObject>(() =>
                usersStorePath)
            .AddJsonRepository<AuthorisationCodeDataObject>(() =>
                authorisationCodesStorePath)
            .AddJsonRepository<RefreshTokenDataObject>(() =>
                refreshTokensStorePath)
            .AddSingleton<ILogger, NuciLogger>();
    }
}