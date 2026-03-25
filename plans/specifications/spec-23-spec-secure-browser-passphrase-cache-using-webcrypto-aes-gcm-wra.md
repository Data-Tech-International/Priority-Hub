# Specification: Spec: Secure browser passphrase cache using WebCrypto AES-GCM wrapping

## Metadata
- Source issue: #23
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/23
- Author: @ipavlovi
- Created: 2026-03-25T14:28:33Z

## Specification

Problem
The system needs better usability for encrypted credential unlock while preserving security guarantees that server administrators cannot read user secrets. Users should avoid frequent passphrase prompts on trusted devices, without introducing plaintext secret storage.

Goal
Provide optional trusted-device remember mode using WebCrypto AES-GCM wrapping, with IndexedDB persistence and strict expiration, while maintaining zero-admin-access credential-at-rest protections.

Requirements

1. Browser remember mode is optional.
2. Cache duration is 90 days absolute TTL.
3. Persistent cache is blocked on shared/public devices.
4. Browser storage must not contain raw passphrase or plaintext provider credentials.
5. Server storage and logs must not contain raw passphrase or plaintext provider credentials.
6. User can clear remembered unlock state at any time.
7. If passphrase is forgotten, re-entry of provider credentials is required, with no admin bypass.

Technical profile

1. Crypto: WebCrypto AES-GCM wrapping.
2. Storage: IndexedDB only for persistent unlock material.
3. Key handling: non-extractable key material when available.
4. Integrity: unwrap failure or tamper detection triggers secure local wipe and re-prompt.

Functional flow

1. User unlocks with passphrase.
2. User optionally enables remember mode on trusted device.
3. App stores wrapped unlock material plus metadata in IndexedDB.
4. On subsequent use, app checks TTL and integrity before restoring unlock state.
5. After TTL expiry or integrity failure, app prompts for passphrase again.
6. In shared/public mode, app bypasses persistent storage and uses session-only unlock.

Acceptance criteria

1. Unlock persists across browser restart on trusted device within 90 days.
2. Unlock is rejected after 90 days and requires passphrase.
3. Shared/public mode prevents persistent remember behavior.
4. Browser data inspection confirms wrapped-only storage.
5. Server inspection confirms no plaintext passphrase or PAT in storage/logs.
6. Clear remembered passphrase action immediately removes local remembered unlock material.

Verification checklist

1. Trusted flow test before expiry.
2. Expiry boundary test after 90 days.
3. Shared-device path test.
4. Browser storage inspection test.
5. Server log redaction and storage safety test.
6. Clear action invalidation test.

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
