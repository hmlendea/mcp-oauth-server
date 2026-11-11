# Configuration Reference

MCP OAuth Server binds its settings during startup through the standard ASP.NET Core configuration providers.

## OAuthServerSettings

**Configuration section:** `oAuthServerSettings`

| Property | Type | Default | Required | Description |
|----------|------|---------|----------|-------------|
| `issuer` | `string` | `https://auth.example.com` | Yes | Public HTTPS identifier of this authorisation server. It must match the `issuer` published in metadata and the `iss` parameter returned in authorisation responses. |
| `signingKeyPath` | `string` | `Data/signing-key.pem` | Yes | PKCS#8 PEM RSA private key. Generated with `2048`-bit strength when absent. Relative paths resolve from the working directory. |
| `supportedScopes` | `string[]` | `memories`, `openid`, `email`, `profile` | Yes | Scopes offered and granted. At least one entry is required. |
| `allowedResources` | `string[]` | Empty | Yes | Canonical HTTPS identifiers of the MCP servers this authorisation server issues tokens for. At least one entry is required. |
| `allowedRedirectUriPrefixes` | `string[]` | `https://chatgpt.com/`, `http://127.0.0.1`, `http://localhost` | Yes | Permitted prefixes for client redirect URIs. Loopback HTTP URIs and any HTTPS URI matching a prefix are accepted. |
| `authorisationCodeLifetimeSeconds` | `int` | `60` | Yes | Validity of an issued authorisation code. |
| `accessTokenLifetimeSeconds` | `int` | `3600` | Yes | Validity of an issued access token. |
| `idTokenLifetimeSeconds` | `int` | `3600` | Yes | Validity of an issued OpenID Connect ID token. |
| `refreshTokenLifetimeSeconds` | `int` | `2592000` | Yes | Validity of an issued refresh token. |
| `passwordHashIterations` | `int` | `210000` | Yes | PBKDF2-SHA256 iteration count for account passwords and confidential client secrets. |
| `predefinedClients` | `object[]` | Empty | No | Clients inserted at startup when absent. Each entry defines `clientId`, `name`, `redirectUris`, `allowedScopes`, `isPublic`, and `secretHash`. |
| `seedUsers` | `object[]` | Empty | No | Accounts inserted at startup when absent. Each entry defines `id`, `username`, `passwordHash`, `email`, `displayName`, and `isEmailVerified`. |

Environment variables are `OAuthServerSettings__Issuer`, `OAuthServerSettings__SigningKeyPath`, `OAuthServerSettings__SupportedScopes__0`, and so on.

## DataStoreSettings

**Configuration section:** `dataStoreSettings`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `authorisationCodesStorePath` | `string` | `Data/authorisation-codes.json` | JSON store for unconsumed authorisation codes |
| `clientsStorePath` | `string` | `Data/clients.json` | JSON store for dynamically registered and predefined clients |
| `refreshTokensStorePath` | `string` | `Data/refresh-tokens.json` | JSON store for active refresh tokens |
| `usersStorePath` | `string` | `Data/users.json` | JSON store for seeded accounts |

Every store is created with an empty JSON array when absent. Relative paths resolve from the working directory.

## NuciLoggerSettings

**Configuration section:** `nuciLoggerSettings`

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `logFilePath` | `string` | `logfile.log` | Structured log destination |
| `isFileOutputEnabled` | `boolean` | `true` | Activates file log output |

Log entries contain operation names, statuses, client identifiers, user identifiers, scopes, and audiences. Passwords, secrets, tokens, and authorisation codes are never logged.

## Validation

Startup fails when the issuer is not an absolute HTTPS URI without a path, when no scope or resource is configured, when a lifetime or iteration count is not positive, or when a configured resource or redirect prefix is not an absolute URI. Resolving the signing-key provider also fails startup when the key path is unusable.

## Reload Behaviour

Configuration is bound to singleton settings during startup. Restart the application after revising settings.

## Secret Management

The committed `appsettings.json` contains placeholder values only. Provide the real issuer, signing-key path, account hashes, and client secrets through the deployment platform's secret provider. Never commit a production private key or a password hash derived from a real credential.

Generate a password or secret hash with:

```bash
dotnet run --project McpOAuthServer -- generate-password-hash
```