using System;
using System.Collections.Generic;
using System.Linq;

using McpOAuthServer.Service.Security;
namespace McpOAuthServer.Cli
{
    public static class PasswordHashCommand
    {
        private static string CommandName => "generate-password-hash";

        public static bool IsRequested(IEnumerable<string> arguments)
            => arguments.Any(argument => string.Equals(argument, CommandName, StringComparison.Ordinal));

        public static void Run()
        {
            Console.Write("Password: ");
            string password = Console.ReadLine();

            if (string.IsNullOrEmpty(password))
            {
                Console.Error.WriteLine("A non-empty password is required on standard input.");
                return;
            }

            SecretHasher secretHasher = new(SecretHasher.DefaultIterations);

            Console.WriteLine(secretHasher.HashSecret(password));
        }
    }
}
