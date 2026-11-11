# Testing

## Test Projects

| Project | Level | Scope |
|---------|-------|-------|
| `McpOAuthServer.UnitTests` | Unit | Security helpers, settings validation, client and user services, authorisation and token services |
| `McpOAuthServer.IntegrationTests` | Integration | Hosted HTTP contract: discovery, client registration, authorisation-code flow, refresh, revocation, introspection, UserInfo, and error handling |
| `McpOAuthServer.ArchitectureTests` | Architecture | Layer dependencies, boundary placement, project and solution conventions, and source-convention enforcement |

## Unit Coverage

- `SecretHasherTests` — hashing, verification, salting, malformed hash rejection, iteration validation
- `ProofKeyForCodeExchangeTests` — S256 verification and challenge-method support
- `TokenSigningKeyProviderTests` — key generation, reuse, RS256 credentials, and blank-path rejection
- `OAuthServerSettingsValidatorTests` — issuer, lifetime, scope, resource, and normalisation rules
- `ClientServiceTests` — public and confidential registration, redirect allowlisting, secret authentication, and client seeding
- `UserServiceTests` — credential verification, unknown accounts, and seed behaviour
- `AuthorisationServiceTests` — response type, client, redirect URI, PKCE, scope, and resource validation
- `TokenServiceTests` — audience, scope, ID-token claims, refresh-token issuance, and introspection rejection paths
- `TokenExchangeServiceTests` — code exchange, client authentication, expiry, PKCE mismatch, refresh, scope escalation, and grant-type rejection

## Integration Coverage

- Discovery documents publish the required OAuth and OpenID Connect fields
- The JWKS endpoint publishes an RSA signing key without private parameters
- Startup creates the JSON stores and the signing key
- A complete authorisation-code flow issues an access token, ID token, and refresh token with the `iss` parameter
- UserInfo returns profile claims from a bearer token
- Refresh issues new tokens; revocation invalidates the refresh token
- Introspection reports active and inactive tokens
- Invalid PKCE, unregistered redirect URIs, unrecognised resources, invalid credentials, denied consent, insecure redirect URIs, and code reuse are rejected
- Error responses never echo submitted credentials

## Architecture Coverage

- API, service, configuration, data-access, and logging boundaries prohibit the documented cross-layer dependencies
- Controllers depend on application-service abstractions rather than concrete implementations
- The service layer reaches persistence only through repository interfaces
- One type per identically named file, explicit accessibility, sealed models, primary-constructor services, expression-bodied single-statement methods, and banned language constructs are enforced
- Project declarations, package ordering, and `.slnx` structure are verified, including evaluated MSBuild metadata for `Debug` and `Release`

## Running The Suites

```bash
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

```bash
dotnet test McpOAuthServer.UnitTests/McpOAuthServer.UnitTests.csproj --verbosity normal
dotnet test McpOAuthServer.IntegrationTests/McpOAuthServer.IntegrationTests.csproj --verbosity normal
dotnet test McpOAuthServer.ArchitectureTests/McpOAuthServer.ArchitectureTests.csproj --verbosity normal
```

## Determinism

- Integration tests bind an isolated temporary data directory and a generated signing key.
- Password hashing uses a reduced iteration count, and seeded credentials are produced per test run.
- No test requires network access, a container, a database, or host-specific state.
- Time-dependent assertions use short lifetimes and immediate execution rather than clock control.