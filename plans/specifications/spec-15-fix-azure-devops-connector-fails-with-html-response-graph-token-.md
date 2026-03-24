# Specification: fix: Azure DevOps connector fails with HTML response — Graph token used instead of DevOps-scoped token

## Metadata
- Source issue: #15
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/15
- Author: @ipavlovi
- Created: 2026-03-24T18:21:51Z

## Specification

## Problem

When a user signs in with Microsoft and configures an Azure DevOps connector with an empty PAT, the WIQL request fails with:

> Azure DevOps WIQL request failed: Azure DevOps returned HTML instead of JSON. The sign-in token is not valid for Azure DevOps.

## Root Cause

Two separate token acquisition paths existed:

1. **`GetOauthTokensByProviderAsync` in `Program.cs`** — used by API endpoints. Correctly exchanges the refresh token for an Azure DevOps-scoped token via `RequestAccessTokenFromRefreshTokenAsync`.
2. **`GetOauthTokensAsync` in `DashboardPage.razor`** — used by the Blazor component calling `Aggregator.StreamAsync()` directly. Simply set `tokens["azure-devops"] = accessToken` — the **Microsoft Graph token** — with **no refresh token exchange**.

Since the Blazor UI calls the aggregator directly (not through the API endpoint), it used path #2. The Graph token (audience `graph.microsoft.com`) is rejected by Azure DevOps, which returns an HTML sign-in page.

Additionally:
- **Fallback bug**: When the refresh token exchange failed, `Program.cs` fell back to the Graph token as the azure-devops token — which also fails silently.
- **PAT/OAuth priority**: `BuildAuthorizationHeader` preferred PAT over OAuth, contrary to the design that OAuth should take precedence for Microsoft sign-in users.

## Fix

1. **Extracted `OauthTokenService`** — shared service encapsulating refresh-token-exchange logic, registered in DI.
2. **Updated `DashboardPage.razor`** — delegates to `OauthTokenService` instead of inline broken logic.
3. **Updated `Program.cs`** — delegates to `OauthTokenService`, removed duplicate static methods.
4. **Reversed auth priority** — `BuildAuthorizationHeader` now prefers Bearer (OAuth) over PAT when both are present.
5. **Removed silent fallback** — when refresh exchange fails, the `azure-devops` key is omitted entirely, producing a clear "needs-auth" error.
6. **Added tests** — `AzureDevOps_OauthTokenPreferredOverPat` and `AzureDevOps_PatUsedWhenNoOauthToken`.

## Verification

- `dotnet build PriorityHub.sln` — 0 warnings, 0 errors
- `dotnet test PriorityHub.sln` — 208 tests passed (183 API + 25 UI)

## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
