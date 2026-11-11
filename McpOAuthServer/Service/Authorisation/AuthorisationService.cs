using System;
using System.Collections.Generic;
using System.Linq;

using McpOAuthServer.Configuration;
using McpOAuthServer.Service.AuthorisationCodes;
using McpOAuthServer.Service.Clients;
using McpOAuthServer.Service.Models;
using McpOAuthServer.Service.Security;

namespace McpOAuthServer.Service.Authorisation
{
    public sealed class AuthorisationService(
        OAuthServerSettings settings,
        IClientService clientService,
        IAuthorisationCodeService authorisationCodeService) : IAuthorisationService
    {
        private static string AuthorisationCodeResponseType => "code";

        public AuthorisationValidation Validate(AuthorisationGrant request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!string.Equals(request.ResponseType, AuthorisationCodeResponseType, StringComparison.Ordinal))
            {
                return Invalid("unsupported_response_type", "Only the code response type is supported.");
            }

            OAuthClient client = clientService.TryGet(request.ClientId);

            if (client is null)
            {
                return Invalid("invalid_request", "The client identifier is unknown.");
            }

            if (!clientService.IsRedirectUriAllowed(client, request.RedirectUri))
            {
                return Invalid("invalid_request", "The redirect URI is not registered for this client.");
            }

            AuthorisationValidation validation = new()
            {
                RedirectUri = request.RedirectUri
            };

            if (!ProofKeyForCodeExchange.IsSupportedChallengeMethod(request.CodeChallengeMethod))
            {
                return Invalid(validation, "invalid_request", "The S256 code challenge method is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CodeChallenge))
            {
                return Invalid(validation, "invalid_request", "A code challenge is required.");
            }

            List<string> requestedScopes = [.. request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
            List<string> grantedScopes = [.. OAuthServerSettingsValidator.NormaliseScopes(
                requestedScopes,
                client.AllowedScopes)];

            if (grantedScopes.Count == 0)
            {
                return Invalid("invalid_scope", "None of the requested scopes are permitted.");
            }

            string audience = request.Resource;

            if (string.IsNullOrWhiteSpace(audience))
            {
                audience = settings.AllowedResources.First();
            }

            if (!settings.AllowedResources.Any(allowed =>
                string.Equals(allowed, audience, StringComparison.Ordinal)))
            {
                return Invalid(validation, "invalid_target", "The requested resource is not recognised.");
            }

            validation.IsValid = true;
            validation.Scope = string.Join(' ', grantedScopes);
            validation.Audience = audience;
            validation.ScopeValues = grantedScopes;

            return validation;
        }

        public string CreateCode(
            AuthorisationGrant request,
            AuthorisationValidation validation,
            string userId)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(validation);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            AuthorisationCode authorisationCode = new()
            {
                ClientId = request.ClientId,
                UserId = userId,
                RedirectUri = validation.RedirectUri,
                Scope = validation.Scope,
                Audience = validation.Audience,
                CodeChallenge = request.CodeChallenge,
                Nonce = request.Nonce,
                AuthenticationDateTime = now,
                ExpiryDateTime = now.AddSeconds(settings.AuthorisationCodeLifetimeSeconds)
            };

            return authorisationCodeService.Create(authorisationCode);
        }

        private static AuthorisationValidation Invalid(string error, string description)
            => Invalid(new AuthorisationValidation(), error, description);

        private static AuthorisationValidation Invalid(
            AuthorisationValidation validation,
            string error,
            string description)
        {
            validation.IsValid = false;
            validation.Error = error;
            validation.ErrorDescription = description;

            return validation;
        }
    }
}