# Development

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Setup

```bash
git clone https://github.com/hmlendea/mcp-oauth-server.git
cd mcp-oauth-server
dotnet restore
```

## Build

```bash
dotnet build --no-restore
```

## Run

Create an account hash first:

```bash
dotnet run --project McpOAuthServer -- generate-password-hash
```

Then set the minimum configuration and start the service:

```bash
export OAuthServerSettings__Issuer="https://localhost:7099"
export OAuthServerSettings__AllowedResources__0="https://memory.example.com/mcp"
export OAuthServerSettings__SupportedScopes__0="memories"
export OAuthServerSettings__SeedUsers__0__Username="solaire_of_astora"
export OAuthServerSettings__SeedUsers__0__PasswordHash="<generated hash>"
export OAuthServerSettings__SeedUsers__0__Email="solaire@astora.example"
export OAuthServerSettings__SeedUsers__0__IsEmailVerified="true"

dotnet run --project McpOAuthServer -- --urls "http://127.0.0.1:5099"
```

A browser-based OAuth flow expects an HTTPS issuer. For local development, either terminate TLS with a proxy or use `dotnet dev-certs https` and an `https` listener.

## Verify Locally

```bash
curl -s http://127.0.0.1:5099/.well-known/oauth-authorization-server
curl -s http://127.0.0.1:5099/jwks

curl -s -X POST http://127.0.0.1:5099/register \
  -H "Content-Type: application/json" \
  -d '{
    "client_name": "ChatGPT",
    "redirect_uris": ["https://chatgpt.com/connector_platform_oauth_redirect"],
    "grant_types": ["authorization_code"],
    "scope": ["memories"],
    "token_endpoint_auth_method": "none"
  }'
```

Open `http://127.0.0.1:5099/authorize?response_type=code&client_id=<client_id>&redirect_uri=https%3A%2F%2Fchatgpt.com%2Fconnector_platform_oauth_redirect&scope=memories&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256&resource=https%3A%2F%2Fmemory.example.com%2Fmcp` to render the consent page.

## Project Structure

| Directory | Purpose |
|-----------|---------|
| `McpOAuthServer/Api` | Controllers, request and response contracts, page rendering |
| `McpOAuthServer/Cli` | Console commands |
| `McpOAuthServer/Configuration` | Strongly typed settings and validation |
| `McpOAuthServer/DataAccess` | Persisted data objects |
| `McpOAuthServer/Logging` | Structured operation and context identifiers |
| `McpOAuthServer/Service` | Authorisation, client, user, token, and security services |
| `McpOAuthServer.UnitTests` | Unit tests |
| `McpOAuthServer.IntegrationTests` | HTTP integration tests |
| `McpOAuthServer.ArchitectureTests` | Architecture and source-convention tests |

## Test

```bash
dotnet build --no-restore
dotnet test --no-build --verbosity normal
```

Individual suites:

```bash
dotnet test McpOAuthServer.UnitTests/McpOAuthServer.UnitTests.csproj
dotnet test McpOAuthServer.IntegrationTests/McpOAuthServer.IntegrationTests.csproj
dotnet test McpOAuthServer.ArchitectureTests/McpOAuthServer.ArchitectureTests.csproj
```

The integration suite hosts the application with an isolated temporary data directory and a short password-hash iteration count, so no external service or elevated cost is required. See [Testing](testing.md) for the coverage map.