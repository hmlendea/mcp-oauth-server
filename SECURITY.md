# Security Policy

This policy defines responsible vulnerability reporting for MCP OAuth Server and applies to the latest supported release distributed through GitHub Releases.

## 📑 Table of Contents

- [Table of Contents](#table-of-contents)
- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)
- [Automated Security Scanning](#automated-security-scanning)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/mcp-oauth-server/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

The subsequent report categories are in scope for this repository:
- Authentication or authorisation defects affecting token issuance, validation, introspection, or revocation
- Weaknesses in client registration, redirect-URI validation, PKCE verification, or scope enforcement
- Exposure of signing-key material, password hashes, client secrets, authorisation codes, or refresh tokens
- Vulnerabilities in request handling, JSON persistence, or HTML rendering of the consent page

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in unsupported preceding releases or unofficial distributions
- Deployment security issues external to this repository, including reverse-proxy TLS configuration, host hardening, and secret-provider configuration
- Weaknesses in a configured ChatGPT or other MCP client or resource server

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🔍 Automated Security Scanning

This repository runs automated security scans on every push and pull request to `master`, plus a weekly schedule. The scan includes:
- **Dependency vulnerability scanning** Checks all NuGet packages for known vulnerabilities using `dotnet list package --vulnerable --include-transitive`
- **CodeQL static analysis** Performs semantic code analysis for C# to detect security flaws and quality issues

Results are published to the [Security](https://github.com/hmlendea/mcp-oauth-server/security) tab and the [Actions](https://github.com/hmlendea/mcp-oauth-server/actions/workflows/security.yml) workflow page.