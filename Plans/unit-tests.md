# Plan: Unit Test Infrastructure + Retroactive Coverage

Add complete unit test infrastructure from scratch for both the backend (.NET/xUnit) and frontend (Vitest), then write retroactive tests for all existing logic-heavy code before applying TDD to new features going forward.

**Status**: COMPLETED — backend (xUnit) and UI (bUnit) test projects are fully implemented.

---

## Decisions

- xUnit + custom `HttpMessageHandler` (or Moq) for backend; Vitest + jsdom + Testing Library for frontend.
- Connector private mapping methods will be tested via public overload or by making them `internal` + using `InternalsVisibleTo`, since the most fragile logic lives in parsing/mapping helpers.
- Retroactive tests first; TDD is the forward-looking approach once infrastructure exists.
- HTTP integration tests against a real test server are excluded; mocked HttpClient is the boundary.
- `Program.cs` endpoint integration tests are excluded from this plan; they can be a separate future task.

---

## Steps

### Phase 1 — Backend test project setup *(blocks all backend tests)*

1. Create `backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj` targeting `net10.0` with xUnit 2.x, xUnit runner, Moq (or NSubstitute) for HttpClient mocking, and a project reference to the main API project.
2. Add a `dotnet test` script to `package.json` alongside build, so the CI and dev flow can run tests in one step.

### Phase 2 — Backend: LocalConfigStore tests *(depends on Phase 1)*

3. Test `SanitizeForFileName` with valid input, email-style addresses, and edge cases (empty, already clean, all-special characters).
4. Test `Normalize` ensures every provider list and `Preferences` is non-null after calling it on a partially constructed `ProviderConfiguration`.
5. Test `LoadAsync` returns a new empty `ProviderConfiguration` when the user config file does not exist (use a temp directory for isolation).
6. Test `SaveAsync` round-trips a configuration through save + load and gets back identical normalized data.

### Phase 3 — Backend: Connector mapping logic tests *(depends on Phase 1, parallel with Phase 2)*

7. **AzureDevOpsConnector** — Test `MapStatus` for each branch: done/closed/resolved, review/validate, block, active/progress/implement, and unknown. Test `ParseTags` splitting by `;`. Test `DaysSince` with a known past date and an unparseable string. Test `Impact`/`Urgency` from priority/severity field values using the `Math.Clamp(11 - value, 1, 10)` formula.
8. **GitHubIssuesConnector** — Test `MapStatus` for closed, open-blocked, open-review, open-in-progress, and plain open issues. Test `CalculateImpact` and `CalculateUrgency` with label combinations. Test `ResolveAccessToken` returns PAT when present and oauthToken when PAT is blank. Test `BuildSearchUrl` constructs correct query strings for repo + query combos.
9. **JiraConnector** — Test `MapStatus` for all branches. Test `PriorityToScore` for each keyword tier. Test `BuildIssueUrl` with valid and empty keys. Test `DaysSince` and `DueInDays`.
10. **TrelloConnector** — Test `MapStatus` for card list name normalization. Test `DaysSince`.
11. **MicrosoftTasksConnector** — Test `MapStatus` for all To Do status values. Test `MapImpact` and `MapUrgency` for importance levels and due date overrides. Test `ReadDateOffsetDays` with valid, null, and missing property inputs.
12. **OutlookFlaggedMailConnector** — Test `IsFlagged` for flagged, complete, notFlagged, and missing flag property. Test `MapStatus` for read vs unread and complete vs flagged flag state. Test `MapUrgency` and `MapImpact` for all importance strings.

### Phase 4 — Backend: HTTP fetch tests with mocked HttpClient *(depends on Phase 1, after Phase 3)*

13. Mock `HttpClient` using a custom `HttpMessageHandler` (or Moq) and wire into the connector constructor. For each connector, provide a minimal valid JSON response and assert the returned `ConnectorResult` contains the expected `WorkItem` count and `BoardConnection` status.
14. For each connector, provide an HTTP error response (e.g., 401) and assert that the returned `ConnectorResult` contains a `ProviderIssue` and the `BoardConnection.SyncStatus` is `"needs-auth"`.
15. For Microsoft Tasks and Outlook connectors, assert that a null/empty oauthToken throws the expected `InvalidOperationException` message without making any HTTP request.

### Phase 5 — Backend: DashboardAggregator tests *(depends on Phase 1, after Phase 4)*

16. Mock `ConnectorRegistry` and `LocalConfigStore`. Test that `BuildAsync` returns an aggregated `DashboardPayload` whose `WorkItems` equals the union of results from two fake connectors.
17. Test that `Merge` sets `IsNew = true` for items not in `orderedItemIds` and `IsNew = false` for known items.
18. Test `StreamAsync` yields exactly `totalConnections + 1` events (one initial empty event, one per connection) with correct `Progress.IsComplete` values.

### Phase 6 — Frontend test infrastructure setup *(parallel with Phase 1)*

19. Install `vitest`, `@vitest/ui`, `@testing-library/react`, `@testing-library/user-event`, and `jsdom` as devDependencies using `npm.cmd install`.
20. Add `vitest.config.js` (or extend `vite.config.js`) to configure `jsdom` as the test environment.
21. Add a `test` script to `package.json`.

### Phase 7 — Frontend: priority scoring and formatting tests *(depends on Phase 6)*

22. Test `rankWorkItems` in `src/lib/priorities.js` — verify item ordering by score, manual order override, and `isNew` items appearing first regardless of score.
23. Test `formatProviderName` for every provider key including the two new ones. Test `formatPriorityBand` for all three band values and an unknown value.
24. Test the scoring formula directly with known inputs: an item with a due date of 1 day, high priority, and a blocker must score above a low-priority item with no due date.

### Phase 8 — Frontend: SettingsPage config logic tests *(depends on Phase 6)*

25. Test `toConfigKey` (exported from SettingsPage or extracted to a helper) converts kebab-case keys correctly including `"azure-devops"` → `"azureDevOps"`, `"microsoft-tasks"` → `"microsoftTasks"`.
26. Test `normalizeConfig` fills in empty arrays for all provider keys when the input has missing or null fields.
27. Test `validateConnection` for a required field blank, a valid URL in baseUrl, an invalid URL in baseUrl, and a valid email.

> **Prerequisite for Phase 8**: Extract `toConfigKey`, `normalizeConfig`, and `validateConnection` from `src/pages/SettingsPage.jsx` into `src/lib/settingsHelpers.js` before writing tests.

### Phase 9 — Verification

28. Run `dotnet test backend/PriorityHub.Api.Tests/` — all tests green, no skip/ignored tests.
29. Run `npm.cmd run test` — all frontend tests green.
30. Run `npm.cmd run build` — production build still succeeds.

---

## Relevant Files

- `backend/PriorityHub.Api.Tests/PriorityHub.Api.Tests.csproj` — new, to create
- `backend/PriorityHub.Api/Services/LocalConfigStore.cs` — unit under test
- `backend/PriorityHub.Api/Services/DashboardAggregator.cs` — unit under test
- `backend/PriorityHub.Api/Services/ConnectorRegistry.cs` — unit under test
- All six connector files under `backend/PriorityHub.Api/Services/Connectors/` — map/parse logic is pure and can be tested without real HTTP
- `src/lib/priorities.js` — scoring and formatting logic
- `src/pages/SettingsPage.jsx` — config normalization and validation functions
- `package.json` — add test script and devDependencies
- `vitest.config.js` — new, to create

---

## Further Considerations

1. **Access modifiers**: most mapping helpers (`MapStatus`, `ParseTags`, etc.) are currently `private static`. Make them `internal static` + add `[assembly: InternalsVisibleTo("PriorityHub.Api.Tests")]` in the main project — avoids reflection and keeps tests readable.
2. **Test data**: a small `TestHelpers.JsonOf(...)` factory in the test project will let each test build arbitrary `JsonElement` fixtures cleanly.
3. **Frontend extraction**: `toConfigKey` and `validateConnection` are currently defined inside `SettingsPage.jsx`. Move to `src/lib/settingsHelpers.js` before writing Phase 8 tests.
