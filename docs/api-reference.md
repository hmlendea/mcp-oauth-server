# API Reference

MCP OAuth Server implements the authorisation-server side of the Model Context Protocol authorisation profile: OAuth 2.1 authorisation-code flow with PKCE, dynamic client registration, protected-resource metadata, and OpenID Connect discovery.

## Endpoints

| Method | Route | Authentication | Purpose |
|--------|-------|----------------|---------|
| `GET` | `/.well-known/oauth-authorization-server` | None | OAuth 2.0 authorisation-server metadata |
| `GET` | `/.well-known/openid-configuration` | None | OpenID Connect discovery metadata |
| `GET` | `/jwks` | None | RSA public signing keys |
| `POST` | `/register` | None | Dynamic client registration (JSON) |
| `GET` | `/authorize` | User session form | Authorisation consent page |
| `POST` | `/authorize` | User credentials | Consent decision and code issuance |
| `POST` | `/token` | Client authentication | Authorisation-code and refresh-token exchange |
| `POST` | `/revoke` | Client authentication | Refresh-token revocation |
| `POST` | `/introspect` | None | Access-token introspection |
| `GET` | `/userinfo` | Bearer token | OpenID Connect profile claims |

## Discovery Metadata

Both discovery documents publish the same document, including `issuer`, `authorization_endpoint`, `token_endpoint`, `registration_endpoint`, `revocation_endpoint`, `userinfo_endpoint`, `jwks_uri`, `response_types_supported`, `grant_types_supported`, `code_challenge_methods_supported`, `token_endpoint_auth_methods_supported`, `scopes_supported`, and `authorization_response_iss_parameter_supported`.

`code_challenge_methods_supported` always contains `S256`. `authorization_response_iss_parameter_supported` is always `true`, and every authorisation response, including error responses, carries `iss`.

## Dynamic Client Registration

`POST /register` accepts JSON:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `client_name` | `string` | No | Display name shown during consent |
| `redirect_uris` | `string[]` | Yes | Registered redirect URIs |
| `grant_types` | `string[]` | No | Advertised grant types |
| `scope` | `string[]` | No | Requested scopes; intersected with the configured supported scopes |
| `token_endpoint_auth_method` | `string` | No | `none` creates a public client; any other value creates a confidential client |

A successful response returns `201` with `client_id`, `client_secret` (empty for public clients), `client_name`, `client_id_issued_at`, `scope`, and `token_endpoint_auth_method`. An unacceptable redirect URI returns `400` with `invalid_redirect_uri`.

## Authorisation

`GET /authorize` accepts `response_type`, `client_id`, `redirect_uri`, `scope`, `state`, `code_challenge`, `code_challenge_method`, `resource`, and `nonce`. It renders a consent form after validating the request.

`POST /authorize` accepts the same parameters as form fields plus `username`, `password`, and `decision`. `decision=approve` authenticates the account and redirects to the registered redirect URI with:

| Parameter | Description |
|-----------|-------------|
| `code` | Single-use authorisation code |
| `state` | Echoed client state |
| `iss` | Issuer identifier (RFC 9207) |

`decision=deny` redirects with `error=access_denied`. Validation failures redirect with `error`, `error_description`, `state`, and `iss` when the redirect URI could be validated; otherwise the response is `400` plain text.

## Token Exchange

`POST /token` accepts `application/x-www-form-urlencoded`:

| Parameter | Grant | Description |
|-----------|-------|-------------|
| `grant_type` | Both | `authorization_code` or `refresh_token` |
| `client_id` | Both | Registered client identifier |
| `client_secret` | Both | Required for confidential clients |
| `code` | Authorisation code | Issued authorisation code |
| `redirect_uri` | Authorisation code | Must match the authorisation request |
| `code_verifier` | Authorisation code | PKCE verifier matching the stored challenge |
| `refresh_token` | Refresh token | Refresh token to exchange |
| `scope` | Refresh token | Optional narrower scope |
| `resource` | Both | Optional resource indicator |

A successful response contains `access_token`, `token_type`, `expires_in`, `scope`, `id_token` (when `openid` was granted), and `refresh_token`.

Errors return the standard `error` and `error_description` body. `invalid_client` uses `401`; every other error uses `400`. The supported error codes are `invalid_client`, `invalid_grant`, `invalid_scope`, `unsupported_grant_type`, `invalid_request`, `invalid_target`, `unsupported_response_type`, and `access_denied`.

Access tokens are signed RS256 JWTs with `typ` set to `at+jwt`, `iss`, `aud`, `sub`, `client_id`, `scope`, `iat`, `nbf`, and `exp`.

## Revocation and Introspection

`POST /revoke` accepts `token`, `token_type_hint`, `client_id`, and `client_secret`. It always returns `200` once the client is authenticated, so unknown tokens cannot be probed.

`POST /introspect` accepts `token` and always returns `200`. Unknown, expired, unusable, or non-access tokens return `{"active": false}`.

## UserInfo

`GET /userinfo` requires `Authorization: Bearer <access token>`. It validates signature, issuer, lifetime, and token type, then returns `sub`, `email`, `email_verified`, `name`, and `preferred_username`. The `email` and `email_verified` claims support ChatGPT Enterprise workspace domain restrictions.