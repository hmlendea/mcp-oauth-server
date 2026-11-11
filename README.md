[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Build Status](https://github.com/hmlendea/mcp-oauth-server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/mcp-oauth-server/actions/workflows/dotnet.yml)
[![Security Scan](https://github.com/hmlendea/mcp-oauth-server/actions/workflows/security.yml/badge.svg)](https://github.com/hmlendea/mcp-oauth-server/actions/workflows/security.yml)
[![License](https://img.shields.io/github/license/hmlendea/mcp-oauth-server)](https://github.com/hmlendea/mcp-oauth-server/blob/master/LICENSE)

# MCP OAuth Server

MCP OAuth Server is a self-hosted ASP.NET Core OAuth 2.1 authorisation server that issues tokens for Model Context Protocol servers, including the ChatGPT MCP connection flow.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Capabilities](#capabilities)
- [Use Cases](#use-cases)
- [Usage](#usage)
- [Known Limitations](#known-limitations)
- [Installation](#installation)
  - [Manual Installation](#manual-installation)
- [Configuration](#configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Reload Behaviour](#reload-behaviour)
  - [Secret Management](#secret-management)
- [Compatibility](#compatibility)
- [Authentication and Authorisation](#authentication-and-authorisation)
  - [OAuth Flows](#oauth-flows)
- [Privacy and Data](#privacy-and-data)
  - [Data Locations](#data-locations)
- [Development](#development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Continuous Integration](#continuous-integration)
  - [Release](#release)
- [GitHub Actions](#github-actions)
- [Project Structure](#project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Architecture](#architecture)
- [Deployment](#deployment)
  - [Backup and Restore](#backup-and-restore)
- [Documentation](#documentation)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [Security](#security)
- [Support](#support)
- [Project Engagement](#project-engagement)
- [License](#license)

## ✨ Capabilities

- Publish OAuth 2.0 and OpenID Connect discovery metadata with `S256` PKCE and RFC 9207 issuer identification.
- Register MCP clients dynamically, or seed predefined public and confidential clients.
- Render a consent page, authenticate a stored account, and issue single-use authorisation codes.
- Exchange authorisation codes and refresh tokens for RS256-signed access tokens, ID tokens, and rotated refresh tokens.
- Publish a JSON Web Key Set so resource servers can verify token signatures.
- Expose OpenID Connect profile claims, including verified email, for workspace domain restrictions.
- Introspect and revoke tokens and reject codes, credentials, redirect URIs, and resources that violate policy.

## 🎯 Use Cases

- **ChatGPT MCP connections:** Supply the OAuth 2.1 authorisation server that a ChatGPT MCP connection expects.
- **Self-hosted MCP servers:** Add standards-based authentication to a private MCP server without an external identity provider.
- **Development and testing:** Exercise an MCP authorisation flow locally with an isolated JSON-backed data store.

## 🚀 Usage

Register a client, complete consent, and exchange the code:

```bash
curl --request POST \
  --url "https://auth.example.com/register" \
  --header "Content-Type: application/json" \
  --data '{
    "client_name": "ChatGPT",
    "redirect_uris": [ "https://chatgpt.com/connector_platform_oauth_redirect" ],
    "grant_types": [ "authorization_code" ],
    "scope": [ "memories", "openid", "email" ],
    "token_endpoint_auth_method": "none"
  }'
```

Then direct the browser to `/authorize` with the returned `client_id`, a PKCE challenge, and the MCP `resource`, and exchange the returned `code` at `/token` with `grant_type=authorization_code`.

## ⚠️ Known Limitations

- All state is stored in local JSON files; there is no database, clustering, or multi-instance coordination.
- A single RSA signing key is published; key rotation requires a configuration change and a restart.
- Client ID Metadata Documents are not supported, so clients register dynamically or use predefined entries.
- Accounts and client registrations are managed through configuration or JSON stores rather than administration endpoints.
- The consent page authenticates a password directly; there is no external identity provider, multifactor authentication, or session persistence.

## 📦 Installation

### Manual Installation

1. Download the archive corresponding to your operating system and processor from the latest release.
2. Extract the archive into its permanent directory.
3. Configure the issuer, allowed resources, and signing-key path through your secret provider.
4. Start the included `McpOAuthServer` executable behind an HTTPS reverse proxy.

## ⚙️ Configuration

The application binds settings during startup through the standard ASP.NET Core configuration providers.

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `McpOAuthServer/appsettings.json` | Application | Defines issuer, scopes, resources, lifetimes, stores, and logging |

### Settings

The subsequent settings are recognised:

| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `oAuthServerSettings` | `issuer` | `string` | No usable default | Yes | Public HTTPS identifier of this authorisation server |
| `oAuthServerSettings` | `signingKeyPath` | `string` | `Data/signing-key.pem` | Yes | PKCS#8 PEM RSA signing key; generated when absent |
| `oAuthServerSettings` | `supportedScopes` | `string[]` | `memories`, `openid`, `email`, `profile` | Yes | Scopes offered to clients |
| `oAuthServerSettings` | `allowedResources` | `string[]` | Empty | Yes | MCP resource identifiers accepted as token audiences |
| `oAuthServerSettings` | `allowedRedirectUriPrefixes` | `string[]` | ChatGPT and loopback prefixes | Yes | Permitted client redirect URIs |
| `oAuthServerSettings` | `authorisationCodeLifetimeSeconds` | `int` | `60` | Yes | Authorisation-code lifetime |
| `oAuthServerSettings` | `accessTokenLifetimeSeconds` | `int` | `3600` | Yes | Access-token lifetime |
| `oAuthServerSettings` | `idTokenLifetimeSeconds` | `int` | `3600` | Yes | ID-token lifetime |
| `oAuthServerSettings` | `refreshTokenLifetimeSeconds` | `int` | `2592000` | Yes | Refresh-token lifetime |
| `oAuthServerSettings` | `passwordHashIterations` | `int` | `210000` | Yes | PBKDF2-SHA256 iteration count |
| `oAuthServerSettings` | `predefinedClients` | `object[]` | Empty | No | Clients inserted at startup when absent |
| `oAuthServerSettings` | `seedUsers` | `object[]` | Empty | No | Accounts inserted at startup when absent |
| `dataStoreSettings` | `authorisationCodesStorePath` | `string` | `Data/authorisation-codes.json` | Yes | Authorisation-code store |
| `dataStoreSettings` | `clientsStorePath` | `string` | `Data/clients.json` | Yes | Client store |
| `dataStoreSettings` | `refreshTokensStorePath` | `string` | `Data/refresh-tokens.json` | Yes | Refresh-token store |
| `dataStoreSettings` | `usersStorePath` | `string` | `Data/users.json` | Yes | Account store |
| `nuciLoggerSettings` | `logFilePath` | `string` | `logfile.log` | When file output is active | Structured log destination |
| `nuciLoggerSettings` | `isFileOutputEnabled` | `boolean` | `true` | Yes | Activates file log output |

When a configured store is absent, the application creates its parent directory and initialises the file with an empty JSON array.

### Reload Behaviour

Configuration is bound to singleton settings during startup. Restart the application after revising settings.

### Secret Management

The committed configuration contains placeholder values only. Provide the genuine issuer, signing-key path, account hashes, and client secrets through a trusted secret provider, and never commit them to source control. Generate account and secret hashes with `dotnet run --project McpOAuthServer -- generate-password-hash`.

## 🧩 Compatibility

| Component | Supported Versions | Notes |
|-----------|--------------------|-------|
| .NET SDK | 10.0 | Required when compiling or running from source |
| OAuth 2.1 | Authorisation code with PKCE `S256` | Required by the MCP authorisation profile |
| OpenID Connect | Discovery and UserInfo | Used for profile claims and workspace domain restrictions |
| ChatGPT MCP connections | Stable and callback-specific redirect URIs | Issuer identification is always advertised |

## 🔐 Authentication and Authorisation

The server authenticates clients through dynamic registration, predefined client entries, or no authentication for public clients. Confidential clients present a secret, verified against a PBKDF2-SHA256 hash. Accounts authenticate with a username and password at consent time; the password is likewise verified against a stored hash.

Every token exchange validates the client, the redirect URI, the authorisation-code lifetime, and the PKCE verifier. Access and ID tokens are signed with RS256 and validated by resource servers through the published JSON Web Key Set.

### OAuth Flows

| Role or Scope | Access | Intended Audience |
|---------------|--------|-------------------|
| `authorization_code` | Issue tokens after user consent with PKCE | MCP clients such as ChatGPT |
| `refresh_token` | Rotate an access token within the original grant | MCP clients with a refresh token |
| `memories` | Example resource scope in the default configuration | MCP servers that accept it |
| `openid` | Request an ID token and UserInfo access | Clients that need profile claims |

## 🛡️ Privacy and Data

MCP OAuth Server processes credentials, client registrations, and token metadata locally. It transmits nothing to the project maintainers.

For the detailed description of how the application handles privacy and personal data, see the [Privacy Statement](./PRIVACY.md).

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Account identifiers, usernames, email addresses, display names, and password hashes | Authenticate users and issue profile claims | `users.json` | Until the operator deletes the record | No |
| Client identifiers, names, redirect URIs, scopes, and secret hashes | Validate clients and redirect destinations | `clients.json` | Until the operator deletes the record | No |
| Authorisation codes and PKCE challenges | Exchange a consent grant for tokens | `authorisation-codes.json` | Until consumed or expired | No |
| Refresh tokens | Continue a grant beyond access-token lifetime | `refresh-tokens.json` | Until consumed, revoked, or expired | No |
| Operation status and context | Diagnostics and operational records | Configured log file when file output is active | No retention policy is configured by this project | File output is optional |

### Data Locations

| Platform or Scope | Location | Contents |
|-------------------|----------|----------|
| Default application working directory | `Data/signing-key.pem` | RSA signing key |
| Default application working directory | `Data/clients.json`, `Data/users.json` | Client and account records |
| Default application working directory | `Data/authorisation-codes.json`, `Data/refresh-tokens.json` | Short-lived grant material |
| Default application working directory | `logfile.log` | Structured application logs when file output is active |

All locations can be revised through configuration.

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

```bash
git clone https://github.com/hmlendea/mcp-oauth-server.git
cd mcp-oauth-server
dotnet restore
```

### Build

```bash
dotnet build --no-restore
```

### Run

Provide the minimum configuration, then execute:

```bash
OAuthServerSettings__Issuer="https://localhost:7099" \
OAuthServerSettings__AllowedResources__0="https://memory.example.com/mcp" \
dotnet run --project McpOAuthServer -- --urls "http://127.0.0.1:5099"
```

### Test

Compile the solution first, then execute:

```bash
dotnet test --no-build --verbosity normal
```

The integration suite hosts the application with an isolated temporary data directory:

```bash
dotnet test McpOAuthServer.IntegrationTests/McpOAuthServer.IntegrationTests.csproj
```

### Continuous Integration

The `.github/workflows/dotnet.yml` workflow restores dependencies, compiles the solution, and executes the test suite on Ubuntu for pushes and pull requests associated with `master`. The `.github/workflows/security.yml` workflow performs vulnerability scanning and CodeQL analysis on the same triggers plus a weekly schedule. The commands above reproduce those checks locally.

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [.NET](https://github.com/hmlendea/mcp-oauth-server/blob/master/.github/workflows/dotnet.yml) | Build and test | Restores dependencies, builds the solution, and runs the full test suite on Ubuntu |
| [Security](https://github.com/hmlendea/mcp-oauth-server/blob/master/.github/workflows/security.yml) | Security scanning | Checks NuGet packages for known vulnerabilities and runs CodeQL analysis for C# |

## 🗂️ Project Structure

The solution separates the authorisation server from its unit, integration, and architecture test projects.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `McpOAuthServer/McpOAuthServer.csproj` | ASP.NET Core web API | Hosts discovery, registration, consent, token, revocation, introspection, and UserInfo endpoints |
| `McpOAuthServer.UnitTests/McpOAuthServer.UnitTests.csproj` | NUnit test project | Verifies services and security helpers in isolation |
| `McpOAuthServer.IntegrationTests/McpOAuthServer.IntegrationTests.csproj` | NUnit integration-test project | Verifies the hosted HTTP contract end to end |
| `McpOAuthServer.ArchitectureTests/McpOAuthServer.ArchitectureTests.csproj` | NUnit architecture-test project | Verifies boundaries, placement, and source conventions |

### Directories

| Directory | Purpose |
|-----------|---------|
| `McpOAuthServer/Api` | Controllers, request and response contracts, consent page rendering |
| `McpOAuthServer/Cli` | Console commands such as password-hash generation |
| `McpOAuthServer/Configuration` | Strongly typed settings and validation |
| `McpOAuthServer/DataAccess` | Persisted data objects |
| `McpOAuthServer/Logging` | Structured operation and context identifiers |
| `McpOAuthServer/Service` | Authorisation, client, user, token, and security logic |
| `docs` | Configuration, API, integration, deployment, development, and testing references |

## 🏗️ Architecture

MCP OAuth Server is a layered ASP.NET Core service that acts as an OAuth 2.1 authorisation server for MCP resources, keeping HTTP contracts in controllers, policy in application services, and persistence behind repository interfaces.

See the [Architecture Documentation](ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🚢 Deployment

Deploy the service behind a reverse proxy that terminates TLS at the public issuer hostname, point the issuer and allowed resources at public URLs, and persist the signing key and JSON stores outside the release directory.

See the [Deployment Guide](docs/deployment.md) for topology, key handling, and upgrade steps.

### Backup and Restore

To create a consistent backup, suspend the service and copy `clients.json`, `users.json`, `refresh-tokens.json`, and the signing key. To restore, place valid copies at the configured paths before starting the service.

## 📚 Documentation

| Resource | Description |
|----------|-------------|
| [Configuration Reference](docs/configuration.md) | Settings, environment variables, validation, and secret management |
| [API Reference](docs/api-reference.md) | Endpoints, parameters, responses, and error codes |
| [ChatGPT Integration](docs/chatgpt-integration.md) | Connecting a ChatGPT MCP connection |
| [Deployment Guide](docs/deployment.md) | Topology, TLS, keys, and persistence |
| [Development Guide](docs/development.md) | Build, run, and verification workflow |
| [Testing Guide](docs/testing.md) | Test suites and coverage |

## � Troubleshooting

| Symptom | Probable Cause | Resolution |
|---------|----------------|------------|
| `invalid_target` during consent | `resource` is not listed in `allowedResources` | Add the exact public MCP resource identifier and restart |
| `invalid_request` before the consent page renders | Response type is not `code`, the redirect URI is unregistered, or the PKCE method is not `S256` | Correct the client request or registration |
| `invalid_grant` at token exchange | Code already consumed, expired, or verifier mismatch | Restart the flow and verify PKCE handling |
| `invalid_client` | Unknown client or incorrect confidential secret | Re-register the client or verify the stored hash |
| Startup fails | Issuer is not a bare HTTPS URI, no scope or resource is configured, or the signing key is unusable | Correct the configuration and restart |
| Resource server rejects the token | Audience, issuer, or scope mismatch | Align the resource identifier and required scope on both services |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/mcp-oauth-server/issues) for problems or suggestions

See the [Contributing Guidelines](./CONTRIBUTING.md) for details on how to report issues, suggest enhancements, and submit changes.

## 📜 Code of Conduct

See the [Code of Conduct](./CODE_OF_CONDUCT.md) for the standards of conduct expected in all project spaces.

## 🔒 Security

MCP OAuth Server handles credentials and signing material, so vulnerabilities in token validation, secret storage, or request handling are treated as high priority.

For information on reporting security vulnerabilities, see the [Security Policy](./SECURITY.md).

## 💬 Support

For support, please [open an issue](https://github.com/hmlendea/mcp-oauth-server/issues).

## 💝 Project Engagement

Discovered a problem with MCP OAuth Server or have a suggestion for it? [Open an issue](https://github.com/hmlendea/mcp-oauth-server/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0`.
See [LICENSE](./LICENSE) for additional information.