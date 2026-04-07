# Specification: [Specification] Azure Application Insights Telemetry for Application Usage Tracking

## Metadata
- Source issue: #74
- Source URL: https://github.com/Data-Tech-International/Priority-Hub/issues/74
- Author: @ipavlovi
- Created: 2026-04-07T12:30:01Z

## Specification

---
title: Azure Application Insights Telemetry for Application Usage Tracking
version: 1.1
date_created: 2026-04-07
last_updated: 2026-04-07
owner: Priority Hub Team
tags: infrastructure, telemetry, observability, application-insights, azure-monitor
---

# Introduction

This specification defines the integration of Azure Application Insights into Priority Hub to provide administrators with centralized, real-time visibility into application usage. The integration uses the Application Insights SDK (`Microsoft.ApplicationInsights.AspNetCore`) directly, leveraging its full feature set: `TelemetryClient` for custom events and metrics, automatic HTTP request and dependency tracking, `ITelemetryInitializer` for user-context enrichment, Live Metrics Stream, and Application Map. The telemetry covers authentication provider usage (sign-in and sign-out), connector activity, page views, configuration changes, linked-account operations, distinct-user counts, and total registered-user counts. These metrics enable capacity planning, provider-adoption analysis, and operational monitoring without requiring custom dashboards or manual log analysis.

## 1. Purpose & Scope

**Purpose**: Enable Priority Hub administrators to track application usage through Azure Application Insights, capturing structured telemetry about authentication providers, connector usage, user activity, page navigation, configuration changes, and registration counts — using the Application Insights SDK directly wherever feasible.

**Scope**:
- Instrument the PriorityHub.Ui host process (Blazor Server + ASP.NET Core BFF) with the Application Insights SDK (`Microsoft.ApplicationInsights.AspNetCore`).
- Use `TelemetryClient` directly for custom events (`TrackEvent`), custom metrics (`GetMetric`), exception tracking (`TrackException`), and page-view tracking (`TrackPageView`).
- Enable automatic HTTP request telemetry, outbound dependency tracking (connector HTTP calls), and Live Metrics Stream provided by the Application Insights SDK.
- Use `ITelemetryInitializer` to enrich all telemetry with authenticated-user context (hashed user ID, authentication provider).
- Emit custom events and metrics for: authentication sign-ins and sign-outs, connector fetches, Blazor page views, configuration save operations, linked-account link/unlink operations, distinct active users, and total registered users.
- Support configuration-driven enablement so that Application Insights remains optional and does not affect deployments that do not use it.

**Intended audience**: Coding agents implementing the feature; Priority Hub administrators configuring and consuming the telemetry.

**Assumptions**:
- The application runs as a single host process (`PriorityHub.Ui.dll`) that embeds backend services from `PriorityHub.Api`.
- Administrators have access to an Azure subscription and can provision an Application Insights resource.
- The telemetry is for operational monitoring and does not replace application-level audit logging.

## 2. Definitions

| Term | Definition |
|---|---|
| **Azure Monitor** | Microsoft's cloud-based monitoring platform that collects, analyzes, and acts on telemetry from cloud and on-premises environments. |
| **Application Insights** | A feature of Azure Monitor that provides application performance monitoring (APM) for live web applications. |
| **Application Insights SDK** | The `Microsoft.ApplicationInsights.AspNetCore` NuGet package providing `TelemetryClient`, automatic request/dependency tracking, `ITelemetryInitializer`, and Live Metrics. |
| **TelemetryClient** | The primary Application Insights SDK class for emitting custom events (`TrackEvent`), metrics (`GetMetric`/`TrackMetric`), exceptions (`TrackException`), page views (`TrackPageView`), and dependencies (`TrackDependency`). |
| **ITelemetryInitializer** | An Application Insights interface that enriches every outgoing telemetry item with additional properties (e.g., authenticated user ID, provider). |
| **Live Metrics Stream** | A real-time Application Insights feature showing incoming requests, outgoing dependencies, exceptions, and custom metrics with ~1-second latency. |
| **Application Map** | An Application Insights visualization that shows the application's components and their dependencies, including connector HTTP calls. |
| **Custom Metric** | A numeric measurement (counter, gauge, histogram) emitted via `TelemetryClient.GetMetric()` and exported to Azure Monitor Metrics. |
| **Custom Event** | A named telemetry record with properties, emitted via `TelemetryClient.TrackEvent()` and exported to the `customEvents` table in Application Insights. |
| **Authentication Provider** | An OAuth identity provider used for sign-in (Microsoft, GitHub, Google, Facebook, Jira, Trello, Yandex). |
| **Connector** | A `IConnector` implementation that fetches work items from an external source (azure-devops, github, jira, trello, microsoft-tasks, outlook-flagged-mail, imap-flagged-mail). |
| **Connector Instance** | A single configured connection within a connector type (e.g., one Azure DevOps connection targeting a specific project). |
| **Distinct User** | A unique user identified by the `GetUserIdentityKey()` value (email or `{sub}_{provider}`). |
| **Registered User** | A user who has at least one persisted configuration entry in the `IConfigStore`. |
| **BFF** | Backend for Frontend — the architectural pattern where PriorityHub.Ui hosts both Blazor Server UI and API endpoints in a single process. |

## 3. Requirements, Constraints & Guidelines

### Telemetry Infrastructure

- **REQ-001**: The application MUST integrate Application Insights using the `Microsoft.ApplicationInsights.AspNetCore` SDK package, registered via `builder.Services.AddApplicationInsightsTelemetry()`.
- **REQ-002**: Telemetry MUST be enabled only when a valid `ApplicationInsights:ConnectionString` configuration value is present. When absent, the application MUST start and function normally without Application Insights.
- **REQ-003**: All custom metrics MUST be emitted via `TelemetryClient.GetMetric()` for pre-aggregated metrics exported to Azure Monitor Metrics.
- **REQ-004**: All custom events MUST be emitted via `TelemetryClient.TrackEvent()` with structured custom properties.
- **REQ-005**: The application MUST enable automatic HTTP request telemetry collection provided by the Application Insights SDK. All API endpoint requests (`/api/*`) and Blazor Server hub requests appear automatically in the `requests` table.
- **REQ-006**: The application MUST enable automatic outbound dependency tracking provided by the Application Insights SDK. All outgoing HTTP calls to connector APIs (Azure DevOps, GitHub, Jira, Trello, Microsoft Graph, IMAP) appear automatically in the `dependencies` table.
- **REQ-007**: The application MUST enable Live Metrics Stream so administrators can observe real-time request rates, failure rates, and custom metrics in the Azure portal.
- **REQ-008**: The application MUST register an `ITelemetryInitializer` that enriches every telemetry item with the authenticated user's hashed identity and authentication provider, using `telemetryItem.Context.User.AuthenticatedUserId` and a custom property `authProvider`.

### Authentication Tracking

- **REQ-010**: The application MUST emit a custom event `AuthenticationSignIn` via `TelemetryClient.TrackEvent()` each time a user successfully completes an OAuth sign-in flow.
- **REQ-011**: The `AuthenticationSignIn` event MUST include the following custom properties:
  - `provider` — the authentication provider name (e.g., `microsoft`, `github`, `google`, `facebook`, `jira`, `trello`, `yandex`).
  - `userId` — the anonymized or hashed user identity key (NOT the raw email or PII).
- **REQ-012**: The application MUST emit a pre-aggregated metric `AuthSignInCount` via `TelemetryClient.GetMetric("AuthSignInCount", "Provider")`, incremented on each successful sign-in, dimensioned by `provider`.
- **REQ-013**: The application MUST NOT log or emit raw email addresses, display names, or other PII into telemetry. User identity MUST be hashed (SHA-256 of the `GetUserIdentityKey()` value) before inclusion in any telemetry property.
- **REQ-014**: The application MUST emit a custom event `AuthenticationSignOut` via `TelemetryClient.TrackEvent()` each time a user signs out via the `/api/auth/logout` endpoint. The event MUST include `userId` (hashed).

### Connector Usage Tracking

- **REQ-020**: The application MUST emit a custom event `ConnectorFetch` via `TelemetryClient.TrackEvent()` each time a connector instance completes a fetch operation (success or failure).
- **REQ-021**: The `ConnectorFetch` event MUST include the following custom properties:
  - `provider` — the connector's `ProviderKey` (e.g., `azure-devops`, `github`).
  - `connectionId` — the connector instance ID.
  - `status` — `success` or `error`.
  - `itemCount` — the number of work items returned (0 on error).
  - `durationMs` — the elapsed time in milliseconds for the fetch operation.
  - `userId` — the hashed user identity key.
- **REQ-022**: The application MUST emit a pre-aggregated metric `ConnectorFetchCount` via `TelemetryClient.GetMetric("ConnectorFetchCount", "Provider", "Status")`, incremented on each fetch, dimensioned by `provider` and `status`.
- **REQ-023**: The application MUST emit a pre-aggregated metric `ConnectorFetchDuration` via `TelemetryClient.GetMetric("ConnectorFetchDuration", "Provider")`, recording the fetch duration in milliseconds, dimensioned by `provider`.
- **REQ-024**: The application MUST emit a pre-aggregated metric `ConnectorItemsCount` via `TelemetryClient.GetMetric("ConnectorItemsCount", "Provider")`, recording the number of items fetched, dimensioned by `provider`.
- **REQ-025**: When a connector fetch throws an exception, the application MUST call `TelemetryClient.TrackException()` with the exception, including custom properties `provider`, `connectionId`, and `userId` (hashed). This is in addition to the `ConnectorFetch` event with `status=error`.

### Page View Tracking

- **REQ-035**: The application MUST emit a page-view event via `TelemetryClient.TrackPageView()` each time a user navigates to a Blazor page (Dashboard, Settings, Login). The page name MUST be the route path (e.g., `/`, `/settings`, `/login`).
- **REQ-036**: Page-view events MUST include the hashed `userId` as a custom property (for authenticated pages) to enable per-user navigation analysis.

### Configuration & Account Operations Tracking

- **REQ-040**: The application MUST emit a custom event `ConfigurationSaved` via `TelemetryClient.TrackEvent()` each time a user saves their provider configuration via `PUT /api/config`. The event MUST include custom properties: `userId` (hashed), `connectorCount` (total number of enabled connector instances across all providers).
- **REQ-041**: The application MUST emit a custom event `LinkedAccountAdded` via `TelemetryClient.TrackEvent()` each time a user links a Microsoft account via `/api/auth/link/microsoft/callback`. The event MUST include `userId` (hashed).
- **REQ-042**: The application MUST emit a custom event `LinkedAccountRemoved` via `TelemetryClient.TrackEvent()` each time a user unlinks a Microsoft account via `DELETE /api/auth/link/microsoft/{accountId}`. The event MUST include `userId` (hashed).

### User Counting

- **REQ-030**: The application MUST emit a metric via `TelemetryClient.GetMetric("ActiveUsers")` reporting the number of distinct users who have accessed the dashboard within a configurable rolling window (default: 24 hours).
- **REQ-031**: The application MUST emit a metric via `TelemetryClient.GetMetric("RegisteredUsers")` reporting the total number of users with persisted configurations in the `IConfigStore`.
- **REQ-032**: The registered-user count MUST be refreshed periodically (default interval: 5 minutes) via a background service, not on every request.
- **REQ-033**: The active-user tracking MUST use an in-memory data structure (e.g., `ConcurrentDictionary<string, DateTimeOffset>`) to track distinct hashed user IDs and their last-seen timestamps.

### Security

- **SEC-001**: The Application Insights connection string MUST be treated as a secret and MUST NOT be committed to source control. It MUST be provided via environment variables (`APPLICATIONINSIGHTS_CONNECTION_STRING`), Azure App Configuration, or `appsettings.{Environment}.json` files excluded from version control.
- **SEC-002**: No PII (email, name, IP address) MUST appear in custom events, custom metrics dimensions, request telemetry, or log messages exported to Application Insights. Only hashed user identifiers are permitted. The `ITelemetryInitializer` MUST set `Context.User.AuthenticatedUserId` to the hashed value, never the raw identity.
- **SEC-003**: The telemetry service MUST NOT interfere with the application's authentication or authorization mechanisms.
- **SEC-004**: The `ITelemetryInitializer` MUST NOT store or cache raw user identity values. It MUST hash the identity key inline on each telemetry item.

### Constraints

- **CON-001**: The telemetry integration MUST NOT introduce a hard dependency on Application Insights at compile time. When the connection string is absent, a no-op `ITelemetryService` implementation is registered. The `Microsoft.ApplicationInsights.AspNetCore` package may be present but inactive.
- **CON-002**: The telemetry background service for user counting MUST NOT query the `IConfigStore` more frequently than once per minute to avoid performance impact.
- **CON-003**: The in-memory active-user tracker MUST be bounded to a maximum of 100,000 entries to prevent unbounded memory growth.
- **CON-004**: The telemetry integration MUST target .NET 10 and use the same SDK version as the rest of the project.

### Guidelines

- **GUD-001**: Follow the existing DI-first pattern. Register all telemetry services in the DI container via an extension method (e.g., `AddPriorityHubTelemetry()`).
- **GUD-002**: Place telemetry service code in `backend/PriorityHub.Api/Services/Telemetry/` to keep it separate from business logic.
- **GUD-003**: Place the telemetry DI registration extension in `backend/PriorityHub.Api/Extensions/TelemetryServiceExtensions.cs`.
- **GUD-004**: Use the existing `ILogger<T>` for internal diagnostic logging of the telemetry subsystem itself; do not introduce a separate logging framework.
- **GUD-005**: Prefer Application Insights SDK-specific APIs (`TelemetryClient.TrackEvent`, `TelemetryClient.GetMetric`, `TelemetryClient.TrackException`, `TelemetryClient.TrackPageView`, `ITelemetryInitializer`) over generic OpenTelemetry APIs, to maximize use of Application Insights features (Live Metrics, Application Map, smart detection, analytics queries).
- **GUD-006**: Use `TelemetryClient.GetMetric()` for pre-aggregated metrics rather than `TrackMetric()`. Pre-aggregated metrics reduce ingestion cost and support multi-dimensional analysis in Azure Monitor Metrics Explorer.

### Patterns

- **PAT-001**: Emit telemetry at the point of action completion, not at the start. For connector fetches, emit after the `FetchConnectionAsync` call completes. For sign-ins, emit in the `OnCreatingTicket` event after claims are populated.
- **PAT-002**: Use a decorator or middleware pattern for connector fetch instrumentation to avoid modifying each connector's implementation directly. Instrument in `DashboardAggregator.BuildPendingFetches()` where fetch tasks are created. Inject `TelemetryClient` into `DashboardAggregator` via constructor injection.
- **PAT-003**: Use a `BackgroundService` for periodic metric emission (registered users, active-user reporting) via `TelemetryClient.GetMetric()`.
- **PAT-004**: Use `ITelemetryInitializer` to enrich all telemetry items with user context, rather than adding user properties to each individual `TrackEvent` call. This ensures consistent user attribution across request telemetry, dependency telemetry, exception telemetry, and custom events.

## 4. Interfaces & Data Contracts

### 4.1 Configuration Schema

Add the following section to `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Telemetry": {
    "ActiveUserWindowMinutes": 1440,
    "RegisteredUserRefreshIntervalSeconds": 300,
    "MaxActiveUserEntries": 100000
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `ApplicationInsights:ConnectionString` | string | `""` (empty) | Application Insights connection string. When empty, Application Insights telemetry is disabled and a no-op `ITelemetryService` is registered. Also supported via environment variable `APPLICATIONINSIGHTS_CONNECTION_STRING`. |
| `Telemetry:ActiveUserWindowMinutes` | int | `1440` | Rolling window in minutes for counting distinct active users. |
| `Telemetry:RegisteredUserRefreshIntervalSeconds` | int | `300` | Interval in seconds between registered-user count refreshes. |
| `Telemetry:MaxActiveUserEntries` | int | `100000` | Maximum number of tracked active-user entries before pruning. |

### 4.2 Telemetry Service Interface

```csharp
namespace PriorityHub.Api.Services.Telemetry;

/// <summary>
/// Provides methods for recording application-level telemetry events and metrics
/// using Application Insights TelemetryClient.
/// </summary>
public interface ITelemetryService
{
    /// <summary>Records a successful authentication sign-in via TelemetryClient.TrackEvent("AuthenticationSignIn").</summary>
    /// <param name="provider">The authentication provider name (e.g., "microsoft").</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordSignIn(string provider, string userIdentityKey);

    /// <summary>Records a sign-out via TelemetryClient.TrackEvent("AuthenticationSignOut").</summary>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordSignOut(string userIdentityKey);

    /// <summary>Records a connector fetch completion via TelemetryClient.TrackEvent("ConnectorFetch") and pre-aggregated metrics.</summary>
    /// <param name="provider">The connector's ProviderKey.</param>
    /// <param name="connectionId">The connector instance ID.</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    /// <param name="itemCount">Number of work items returned.</param>
    /// <param name="durationMs">Elapsed time in milliseconds.</param>
    /// <param name="success">Whether the fetch succeeded.</param>
    void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success);

    /// <summary>Records a connector fetch exception via TelemetryClient.TrackException().</summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="provider">The connector's ProviderKey.</param>
    /// <param name="connectionId">The connector instance ID.</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey);

    /// <summary>Records a user accessing the dashboard (for active-user tracking).</summary>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordUserActivity(string userIdentityKey);

    /// <summary>Records a Blazor page view via TelemetryClient.TrackPageView().</summary>
    /// <param name="pageName">The route path (e.g., "/", "/settings", "/login").</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally). Null for unauthenticated pages.</param>
    void RecordPageView(string pageName, string? userIdentityKey);

    /// <summary>Records a configuration save operation via TelemetryClient.TrackEvent("ConfigurationSaved").</summary>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    /// <param name="connectorCount">Total number of enabled connector instances.</param>
    void RecordConfigSave(string userIdentityKey, int connectorCount);

    /// <summary>Records a linked account operation via TelemetryClient.TrackEvent().</summary>
    /// <param name="operation">"added" or "removed".</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordLinkedAccountOperation(string operation, string userIdentityKey);
}
```

### 4.3 Active-User Tracker Interface

```csharp
namespace PriorityHub.Api.Services.Telemetry;

/// <summary>
/// Tracks distinct active users within a configurable rolling time window.
/// Thread-safe for concurrent access from request-processing threads.
/// </summary>
public interface IActiveUserTracker
{
    /// <summary>Records a user activity timestamp.</summary>
    /// <param name="hashedUserId">SHA-256 hashed user identity.</param>
    void RecordActivity(string hashedUserId);

    /// <summary>Returns the count of distinct users active within the configured window.</summary>
    int GetActiveUserCount();
}
```

### 4.4 Custom Metrics (via TelemetryClient.GetMetric)

| Metric Name | Dimensions | Description |
|---|---|---|
| `AuthSignInCount` | `Provider` | Total authentication sign-ins per provider. |
| `ConnectorFetchCount` | `Provider`, `Status` | Total connector fetch operations per provider and outcome. |
| `ConnectorFetchDuration` | `Provider` | Duration of connector fetch operations in milliseconds per provider. |
| `ConnectorItemsCount` | `Provider` | Total work items fetched per provider. |
| `ActiveUsers` | (none) | Distinct active users in the rolling window (emitted periodically by background service). |
| `RegisteredUsers` | (none) | Total registered users in the config store (emitted periodically by background service). |

### 4.5 Custom Events (via TelemetryClient.TrackEvent)

#### AuthenticationSignIn

| Property | Type | Description |
|---|---|---|
| `provider` | string | Authentication provider name. |
| `userId` | string | SHA-256 hash of the user identity key. |

#### AuthenticationSignOut

| Property | Type | Description |
|---|---|---|
| `userId` | string | SHA-256 hash of the user identity key. |

#### ConnectorFetch

| Property | Type | Description |
|---|---|---|
| `provider` | string | Connector ProviderKey. |
| `connectionId` | string | Connector instance ID. |
| `userId` | string | SHA-256 hash of the user identity key. |
| `status` | string | `success` or `error`. |
| `itemCount` | int | Number of work items returned. |
| `durationMs` | double | Elapsed time in milliseconds. |

#### ConfigurationSaved

| Property | Type | Description |
|---|---|---|
| `userId` | string | SHA-256 hash of the user identity key. |
| `connectorCount` | int | Total number of enabled connector instances. |

#### LinkedAccountAdded

| Property | Type | Description |
|---|---|---|
| `userId` | string | SHA-256 hash of the user identity key. |

#### LinkedAccountRemoved

| Property | Type | Description |
|---|---|---|
| `userId` | string | SHA-256 hash of the user identity key. |

### 4.6 ITelemetryInitializer for User Context

```csharp
namespace PriorityHub.Api.Services.Telemetry;

/// <summary>
/// Enriches every outgoing telemetry item with the authenticated user's
/// hashed identity and authentication provider. Reads from HttpContext.User.
/// </summary>
public sealed class PriorityHubTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PriorityHubTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        var identityKey = GetUserIdentityKey(user);
        var hashedUserId = HashUserId(identityKey);
        var provider = user.FindFirstValue("provider") ?? "unknown";

        telemetry.Context.User.AuthenticatedUserId = hashedUserId;
        if (telemetry is ISupportProperties propTelemetry)
        {
            propTelemetry.Properties["authProvider"] = provider;
        }
    }
}
```

### 4.7 DI Registration Extension

```csharp
namespace PriorityHub.Api.Extensions;

public static class TelemetryServiceExtensions
{
    /// <summary>
    /// Registers Application Insights telemetry services when a connection string is configured.
    /// When no connection string is present, registers a no-op ITelemetryService.
    /// </summary>
    public static IServiceCollection AddPriorityHubTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["ApplicationInsights:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // Register Application Insights SDK with automatic request and dependency tracking.
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = connectionString;
                options.EnableAdaptiveSampling = true;
            });

            // Register the ITelemetryInitializer for user context enrichment.
            services.AddSingleton<ITelemetryInitializer, PriorityHubTelemetryInitializer>();

            // Register the live telemetry service backed by TelemetryClient.
            services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();
        }
        else
        {
            // No-op: application runs without Application Insights.
            services.AddSingleton<ITelemetryService, NoOpTelemetryService>();
        }

        // Always register the active-user tracker (used by ITelemetryService implementations).
        services.AddSingleton<IActiveUserTracker, InMemoryActiveUserTracker>();

        // Background service for periodic metric emission.
        services.AddHostedService<TelemetryMetricsBackgroundService>();

        return services;
    }
}
```

### 4.8 Registered-User Count Provider

For the `IConfigStore` implementations that support listing all users:

```csharp
namespace PriorityHub.Api.Services;

/// <summary>
/// Optional interface for config stores that can report the total number of registered users.
/// </summary>
public interface IConfigStoreUserCounter
{
    /// <summary>Returns the total count of users with saved configurations.</summary>
    Task<int> GetRegisteredUserCountAsync(CancellationToken cancellationToken);
}
```

- `PostgresConfigStore` implements this by executing `SELECT COUNT(*) FROM user_config`.
- `LocalConfigStore` implements this by counting JSON files in the config directory.
- If the active `IConfigStore` does not implement `IConfigStoreUserCounter`, the registered-user gauge reports -1.

## 5. Acceptance Criteria

- **AC-001**: Given a valid `ApplicationInsights:ConnectionString` is configured, When the application starts, Then Application Insights telemetry collection is active and request telemetry appears in the Application Insights portal within 5 minutes.
- **AC-002**: Given `ApplicationInsights:ConnectionString` is empty or absent, When the application starts, Then the application starts successfully, no telemetry is exported, and no errors or warnings are logged about missing Application Insights configuration.
- **AC-003**: Given a user signs in via the Microsoft OAuth provider, When the sign-in completes, Then a `AuthenticationSignIn` custom event with `provider=microsoft` and a hashed `userId` appears in Application Insights `customEvents`.
- **AC-004**: Given a user signs in via the GitHub OAuth provider, When the sign-in completes, Then the `AuthSignInCount` metric is incremented with dimension `Provider=github`.
- **AC-005**: Given a connector fetch completes successfully returning 15 items in 320ms, When the `ConnectorFetch` event is emitted, Then the event properties include `status=success`, `itemCount=15`, `durationMs≈320`, and the `ConnectorFetchDuration` metric records the value.
- **AC-006**: Given a connector fetch fails with an exception, When the telemetry is emitted, Then `TelemetryClient.TrackException` is called with the exception and custom properties, a `ConnectorFetch` event with `status=error` and `itemCount=0` is emitted, and the `ConnectorFetchCount` metric is incremented with dimension `Status=error`.
- **AC-007**: Given 5 distinct users access the dashboard within the configured rolling window, When the `ActiveUsers` metric is observed, Then it reports 5.
- **AC-008**: Given the config store contains 42 registered users, When the background service refreshes, Then the `RegisteredUsers` metric reports 42.
- **AC-009**: Given a user's email is `user@example.com`, When telemetry is emitted, Then the `userId` property is the SHA-256 hex string of `user@example.com`, and the raw email does NOT appear anywhere in the telemetry payload.
- **AC-010**: Given the application is running with Application Insights enabled, When the administrator queries Azure Monitor Metrics, Then the custom metrics (`AuthSignInCount`, `ConnectorFetchCount`, `ConnectorFetchDuration`, `ActiveUsers`, `RegisteredUsers`) are available for charting and alerting.
- **AC-011**: Given a user signs out via `/api/auth/logout`, When the sign-out completes, Then an `AuthenticationSignOut` custom event with a hashed `userId` appears in Application Insights `customEvents`.
- **AC-012**: Given Application Insights is enabled, When the user loads the dashboard page, Then a page-view telemetry item for route `/` appears in the `pageViews` table.
- **AC-013**: Given Application Insights is enabled, When outgoing HTTP calls are made to connector APIs, Then dependency telemetry items appear automatically in the `dependencies` table with the target host and duration.
- **AC-014**: Given Application Insights is enabled, When the administrator opens the Live Metrics blade, Then real-time request rates, failure rates, and custom metrics are visible.
- **AC-015**: Given the `ITelemetryInitializer` is registered, When any authenticated request is processed, Then the telemetry item's `user_AuthenticatedId` field contains the hashed user ID and the `authProvider` custom property is set.
- **AC-016**: Given a user saves their configuration via `PUT /api/config`, When the save completes, Then a `ConfigurationSaved` custom event appears with `connectorCount` reflecting the total enabled connectors.
- **AC-017**: Given a user links a Microsoft account, When the link completes, Then a `LinkedAccountAdded` custom event appears with a hashed `userId`.
- **AC-018**: Given a user unlinks a Microsoft account, When the delete completes, Then a `LinkedAccountRemoved` custom event appears with a hashed `userId`.

## 6. Test Automation Strategy

- **Test Levels**: Unit tests for all telemetry service implementations; integration tests for DI registration and conditional enablement.
- **Frameworks**: MSTest (existing project convention), FluentAssertions for readable assertions, Moq for mocking `TelemetryClient`, `IConfigStore`, and verifying method calls.
- **Unit Tests** (`backend/PriorityHub.Api.Tests/`):
  - `TelemetryServiceTests.cs` — Verify `RecordSignIn` calls `TelemetryClient.TrackEvent("AuthenticationSignIn")` with correct properties and `GetMetric("AuthSignInCount", "Provider").TrackValue()`. Verify `RecordSignOut` emits `AuthenticationSignOut`. Verify PII hashing.
  - `TelemetryServiceTests.cs` — Verify `RecordConnectorFetch` calls `TrackEvent("ConnectorFetch")` and `GetMetric` for count, duration, items. Verify `RecordConnectorException` calls `TrackException`. Verify `RecordPageView` calls `TrackPageView`. Verify `RecordConfigSave` and `RecordLinkedAccountOperation` emit correct events.
  - `ActiveUserTrackerTests.cs` — Verify distinct-user counting, rolling-window expiry, and max-entry pruning.
  - `TelemetryServiceExtensionsTests.cs` — Verify that `AddPriorityHubTelemetry` registers `NoOpTelemetryService` when connection string is empty, and `ApplicationInsightsTelemetryService` + `ITelemetryInitializer` when present.
  - `PriorityHubTelemetryInitializerTests.cs` — Verify `Initialize` sets `Context.User.AuthenticatedUserId` to hashed value and adds `authProvider` property. Verify no PII is passed through.
  - `RegisteredUserCountTests.cs` — Verify `IConfigStoreUserCounter` implementations return correct counts.
- **Test Data Management**: Use in-memory config stores and mock dependencies. No external services required. Use `TelemetryConfiguration.CreateDefault()` with `InMemoryChannel` for testing `TelemetryClient` calls.
- **CI/CD Integration**: Tests run as part of existing `dotnet test PriorityHub.sln` pipeline. No additional CI configuration required.
- **Coverage Requirements**: All public methods on `ITelemetryService`, `IActiveUserTracker`, `IConfigStoreUserCounter`, and `PriorityHubTelemetryInitializer` must have corresponding unit tests.
- **Performance Testing**: Not in scope for initial implementation. The telemetry overhead should be validated manually by comparing dashboard load times with and without telemetry enabled.

## 7. Rationale & Context

### Why Application Insights SDK directly?

The Application Insights SDK (`Microsoft.ApplicationInsights.AspNetCore`) provides the fullest feature set for a .NET application targeting Azure:
- **`TelemetryClient`** — A single, well-documented API for emitting custom events, pre-aggregated metrics, exceptions, page views, and dependencies. No separate Meter/ActivitySource setup required.
- **Automatic request tracking** — All incoming HTTP requests (API endpoints, Blazor hub connections) are tracked without additional code.
- **Automatic dependency tracking** — All outgoing HTTP calls to connector APIs (Azure DevOps, GitHub, Jira, Trello, Microsoft Graph) appear in the Application Map with latency and failure data, enabling connector health monitoring without custom instrumentation.
- **`ITelemetryInitializer`** — Enriches every telemetry item with user context (hashed user ID, authentication provider), ensuring consistent user attribution across all telemetry types without per-call boilerplate.
- **Live Metrics Stream** — Real-time request rates, failure rates, and custom metrics visible in the Azure portal with ~1-second latency.
- **Application Map** — Automatic visualization of the application's dependencies on external connector APIs.
- **Smart Detection** — Automatic anomaly detection on request failures, dependency failures, and response times.
- **Pre-aggregated metrics via `GetMetric()`** — More efficient than individual `TrackMetric()` calls; supports multi-dimensional analysis in Azure Monitor Metrics Explorer.
- **Adaptive sampling** — Built-in sampling to control telemetry volume and cost.

### Why not OpenTelemetry-first?

While OpenTelemetry provides vendor-neutral abstractions, the Application Insights SDK offers richer features (Live Metrics, Application Map, Smart Detection, `ITelemetryInitializer`) that would require significant custom work to replicate with OpenTelemetry. Since the target backend is Application Insights, using the SDK directly provides the best developer experience and the most comprehensive monitoring capabilities. If a future migration to a different backend is needed, the `ITelemetryService` abstraction allows swapping the implementation without changing call sites.

### Why hash user IDs?

GDPR and privacy best practices require minimizing PII in telemetry. Hashing the user identity key with SHA-256 provides a consistent, deterministic identifier for counting and correlation without exposing email addresses or usernames.

### Why a background service for periodic metrics?

Periodic metrics (active users, registered users) are point-in-time values. Reporting them on every request would be wasteful and could create performance issues. A background service that periodically calls `TelemetryClient.GetMetric().TrackValue()` ensures these values are reported at a controlled interval.

### Why conditional enablement?

Priority Hub supports local file-based deployments where Application Insights is not available. Making the telemetry opt-in (via connection string presence) preserves the zero-dependency local development experience.

### Why track page views, config saves, and linked accounts?

These additional events provide a complete picture of application usage beyond authentication and connector activity:
- **Page views** — Show which features users actually navigate to and how frequently.
- **Configuration saves** — Indicate user engagement with connector setup and changes.
- **Linked account operations** — Track adoption of the multi-account Microsoft linking feature.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: Azure Application Insights — Receives and stores all exported telemetry (requests, dependencies, exceptions, custom events, custom metrics, page views). Requires an Application Insights resource provisioned in an Azure subscription.

### Third-Party Services
- **SVC-001**: Application Insights ingestion endpoint — The application sends telemetry via HTTPS to the endpoint specified in the connection string. Availability is governed by Azure's SLA for Application Insights (99.9%).
- **SVC-002**: Live Metrics Stream endpoint — The Application Insights SDK maintains a persistent connection for real-time metrics. Endpoint: `rt.services.visualstudio.com`.

### Infrastructure Dependencies
- **INF-001**: Outbound HTTPS connectivity — The application host must be able to reach `*.in.applicationinsights.azure.com` and `rt.services.visualstudio.com` on port 443. Deployments behind restrictive firewalls must allowlist these endpoints.

### Data Dependencies
- **DAT-001**: `IConfigStore` — The registered-user count depends on the active config store implementation (`LocalConfigStore` or `PostgresConfigStore`).

### Technology Platform Dependencies
- **PLT-001**: .NET 10 SDK — Required by the project. The `Microsoft.ApplicationInsights.AspNetCore` package must support .NET 10.
- **PLT-002**: `Microsoft.ApplicationInsights.AspNetCore` NuGet package — The Application Insights SDK providing `TelemetryClient`, automatic request/dependency tracking, `ITelemetryInitializer`, adaptive sampling, and Live Metrics.
- **PLT-003**: `Microsoft.ApplicationInsights` NuGet package — Core library (transitive dependency of PLT-002) providing `TelemetryClient`, `ITelemetryInitializer`, and telemetry data model.

### Compliance Dependencies
- **COM-001**: GDPR / Privacy — All user-identifying information must be hashed before inclusion in telemetry. No raw PII in Application Insights. The `ITelemetryInitializer` must use hashed user IDs exclusively.

## 9. Examples & Edge Cases

### Example: Authentication sign-in telemetry emission

```csharp
// In the OnCreatingTicket event for each OAuth provider:
OnCreatingTicket = async context =>
{
    // ... existing claim population ...
    context.Identity?.AddClaim(new Claim("provider", "microsoft"));

    // Emit sign-in telemetry via TelemetryClient
    var telemetry = context.HttpContext.RequestServices.GetService<ITelemetryService>();
    var userId = GetUserIdentityKey(new ClaimsPrincipal(context.Identity!));
    telemetry?.RecordSignIn("microsoft", userId);
};
```

### Example: Sign-out telemetry emission

```csharp
app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    var telemetry = httpContext.RequestServices.GetService<ITelemetryService>();
    var userId = GetUserIdentityKey(httpContext.User);
    telemetry?.RecordSignOut(userId);

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();
```

### Example: Connector fetch instrumentation in DashboardAggregator

```csharp
// In BuildPendingFetches, wrap the fetch task with timing and telemetry:
var stopwatch = Stopwatch.StartNew();
var fetchTask = connector.FetchConnectionAsync(connectionConfig, oauthToken, cancellationToken);
var instrumentedTask = fetchTask.ContinueWith(t =>
{
    stopwatch.Stop();
    if (t.IsCompletedSuccessfully)
    {
        var result = t.Result;
        _telemetryService.RecordConnectorFetch(
            connector.ProviderKey, id, userId,
            result.WorkItems.Count, stopwatch.Elapsed.TotalMilliseconds, success: true);
        return result;
    }
    else
    {
        _telemetryService.RecordConnectorException(
            t.Exception!.InnerException ?? t.Exception, connector.ProviderKey, id, userId);
        _telemetryService.RecordConnectorFetch(
            connector.ProviderKey, id, userId,
            0, stopwatch.Elapsed.TotalMilliseconds, success: false);
        throw t.Exception!;
    }
}, TaskScheduler.Default);
```

### Example: TelemetryClient usage in ApplicationInsightsTelemetryService

```csharp
public sealed class ApplicationInsightsTelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IActiveUserTracker _activeUserTracker;

    public ApplicationInsightsTelemetryService(
        TelemetryClient telemetryClient,
        IActiveUserTracker activeUserTracker)
    {
        _telemetryClient = telemetryClient;
        _activeUserTracker = activeUserTracker;
    }

    public void RecordSignIn(string provider, string userIdentityKey)
    {
        var hashedUserId = HashUserId(userIdentityKey);
        _activeUserTracker.RecordActivity(hashedUserId);

        _telemetryClient.TrackEvent("AuthenticationSignIn", new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["userId"] = hashedUserId
        });

        _telemetryClient.GetMetric("AuthSignInCount", "Provider").TrackValue(1, provider);
    }

    public void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey)
    {
        var hashedUserId = HashUserId(userIdentityKey);
        _telemetryClient.TrackException(exception, new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["connectionId"] = connectionId,
            ["userId"] = hashedUserId
        });
    }

    public void RecordPageView(string pageName, string? userIdentityKey)
    {
        var pageView = new PageViewTelemetry(pageName);
        if (userIdentityKey is not null)
        {
            pageView.Properties["userId"] = HashUserId(userIdentityKey);
        }
        _telemetryClient.TrackPageView(pageView);
    }

    // ... other methods follow the same pattern ...
}
```

### Example: SHA-256 hashing for user identity

```csharp
using System.Security.Cryptography;
using System.Text;

public static string HashUserId(string userIdentityKey)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userIdentityKey));
    return Convert.ToHexStringLower(bytes);
}
```

### Example: Configuration save telemetry emission

```csharp
app.MapPut("/api/config", async (HttpContext httpContext, ProviderConfiguration configuration,
    IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    await configStore.SaveAsync(userId, configuration, cancellationToken);
    var saved = await configStore.LoadAsync(userId, cancellationToken);

    // Emit configuration save telemetry
    var telemetry = httpContext.RequestServices.GetService<ITelemetryService>();
    var enabledCount = CountEnabledConnectors(saved);
    telemetry?.RecordConfigSave(userId, enabledCount);

    return Results.Ok(saved);
}).RequireAuthorization();
```

### Example: Querying telemetry in Application Insights (Kusto)

```kusto
// Authentication sign-ins by provider in the last 7 days
customEvents
| where name == "AuthenticationSignIn"
| where timestamp > ago(7d)
| summarize count() by tostring(customDimensions.provider)
| render piechart

// Connector fetch success rate by provider
customEvents
| where name == "ConnectorFetch"
| where timestamp > ago(24h)
| summarize
    total = count(),
    successes = countif(customDimensions.status == "success"),
    failures = countif(customDimensions.status == "error")
    by tostring(customDimensions.provider)
| extend successRate = round(100.0 * successes / total, 1)

// Distinct active users over time
customMetrics
| where name == "ActiveUsers"
| where timestamp > ago(7d)
| summarize avg(value) by bin(timestamp, 1h)
| render timechart

// Connector fetch exceptions by provider
exceptions
| where timestamp > ago(24h)
| where customDimensions.provider != ""
| summarize count() by tostring(customDimensions.provider), type
```

### Edge Cases

1. **No authentication provider claim**: If `context.Identity` is null or missing the `provider` claim during sign-in, the telemetry service should log a warning and skip the event emission. It MUST NOT throw.
2. **Connector fetch timeout**: If a connector fetch is cancelled via `CancellationToken`, it should be recorded as `status=error` with whatever duration elapsed, and `TrackException` should be called with the `OperationCanceledException`.
3. **Config store unavailable**: If `IConfigStoreUserCounter.GetRegisteredUserCountAsync` throws, the background service should log the error and report -1 for the metric. It MUST NOT crash.
4. **Active-user tracker overflow**: When the tracker reaches `MaxActiveUserEntries`, it should evict the oldest entries (by last-seen timestamp) to make room for new entries.
5. **Multiple sign-ins by same user**: Each sign-in is counted individually in the metric. The active-user tracker updates the last-seen timestamp but does not double-count the user.
6. **Application restart**: The in-memory active-user tracker resets on restart. This is acceptable because the metrics represent a best-effort snapshot. The counter metrics are cumulative and persisted in Application Insights.
7. **TelemetryClient not available**: When `ApplicationInsights:ConnectionString` is absent, `NoOpTelemetryService` is registered. All `RecordXxx` methods are no-ops. The application functions identically, just without telemetry export.
8. **Blazor SignalR hub telemetry**: The Application Insights SDK automatically tracks SignalR hub connections as request telemetry. No additional instrumentation is needed for Blazor Server circuit management.
9. **Adaptive sampling**: The Application Insights SDK applies adaptive sampling by default. High-volume custom events (e.g., `ConnectorFetch` for users with many connectors) may be sampled. Pre-aggregated metrics via `GetMetric()` are NOT subject to sampling, ensuring accurate counts.

## 10. Validation Criteria

1. **Build validation**: `dotnet build PriorityHub.sln` succeeds with no warnings related to the telemetry integration.
2. **Test validation**: `dotnet test PriorityHub.sln` passes all existing and new tests.
3. **No-op validation**: Remove or empty `ApplicationInsights:ConnectionString`, start the application, sign in, load dashboard — verify no errors in logs and application functions normally.
4. **Telemetry validation**: With a valid connection string, perform sign-in, dashboard load, config save, linked account link/unlink, and sign-out. Verify in Application Insights:
   - `requests` table contains API endpoint request telemetry with user context.
   - `dependencies` table contains outgoing HTTP calls to connector APIs.
   - `customEvents` contains `AuthenticationSignIn`, `AuthenticationSignOut`, `ConnectorFetch`, `ConfigurationSaved`, `LinkedAccountAdded`, and `LinkedAccountRemoved` records.
   - `pageViews` contains page-view records for Blazor pages.
   - `exceptions` contains any connector fetch exceptions with provider properties.
   - `customMetrics` shows `AuthSignInCount`, `ConnectorFetchCount`, `ConnectorFetchDuration`, `ConnectorItemsCount`, `ActiveUsers`, and `RegisteredUsers`.
   - Live Metrics Stream shows real-time request and metric data.
   - Application Map shows the application and its connector API dependencies.
5. **PII validation**: Inspect all telemetry payloads in Application Insights — verify no raw email addresses, display names, or other PII appears in any event property, metric dimension, request telemetry, dependency telemetry, or log message. Verify `user_AuthenticatedId` contains only hashed values.
6. **Performance validation**: Dashboard load time with telemetry enabled should not exceed 110% of baseline load time without telemetry (measured over 10 consecutive loads).
7. **ITelemetryInitializer validation**: Verify that all request telemetry items for authenticated endpoints contain `user_AuthenticatedId` (hashed) and `authProvider` custom property.

## 11. Related Specifications / Further Reading

- [Application Insights for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
- [Application Insights SDK for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core#enable-application-insights-server-side-telemetry)
- [TelemetryClient API reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.applicationinsights.telemetryclient)
- [Pre-aggregated metrics with GetMetric()](https://learn.microsoft.com/en-us/azure/azure-monitor/app/get-metric)
- [Custom events and metrics](https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)
- [ITelemetryInitializer](https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-filtering-sampling#addmodify-properties-itelemetryinitializer)
- [Live Metrics Stream](https://learn.microsoft.com/en-us/azure/azure-monitor/app/live-stream)
- [Application Map](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-map)
- [Adaptive sampling](https://learn.microsoft.com/en-us/azure/azure-monitor/app/sampling)
- [GDPR and Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/personal-data-mgmt)
- [Kusto Query Language (KQL) for Application Insights](https://learn.microsoft.com/en-us/azure/data-explorer/kusto/query/)
- [Priority Hub multi-source email aggregation spec](plans/specifications/spec-62-spec-multi-source-email-aggregation-with-credential-encryption.md)


## Clarifications
- [ ] Confirm assumptions before planning if anything is unclear.
