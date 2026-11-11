using System;
using System.Collections.Generic;

using NuciDAL.Repositories;

using NuciLog.Core;

using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.Mapping;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.RefreshTokens
{
    public sealed class RefreshTokenService(
        IFileRepository<RefreshTokenDataObject> repository,
        ILogger logger) : IRefreshTokenService
    {
        public string Create(RefreshToken refreshToken)
        {
            ArgumentNullException.ThrowIfNull(refreshToken);

            string token = SecureTokenGenerator.GenerateToken();

            refreshToken.Id = token;
            repository.Add(refreshToken.ToDataObject());
            repository.SaveChanges();

            logger.Debug(
                OAuthServerOperation.ExchangeRefreshToken,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.ClientId, refreshToken.ClientId));

            return token;
        }

        public RefreshToken Consume(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            RefreshToken refreshToken;

            try
            {
                refreshToken = repository.Get(token).ToServiceModel();
            }
            catch (EntityNotFoundException exception)
            {
                throw new KeyNotFoundException(
                    "The refresh token is unknown or was already used.",
                    exception);
            }

            repository.Remove(token);
            repository.SaveChanges();

            return refreshToken;
        }

        public bool Revoke(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || repository.TryGet(token) is null)
            {
                return false;
            }

            repository.Remove(token);
            repository.SaveChanges();

            logger.Info(
                OAuthServerOperation.RevokeRefreshToken,
                OperationStatus.Success);

            return true;
        }
    }
}