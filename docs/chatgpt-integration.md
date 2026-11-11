# ChatGPT Integration

This document describes how to connect a ChatGPT MCP connection to MCP OAuth Server.

## Prerequisites

- The MCP server that ChatGPT connects to must publish protected-resource metadata pointing at this authorisation server.
- This authorisation server must be reachable over public HTTPS.
- At least one account must exist in the user store.

## 1. Configure The Authorisation Server

Set the issuer to the public HTTPS base URL of this application, for example `https://auth.example.com`, and register the MCP resource identifier:

```json
{
  "oAuthServerSettings": {
    "issuer": "https://auth.example.com",
    "allowedResources": [ "https://memory.example.com/mcp" ],
    "supportedScopes": [ "memories", "openid", "email" ]
  }
}
```

The `allowedResources` values must exactly match the `resource` value the MCP server advertises and the `resource` parameter ChatGPT sends. Tokens carry that value as `aud`.

## 2. Create An Account

Generate a password hash and add the account to the user store through `seedUsers` or by inserting a record into `users.json` while the application is stopped:

```bash
dotnet run --project McpOAuthServer -- generate-password-hash
```

```json
{
  "oAuthServerSettings": {
    "seedUsers": [
      {
        "username": "solaire_of_astora",
        "passwordHash": "<generated hash>",
        "email": "solaire@astora.example",
        "displayName": "Solaire of Astora",
        "isEmailVerified": true
      }
    ]
  }
}
```

`email` and `isEmailVerified` are optional but required for ChatGPT Enterprise workspace domain restrictions.

## 3. Point The MCP Server At This Authorisation Server

The MCP server must publish protected-resource metadata whose `authorization_servers` entry is this issuer, and it must validate the same audience and scope. For GPT Memory Store, configure:

```text
McpSettings__IsEnabled=true
McpSettings__Authority=https://auth.example.com
McpSettings__Resource=https://memory.example.com/mcp
McpSettings__RequiredScope=memories
```

## 4. Connect From ChatGPT

1. Open the MCP connection settings and enter the MCP server URL.
2. Choose OAuth authentication.
3. ChatGPT fetches `/.well-known/oauth-protected-resource` from the MCP server, then this server's `/.well-known/oauth-authorization-server`.
4. ChatGPT registers a client through `POST /register` unless the connection uses a predefined client.
5. The consent page from `GET /authorize` prompts for account credentials.
6. ChatGPT exchanges the code at `POST /token` and attaches the access token to MCP requests.

## Client Registration Modes

Dynamic client registration is supported and is the default path. To avoid dynamic registration, define a predefined public client whose redirect URI matches the ChatGPT callback:

```json
{
  "oAuthServerSettings": {
    "predefinedClients": [
      {
        "clientId": "chatgpt",
        "name": "ChatGPT",
        "redirectUris": [ "https://chatgpt.com/connector_platform_oauth_redirect" ],
        "allowedScopes": [ "memories", "openid", "email" ],
        "isPublic": true
      }
    ]
  }
}
```

Client ID Metadata Documents are not implemented. This server never fetches remote client metadata, which avoids server-side request forgery through the registration path.

## Redirect URIs

Two ChatGPT callback modes exist:

- `https://chatgpt.com/connector_platform_oauth_redirect` when the authorisation server publishes `authorization_response_iss_parameter_supported: true`, which this server always does.
- `https://chatgpt.com/connector/oauth/{callback_id}` for connections created before stable redirects were introduced.

Both are covered by the default `https://chatgpt.com/` prefix.

## Verifying The Connection

1. Confirm discovery responds: `curl https://auth.example.com/.well-known/oauth-authorization-server`.
2. Confirm the signing key is published: `curl https://auth.example.com/jwks`.
3. Complete the consent flow in a browser.
4. Exchange a code manually to confirm `aud` equals the MCP resource identifier.
5. Invoke a read-only MCP tool before any write tool.

## Troubleshooting

| Symptom | Probable Cause | Resolution |
|---------|----------------|------------|
| ChatGPT reports a missing `S256` method | Discovery unreachable | Confirm HTTPS reachability of the discovery document |
| `invalid_target` | `resource` not listed in `allowedResources` | Add the exact MCP resource identifier |
| `invalid_grant` at token exchange | Code already consumed, expired, or verifier mismatch | Restart the flow; verify PKCE handling |
| `invalid_client` | Unknown client or wrong confidential secret | Re-register the client or verify the stored secret hash |
| `403` at the MCP server | Token issued without the required scope | Add the scope to the client and the resource request |
| Account connection fails with an invalid profile | Missing or unverifiable `email` claim | Seed the account with an email address |