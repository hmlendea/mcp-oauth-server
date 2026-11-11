using System;
using System.Collections.Generic;
using System.Linq;

namespace McpOAuthServer.Configuration
{
    public static class OAuthServerSettingsValidator
    {
        public static void Validate(OAuthServerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Uri issuer = ParseAbsoluteUri(settings.Issuer, "Issuer");

            if (!string.Equals(issuer.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The OAuth issuer must use HTTPS.");
            }

            if (!string.IsNullOrEmpty(issuer.PathAndQuery) && !string.Equals(issuer.PathAndQuery, "/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The OAuth issuer must not contain a path or query component.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(settings.SigningKeyPath);
            RequirePositive(settings.AuthorisationCodeLifetimeSeconds, nameof(settings.AuthorisationCodeLifetimeSeconds));
            RequirePositive(settings.AccessTokenLifetimeSeconds, nameof(settings.AccessTokenLifetimeSeconds));
            RequirePositive(settings.IdTokenLifetimeSeconds, nameof(settings.IdTokenLifetimeSeconds));
            RequirePositive(settings.RefreshTokenLifetimeSeconds, nameof(settings.RefreshTokenLifetimeSeconds));
            RequirePositive(settings.PasswordHashIterations, nameof(settings.PasswordHashIterations));

            if (!settings.SupportedScopes.Any())
            {
                throw new InvalidOperationException("At least one supported scope must be configured.");
            }

            if (!settings.AllowedResources.Any())
            {
                throw new InvalidOperationException("At least one allowed resource must be configured.");
            }

            foreach (string resource in settings.AllowedResources)
            {
                ParseAbsoluteUri(resource, "AllowedResources entry");
            }

            foreach (string prefix in settings.AllowedRedirectUriPrefixes)
            {
                ParseAbsoluteUri(prefix, "AllowedRedirectUriPrefixes entry");
            }
        }

        public static IEnumerable<string> NormaliseScopes(
            IEnumerable<string> requestedScopes,
            IEnumerable<string> supportedScopes)
        {
            HashSet<string> supported = [.. supportedScopes];

            return requestedScopes
                .Where(scope => supported.Contains(scope))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(scope => scope, StringComparer.Ordinal);
        }

        private static Uri ParseAbsoluteUri(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Uri.TryCreate(value, UriKind.Absolute, out Uri? uri))
            {
                throw new InvalidOperationException($"The {name} value must be an absolute URI.");
            }

            return uri;
        }

        private static void RequirePositive(int value, string name)
        {
            if (value <= 0)
            {
                throw new InvalidOperationException($"The {name} value must be positive.");
            }
        }
    }
}