using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using McpOAuthServer.Cli;

namespace McpOAuthServer
{
    public sealed class Program
    {
        public static void Main(string[] arguments)
        {
            if (PasswordHashCommand.IsRequested(arguments))
            {
                PasswordHashCommand.Run();

                return;
            }

            CreateHostBuilder(arguments).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] arguments) => Host
            .CreateDefaultBuilder(arguments)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}