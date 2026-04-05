# Specification: spec: Multi-Source Email Aggregation with Credential Encryption and Linked Microsoft Accounts

## Metadata
- Source issue: #62
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/62
- Author: @ipavlovi
- Created: 2026-04-05T14:58:10Z

## Specification

# Multi-Source Email Aggregation with Credential Encryption and Linked Microsoft Accounts

## Summary

Priority Hub's Outlook Flagged Mail connector currently aggregates flagged emails only from the Microsoft account used to log in. Users with multiple Microsoft accounts (business and personal) or non-Microsoft email providers have no way to aggregate flagged emails from those sources. Additionally, all connector credentials (PATs, API tokens) are stored in plaintext at rest.

This specification defines three independently shippable phases:

1. **Phase 1 — Backend Credential Encryption**: Encrypt all sensitive credential fields at rest using .NET Data Protection API in both file-based and PostgreSQL storage backends.
2. **Phase 2 — IMAP Flagged Mail Connector**: A new connector (`imap-flagged-mail`) that fetches flagged and keyword-tagged messages over TLS from any IMAP server using MailKit.
3. **Phase 3 — Linked Microsoft Accounts**: Allow users to link additional Microsoft accounts and assign them to Outlook and Tasks connector instances.

## Specification

See [`plans/specifications/spec-62-spec-multi-source-email-aggregation-with-credential-encryption.md`](plans/specifications/spec-62-spec-multi-source-email-aggregation-with-credential-encryption.md) for the full specification including requirements, interfaces, data contracts, acceptance criteria, and test strategy.

## Key Requirements

### Phase 1 — Credential Encryption
- `[SensitiveField]` attribute marks credential properties for encryption
- `ICredentialProtector` abstraction with `Protect`/`Unprotect` methods
- Seamless plaintext-to-encrypted migration on first load+save cycle
- Applies to both `LocalConfigStore` and `PostgresConfigStore`

### Phase 2 — IMAP Flagged Mail Connector
- Implicit TLS only (port 993); plaintext IMAP rejected
- Supports `\Flagged` system flag and custom IMAP keywords
- Configurable folder path and max results
- Uses MailKit for IMAP operations

### Phase 3 — Linked Microsoft Accounts
- Global linked accounts section in Settings UI
- Per-connection account selection for Outlook and Tasks connectors
- Stored refresh tokens encrypted at rest (Phase 1 dependency)
- OAuth flow with `prompt=consent` for account picker

## Acceptance Criteria

See specification sections 5 and 10 for detailed acceptance criteria per phase.

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
