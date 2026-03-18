# Plan: Add Microsoft Tasks and Outlook Flagged Mail Connectors

**Status**: COMPLETED

Add two separate Microsoft-backed connectors to the existing connector framework: one for Microsoft To Do tasks and one for Outlook flagged email across the mailbox. Reuse the current Microsoft login, extend its delegated Graph scopes, implement both connectors against Microsoft Graph, and make the frontend recognize the two new provider keys with minimal targeted UI changes.

---

## Decisions

- Provider keys: `microsoft-tasks` and `outlook-flagged-mail` — keep semantics explicit and avoid colliding with the existing `microsoft` auth provider label.
- Auth model: delegated Microsoft Graph only; no PAT-style fallback for these connectors.
- Reuse the existing Microsoft OAuth session rather than adding a second auth provider.
- Scope additions: `Tasks.Read` for To Do and `Mail.Read` for flagged mail, preserving existing Graph identity scopes and Azure DevOps scope.
- Excluded scope: Planner tasks, calendar tasks, Outlook send/reply workflows, background sync/delta caching, dynamic provider registry refactor.

---

## Steps

1. **Phase 1 — Finalize provider shape and auth reuse**: use provider keys `microsoft-tasks` and `outlook-flagged-mail`; keep them as separate connectors in Settings and Dashboard; continue using the existing Microsoft OAuth session.
2. **Phase 2 — Extend Microsoft OAuth scopes** in `Program.cs`: add `Tasks.Read` and `Mail.Read` delegated Graph permissions while preserving current scopes.
3. **Phase 3 — Add backend config models** in `ConfigModels.cs`: add `MicrosoftTasksConnection` (with `TaskListName`) and `OutlookFlaggedMailConnection` (with `FolderId`, `MaxResults`). Update `ProviderConfiguration` and `GetConnections()` for both new provider keys.
4. **Phase 4 — Implement MicrosoftTasksConnector**: calls Graph To Do endpoints using the signed-in Microsoft token. Enumerates `/me/todo/lists` when no list is configured, fetches `/me/todo/lists/{listId}/tasks`, maps each task to `WorkItem`. Maps status from task state, uses due date and importance to derive urgency/impact.
5. **Phase 5 — Implement OutlookFlaggedMailConnector**: reuses the same Microsoft OAuth token. Queries `/me/messages` or `/me/mailFolders/{folderId}/messages` with a filter for flagged messages. Maps each message to `WorkItem`; uses sender, received date, importance, and flag state to derive `Assignee`, `AgeDays`, `Urgency`, and `Impact`.
6. **Phase 6 — Register and route both connectors** in `Program.cs`: `AddHttpClient<...>()` registrations, include both in `ConnectorRegistry`, extend `GetOauthTokensByProvider()` so a Microsoft login provides the same access token to `azure-devops`, `microsoft-tasks`, and `outlook-flagged-mail`.
7. **Phase 7 — Update SettingsPage** in `SettingsPage.jsx`: extend `emptyConfig` and `normalizeConfig` so both new connector arrays round-trip cleanly.
8. **Phase 8 — Add provider display and help content** in `priorities.js`, `DashboardPage.jsx`, and `helpContent.js`: add provider labels, dashboard filter options, and setup/help text.
9. **Phase 9 — Harden auth and failure behavior**: return `needs-auth` and `ProviderIssue` when the Microsoft token is missing, expired, or lacks consent.
10. **Phase 10 — Verification**: build the app, configure connector instances, confirm config persistence, validate dashboard results.

---

## Relevant Files Modified

- `backend/PriorityHub.Api/Program.cs` — extended scopes, DI registration, connector registry, provider token mapping
- `backend/PriorityHub.Api/Models/ConfigModels.cs` — added typed connection models, provider arrays, `GetConnections()` cases
- `backend/PriorityHub.Api/Services/Connectors/MicrosoftTasksConnector.cs` — new file
- `backend/PriorityHub.Api/Services/Connectors/OutlookFlaggedMailConnector.cs` — new file
- `backend/PriorityHub.Api/Services/LocalConfigStore.cs` — added normalization for new arrays
- `src/pages/SettingsPage.jsx` — added `microsoftTasks: []` and `outlookFlaggedMail: []` to `emptyConfig`/`normalizeConfig`
- `src/pages/DashboardPage.jsx` — added `'microsoft-tasks'` and `'outlook-flagged-mail'` to `providerOptions`
- `src/lib/priorities.js` — added `formatProviderName` cases for both new providers
- `src/data/helpContent.js` — added help entries for both new connectors
