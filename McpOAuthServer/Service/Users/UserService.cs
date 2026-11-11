using System;
using System.Linq;

using NuciDAL.Repositories;

using NuciLog.Core;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.Mapping;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.Users
{
    public sealed class UserService(
        IFileRepository<OAuthUserDataObject> repository,
        OAuthServerSettings settings,
        ISecretHasher secretHasher,
        ILogger logger) : IUserService
    {
        public OAuthUser Get(string userId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            return repository.Get(userId).ToServiceModel();
        }

        public OAuthUser TryGetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            OAuthUserDataObject dataObject = repository
                .Find(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (dataObject is null)
            {
                return null;
            }

            return dataObject.ToServiceModel();
        }

        public UserAuthentication Authenticate(string username, string password)
        {
            OAuthUser candidate = TryGetByUsername(username);

            if (candidate is null || password is null ||
                !secretHasher.VerifySecret(password, candidate.PasswordHash))
            {
                logger.Info(
                    OAuthServerOperation.AuthenticateUser,
                    OperationStatus.Failure,
                    new LogInfo(OAuthServerLogInfoKey.Error, "invalid_credentials"));

                return new() { IsAuthenticated = false };
            }

            logger.Info(
                OAuthServerOperation.AuthenticateUser,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.UserId, candidate.Id));

            return new()
            {
                IsAuthenticated = true,
                User = candidate
            };
        }

        public void SeedUsers()
        {
            int count = 0;

            foreach (SeedUserSettings definition in settings.SeedUsers)
            {
                if (string.IsNullOrWhiteSpace(definition.Username) ||
                    string.IsNullOrWhiteSpace(definition.PasswordHash) ||
                    TryGetByUsername(definition.Username) is not null)
                {
                    continue;
                }

                string userId = definition.Id;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = Guid.NewGuid().ToString();
                }

                OAuthUser user = new()
                {
                    Id = userId,
                    Username = definition.Username,
                    PasswordHash = definition.PasswordHash,
                    Email = definition.Email,
                    DisplayName = definition.DisplayName,
                    IsEmailVerified = definition.IsEmailVerified
                };

                repository.Add(user.ToDataObject());

                count += 1;
            }

            if (count > 0)
            {
                repository.SaveChanges();
            }

            logger.Info(
                OAuthServerOperation.SeedUsers,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.Count, count));
        }
    }
}