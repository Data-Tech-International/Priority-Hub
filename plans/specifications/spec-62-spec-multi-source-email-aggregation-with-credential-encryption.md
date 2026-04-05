---
title: "Multi-Source Email Aggregation with Credential Encryption and Linked Microsoft Accounts"
version: 1.0
date_created: 2026-04-01
owner: "@ipavlovi"
tags:
  - design
  - infrastructure
  - process
  - app
---

# Introduction

Priority Hub's Outlook Flagged Mail connector currently aggregates flagged emails only from the Microsoft account used to log in. Users with multiple Microsoft accounts (business and personal) or non-Microsoft email providers have no way to aggregate flagged emails from those sources. Additionally, all connector credentials (PATs, API tokens) are stored in plaintext at rest.

This specification defines three independently shippable phases: (1) backend credential encryption for all connector secrets, (2) a generic IMAP flagged-mail connector for any IMAP-capable email provider, and (3) a global linked Microsoft accounts system with per-connection account selection for Outlook and Tasks connectors.

## 1. Purpose & Scope

**Purpose**: Enable users to aggregate flagged emails from multiple email sources — their primary Microsoft account, additional linked Microsoft accounts, and any IMAP-capable email server — while ensuring all stored credentials are encrypted at rest.

**Scope**:
- Phase 1: Encrypt all sensitive credential fields at rest in both file-based (`LocalConfigStore`) and PostgreSQL (`PostgresConfigStore`) storage backends.
- Phase 2: Implement a new IMAP Flagged Mail connector that fetches flagged and keyword-tagged messages over TLS from any IMAP server.
- Phase 3: Allow users to link additional Microsoft accounts and assign them to Outlook Flagged Mail and Microsoft Tasks connector instances.

**Intended audience**: Implementation agents, code reviewers, QA agents.

**Assumptions**:
- The user is authenticated via Microsoft or GitHub OAuth (existing login system).
- The existing `IConnector` interface and `ConnectorRegistry` pattern remain unchanged.
- The .NET 10 runtime and Blazor Server architecture are preserved.

**Out of scope**:
- STARTTLS (port 143) support — TLS-only (port 993) is enforced.
- OAuth2 XOAUTH2 for IMAP providers (Gmail, Yahoo) — app passwords are the supported mechanism.
- Sending, replying to, or composing emails.
- Background sync, delta caching, or push notifications.
- Google-specific or Yahoo-specific OAuth flows for email.

## 2. Definitions

| Term | Definition |
|------|-----------|
| **IMAP** | Internet Message Access Protocol — standard protocol for reading email from a mail server. |
| **TLS** | Transport Layer Security — cryptographic protocol providing encrypted communication. |
| **Implicit TLS** | TLS connection established immediately on connect (port 993 for IMAP), as opposed to STARTTLS upgrade. |
| **`\Flagged`** | IMAP system flag indicating a message is flagged/starred by the user. |
| **IMAP keyword** | User-defined or server-defined label applied to messages (e.g., Gmail's `\\Starred`, custom labels). |
| **App password** | A provider-generated password allowing IMAP access when 2FA is enabled (used by Gmail, Outlook.com, Yahoo). |
| **Data Protection API** | .NET `Microsoft.AspNetCore.DataProtection` framework providing symmetric encryption with automatic key management. |
| **Key ring** | The set of encryption keys managed by .NET Data Protection, stored in a configurable directory. |
| **Purpose string** | A Data Protection concept that scopes a protector to a specific usage context, preventing cross-purpose decryption. |
| **Linked account** | A secondary Microsoft account connected to the user's Priority Hub profile for accessing that account's Graph API resources. |
| **Primary account** | The Microsoft or GitHub account used to log in to Priority Hub. |
| **Refresh token** | An OAuth2 token used to obtain new access tokens without user re-authentication. |
| **`IConnector`** | The connector interface (`ProviderKey`, `ConfigFields`, `FetchConnectionAsync`) all Priority Hub connectors implement. |
| **`ConnectorRegistry`** | The DI-registered collection of all available `IConnector` implementations. |
| **`ProviderConfiguration`** | The root config model storing all connector connection lists and user preferences per user. |
| **MailKit** | Open-source .NET library for IMAP/SMTP operations — the de facto standard for .NET email protocols. |
| **BFF** | Backend-for-Frontend — the architectural pattern where UI and backend run in the same host process. |

## 3. Requirements, Constraints & Guidelines

### Phase 1 — Backend Credential Encryption

- **REQ-ENC-001**: All sensitive connector credential fields must be encrypted before being written to storage (file or database).
- **REQ-ENC-002**: Encrypted fields must be decrypted transparently after loading from storage, so connectors receive plaintext values at runtime.
- **REQ-ENC-003**: Existing plaintext configuration files must auto-migrate to encrypted form on the first load+save cycle without data loss or user action.
- **REQ-ENC-004**: If decryption fails for a field value (e.g., the value is still plaintext or the key ring has rotated), the system must treat the value as plaintext, operate normally, and re-encrypt on the next save.
- **REQ-ENC-005**: A `[SensitiveField]` attribute must mark each credential property that requires encryption.
- **REQ-ENC-006**: The encryption mechanism must use .NET Data Protection API with a configurable file-system key ring directory (default: `config/keys/`).
- **REQ-ENC-007**: The Data Protection purpose string must be `"PriorityHub.Credentials.v1"`.
- **SEC-ENC-001**: The `config/keys/` directory must be excluded from version control via `.gitignore`.
- **SEC-ENC-002**: Encryption keys must not be logged or exposed through any API endpoint.
- **SEC-ENC-003**: Loss of the key ring renders all encrypted credentials unreadable. Documentation must warn users to back up the `config/keys/` directory.
- **CON-ENC-001**: Both `LocalConfigStore` and `PostgresConfigStore` must apply the same encryption/decryption layer.
- **CON-ENC-002**: The encryption layer must not change the `IConfigStore` interface signature.
- **GUD-ENC-001**: Use reflection on `[SensitiveField]` via a shared `ConfigEncryptionHelper` to avoid per-model encryption logic.
- **GUD-ENC-002**: Create an `ICredentialProtector` abstraction (`Protect`/`Unprotect` methods) to enable testing with a no-op implementation.

**Fields requiring `[SensitiveField]`**:

| Connection Type | Field |
|----------------|-------|
| `AzureDevOpsConnection` | `PersonalAccessToken` |
| `JiraConnection` | `ApiToken` |
| `GitHubConnection` | `PersonalAccessToken` |
| `TrelloConnection` | `ApiKey` |
| `TrelloConnection` | `Token` |
| `ImapFlaggedMailConnection` (Phase 2) | `Password` |
| `LinkedMicrosoftAccount` (Phase 3) | `RefreshToken` |

### Phase 2 — IMAP Flagged Mail Connector

- **REQ-IMAP-001**: A new connector with `ProviderKey = "imap-flagged-mail"` must be implemented following the existing `IConnector` pattern.
- **REQ-IMAP-002**: The connector must connect to IMAP servers using implicit TLS on port 993 with TLS 1.2 or TLS 1.3.
- **REQ-IMAP-003**: The connector must search for messages with the `\Flagged` IMAP system flag.
- **REQ-IMAP-004**: The connector must support optional user-configured IMAP keywords (comma-separated) to search in addition to `\Flagged`.
- **REQ-IMAP-005**: The connector must support configurable folder path (default: `INBOX`).
- **REQ-IMAP-006**: The connector must support configurable max results (default: 100).
- **REQ-IMAP-007**: Each fetched email must be mapped to a `WorkItem` with: Subject → `Title`, From address → `Assignee`, Date → `AgeDays`, flags/keywords → `Tags`.
- **REQ-IMAP-008**: The connector must use per-connection credentials (`Email`, `Password`) — no OAuth token routing from the login session.
- **REQ-IMAP-009**: The connector must register in both `PriorityHub.Api/Program.cs` and `PriorityHub.Ui/Program.cs` and appear in the `ConnectorRegistry`.
- **REQ-IMAP-010**: An example IMAP connection must be added to `providers.example.json`.
- **SEC-IMAP-001**: The connector must reject connections that do not use implicit TLS — plaintext IMAP (port 143 without TLS) is not permitted.
- **SEC-IMAP-002**: The `Password` field must be marked with `[SensitiveField]` for at-rest encryption (Phase 1 dependency).
- **SEC-IMAP-003**: IMAP credentials must not appear in application logs.
- **CON-IMAP-001**: The connector must use the MailKit library (`MailKit` NuGet package) for IMAP operations.
- **CON-IMAP-002**: The connector must not require changes to `DashboardAggregator`, `OauthTokenService`, or the `IConnector` interface.
- **GUD-IMAP-001**: Documentation must include server addresses and port numbers for common email providers: Gmail (`imap.gmail.com:993`), Outlook.com (`outlook.office365.com:993`), Yahoo (`imap.mail.yahoo.com:993`).
- **GUD-IMAP-002**: Documentation must note that app passwords are required for providers with 2FA enabled.

### Phase 3 — Linked Microsoft Accounts

- **REQ-LINK-001**: Users must be able to link additional Microsoft accounts from the Settings page without changing their login session.
- **REQ-LINK-002**: A "Linked Accounts" section in Settings must display all linked Microsoft accounts with display name, email, and linked date.
- **REQ-LINK-003**: The primary (login) Microsoft account must always appear in the list as "Primary (login)" and must not be removable.
- **REQ-LINK-004**: Users must be able to remove any non-primary linked account.
- **REQ-LINK-005**: A new `/api/auth/link/microsoft` endpoint must initiate an OAuth flow that forces the Microsoft account picker (`prompt=consent`).
- **REQ-LINK-006**: On successful OAuth callback, the endpoint must store the linked account's refresh token (encrypted) and metadata (display name, email) in the user's `ProviderConfiguration` — without modifying the login session.
- **REQ-LINK-007**: `OutlookFlaggedMailConnection` and `MicrosoftTasksConnection` must gain an optional `LinkedAccountId` field.
- **REQ-LINK-008**: When `LinkedAccountId` is set on a connection, `OauthTokenService` must exchange the linked account's stored refresh token for an access token instead of using the session token.
- **REQ-LINK-009**: When `LinkedAccountId` is null or empty, the connection must continue using the primary (login) session token (no behavior change).
- **REQ-LINK-010**: In the Settings UI, Outlook and Tasks connection forms must include a "Microsoft Account" dropdown listing "Primary (login)" plus all linked accounts.
- **REQ-LINK-011**: If a linked account's refresh token exchange fails, the connection must show `SyncStatus = "needs-reauth"` with a `ProviderIssue` message prompting the user to re-link.
- **SEC-LINK-001**: The `LinkedMicrosoftAccount.RefreshToken` field must be marked with `[SensitiveField]` for at-rest encryption.
- **SEC-LINK-002**: The linking OAuth flow must use the same `ClientId`/`ClientSecret` as the login flow but must not overwrite the login session cookies.
- **SEC-LINK-003**: Removing a linked account must permanently delete its stored refresh token from the config.
- **CON-LINK-001**: The `DashboardAggregator.BuildPendingFetches()` method signature must change to support per-connection token resolution (currently resolves tokens per-provider-key only).
- **CON-LINK-002**: The `OauthTokenService.GetTokensByProviderAsync()` method must accept `ProviderConfiguration` (or linked accounts list) in addition to `HttpContext` to resolve per-connection tokens from stored refresh tokens.

## 4. Interfaces & Data Contracts

### Phase 1 — Credential Protection Abstractions

```csharp
/// <summary>Marks a string property as containing sensitive data requiring at-rest encryption.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveFieldAttribute : Attribute { }

/// <summary>Encrypts and decrypts sensitive credential strings.</summary>
public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}
```

### Phase 2 — IMAP Config Model

```csharp
public sealed class ImapFlaggedMailConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📨";
    public bool Enabled { get; set; } = true;
    public string ImapServer { get; set; } = string.Empty;       // e.g., "imap.gmail.com"
    public string Port { get; set; } = "993";
    public string Email { get; set; } = string.Empty;
    [SensitiveField]
    public string Password { get; set; } = string.Empty;         // app password or account password
    public string FolderPath { get; set; } = "INBOX";
    public string Keywords { get; set; } = string.Empty;         // comma-separated custom IMAP keywords
    public string MaxResults { get; set; } = "100";
}
```

**IMAP Connector Field Spec**:

| Key | Label | InputKind | Required | Default |
|-----|-------|-----------|----------|---------|
| `name` | Connection name | `text` | yes | — |
| `imapServer` | IMAP server | `text` | yes | — |
| `port` | Port | `text` | no | `993` |
| `email` | Email address | `text` | yes | — |
| `password` | Password / App password | `password` | yes | — |
| `folderPath` | Folder path | `text` | no | `INBOX` |
| `keywords` | Custom IMAP keywords (comma-separated) | `text` | no | — |
| `maxResults` | Max results | `text` | no | `100` |

**IMAP WorkItem Mapping**:

| Source Field | WorkItem Field | Notes |
|-------------|---------------|-------|
| Subject | `Title` | Falls back to "Untitled email" |
| From address | `Assignee` | Formatted as display name or email |
| Message date | `AgeDays` | Days since received |
| Message-ID + connection ID | `Id` | `IMAP-{hash}` to ensure uniqueness |
| IMAP flags + keywords | `Tags` | Includes `"email"`, `"flagged"`, folder name, matched keywords |
| Web link | `SourceUrl` | Empty string (IMAP has no web URL) |
| — | `Provider` | `"imap-flagged-mail"` |
| — | `BoardId` | Connection `Id` |
| — | `Status` | `"to-do"` (flagged implies action needed) |
| — | `Effort` | `2` (default for email items) |
| — | `Impact` | `5` (default; no importance metadata in IMAP flags) |
| — | `Urgency` | `5` (default) |
| — | `Confidence` | `6` |

### Phase 3 — Linked Account Models

```csharp
public sealed class LinkedMicrosoftAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [SensitiveField]
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Extended connection models** (additions only):

```csharp
// Added to OutlookFlaggedMailConnection
public string LinkedAccountId { get; set; } = string.Empty;

// Added to MicrosoftTasksConnection
public string LinkedAccountId { get; set; } = string.Empty;
```

**Extended ProviderConfiguration** (additions only):

```csharp
public List<ImapFlaggedMailConnection> ImapFlaggedMail { get; set; } = [];
public List<LinkedMicrosoftAccount> LinkedMicrosoftAccounts { get; set; } = [];
```

**Linking endpoint**:

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/auth/link/microsoft` | Initiates Microsoft OAuth flow with `prompt=consent` for account linking. Redirects to Microsoft login. |
| `GET` | `/api/auth/link/microsoft/callback` | OAuth callback that stores refresh token in user config and redirects back to Settings page. |
| `DELETE` | `/api/auth/link/microsoft/{accountId}` | Removes a linked account and its stored refresh token. |

### Example `providers.example.json` Entries

```json
{
  "imapFlaggedMail": [
    {
      "id": "imap-gmail",
      "name": "Personal Gmail",
      "emoji": "📨",
      "enabled": true,
      "imapServer": "imap.gmail.com",
      "port": "993",
      "email": "your-email@gmail.com",
      "password": "<app-password>",
      "folderPath": "INBOX",
      "keywords": "",
      "maxResults": "100"
    },
    {
      "id": "imap-outlook",
      "name": "Personal Outlook",
      "emoji": "📬",
      "enabled": true,
      "imapServer": "outlook.office365.com",
      "port": "993",
      "email": "your-email@outlook.com",
      "password": "<app-password>",
      "folderPath": "INBOX",
      "keywords": "",
      "maxResults": "50"
    }
  ],
  "linkedMicrosoftAccounts": [
    {
      "id": "linked-secondary",
      "displayName": "Work Account",
      "email": "user@company.com",
      "refreshToken": "<encrypted-refresh-token>",
      "linkedAt": "2026-03-15T10:30:00Z"
    }
  ]
}
```

## 5. Acceptance Criteria

### Phase 1 — Backend Credential Encryption

- **AC-ENC-001**: Given an existing plaintext config file, when the application loads and saves it, then all `[SensitiveField]` values in the saved file are encrypted ciphertext (not the original plaintext).
- **AC-ENC-002**: Given an encrypted config file, when the application loads it, then connectors receive decrypted plaintext credentials and operate normally (dashboard loads data).
- **AC-ENC-003**: Given a config file with mixed plaintext and encrypted values (partial migration), when loaded, then plaintext values are used as-is and re-encrypted on the next save.
- **AC-ENC-004**: Given a corrupted or tampered encrypted value, when loaded, then the system logs a warning, treats the value as empty, and does not crash.
- **AC-ENC-005**: The `config/keys/` directory exists in `.gitignore` and is never committed.
- **AC-ENC-006**: `dotnet build PriorityHub.sln` succeeds; `dotnet test PriorityHub.sln` passes all tests including new encryption tests.

### Phase 2 — IMAP Flagged Mail Connector

- **AC-IMAP-001**: Given an IMAP connection configured with valid server, email, and password, when the dashboard loads, then flagged emails from that IMAP server appear as work items.
- **AC-IMAP-002**: Given an IMAP connection with custom keywords `"important,urgent"`, when the dashboard loads, then messages matching `\Flagged` OR the keyword `important` OR the keyword `urgent` appear as work items.
- **AC-IMAP-003**: Given an IMAP connection with `folderPath = "Work"`, when the dashboard loads, then only messages from the `Work` IMAP folder are searched.
- **AC-IMAP-004**: Given an IMAP connection targeting a server on port 143 without TLS, when the connector attempts to connect, then the connection is rejected and a `ProviderIssue` with a clear error message is returned.
- **AC-IMAP-005**: The IMAP connector appears in the Settings UI under a new section, and users can add, configure, enable/disable, and remove IMAP connections using the standard connection management pattern.
- **AC-IMAP-006**: Given an IMAP connection with invalid credentials, when the dashboard loads, then the connection shows `SyncStatus = "needs-auth"` and a `ProviderIssue` with the error message.
- **AC-IMAP-007**: The `Password` field is stored encrypted at rest (Phase 1 dependency verified).
- **AC-IMAP-008**: IMAP connector unit tests pass, covering: successful fetch, TLS rejection, bad credentials, empty inbox, custom keywords, max results limit.

### Phase 3 — Linked Microsoft Accounts

- **AC-LINK-001**: Given a Microsoft-authenticated user, when they click "Link Microsoft Account" in Settings, then a Microsoft OAuth popup/redirect opens with the account picker, allowing them to sign in to a different Microsoft account.
- **AC-LINK-002**: Given a successfully completed linking OAuth flow, when the callback completes, then the linked account appears in the "Linked Accounts" section with display name, email, and linked date.
- **AC-LINK-003**: Given a linked account, when a user assigns it to an Outlook Flagged Mail connection via the account dropdown, then that connection fetches flagged emails from the linked account (not the primary login account).
- **AC-LINK-004**: Given a linked account with an expired or invalid refresh token, when the dashboard loads a connection using that account, then the connection shows `SyncStatus = "needs-reauth"` with a message prompting re-linking.
- **AC-LINK-005**: Given a user removes a linked account, when the removal is confirmed, then the account disappears from the list, its stored refresh token is deleted, and connections referencing it show `SyncStatus = "needs-reauth"`.
- **AC-LINK-006**: The primary (login) Microsoft account is always listed and cannot be removed.
- **AC-LINK-007**: Connections with no `LinkedAccountId` (or empty) continue using the primary session token with no behavior change (backward compatibility).
- **AC-LINK-008**: `dotnet build PriorityHub.sln` succeeds; `dotnet test PriorityHub.sln` passes all tests including new linked account tests.

## 6. Test Automation Strategy

- **Test Levels**: Unit, Integration.
- **Frameworks**: MSTest, FluentAssertions, Moq.
- **Phase 1 Tests** (`backend/PriorityHub.Api.Tests/`):
  - `CredentialProtectorTests`: Round-trip encrypt/decrypt, empty string handling, null handling.
  - `ConfigEncryptionHelperTests`: Encrypt all `[SensitiveField]` properties across all connection types, plaintext migration (decrypt failure falls back to plaintext), mixed encrypted/plaintext config.
  - `LocalConfigStoreTests` (extend): Verify saved file contains encrypted values, verify loaded config has decrypted values.
- **Phase 2 Tests** (`backend/PriorityHub.Api.Tests/Connectors/`):
  - `ImapFlaggedMailConnectorTests`: Mock MailKit `ImapClient` via interface wrapper or test doubles. Test: successful fetch with flagged messages, empty folder, custom keyword matching, TLS rejection, authentication failure, max results limit, folder path selection.
- **Phase 3 Tests** (`backend/PriorityHub.Api.Tests/`):
  - `OauthTokenServiceTests` (extend): Per-connection token resolution with linked account ID, fallback to primary session token, refresh token exchange failure handling.
  - `LinkedAccountEndpointTests`: Linking flow callback stores account, removal deletes account and token, primary account cannot be removed.
- **CI/CD Integration**: All tests run via `dotnet test PriorityHub.sln` in existing GitHub Actions pipeline.
- **Coverage Requirements**: New code should maintain or improve existing coverage levels; no minimum threshold enforced.
- **Test Data Management**: Use in-memory config stores and mock HTTP clients; no external IMAP server required for automated tests.

## 7. Rationale & Context

### Why encrypt credentials at rest now?

Currently all connector credentials (PATs, API tokens) are stored as plaintext JSON in `config/users/*.json` files and in the PostgreSQL `user_config.config` JSONB column. This was acceptable when Priority Hub was a single-user local tool, but the addition of IMAP passwords (Phase 2) and stored refresh tokens (Phase 3) raises the bar. Encrypting before adding new credential types prevents a security gap from widening.

### Why .NET Data Protection API?

- Built into ASP.NET Core — no external dependencies.
- Handles key generation, rotation, and revocation automatically.
- File-system key ring is portable across OS (Windows, Linux, macOS) — important for Docker deployments.
- Purpose strings prevent cross-context decryption (e.g., password protection keys cannot decrypt cookie tokens).

### Why MailKit for IMAP?

- De facto standard for .NET IMAP/SMTP — actively maintained, well-documented.
- Supports implicit TLS, STARTTLS, SASL authentication, IDLE, and custom streams for testing.
- Handles IMAP quirks across providers (Gmail extensions, Exchange IMAP, etc.).
- MIT licensed — compatible with Priority Hub's open-source model.

### Why TLS-only?

STARTTLS on port 143 is vulnerable to downgrade attacks where a man-in-the-middle strips the STARTTLS capability. Implicit TLS on port 993 establishes encryption before any data is exchanged. All major email providers support port 993. Simplifying to TLS-only reduces the connector's attack surface.

### Why global linked accounts instead of per-connection OAuth?

Per-connection OAuth flows would require each Outlook/Tasks connection to independently manage its own OAuth popup and token storage. A global "Linked Accounts" model is simpler because: (a) a single linked account can be reused across multiple connections, (b) the token lifecycle (refresh, re-auth) is managed in one place, and (c) the UX is clearer — users first link accounts, then assign them.

### Why phased delivery?

Each phase builds on the previous but delivers independent user value:
- Phase 1 (encryption) is a security improvement that benefits all existing connectors.
- Phase 2 (IMAP) enables non-Microsoft email aggregation.
- Phase 3 (linked accounts) resolves the multi-Microsoft-account problem.

Phasing reduces risk and allows earlier feedback on each capability.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: IMAP mail servers — The IMAP connector communicates with user-specified IMAP servers over TLS port 993. No SLA guarantees; connector handles timeouts and errors gracefully.
- **EXT-002**: Microsoft Identity Platform — The linked accounts feature uses `login.microsoftonline.com` for OAuth2 flows, consistent with existing Microsoft login.
- **EXT-003**: Microsoft Graph API — Existing Outlook and Tasks connectors use Graph to fetch data. Linked accounts extend this to additional Microsoft identities.

### Third-Party Services
- **SVC-001**: No additional third-party services beyond those already used (Microsoft Identity, Microsoft Graph, GitHub OAuth).

### Infrastructure Dependencies
- **INF-001**: File system access — Credential encryption requires a writable directory for the Data Protection key ring (`config/keys/`). Docker deployments must mount this as a persistent volume.
- **INF-002**: Config store — Both `LocalConfigStore` (file) and `PostgresConfigStore` (database) must support the encryption layer.

### Data Dependencies
- **DAT-001**: Existing `config/users/*.json` files — Phase 1 must handle plaintext-to-encrypted migration without data loss.
- **DAT-002**: Existing PostgreSQL `user_config` table — Same migration behavior for JSONB credential values.

### Technology Platform Dependencies
- **PLT-001**: .NET 10 SDK — Required for `Microsoft.AspNetCore.DataProtection` and the existing Blazor Server runtime.
- **PLT-002**: MailKit NuGet package — Required for Phase 2 IMAP operations. Must be a stable release compatible with .NET 10.

### Compliance Dependencies
- **COM-001**: No specific regulatory compliance requirements beyond the existing security posture. Credential encryption (Phase 1) aligns with OWASP guidance on protecting secrets at rest.

## 9. Examples & Edge Cases

### Phase 1 — Encryption Migration

```
Scenario: First load of existing plaintext config
──────────────────────────────────────────────────
1. User has config/users/user.json with:
   { "jira": [{ "apiToken": "my-secret-token" }] }

2. App loads → ConfigEncryptionHelper.Decrypt() runs on "my-secret-token"
   → DataProtection.Unprotect() throws CryptographicException
   → Helper treats "my-secret-token" as plaintext, logs info message

3. Connector receives "my-secret-token" → works normally

4. On next Save, ConfigEncryptionHelper.Encrypt() runs
   → "my-secret-token" → "CfDJ8N..." (base64 ciphertext)
   → File now contains: { "jira": [{ "apiToken": "CfDJ8N..." }] }

5. Subsequent loads → Unprotect("CfDJ8N...") → "my-secret-token" ✓
```

### Phase 2 — IMAP Edge Cases

```
Edge case: IMAP server rejects credentials
──────────────────────────────────────────
→ MailKit throws AuthenticationException
→ Connector catches, sets SyncStatus = "needs-auth"
→ ProviderIssue: "IMAP authentication failed. Check email and password."

Edge case: IMAP folder does not exist
─────────────────────────────────────
→ MailKit throws FolderNotFoundException
→ Connector catches, sets SyncStatus = "error"
→ ProviderIssue: "IMAP folder 'Work' not found on server."

Edge case: No flagged messages
──────────────────────────────
→ Search returns empty set
→ ConnectorResult has 0 WorkItems, BoardConnection shows SyncStatus = "connected"

Edge case: IMAP server on port 143 without TLS
───────────────────────────────────────────────
→ Connector validates port + TLS requirement before connecting
→ Throws InvalidOperationException: "IMAP connections require TLS (port 993)."
→ ProviderIssue returned; connection never established.

Edge case: Custom keywords with flagged
───────────────────────────────────────
→ Keywords = "important,followup"
→ IMAP search: OR(Flagged, Keyword("important"), Keyword("followup"))
→ Messages matching ANY condition are returned
→ Tags include matched keywords
```

### Phase 3 — Linked Account Edge Cases

```
Edge case: Linked account refresh token expired (90 days inactive)
─────────────────────────────────────────────────────────────────
→ OauthTokenService exchanges refresh token → Microsoft returns error
→ Connection shows SyncStatus = "needs-reauth"
→ ProviderIssue: "Linked account 'user@company.com' needs re-authorization."
→ User re-links from Settings

Edge case: Linked account removed while connections reference it
───────────────────────────────────────────────────────────────
→ User removes linked account from Settings
→ Refresh token deleted from config
→ Connections still have LinkedAccountId pointing to removed account
→ On next dashboard load: token resolution finds no matching account
→ SyncStatus = "needs-reauth" with message: "Linked account not found."

Edge case: GitHub-authenticated user tries to link Microsoft account
───────────────────────────────────────────────────────────────────
→ Linking flow works independently of login provider
→ Linked account refresh token is stored in user config
→ OauthTokenService exchanges linked account's refresh token directly
→ GitHub login user can access Microsoft connectors via linked accounts
```

## 10. Validation Criteria

1. `dotnet build PriorityHub.sln` succeeds with zero errors and zero warnings for each phase.
2. `dotnet test PriorityHub.sln` passes all existing and new tests for each phase.
3. **Phase 1**: After a save cycle, `config/users/*.json` files contain encrypted ciphertext for all `[SensitiveField]` values. Re-loading and running the dashboard produces the same results as before encryption.
4. **Phase 2**: An IMAP connection configured with `imap.gmail.com:993` and valid app password fetches flagged emails that appear as work items on the dashboard. A connection targeting port 143 is rejected.
5. **Phase 3**: A linked Microsoft account's Outlook flagged mail appears on the dashboard through a connection assigned to that linked account. Removing the linked account causes the connection to show "needs-reauth".
6. No secrets (passwords, tokens, refresh tokens, encryption keys) appear in application logs at any log level.
7. Existing connectors (Azure DevOps, GitHub, Jira, Trello, Microsoft Tasks, Outlook Flagged Mail) continue working without configuration changes (backward compatibility).

## 11. Related Specifications / Further Reading

- [spec-23: Secure browser passphrase cache](spec-23-spec-secure-browser-passphrase-cache-using-webcrypto-aes-gcm-wra.md) — Browser-side passphrase caching (complementary, does not overlap with backend credential encryption)
- [Microsoft Data Protection API documentation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction)
- [MailKit documentation](https://github.com/jstedfast/MailKit)
- [IMAP RFC 9051 (IMAP4rev2)](https://www.rfc-editor.org/rfc/rfc9051)
- [Gmail IMAP settings and app passwords](https://support.google.com/mail/answer/7126229)
- [Outlook.com IMAP settings](https://support.microsoft.com/en-us/office/pop-imap-and-smtp-settings-8361e398-8af4-4e97-b147-6c6c4ac95353)
- [Microsoft identity platform refresh tokens](https://learn.microsoft.com/en-us/entra/identity-platform/refresh-tokens)
- [OWASP: Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
