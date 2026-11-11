using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using McpOAuthServer.Configuration;

namespace McpOAuthServer
{
    public sealed class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration => configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services
                .AddConfigurations(Configuration)
                .AddCustomServices();
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
        {
            SeedStores(application.ApplicationServices);

            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }

            application.UseHttpsRedirection();
            application.UseRouting();
            application.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private static void SeedStores(System.IServiceProvider serviceProvider)
        {
            DataStoreSettings dataStoreSettings = serviceProvider.GetRequiredService<DataStoreSettings>();
            CreateStoresIfMissing(dataStoreSettings);

            // Resolve the signing key eagerly so a missing or unusable key is reported at startup.
            serviceProvider.GetRequiredService<Service.Security.ITokenSigningKeyProvider>();

            serviceProvider.GetRequiredService<Service.Clients.IClientService>().SeedClients();
            serviceProvider.GetRequiredService<Service.Users.IUserService>().SeedUsers();
        }

        private static void CreateStoresIfMissing(DataStoreSettings settings)
        {
            CreateStoreIfMissing(settings.AuthorisationCodesStorePath);
            CreateStoreIfMissing(settings.ClientsStorePath);
            CreateStoreIfMissing(settings.RefreshTokensStorePath);
            CreateStoreIfMissing(settings.UsersStorePath);
        }

        private static void CreateStoreIfMissing(string storePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(storePath);

            string fullPath = System.IO.Path.GetFullPath(storePath);
            string storeDirectory = System.IO.Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(storeDirectory))
            {
                storeDirectory = string.Empty;
            }

            if (!string.IsNullOrEmpty(storeDirectory) && !System.IO.Directory.Exists(storeDirectory))
            {
                System.IO.Directory.CreateDirectory(storeDirectory);
            }

            if (!System.IO.File.Exists(fullPath))
            {
                System.IO.File.WriteAllText(fullPath, "[]");
            }
        }
    }
}