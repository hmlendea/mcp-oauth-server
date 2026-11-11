# Privacy and Personal Data

This document describes how MCP OAuth Server handles personal data when deployed as a self-hosted ASP.NET Core OAuth 2.1 authorisation server. The application stores account, client, grant, token, and diagnostic data locally and does not transmit data to the project maintainers by default.

**Information reviewed:** 2026-11-11

## 📑 Table of Contents

- What This Document Covers
- Self-Hosted Deployments
- Data We Handle
- Processing and Use
- Storage, Retention, and Deletion
- External Processing and Integrations
- User Controls and Requests
- Data Protection and Security
- Document Changes
- Contact

## 🔎 What This Document Covers

This document describes how MCP OAuth Server at https://github.com/hmlendea/mcp-oauth-server manages personal data. It covers the application conduct and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

MCP OAuth Server is designed for self-hosted deployment. The instance operator controls the application's configuration, local storage, logs, backups, access controls, preservation, and request handling. The project maintainers do not operate a hosted service and do not receive data from self-hosted instances.

The application has no built-in telemetry, update checks, crash reporting, email delivery, or external identity-provider integration. ChatGPT, MCP resource servers, reverse proxies, and monitoring systems configured by the operator may receive request data according to their own policies. Operators can deactivate optional file logging through `nuciLoggerSettings.isFileOutputEnabled`.

## 📥 Data We Handle

### Data Provided to the Application

- Account records: opaque identifier, username, PBKDF2 password hash, email address, display name, and email-verification flag.
- Client records: client identifier, display name, redirect URIs, allowed scopes, public/confidential mode, and confidential-client secret hash.
- OAuth requests: redirect URI, scopes, resource identifier, PKCE challenge, nonce, and consent credentials.

### Data Generated or Collected by the Application

- Authorisation codes and refresh tokens, stored as random opaque identifiers with client, account, scope, audience, nonce, and expiry metadata.
- Signed access tokens and ID tokens containing issuer, audience, subject, client, scope, lifetime, and optional profile claims.
- Structured diagnostics when file output is enabled: operation status, client identifiers, user identifiers, scopes, and audiences. Passwords, secrets, tokens, and authorisation codes are not logged by the application.

### Data Received from Integrations

- The application receives OAuth requests from clients such as ChatGPT and bearer-token requests from MCP resource servers or clients. The configured reverse proxy and MCP resource server are external deployment components and may process the same request data.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Authenticate accounts and issue OAuth grants — Usernames, password hashes, credentials, client identifiers, scopes, and redirect parameters
- Register and authenticate OAuth clients — Client identifiers, redirect URIs, scopes, and secret hashes
- Issue, validate, rotate, revoke, and introspect tokens — Grant metadata, token identifiers, audiences, scopes, and account identifiers
- Return OpenID Connect profile claims — Account identifier, email address, verification flag, display name, and username
- Record structured lifecycle events — Operation status and non-secret identifiers, scopes, and audiences

## 🗄️ Storage, Retention, and Deletion

Account records are stored in `dataStoreSettings.usersStorePath`; clients in `clientsStorePath`; authorisation codes in `authorisationCodesStorePath`; refresh tokens in `refreshTokensStorePath`; and the RSA private key in `oAuthServerSettings.signingKeyPath`. Structured logs are written to `nuciLoggerSettings.logFilePath` when file output is enabled.

Authorisation codes are removed when consumed and become unusable after expiry. Refresh tokens are removed when consumed for rotation or revoked. Account and client records remain until the operator removes them from the configured store or reseeds the deployment. Logs and backups have no application-enforced retention period.

For self-hosted deployments, the instance operator controls storage, deletion, and backups for all data and key files.

## 🔗 External Processing and Integrations

The application has no built-in transfer to the project maintainers. An operator may configure external clients and services that receive data as part of normal OAuth operation:

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| ChatGPT or another MCP client | Client registration, consent, and token exchange | Client metadata, redirect parameters, scopes, and tokens | [ChatGPT Integration](docs/chatgpt-integration.md) |
| MCP resource server | Consumes access tokens and enforces resource access | Bearer tokens and scopes | Operator's MCP resource-server configuration |
| Reverse proxy or monitoring service | TLS termination, routing, and operational diagnostics | HTTP metadata and potentially request logs | Operator's deployment configuration |

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Account deletion — The instance operator can remove the matching account record from `users.json` while the service is stopped.
- Client deletion — The instance operator can remove the matching client record from `clients.json` while the service is stopped.
- Refresh-token revocation — Authenticated clients can call `POST /revoke` with the refresh token.
- Log deactivation — Set `nuciLoggerSettings.isFileOutputEnabled` to `false` and restart the service.
- Store deletion — The operator can delete configured JSON stores and backups according to the deployment's retention requirements.

## 🛡️ Data Protection and Security

- Authentication: Accounts use PBKDF2-SHA256 password hashes, and confidential clients use hashed secrets verified with fixed-time comparison.
- Transport: The instance operator is responsible for public TLS termination and must keep the issuer HTTPS-only.
- Tokens: Access and ID tokens are RS256-signed; the JSON Web Key Set publishes no private key material.
- Storage: The instance operator controls filesystem permissions, encryption at rest, backups, and signing-key protection.
- Logs: Structured context can include client and user identifiers, scopes, and audiences. Operators should protect log files equivalently to the stores.
- External processing: ChatGPT and MCP resource servers receive OAuth data when configured by the operator; their policies are separate.
- No absolute security is promised; the operator must apply defence-in-depth appropriate to the threat model.

The original maintainers are not responsible for changes, modifications, or privacy issues introduced by downstream forks.

## 🔄 Document Changes

Update this document when application data processes, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/mcp-oauth-server/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers. For a self-hosted instance, contact the instance operator, unless the project explicitly manages the request. Include the deployment identifier or data process concerned; do not send passwords, access tokens, or other secrets.