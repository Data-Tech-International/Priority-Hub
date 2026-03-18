# Plan: User-Scoped Connector Settings

**Status**: COMPLETED

Migrate connector configuration from a single shared `config/providers.local.json` to per-user JSON files keyed by email address. Each authenticated user gets their own config file at `config/users/{sanitized-email}.json`. The dashboard aggregator and all config endpoints become user-aware by extracting the email claim from the authenticated `ClaimsPrincipal`.

---

## Decisions

- **Storage**: File-per-user under `config/users/`
- **Identity key**: Email address (shared across Microsoft and GitHub logins for the same person)
- **Migration**: Ignore existing global config — every user starts fresh (empty config)
- **Global file**: `config/providers.local.json` is no longer used at runtime; `providers.example.json` stays as documentation
- **Concurrency**: Same file-based approach as today; single-user tool so no locking needed
- **Sanitization**: Email converted to a filesystem-safe filename by replacing `@` and `.` with underscores, lowercased

---

## Steps

### Phase 1 — Backend: Make LocalConfigStore user-aware

**Step 1: Add userId parameter to LocalConfigStore** *(blocks all other steps)*
- `LocalConfigStore.cs`: Change `ConfigPath` from a single static path to a method `GetConfigPath(string userId)`
  - Path: `config/users/{sanitizedEmail}.json`
  - Sanitize email: lowercase, replace `@` → `_at_`, remove/replace characters unsafe for filenames
- Update `LoadAsync(string userId, CancellationToken)` — takes userId, uses `GetConfigPath(userId)`
- Update `SaveAsync(string userId, ProviderConfiguration, CancellationToken)` — same
- Remove old `ConfigPath` property

**Step 2: Update config API endpoints in Program.cs** *(depends on Step 1)*
- `GET /api/config`: Extract user email from `ClaimsPrincipal`, pass to `configStore.LoadAsync(email, ct)`
- `PUT /api/config`: Same — pass email to `configStore.SaveAsync(email, config, ct)`
- `PUT /api/preferences/order`: Same — load with email, save with email
- Helper: add `GetUserEmail(ClaimsPrincipal)` that reads `ClaimTypes.Email` and falls back to sub+provider if email is missing

**Step 3: Update DashboardAggregator to accept userId** *(depends on Step 1)*
- `DashboardAggregator.StreamAsync` and `BuildAsync`: add `string userId` parameter
- Pass `userId` to `configStore.LoadAsync(userId, ct)` instead of parameterless load
- Dashboard endpoints (`/api/dashboard`, `/api/dashboard/stream`): extract email from ClaimsPrincipal and pass to aggregator

**Step 4: Gitignore the user config directory**
- `.gitignore`: add `config/users/`

### Phase 2 — Frontend: No structural changes needed

The frontend already calls the same API endpoints (`GET /api/config`, `PUT /api/config`, etc.) with `credentials: 'include'`. The cookie carries user identity, so the backend resolves the correct file. Settings page, dashboard page, and export all work unchanged.

---

## Relevant Files Modified

- `backend/PriorityHub.Api/Services/LocalConfigStore.cs` — added `userId` parameter, changed path logic
- `backend/PriorityHub.Api/Program.cs` — extract email from `ClaimsPrincipal` in all config/dashboard endpoints, pass to store/aggregator
- `backend/PriorityHub.Api/Services/DashboardAggregator.cs` — added `userId` parameter to `BuildAsync`/`StreamAsync`
- `.gitignore` — added `config/users/`

## Unchanged Files

- `src/pages/SettingsPage.jsx`
- `src/pages/DashboardPage.jsx`
- `src/lib/api.js`
- `backend/PriorityHub.Api/Models/ConfigModels.cs`

---

## Verification

1. `npm.cmd run build` succeeds
2. Sign in as User A → go to Settings → add a connector → save → verify `config/users/usera_at_example_com.json` exists with that connector
3. Sign in as User B (different email) → Settings shows empty config (no connectors)
4. User A's dashboard streams only User A's connectors
5. User B's dashboard is empty (no connectors configured)
6. Sign in via Microsoft → configure connectors → sign out → sign in via GitHub with same email → same config is loaded
7. `config/providers.local.json` is not read or written by any endpoint

---

## Scope

- **In**: Per-user config file storage, user-aware API endpoints, user-aware dashboard aggregation
- **Out**: Database migration, admin view of all users' configs, config sharing between users
