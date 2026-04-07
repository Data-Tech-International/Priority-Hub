---
title: Azure Monitor Telemetry for Application Usage Tracking
version: 1.0
date_created: 2026-04-07
owner: Priority Hub Team
tags: infrastructure, telemetry, observability, azure-monitor
---

# Introduction

This specification defines the integration of Azure Monitor (Application Insights) into Priority Hub to provide administrators with centralized, real-time visibility into application usage. The telemetry covers authentication provider usage, connector activity, distinct-user counts, and total registered-user counts. These metrics enable capacity planning, provider-adoption analysis, and operational monitoring without requiring custom dashboards or manual log analysis.

## 1. Purpose & Scope

**Purpose**: Enable Priority Hub administrators to track application usage through Azure Monitor, capturing structured telemetry about authentication providers, connector usage, user activity, and registration counts.

**Scope**:
- Instrument the PriorityHub.Ui host process (Blazor Server + ASP.NET Core BFF) with the Azure Monitor OpenTelemetry Distro.
- Emit custom metrics and events for authentication sign-ins, connector fetches, distinct active users, and total registered users.
- Support configuration-driven enablement so that Azure Monitor remains optional and does not affect deployments that do not use it.

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
| **OpenTelemetry** | An open-source observability framework for generating, collecting, and exporting telemetry data (traces, metrics, logs). |
| **Azure Monitor OpenTelemetry Distro** | The `Azure.Monitor.OpenTelemetry.AspNetCore` NuGet package that configures OpenTelemetry exporters for Azure Monitor. |
| **Custom Metric** | A numeric measurement (counter, gauge, histogram) emitted by application code and exported to Azure Monitor Metrics. |
| **Custom Event** | A named telemetry record with properties, exported to the `customEvents` table in Application Insights. |
| **Authentication Provider** | An OAuth identity provider used for sign-in (Microsoft, GitHub, Google, Facebook, Jira, Trello, Yandex). |
| **Connector** | A `IConnector` implementation that fetches work items from an external source (azure-devops, github, jira, trello, microsoft-tasks, outlook-flagged-mail, imap-flagged-mail). |
| **Connector Instance** | A single configured connection within a connector type (e.g., one Azure DevOps connection targeting a specific project). |
| **Distinct User** | A unique user identified by the `GetUserIdentityKey()` value (email or `{sub}_{provider}`). |
| **Registered User** | A user who has at least one persisted configuration entry in the `IConfigStore`. |
| **BFF** | Backend for Frontend — the architectural pattern where PriorityHub.Ui hosts both Blazor Server UI and API endpoints in a single process. |

## 3. Requirements, Constraints & Guidelines

### Telemetry Infrastructure

- **REQ-001**: The application MUST integrate Azure Monitor using the `Azure.Monitor.OpenTelemetry.AspNetCore` distro package.
- **REQ-002**: Telemetry MUST be enabled only when a valid `AzureMonitor:ConnectionString` configuration value is present. When absent, the application MUST start and function normally without Azure Monitor.
- **REQ-003**: All custom metrics MUST be emitted via the `System.Diagnostics.Metrics` API using a dedicated `Meter` named `PriorityHub`.
- **REQ-004**: All custom events MUST be emitted via the `System.Diagnostics.ActivitySource` API or `TelemetryClient.TrackEvent` using structured properties.

### Authentication Tracking

- **REQ-010**: The application MUST emit a custom event `AuthenticationSignIn` each time a user successfully completes an OAuth sign-in flow.
- **REQ-011**: The `AuthenticationSignIn` event MUST include the following properties:
  - `provider` — the authentication provider name (e.g., `microsoft`, `github`, `google`, `facebook`, `jira`, `trello`, `yandex`).
  - `userId` — the anonymized or hashed user identity key (NOT the raw email or PII).
- **REQ-012**: The application MUST emit a counter metric `priorityhub.auth.signin.count` incremented on each successful sign-in, tagged with `provider`.
- **REQ-013**: The application MUST NOT log or emit raw email addresses, display names, or other PII into telemetry. User identity MUST be hashed (SHA-256 of the `GetUserIdentityKey()` value) before inclusion in any telemetry property.

### Connector Usage Tracking

- **REQ-020**: The application MUST emit a custom event `ConnectorFetch` each time a connector instance completes a fetch operation (success or failure).
- **REQ-021**: The `ConnectorFetch` event MUST include the following properties:
  - `provider` — the connector's `ProviderKey` (e.g., `azure-devops`, `github`).
  - `connectionId` — the connector instance ID.
  - `status` — `success` or `error`.
  - `itemCount` — the number of work items returned (0 on error).
  - `durationMs` — the elapsed time in milliseconds for the fetch operation.
  - `userId` — the hashed user identity key.
- **REQ-022**: The application MUST emit a counter metric `priorityhub.connector.fetch.count` incremented on each fetch, tagged with `provider` and `status`.
- **REQ-023**: The application MUST emit a histogram metric `priorityhub.connector.fetch.duration` recording the fetch duration in milliseconds, tagged with `provider`.
- **REQ-024**: The application MUST emit a counter metric `priorityhub.connector.items.count` recording the number of items fetched, tagged with `provider`.

### User Counting

- **REQ-030**: The application MUST emit a gauge metric `priorityhub.users.active` reporting the number of distinct users who have accessed the dashboard within a configurable rolling window (default: 24 hours).
- **REQ-031**: The application MUST emit a gauge metric `priorityhub.users.registered` reporting the total number of users with persisted configurations in the `IConfigStore`.
- **REQ-032**: The registered-user count MUST be refreshed periodically (default interval: 5 minutes) via a background service, not on every request.
- **REQ-033**: The active-user tracking MUST use an in-memory data structure (e.g., `ConcurrentDictionary<string, DateTimeOffset>`) to track distinct hashed user IDs and their last-seen timestamps.

### Security

- **SEC-001**: The Azure Monitor connection string MUST be treated as a secret and MUST NOT be committed to source control. It MUST be provided via environment variables, Azure App Configuration, or `appsettings.{Environment}.json` files excluded from version control.
- **SEC-002**: No PII (email, name, IP address) MUST appear in custom events, custom metrics tags, or log messages exported to Azure Monitor. Only hashed user identifiers are permitted.
- **SEC-003**: The telemetry service MUST NOT interfere with the application's authentication or authorization mechanisms.

### Constraints

- **CON-001**: The telemetry integration MUST NOT introduce a hard dependency on Azure Monitor. The application MUST compile and run without an Azure subscription.
- **CON-002**: The telemetry background service for user counting MUST NOT query the `IConfigStore` more frequently than once per minute to avoid performance impact.
- **CON-003**: The in-memory active-user tracker MUST be bounded to a maximum of 100,000 entries to prevent unbounded memory growth.
- **CON-004**: The telemetry integration MUST target .NET 10 and use the same SDK version as the rest of the project.

### Guidelines

- **GUD-001**: Follow the existing DI-first pattern. Register all telemetry services in the DI container via an extension method (e.g., `AddPriorityHubTelemetry()`).
- **GUD-002**: Place telemetry service code in `backend/PriorityHub.Api/Services/Telemetry/` to keep it separate from business logic.
- **GUD-003**: Place the telemetry DI registration extension in `backend/PriorityHub.Api/Extensions/TelemetryServiceExtensions.cs`.
- **GUD-004**: Use the existing `ILogger<T>` for internal diagnostic logging of the telemetry subsystem itself; do not introduce a separate logging framework.
- **GUD-005**: Prefer OpenTelemetry-native APIs (`System.Diagnostics.Metrics`, `System.Diagnostics.ActivitySource`) over Application Insights SDK-specific APIs for custom metrics and traces, to remain vendor-neutral where possible.

### Patterns

- **PAT-001**: Emit telemetry at the point of action completion, not at the start. For connector fetches, emit after the `FetchConnectionAsync` call completes. For sign-ins, emit in the `OnCreatingTicket` event after claims are populated.
- **PAT-002**: Use a decorator or middleware pattern for connector fetch instrumentation to avoid modifying each connector's implementation directly. Instrument in `DashboardAggregator.BuildPendingFetches()` where fetch tasks are created.
- **PAT-003**: Use a `BackgroundService` for periodic gauge-metric emission (registered users, active-user gauge reporting).

## 4. Interfaces & Data Contracts

### 4.1 Configuration Schema

Add the following section to `appsettings.json`:

```json
{
  "AzureMonitor": {
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
| `AzureMonitor:ConnectionString` | string | `""` (empty) | Application Insights connection string. When empty, Azure Monitor telemetry is disabled. |
| `Telemetry:ActiveUserWindowMinutes` | int | `1440` | Rolling window in minutes for counting distinct active users. |
| `Telemetry:RegisteredUserRefreshIntervalSeconds` | int | `300` | Interval in seconds between registered-user count refreshes. |
| `Telemetry:MaxActiveUserEntries` | int | `100000` | Maximum number of tracked active-user entries before pruning. |

### 4.2 Telemetry Service Interface

```csharp
namespace PriorityHub.Api.Services.Telemetry;

/// <summary>
/// Provides methods for recording application-level telemetry events and metrics.
/// </summary>
public interface ITelemetryService
{
    /// <summary>Records a successful authentication sign-in.</summary>
    /// <param name="provider">The authentication provider name (e.g., "microsoft").</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordSignIn(string provider, string userIdentityKey);

    /// <summary>Records a connector fetch completion.</summary>
    /// <param name="provider">The connector's ProviderKey.</param>
    /// <param name="connectionId">The connector instance ID.</param>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    /// <param name="itemCount">Number of work items returned.</param>
    /// <param name="durationMs">Elapsed time in milliseconds.</param>
    /// <param name="success">Whether the fetch succeeded.</param>
    void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success);

    /// <summary>Records a user accessing the dashboard (for active-user tracking).</summary>
    /// <param name="userIdentityKey">The raw user identity key (will be hashed internally).</param>
    void RecordUserActivity(string userIdentityKey);
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

### 4.4 Custom Metrics

| Metric Name | Type | Unit | Tags | Description |
|---|---|---|---|---|
| `priorityhub.auth.signin.count` | Counter | `{sign_in}` | `provider` | Total authentication sign-ins. |
| `priorityhub.connector.fetch.count` | Counter | `{fetch}` | `provider`, `status` | Total connector fetch operations. |
| `priorityhub.connector.fetch.duration` | Histogram | `ms` | `provider` | Duration of connector fetch operations. |
| `priorityhub.connector.items.count` | Counter | `{item}` | `provider` | Total work items fetched. |
| `priorityhub.users.active` | ObservableGauge | `{user}` | (none) | Distinct active users in the rolling window. |
| `priorityhub.users.registered` | ObservableGauge | `{user}` | (none) | Total registered users in the config store. |

### 4.5 Custom Events

#### AuthenticationSignIn

| Property | Type | Description |
|---|---|---|
| `provider` | string | Authentication provider name. |
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

### 4.6 DI Registration Extension

```csharp
namespace PriorityHub.Api.Extensions;

public static class TelemetryServiceExtensions
{
    /// <summary>
    /// Registers Azure Monitor telemetry services when a connection string is configured.
    /// When no connection string is present, registers a no-op telemetry service.
    /// </summary>
    public static IServiceCollection AddPriorityHubTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Implementation registers ITelemetryService, IActiveUserTracker,
        // BackgroundService for gauge reporting, and Azure Monitor exporter.
    }
}
```

### 4.7 Registered-User Count Provider

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

- **AC-001**: Given a valid `AzureMonitor:ConnectionString` is configured, When the application starts, Then Application Insights telemetry collection is active and request telemetry appears in the Application Insights portal within 5 minutes.
- **AC-002**: Given `AzureMonitor:ConnectionString` is empty or absent, When the application starts, Then the application starts successfully, no telemetry is exported, and no errors or warnings are logged about missing Azure Monitor configuration.
- **AC-003**: Given a user signs in via the Microsoft OAuth provider, When the sign-in completes, Then a `AuthenticationSignIn` custom event with `provider=microsoft` and a hashed `userId` appears in Application Insights `customEvents`.
- **AC-004**: Given a user signs in via the GitHub OAuth provider, When the sign-in completes, Then the `priorityhub.auth.signin.count` metric is incremented with tag `provider=github`.
- **AC-005**: Given a connector fetch completes successfully returning 15 items in 320ms, When the `ConnectorFetch` event is emitted, Then the event properties include `status=success`, `itemCount=15`, `durationMs≈320`, and the `priorityhub.connector.fetch.duration` histogram records the value.
- **AC-006**: Given a connector fetch fails with an exception, When the `ConnectorFetch` event is emitted, Then `status=error`, `itemCount=0`, and the `priorityhub.connector.fetch.count` counter is incremented with tag `status=error`.
- **AC-007**: Given 5 distinct users access the dashboard within the configured rolling window, When the `priorityhub.users.active` gauge is observed, Then it reports 5.
- **AC-008**: Given the config store contains 42 registered users, When the background service refreshes, Then the `priorityhub.users.registered` gauge reports 42.
- **AC-009**: Given a user's email is `user@example.com`, When telemetry is emitted, Then the `userId` property is the SHA-256 hex string of `user@example.com`, and the raw email does NOT appear anywhere in the telemetry payload.
- **AC-010**: Given the application is running with Azure Monitor enabled, When the administrator queries Azure Monitor Metrics, Then the custom metrics prefixed with `priorityhub.` are available for charting and alerting.

## 6. Test Automation Strategy

- **Test Levels**: Unit tests for all telemetry service implementations; integration tests for DI registration and conditional enablement.
- **Frameworks**: MSTest (existing project convention), FluentAssertions for readable assertions, Moq for mocking `IConfigStore` and verifying method calls.
- **Unit Tests** (`backend/PriorityHub.Api.Tests/`):
  - `TelemetryServiceTests.cs` — Verify `RecordSignIn` increments the correct counter and emits the correct event properties. Verify PII hashing.
  - `ActiveUserTrackerTests.cs` — Verify distinct-user counting, rolling-window expiry, and max-entry pruning.
  - `TelemetryServiceExtensionsTests.cs` — Verify that `AddPriorityHubTelemetry` registers no-op when connection string is empty and real services when present.
  - `RegisteredUserCountTests.cs` — Verify `IConfigStoreUserCounter` implementations return correct counts.
- **Test Data Management**: Use in-memory config stores and mock dependencies. No external services required.
- **CI/CD Integration**: Tests run as part of existing `dotnet test PriorityHub.sln` pipeline. No additional CI configuration required.
- **Coverage Requirements**: All public methods on `ITelemetryService`, `IActiveUserTracker`, and `IConfigStoreUserCounter` must have corresponding unit tests.
- **Performance Testing**: Not in scope for initial implementation. The telemetry overhead should be validated manually by comparing dashboard load times with and without telemetry enabled.

## 7. Rationale & Context

### Why Azure Monitor?

Azure Monitor with Application Insights is the natural choice for a .NET application that may be deployed to Azure. The `Azure.Monitor.OpenTelemetry.AspNetCore` distro provides:
- Automatic collection of HTTP request telemetry, dependency tracking, and exceptions.
- Native support for `System.Diagnostics.Metrics` and `System.Diagnostics.ActivitySource`.
- Integration with Azure Monitor Metrics for alerting and Azure Dashboards for visualization.
- Minimal configuration (a single connection string).

### Why OpenTelemetry-native APIs?

Using `System.Diagnostics.Metrics` and `System.Diagnostics.ActivitySource` instead of Application Insights SDK-specific APIs keeps the code vendor-neutral. If the application later needs to export to Prometheus, Grafana, or another backend, only the exporter configuration changes — the instrumentation code remains the same.

### Why hash user IDs?

GDPR and privacy best practices require minimizing PII in telemetry. Hashing the user identity key with SHA-256 provides a consistent, deterministic identifier for counting and correlation without exposing email addresses or usernames.

### Why a background service for gauge metrics?

Gauge metrics (active users, registered users) are point-in-time values. Reporting them on every request would be wasteful and could create performance issues. A background service that periodically reports these values aligns with OpenTelemetry's `ObservableGauge` pattern, where a callback supplies the current value when the metric is observed by the exporter.

### Why conditional enablement?

Priority Hub supports local file-based deployments where Azure Monitor is not available. Making the telemetry opt-in (via connection string presence) preserves the zero-dependency local development experience.

## 8. Dependencies & External Integrations

### External Systems
- **EXT-001**: Azure Monitor / Application Insights — Receives and stores all exported telemetry. Requires an Application Insights resource provisioned in an Azure subscription.

### Third-Party Services
- **SVC-001**: Azure Monitor ingestion endpoint — The application sends telemetry via HTTPS to the endpoint specified in the connection string. Availability is governed by Azure's SLA for Application Insights (99.9%).

### Infrastructure Dependencies
- **INF-001**: Outbound HTTPS connectivity — The application host must be able to reach `*.in.applicationinsights.azure.com` on port 443. Deployments behind restrictive firewalls must allowlist this endpoint.

### Data Dependencies
- **DAT-001**: `IConfigStore` — The registered-user count depends on the active config store implementation (`LocalConfigStore` or `PostgresConfigStore`).

### Technology Platform Dependencies
- **PLT-001**: .NET 10 SDK — Required by the project. The `Azure.Monitor.OpenTelemetry.AspNetCore` package must support .NET 10.
- **PLT-002**: `Azure.Monitor.OpenTelemetry.AspNetCore` NuGet package — The distro package that configures OpenTelemetry with Azure Monitor exporters.
- **PLT-003**: `System.Diagnostics.DiagnosticSource` — Built into the .NET runtime; provides `Meter`, `Counter`, `Histogram`, `ObservableGauge`, and `ActivitySource` APIs.

### Compliance Dependencies
- **COM-001**: GDPR / Privacy — All user-identifying information must be hashed before inclusion in telemetry. No raw PII in Application Insights.

## 9. Examples & Edge Cases

### Example: Authentication sign-in telemetry emission

```csharp
// In the OnCreatingTicket event for each OAuth provider:
OnCreatingTicket = async context =>
{
    // ... existing claim population ...
    context.Identity?.AddClaim(new Claim("provider", "microsoft"));

    // Emit sign-in telemetry
    var telemetry = context.HttpContext.RequestServices.GetService<ITelemetryService>();
    var userId = GetUserIdentityKey(new ClaimsPrincipal(context.Identity!));
    telemetry?.RecordSignIn("microsoft", userId);
};
```

### Example: Connector fetch instrumentation in DashboardAggregator

```csharp
// In BuildPendingFetches, wrap the fetch task with timing and telemetry:
var stopwatch = Stopwatch.StartNew();
var fetchTask = connector.FetchConnectionAsync(connectionConfig, oauthToken, cancellationToken);
var instrumentedTask = fetchTask.ContinueWith(t =>
{
    stopwatch.Stop();
    var telemetry = /* resolve ITelemetryService */;
    if (t.IsCompletedSuccessfully)
    {
        var result = t.Result;
        telemetry?.RecordConnectorFetch(
            connector.ProviderKey, id, userId,
            result.WorkItems.Count, stopwatch.Elapsed.TotalMilliseconds, success: true);
        return result;
    }
    else
    {
        telemetry?.RecordConnectorFetch(
            connector.ProviderKey, id, userId,
            0, stopwatch.Elapsed.TotalMilliseconds, success: false);
        throw t.Exception!;
    }
}, TaskScheduler.Default);
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

### Edge Cases

1. **No authentication provider claim**: If `context.Identity` is null or missing the `provider` claim during sign-in, the telemetry service should log a warning and skip the event emission. It MUST NOT throw.
2. **Connector fetch timeout**: If a connector fetch is cancelled via `CancellationToken`, it should be recorded as `status=error` with whatever duration elapsed.
3. **Config store unavailable**: If `IConfigStoreUserCounter.GetRegisteredUserCountAsync` throws, the background service should log the error and report -1 for the gauge. It MUST NOT crash.
4. **Active-user tracker overflow**: When the tracker reaches `MaxActiveUserEntries`, it should evict the oldest entries (by last-seen timestamp) to make room for new entries.
5. **Multiple sign-ins by same user**: Each sign-in is counted individually in the counter metric. The active-user tracker updates the last-seen timestamp but does not double-count the user.
6. **Application restart**: The in-memory active-user tracker resets on restart. This is acceptable because the gauge represents a best-effort snapshot. The counter metrics are cumulative and persisted in Azure Monitor.

## 10. Validation Criteria

1. **Build validation**: `dotnet build PriorityHub.sln` succeeds with no warnings related to the telemetry integration.
2. **Test validation**: `dotnet test PriorityHub.sln` passes all existing and new tests.
3. **No-op validation**: Remove or empty `AzureMonitor:ConnectionString`, start the application, sign in, load dashboard — verify no errors in logs and application functions normally.
4. **Telemetry validation**: With a valid connection string, perform sign-in and dashboard load. Verify in Application Insights:
   - `customEvents` contains `AuthenticationSignIn` and `ConnectorFetch` records.
   - `customMetrics` shows `priorityhub.auth.signin.count`, `priorityhub.connector.fetch.count`, `priorityhub.connector.fetch.duration`, `priorityhub.users.active`, and `priorityhub.users.registered`.
5. **PII validation**: Inspect all telemetry payloads in Application Insights — verify no raw email addresses, display names, or other PII appears in any event property, metric tag, or log message.
6. **Performance validation**: Dashboard load time with telemetry enabled should not exceed 110% of baseline load time without telemetry (measured over 10 consecutive loads).

## 11. Related Specifications / Further Reading

- [Azure Monitor OpenTelemetry Distro for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)
- [System.Diagnostics.Metrics API](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)
- [Application Insights custom metrics](https://learn.microsoft.com/en-us/azure/azure-monitor/app/custom-metrics-overview)
- [Application Insights custom events](https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)
- [GDPR and Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/logs/personal-data-mgmt)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/dotnet/)
- [Priority Hub multi-source email aggregation spec](plans/specifications/spec-62-spec-multi-source-email-aggregation-with-credential-encryption.md)
