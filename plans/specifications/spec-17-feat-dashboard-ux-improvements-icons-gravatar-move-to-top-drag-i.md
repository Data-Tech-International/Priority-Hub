# Specification: feat: Dashboard UX improvements — icons, Gravatar, move-to-top, drag indicator, logout fix

## Metadata
- Source issue: #17
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/17
- Author: @ipavlovi
- Created: 2026-03-24T21:03:34Z

## Specification

## Specification

Simplify navigation and improve usability across the dashboard and navigation bar with the following changes.

---

### 1. Fix logout redirect bug

**Problem**: Clicking "Sign out" renders raw JSON (`{ "ok": true }`) in the browser instead of navigating to the login page.

**Root cause**: `POST /api/auth/logout` returns `Results.Ok(new { ok = true })`. Because the NavBar uses a standard HTML form POST, the browser renders the response body directly.

**Fix**: Change the endpoint to return `Results.Redirect("/login")` after signing out.

---

### 2. Replace text labels with SVG icons

Replace all text action labels with inline SVG icons and `title` attributes for tooltip/accessibility. No external icon library — use inline SVGs only.

| Action | Icon |
|--------|------|
| Open in source | External-link icon (box with arrow) |
| Move to top | Arrow-to-top icon (up arrow + line) |
| Sign out | Log-out icon (door with arrow) |

---

### 3. Un-bold item titles

Work item titles (`h3` inside `.work-item-heading`) render in bold due to the browser's default `h3` styling. Change to `font-weight: 400` (regular weight).

---

### 4. Add "Move to Top" action

Add a button next to the "Open in source" link on each work item card that moves the item to the top of the manual order queue.

**Behaviour**:
- In-place reorder: mutate `_orderedItemIds`, re-rank, `StateHasChanged()` — no server re-fetch
- Async persist to `config/providers.local.json` via `ConfigStore.SaveAsync()`
- Same pattern as existing drag-drop reorder; share persistence logic via extracted `SaveOrderAsync()` helper

---

### 5. Use Gravatar for logged-in user avatar

When no `picture` claim is present (e.g., Microsoft OAuth), compute a Gravatar URL from the user's email:

```
https://www.gravatar.com/avatar/{md5(email.ToLower().Trim())}?s=68&d=mp
```

- `d=mp` returns a generic silhouette if no Gravatar is registered for the email
- MD5 is used here per Gravatar's specification (not for security)
- Fallback chain: `picture` claim → Gravatar from email → initial-letter avatar

---

### 6. Add sign-out button next to user info

The sign-out button (already present) is restyled as an icon-only button placed next to the user name/email in the navigation bar.

---

### 7. Visual drag-drop indicator

During drag-and-drop reordering, show a 3px blue horizontal bar above the card being hovered to indicate where the dragged item will be dropped.

**Implementation**:
- Track `_dragOverItemId` using `@ondragenter` (fires once per element entry — **not** `@ondragover`, which fires continuously and floods the Blazor Server SignalR circuit)
- Add `.drag-over` CSS class to the hovered card
- Style `.work-item-card.drag-over::before` as a `3px` `var(--dti-blue)` bar at `top: -6px` using `position: absolute`
- Clear on `@ondragleave`, `@ondragend`, and after drop

---

## Files Affected

- `backend/PriorityHub.Ui/Program.cs` — logout redirect fix
- `backend/PriorityHub.Ui/Components/NavBar.razor` — Gravatar, icon sign-out button
- `backend/PriorityHub.Ui/Components/Pages/DashboardPage.razor` — move-to-top, icons, drag indicator, item-actions wrapper, shared `SaveOrderAsync()`
- `backend/PriorityHub.Ui/wwwroot/app.css` — un-bold h3, icon sizing, item-actions flex, drag-over bar, `position: relative` on cards

## Verification

1. `dotnet build PriorityHub.sln` — clean compile
2. `dotnet test PriorityHub.sln` — all tests pass
3. Manual: logout navigates to `/login`
4. Manual: move-to-top moves item instantly and persists after refresh
5. Manual: icons visible with tooltip on hover
6. Manual: Microsoft account shows Gravatar or silhouette in nav
7. Manual: item titles render in regular weight
8. Manual: blue bar appears above hovered card during drag


## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
