using System;
using System.Collections.Generic;

using NuciDAL.Repositories;

using NuciLog.Core;

using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.Mapping;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.AuthorisationCodes
{
    public sealed class AuthorisationCodeService(
        IFileRepository<AuthorisationCodeDataObject> repository,
        ILogger logger) : IAuthorisationCodeService
    {
        public string Create(AuthorisationCode authorisationCode)
        {
            ArgumentNullException.ThrowIfNull(authorisationCode);

            string code = SecureTokenGenerator.GenerateToken();

            authorisationCode.Id = code;
            repository.Add(authorisationCode.ToDataObject());
            repository.SaveChanges();

            logger.Debug(
                OAuthServerOperation.CreateAuthorisationCode,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.ClientId, authorisationCode.ClientId),
                new LogInfo(OAuthServerLogInfoKey.Scope, authorisationCode.Scope));

            return code;
        }

        public AuthorisationCode Consume(string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            AuthorisationCode authorisationCode;

            try
            {
                authorisationCode = repository.Get(code).ToServiceModel();
            }
            catch (EntityNotFoundException exception)
            {
                throw new KeyNotFoundException(
                    "The authorisation code is unknown or was already used.",
                    exception);
            }

            repository.Remove(code);
            repository.SaveChanges();

            return authorisationCode;
        }
    }
}