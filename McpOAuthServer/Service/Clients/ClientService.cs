using System;
using System.Collections.Generic;
using System.Linq;

using NuciDAL.Repositories;

using NuciLog.Core;

using McpOAuthServer.Configuration;
using McpOAuthServer.DataAccess.DataObjects;
using McpOAuthServer.Logging;
using McpOAuthServer.Service.Mapping;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.Clients
{
    public sealed class ClientService(
        IFileRepository<OAuthClientDataObject> repository,
        OAuthServerSettings settings,
        ISecretHasher secretHasher,
        ILogger logger) : IClientService
    {
        public OAuthClient Get(string clientId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

            return repository.Get(clientId).ToServiceModel();
        }

        public OAuthClient TryGet(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return null;
            }

            OAuthClientDataObject dataObject = repository.TryGet(clientId);

            if (dataObject is null)
            {
                return null;
            }

            return dataObject.ToServiceModel();
        }

        public ClientRegistrationResult Register(ClientRegistrationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            List<string> redirectUris = [.. request.RedirectUris];

            if (redirectUris.Count == 0)
            {
                return Failure("At least one redirect URI is required.");
            }

            foreach (string redirectUri in redirectUris)
            {
                string errorDescription = ValidateRedirectUri(redirectUri);

                if (errorDescription.Length > 0)
                {
                    return Failure(errorDescription);
                }
            }

            List<string> allowedScopes = [.. OAuthServerSettingsValidator.NormaliseScopes(
                request.Scopes,
                settings.SupportedScopes)];
            bool isPublic = string.IsNullOrWhiteSpace(request.TokenEndpointAuthenticationMethod) ||
                string.Equals(request.TokenEndpointAuthenticationMethod, "none", StringComparison.Ordinal);
            string clientId = SecureTokenGenerator.GenerateClientId();
            string clientSecret = string.Empty;

            if (!isPublic)
            {
                clientSecret = SecureTokenGenerator.GenerateToken();
            }

            string secretHash = string.Empty;

            if (clientSecret.Length > 0)
            {
                secretHash = secretHasher.HashSecret(clientSecret);
            }

            OAuthClient client = new()
            {
                Id = clientId,
                Name = request.Name,
                RedirectUris = redirectUris,
                AllowedScopes = allowedScopes,
                IsPublic = isPublic,
                SecretHash = secretHash,
                CreatedDateTime = DateTimeOffset.UtcNow
            };

            repository.Add(client.ToDataObject());
            repository.SaveChanges();

            logger.Info(
                OAuthServerOperation.RegisterClient,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.ClientId, clientId));

            return new()
            {
                IsSuccessful = true,
                Client = new()
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Name = client.Name,
                    IsPublic = isPublic,
                    Scope = string.Join(' ', client.AllowedScopes)
                }
            };
        }

        public ClientAuthentication Authenticate(string clientId, string clientSecret)
        {
            OAuthClient client = TryGet(clientId);

            if (client is null)
            {
                return new()
                {
                    IsAuthenticated = false,
                    Error = "invalid_client"
                };
            }

            if (client.IsPublic)
            {
                return new()
                {
                    IsAuthenticated = true,
                    ClientId = client.Id,
                    Client = client
                };
            }

            if (string.IsNullOrWhiteSpace(clientSecret) ||
                !secretHasher.VerifySecret(clientSecret, client.SecretHash))
            {
                return new()
                {
                    IsAuthenticated = false,
                    Error = "invalid_client"
                };
            }

            return new()
            {
                IsAuthenticated = true,
                ClientId = client.Id,
                Client = client
            };
        }

        public bool IsRedirectUriAllowed(OAuthClient client, string redirectUri)
        {
            if (client is null || string.IsNullOrWhiteSpace(redirectUri))
            {
                return false;
            }

            if (!client.RedirectUris.Any(registered =>
                string.Equals(registered, redirectUri, StringComparison.Ordinal)))
            {
                return false;
            }

            return ValidateRedirectUri(redirectUri).Length == 0;
        }

        public void SeedClients()
        {
            int count = 0;

            foreach (PredefinedClientSettings definition in settings.PredefinedClients)
            {
                if (string.IsNullOrWhiteSpace(definition.ClientId) ||
                    repository.ContainsId(definition.ClientId))
                {
                    continue;
                }

                OAuthClient client = new()
                {
                    Id = definition.ClientId,
                    Name = definition.Name,
                    RedirectUris = [.. definition.RedirectUris],
                    AllowedScopes = [.. OAuthServerSettingsValidator.NormaliseScopes(
                        definition.AllowedScopes,
                        settings.SupportedScopes)],
                    IsPublic = definition.IsPublic,
                    SecretHash = definition.SecretHash,
                    CreatedDateTime = DateTimeOffset.UtcNow
                };

                repository.Add(client.ToDataObject());

                count += 1;
            }

            if (count > 0)
            {
                repository.SaveChanges();
            }

            logger.Info(
                OAuthServerOperation.RegisterClient,
                OperationStatus.Success,
                new LogInfo(OAuthServerLogInfoKey.Count, count));
        }

        private static ClientRegistrationResult Failure(string errorDescription)
            => new()
            {
                IsSuccessful = false,
                ErrorDescription = errorDescription
            };

        private string ValidateRedirectUri(string redirectUri)
        {
            if (string.IsNullOrWhiteSpace(redirectUri) ||
                !Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? uri))
            {
                return "Each redirect URI must be an absolute URI.";
            }

            if (uri.IsLoopback &&
                (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) ||
                 string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)))
            {
                return string.Empty;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            {
                return "Non-loopback redirect URIs must use HTTPS.";
            }

            if (!settings.AllowedRedirectUriPrefixes.Any(prefix =>
                redirectUri.StartsWith(prefix, StringComparison.Ordinal)))
            {
                return "The redirect URI is not a permitted destination.";
            }

            return string.Empty;
        }
    }
}