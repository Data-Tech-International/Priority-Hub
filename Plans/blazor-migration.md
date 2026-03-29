# Plan: Migrate Frontend from Vite + React to Blazor

## Status: COMPLETED
**Date:** March 2026
**Owner:** Development team

> **Note:** This plan was written during the Vite + React era. Node.js/npm references below are historical; the project now uses .NET exclusively (see #41).

---

## Goal

Replace the Vite + React JavaScript frontend with a Blazor frontend that:

- Preserves the existing DTI corporate brand (Highway Sans Pro font, CSS custom properties, colour palette).
- Maintains the same route structure (`/`, `/login`, `/settings`).
- Keeps the existing ASP.NET Core backend-for-frontend and its HTTP API unchanged.
- Uses the same testing discipline (unit tests, minimum 60% coverage).
- Reduces the number of distinct technology stacks by co-locating the UI inside the existing .NET solution.

---

## Architecture decision

### Option A — Blazor Server (recommended for this project)

Blazor Server runs component logic on the server and streams UI updates to the browser over a persistent SignalR connection. Because the app is already a local, single-user tool backed by an ASP.NET Core process, there is no meaningful latency penalty.

**Advantages:**
- No JavaScript bundle to build or deploy.
- Components can call backend services *directly* through dependency injection — no separate HTTP API layer needed for UI ↔ backend communication.
- Smaller initial download; the browser loads a tiny WebSocket client only.
- Full .NET debugging experience in Visual Studio and VS Code.

**Disadvantages:**
- Requires a persistent server connection; offline use is not possible.
- Not suitable if the dashboard is ever hosted as a static site.

### Option B — Blazor WebAssembly Hosted (alternative)

The Blazor UI compiles to WebAssembly, runs entirely in the browser, and calls the existing `/api/*` endpoints over HTTP — exactly mirroring the current React + ASP.NET Core topology.

**Advantages:**
- Works offline once the WASM bundle is cached.
- Architecture is identical to the current setup; no API layer changes.

**Disadvantages:**
- Large initial download (~5–10 MB for the .NET runtime).
- Cannot call backend services directly; must continue using the HTTP API.

**Decision:** Start with **Blazor Server** because this is a local personal dashboard and the direct-injection model substantially simplifies the code.

---

## Files to create / modify

### New files
| Path | Purpose |
|------|---------|
| `backend/PriorityHub.Ui/PriorityHub.Ui.csproj` | New Blazor Server project |
| `backend/PriorityHub.Ui/Program.cs` | App startup, DI wiring |
| `backend/PriorityHub.Ui/App.razor` | Root Blazor component |
| `backend/PriorityHub.Ui/Routes.razor` | Route declarations |
| `backend/PriorityHub.Ui/Components/Layout/MainLayout.razor` | Shell layout (NavBar slot) |
| `backend/PriorityHub.Ui/Components/NavBar.razor` | Blazor port of `NavBar.jsx` |
| `backend/PriorityHub.Ui/Components/HelpPanel.razor` | Blazor port of `HelpPanel.jsx` |
| `backend/PriorityHub.Ui/Components/TagFilter.razor` | Blazor port of `TagFilter.jsx` |
| `backend/PriorityHub.Ui/Pages/LoginPage.razor` | Blazor port of `LoginPage.jsx` |
| `backend/PriorityHub.Ui/Pages/DashboardPage.razor` | Blazor port of `DashboardPage.jsx` |
| `backend/PriorityHub.Ui/Pages/SettingsPage.razor` | Blazor port of `SettingsPage.jsx` |
| `backend/PriorityHub.Ui/Services/AuthStateProvider.cs` | Custom `AuthenticationStateProvider` |
| `backend/PriorityHub.Ui/wwwroot/css/app.css` | DTI brand CSS (ported from `src/index.css`) |
| `backend/PriorityHub.Ui/wwwroot/fonts/` | Copy font files from `public/fonts/` |
| `PriorityHub.sln` | Add new project reference |
| `backend/PriorityHub.Ui.Tests/PriorityHub.Ui.Tests.csproj` | bUnit tests for Blazor components |

### Files to retire (after cutover)
- `src/` — entire React source tree
- `index.html` — Vite entry point
- `vite.config.js`
- `package.json`, `package-lock.json`
- `.eslintrc.json`, `.eslintignore`

### Files unchanged
- `backend/PriorityHub.Api/` — HTTP API remains as-is (used during transition; direct injection replaces it post-migration)
- `config/providers.local.json` — same local secrets file
- `Plans/` — documentation

---

## Step-by-step migration

### Phase 1 — Bootstrap (≈ 1 day)

1. Add a Blazor Server project to the solution:
   ```bash
   cd backend
   dotnet new blazorserver -n PriorityHub.Ui --framework net10.0
   dotnet sln ../PriorityHub.sln add PriorityHub.Ui/PriorityHub.Ui.csproj
   ```

2. Add a project reference from `PriorityHub.Ui` to `PriorityHub.Api` so the UI can inject connector services directly:
   ```xml
   <ProjectReference Include="..\PriorityHub.Api\PriorityHub.Api.csproj" />
   ```

3. Register services in `PriorityHub.Ui/Program.cs`:
   ```csharp
   builder.Services.AddRazorComponents().AddInteractiveServerComponents();
   builder.Services.AddScoped<DashboardAggregator>();
   builder.Services.AddScoped<LocalConfigStore>();
   // ... register each connector
   ```

4. Configure `launchSettings.json` to start the UI on port 5173 (same as current Vite dev server) so the VS Code launch config still works.

5. Verify the default Blazor counter sample page is reachable at `https://localhost:5173`.

**Verification:** `dotnet run --project backend/PriorityHub.Ui` serves the app without errors.

---

### Phase 2 — Brand and layout (≈ 0.5 day)

1. Copy `src/index.css` to `backend/PriorityHub.Ui/wwwroot/css/app.css` without modification — the CSS custom properties and font-face declarations are plain CSS and require no changes.

2. Copy `public/fonts/HighwaySansPro-*.woff2` to `backend/PriorityHub.Ui/wwwroot/fonts/`.

3. Reference the stylesheet in `backend/PriorityHub.Ui/Components/App.razor`:
   ```html
   <link rel="stylesheet" href="css/app.css" />
   ```

4. Implement `MainLayout.razor` with the same structural HTML as the current `App.jsx` shell:
   ```html
   <NavBar />
   <main>
       @Body
   </main>
   ```

**Verification:** Browser shows DTI blue/navy/green palette and Highway Sans Pro font.

---

### Phase 3 — Authentication (≈ 1 day)

The current React app uses `AuthContext` which calls `GET /api/me` and `POST /api/signout`. In Blazor Server the equivalent is a custom `AuthenticationStateProvider`.

1. Create `AuthStateProvider.cs` that:
   - Calls `IAuthService` (a new service wrapping the same `GET /api/me` logic) on construction.
   - Exposes `GetAuthenticationStateAsync()` returning an authenticated or anonymous `ClaimsPrincipal`.
   - Exposes `SignOutAsync()` and `NotifyAuthChanged()`.

2. Register `AuthStateProvider` as the `AuthenticationStateProvider` in DI.

3. Replace `<ProtectedRoute>` with Blazor's built-in `<AuthorizeView>` and `[Authorize]` page attribute.

4. Implement `LoginPage.razor`:
   - Show login form (same fields as `LoginPage.jsx`).
   - On submit, call `AuthStateProvider.SignInAsync(...)`.
   - On success, `NavigationManager.NavigateTo("/")`.

5. Add `<CascadingAuthenticationState>` to `App.razor`.

**Verification:** Unauthenticated users redirected to `/login`; authenticated users see the dashboard.

---

### Phase 4 — Dashboard page (≈ 2 days)

`DashboardPage.razor` is the largest component. Port it in sub-steps:

#### 4a — Data loading
- Inject `DashboardAggregator` directly (no HTTP call needed).
- Use `OnInitializedAsync` to load data instead of `useEffect`.
- Replace the streaming progress model (`fetchDashboardStream`) with a `Progress<T>` callback pattern or SignalR if real-time progress is required.

#### 4b — Work item ranking
- Move `rankWorkItems` logic from `src/lib/priorities.js` to a C# service `WorkItemRanker.cs` in `PriorityHub.Api/Services/`.
- This is pure calculation code that is already unit-tested; port the same test cases to xUnit.

#### 4c — Filtering
- Port `TagFilter.jsx` → `TagFilter.razor` as an `EventCallback<string>`-based component.
- Provider, status, and priority filter selects use `<select @bind>` with `@onchange`.

#### 4d — Drag and drop ordering
- Blazor Server does not include a drag-and-drop library.
- Use `SortableJS` via a JavaScript interop shim (`wwwroot/js/sortable-interop.js`).
- Or use `Blazor.Sortable` NuGet package (MIT license).
- Persist the new order by calling `LocalConfigStore` directly on the drag-end event.

#### 4e — New-item highlighting
- Store the set of "seen item IDs" in a `HashSet<string>` injected as a scoped service `NewItemTracker`.
- Add a CSS class `item--new` when `!NewItemTracker.HasSeen(item.Id)`.
- Mark items as seen when the user places them in a manual position.

**Verification:** Dashboard renders work items, filter controls work, drag ordering persists across page reload.

---

### Phase 5 — Settings page (≈ 1 day)

Port `SettingsPage.jsx` → `SettingsPage.razor`:

1. Load connector config via `LocalConfigStore.LoadAsync()` directly.
2. For each provider type, render an edit form using Blazor's `EditForm` + `DataAnnotationsValidator`.
3. On save, call `LocalConfigStore.SaveAsync(config)`.
4. Add client-side validators via `ValidationAttribute` annotations on the config model classes (mirrors the current JS validators).

**Verification:** Connector configuration saves to `config/providers.local.json`; validators prevent save on empty required fields.

---

### Phase 6 — Component library cleanup (≈ 0.5 day)

1. Port `HelpPanel.jsx` → `HelpPanel.razor` (renders the help text data from `src/data/helpContent.js` which becomes a C# `HelpContent.cs` static class).
2. Port `NavBar.jsx` → `NavBar.razor` with `AuthorizeView` for sign-out button visibility.
3. Delete placeholder counter/weather sample components generated by `dotnet new`.

**Verification:** Help panel opens/closes; navbar shows user name when authenticated.

---

### Phase 7 — Tests (≈ 1 day)

1. Add `bUnit` to the new test project:
   ```bash
   cd backend/PriorityHub.Ui.Tests
   dotnet add package bunit --version 1.34.4
   ```

2. Write component tests for:
   - `NavBar.razor` — renders user name from auth state.
   - `TagFilter.razor` — emits correct callback values.
   - `LoginPage.razor` — redirects after successful sign-in.

3. Write service tests (xUnit, no bUnit needed) for:
   - `WorkItemRanker.cs` — same test cases as `priorities.test.js`.
   - `NewItemTracker.cs` — tracks seen/unseen items correctly.

4. Run `dotnet test` and verify ≥ 60% coverage on the new project.

**Verification:** `dotnet test` passes with no failures; coverage threshold met.

---

### Phase 8 — Cutover and cleanup (≈ 0.5 day)

1. Update `package.json` `dev` and `build` scripts to launch `PriorityHub.Ui` instead of Vite:
   ```json
   "dev": "dotnet watch --project backend/PriorityHub.Ui run",
   "build": "dotnet build PriorityHub.sln"
   ```

2. Update `.vscode/launch.json` to point `preLaunchTask` to the new project.

3. Update `.vscode/tasks.json` — remove `dev:client` Vite task; replace with `dotnet watch` for the UI project.

4. Remove the React source tree:
   ```bash
   git rm -r src/ index.html vite.config.js
   git rm package.json package-lock.json .eslintrc.json .eslintignore
   ```

5. Update `README.md`:
   - Remove Vite/Node setup instructions.
   - Update "Local development" section to `dotnet run --project backend/PriorityHub.Ui`.
   - Update tech stack description to "Blazor Server + ASP.NET Core 10".

**Verification:** `dotnet run --project backend/PriorityHub.Ui` serves the full app without `npm install`.

---

## UI style preservation

The current DTI brand is defined entirely in `src/index.css` using CSS custom properties. Because the custom properties and font-face declarations are standard CSS, they transfer to Blazor unchanged:

| React pattern | Blazor equivalent |
|--------------|------------------|
| `className="item item--new"` | `class="item item--new"` (same HTML attribute) |
| `style={{ color: 'var(--dti-blue)' }}` | `style="color: var(--dti-blue)"` |
| `index.css` global import | `<link>` in `App.razor` |
| CSS modules (not used) | Blazor CSS isolation (`.razor.css`) available if needed |

Component-scoped styles (`.razor.css` files) may be added later to scope styles without class-name collisions, but are not required for an equivalent initial port.

---

## Testing strategy

| Layer | Framework | Location |
|-------|-----------|---------|
| Component rendering | bUnit | `backend/PriorityHub.Ui.Tests/Components/` |
| Service / business logic | xUnit | `backend/PriorityHub.Api.Tests/` (existing) + new `PriorityHub.Ui.Tests/Services/` |
| End-to-end | Playwright (optional phase 2) | `e2e/` |

Coverage threshold: **60 %** across all test projects, same as current.

---

## Labels / prerequisites

Before starting implementation:

1. Create a `specification` issue using the Specification Request template.
2. Confirm this plan is committed to `Plans/blazor-migration.md`.
3. Apply label `plan-approved` to the spec issue to trigger the bootstrap workflow.
4. Implementation proceeds on the generated feature branch.

---

## Estimated effort

| Phase | Effort |
|-------|--------|
| 1 Bootstrap | 1 day |
| 2 Brand & layout | 0.5 day |
| 3 Authentication | 1 day |
| 4 Dashboard page | 2 days |
| 5 Settings page | 1 day |
| 6 Component cleanup | 0.5 day |
| 7 Tests | 1 day |
| 8 Cutover | 0.5 day |
| **Total** | **7.5 days** |

---

## Risks and mitigations

| Risk | Likelihood | Mitigation |
|------|-----------|------------|
| Drag-and-drop JS interop adds complexity | Medium | Use `Blazor.Sortable` package rather than hand-rolling interop |
| Streaming progress UX during fetch | Medium | Use Blazor Server's built-in real-time SignalR channel; invoke `StateHasChanged()` from progress callbacks |
| bUnit coverage below threshold | Low | Port all 27 existing React test cases as bUnit tests before deleting the React source tree |
| Highway Sans Pro font licensing | Low | Fonts are already present in `public/fonts/`; move to `wwwroot/fonts/` only |

---

## References

- [Blazor Server documentation](https://learn.microsoft.com/aspnet/core/blazor)
- [bUnit testing library](https://bunit.dev)
- [Blazor.Sortable](https://github.com/Lupusa87/BlazorSortable)
- [Existing CI/CD plan](ci-agents.md)
- [Spec-first workflow](spec-first-agent-workflow.md)
