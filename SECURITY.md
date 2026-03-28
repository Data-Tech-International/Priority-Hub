# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.2.x   | ✅ Yes     |
| < 0.2   | ❌ No      |

## Scope

The following are **in scope** for responsible disclosure:

- Authentication and authorization vulnerabilities in the Blazor UI or ASP.NET Core backend.
- Credential leakage (e.g., provider PATs or API tokens exposed in responses, logs, or markup).
- Remote code execution or server-side request forgery via connector configuration.
- Injection vulnerabilities (SQL, command, SSRF) in connector query fields.
- Dependency vulnerabilities with a credible exploitation path in this application.

The following are **out of scope**:

- Vulnerabilities in provider systems (Azure DevOps, Jira, Trello, GitHub) — report those to the respective vendor.
- Social engineering attacks against maintainers.
- Denial-of-service attacks that require large-scale network resources.
- Issues only reproducible on unsupported versions.

## Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Report vulnerabilities privately using one of the following methods:

1. **GitHub Security Advisory (preferred):** Open a private advisory at
   <https://github.com/Data-Tech-International/Priority-Hub/security/advisories/new>.
   GitHub keeps the report confidential until a fix is released.

2. **Email:** If you cannot use GitHub Security Advisories, email the maintainers
   via the contact details on the [organization profile](https://github.com/Data-Tech-International).

### What to Include

Please include as much detail as possible:

- Description of the vulnerability and its potential impact.
- Affected version(s) and component(s).
- Step-by-step reproduction instructions.
- Proof-of-concept code or screenshots (if available).
- Suggested mitigation or fix (if known).

## Response Timeline

| Step | Target timeframe |
|------|-----------------|
| Acknowledgement of report | Within 3 business days |
| Initial triage and severity assessment | Within 7 business days |
| Fix or mitigation available | Within 30 business days (critical), 60 business days (high/medium) |
| Public disclosure | After fix is released and reporter is notified |

We follow coordinated disclosure: we will work with you to determine the appropriate
disclosure timeline and will credit you in the release notes unless you prefer to
remain anonymous.

## Security Best Practices for Deployers

- Keep `config/providers.local.json` gitignored and never committed.
- Rotate provider PATs and API tokens regularly.
- Run the application behind a network perimeter; do not expose it to the public internet without authentication.
- Monitor the [GitHub Security Advisories](https://github.com/Data-Tech-International/Priority-Hub/security/advisories) page for notifications.
