---
title: Connector Plugin System Architecture
version: 1.0
date_created: 2026-03-30
owner: Data-Tech-International
tags: [architecture, plugins, connectors, extensibility, design]
---

# Introduction

This specification defines two architecturally distinct approaches for enabling Priority-Hub connectors to be added, updated, and removed without rebuilding or redeploying the main application. The goal is to evolve the current compile-time–bound connector model into a runtime-extensible plugin system while preserving clean architecture principles and the existing `IConnector` contract.

## 1. Purpose & Scope

### Purpose

The current connector model requires every new connector to be implemented inside `PriorityHub.Api`, hardcoded in `Program.cs`, and shipped with the main application binary. This prevents third-party developers from contributing connectors and creates an unnecessary coupling between the application lifecycle and connector evolution.

This specification describes two approaches that decouple connector delivery from application deployment:

- **Approach A – In-Process Assembly Plugin Loading**: Connectors are packaged as standalone .NET class libraries and loaded into the host process at startup from a well-known directory, using .NET's `AssemblyLoadContext` mechanism.
- **Approach B – Out-of-Process Remote Connector Protocol**: Connectors run as independent HTTP services (microservices, containers, or sidecars) that the main application discovers and proxies through a standard REST adapter.

### Scope

This specification covers:
- The plugin contract and metadata format for both approaches.
- Discovery, loading, and lifecycle management for both approaches.
- Security and isolation constraints.
- DI integration with the existing `ConnectorRegistry` and `DashboardAggregator`.
- Acceptance criteria and test strategy.

### Out of Scope

- Changes to the `IConnector` interface itself (the contract is preserved as-is).
- Changes to `DashboardAggregator`, `IConfigStore`, or the Blazor UI layer.
- Specific connector implementations (Azure DevOps, GitHub, Jira, etc.).
- Authentication protocol changes.

### Intended Audience

Backend engineers, platform engineers, and open-source connector contributors.

---

## 2. Definitions

| Term | Definition |
|------|------------|
| **Connector** | A class implementing `IConnector` that fetches work items from a third-party system. |
| **Plugin** | A deployable unit (DLL or HTTP service) that packages one or more `IConnector` implementations. |
| **Plugin Host** | The `PriorityHub.Api` process that loads and executes plugins. |
| **ALC** | `AssemblyLoadContext` – a .NET runtime isolation boundary for loading assemblies without polluting the default context. |
| **ProviderKey** | The unique string identifier of a connector (e.g., `"github"`, `"jira"`). |
| **ConnectorRegistry** | The in-memory `ConnectorRegistry` class that holds all active `IConnector` instances. |
| **Remote Connector** | A connector that runs as a separate HTTP process and is proxied by a `RemoteConnectorAdapter` in the host. |
| **Plugin Manifest** | A JSON sidecar file that describes a plugin's identity, entry point, and capabilities. |
| **BFF** | Backend-For-Frontend – the ASP.NET Core host that serves both the API and the Blazor Server UI. |
| **DI** | Dependency Injection via `Microsoft.Extensions.DependencyInjection`. |
| **MEF** | Managed Extensibility Framework – a .NET composition framework (not used here; superseded by ALC). |
| **SDK** | Software Development Kit – the NuGet package(s) a connector author references to build a plugin. |

---

## 3. Requirements, Constraints & Guidelines

### Functional Requirements

- **REQ-001**: The host application MUST load connectors without recompilation or redeployment of `PriorityHub.Api`.
- **REQ-002**: Each loaded connector MUST be discoverable via `ConnectorRegistry.GetAll()` and `ConnectorRegistry.GetByKey(providerKey)` using the same API as built-in connectors.
- **REQ-003**: A plugin MUST expose the `IConnector` interface contract unchanged (`ProviderKey`, `DisplayName`, `Description`, `DefaultEmoji`, `ConfigFields`, `FetchConnectionAsync`).
- **REQ-004**: The host MUST support loading zero or more external plugins at startup. An empty plugin directory MUST NOT cause a startup failure.
- **REQ-005**: Each plugin MUST declare a unique `ProviderKey`. If two plugins claim the same key, the host MUST log a warning and skip the duplicate.
- **REQ-006**: Plugin load failures (bad DLL, unreachable HTTP endpoint) MUST be isolated: one failing plugin MUST NOT prevent other plugins or built-in connectors from operating.
- **REQ-007**: A plugin MUST be accompanied by a machine-readable manifest (JSON) that declares its identity and entry point.
- **REQ-008**: The host MUST expose a diagnostic endpoint (`GET /api/plugins`) that lists loaded connectors, their source (built-in vs. plugin), and load status.

### Security Requirements

- **SEC-001**: Plugin assemblies (Approach A) MUST be loaded from a directory that is configurable and restricted to the host process owner at the OS level.
- **SEC-002**: Remote connector endpoints (Approach B) MUST be called over HTTPS in non-development environments.
- **SEC-003**: Remote connector endpoints (Approach B) MUST support a shared-secret header (`X-Plugin-Secret`) for mutual authentication.
- **SEC-004**: Plugin assemblies MUST NOT be granted elevated permissions. The host MUST NOT grant plugins access to the DI container, configuration secrets, or internal services beyond what `IConnector` requires.
- **SEC-005**: `ProviderKey` values MUST be validated against the pattern `^[a-z0-9][a-z0-9-]{1,62}$` before registration to prevent injection or spoofing.
- **SEC-006**: HTTP responses from remote connectors (Approach B) MUST be size-bounded (configurable, default 10 MB) to prevent memory exhaustion.
- **SEC-007**: Plugin manifests MUST NOT be executed or interpreted as code; they are read-only metadata.

### Constraints

- **CON-001**: The `IConnector` interface MUST NOT be modified as part of implementing either approach. The interface is the stable plugin contract.
- **CON-002**: `DashboardAggregator` MUST NOT be aware of the plugin loading mechanism. It MUST continue to consume `ConnectorRegistry` unchanged.
- **CON-003**: The host process targets **.NET 10** and the plugin SDK MUST target a compatible TFM (`net10.0` or `netstandard2.1` for broader compatibility).
- **CON-004**: Built-in connectors registered via `AddHttpClient<T>()` in `Program.cs` MUST continue to function unchanged alongside any plugins.
- **CON-005**: Plugin loading MUST complete before the HTTP server starts accepting requests (i.e., at DI composition time, not lazily at first request).
- **CON-006**: Approach A (assembly loading) MUST use isolated `AssemblyLoadContext` instances – one per plugin – to prevent type-identity conflicts between plugins that ship different versions of the same dependency.

### Guidelines

- **GUD-001**: Prefer Approach A for deployments where connectors are developed internally and the plugin directory can be controlled by the operations team.
- **GUD-002**: Prefer Approach B for SaaS multi-tenant scenarios or where connectors are developed by untrusted third parties, as out-of-process isolation provides stronger fault and security boundaries.
- **GUD-003**: A connector author SHOULD distribute their plugin as a NuGet package that, when referenced, produces the correct DLL and manifest in an output folder.
- **GUD-004**: Log all plugin load events (`Information` for success, `Warning` for skips, `Error` for failures) using the existing `ILogger` infrastructure.
- **GUD-005**: Use `CancellationToken` propagation throughout; remote HTTP calls (Approach B) MUST respect the token passed to `FetchConnectionAsync`.

### Patterns

- **PAT-001**: The `PluginConnectorLoader` (Approach A) and `RemoteConnectorRegistrar` (Approach B) MUST act as infrastructure services that populate the `ConnectorRegistry` before it is used. Both integrate into the DI composition phase.
- **PAT-002**: Both approaches MUST implement a common `IPluginSource` interface inside the host to allow the diagnostic endpoint to enumerate plugins uniformly.
- **PAT-003**: Use the Options pattern (`IOptions<PluginOptions>`) to configure plugin directory paths and remote endpoint lists, avoiding hardcoded values.

---

## 4. Interfaces & Data Contracts

### 4.1 Stable Plugin Contract (Unchanged)

The `IConnector` interface defined in `PriorityHub.Api.Services.Connectors` is the authoritative plugin contract. Plugin authors reference only this interface and the shared models.

```csharp
// No changes. Reproduced here for reference.
public interface IConnector
{
    string ProviderKey { get; }
    string DisplayName { get; }
    string Description { get; }
    string DefaultEmoji { get; }
    ConnectorFieldSpec[] ConfigFields { get; }

    Task<ConnectorResult> FetchConnectionAsync(
        JsonElement connectionConfig,
        string? oauthToken,
        CancellationToken cancellationToken);
}
```

### 4.2 Plugin Manifest Schema (`plugin.json`)

Every plugin MUST ship a sidecar file named `plugin.json` in the same directory as its entry point. The manifest is read by the host but never executed.

```jsonc
{
  "$schema": "https://priorityhub.io/schemas/plugin-manifest/v1.json",
  "schemaVersion": "1",
  "pluginId": "my-org-my-connector",
  "displayName": "My Custom Connector",
  "version": "1.2.0",
  "author": "My Org <plugins@my-org.example>",
  "minHostVersion": "1.0.0",

  // Approach A only:
  "assembly": "MyOrg.MyConnector.dll",
  "connectorTypes": [
    "MyOrg.MyConnector.MyConnector"
  ],

  // Approach B only:
  "endpoint": "https://my-connector-service.example/api/connector",
  "providerKeys": ["my-connector"]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schemaVersion` | string | Yes | Always `"1"` for this version. |
| `pluginId` | string | Yes | Globally unique plugin identifier. Pattern: `^[a-z0-9][a-z0-9-]{1,62}$`. |
| `displayName` | string | Yes | Human-readable plugin name for diagnostics. |
| `version` | string | Yes | SemVer of the plugin. |
| `author` | string | No | Contact information. |
| `minHostVersion` | string | No | Minimum Priority-Hub version required. Host skips plugin if host version is lower. |
| `assembly` | string | Approach A | Filename of the entry-point DLL relative to `plugin.json`. |
| `connectorTypes` | string[] | Approach A | Fully qualified type names of `IConnector` implementations to instantiate. |
| `endpoint` | string | Approach B | Base URL of the remote connector HTTP service. |
| `providerKeys` | string[] | Approach B | `ProviderKey` values the remote service exposes (used for pre-registration validation). |

### 4.3 IPluginSource Interface (Host-Internal)

```csharp
/// <summary>Represents a source that contributes IConnector instances to the registry.</summary>
internal interface IPluginSource
{
    /// <summary>Source name for diagnostics (e.g., "assembly:my-connector.dll" or "remote:https://...").</summary>
    string SourceId { get; }

    /// <summary>Load or probe connectors from this source. Must be idempotent.</summary>
    Task<IReadOnlyList<IConnector>> LoadAsync(CancellationToken cancellationToken);
}
```

### 4.4 PluginOptions Configuration

```csharp
public sealed class PluginOptions
{
    public const string SectionName = "Plugins";

    /// <summary>Approach A: Path to the directory containing plugin subdirectories.</summary>
    public string PluginsDirectory { get; set; } = "plugins";

    /// <summary>Approach B: List of remote connector endpoint configurations.</summary>
    public List<RemoteConnectorOptions> RemoteConnectors { get; set; } = [];

    /// <summary>Maximum response body size in bytes for remote connectors (default 10 MB).</summary>
    public long RemoteConnectorMaxResponseBytes { get; set; } = 10 * 1024 * 1024;
}

public sealed class RemoteConnectorOptions
{
    public string PluginId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Shared secret sent as X-Plugin-Secret header.</summary>
    public string? Secret { get; set; }
}
```

Configured in `appsettings.json`:

```jsonc
{
  "Plugins": {
    "PluginsDirectory": "plugins",
    "RemoteConnectors": [
      {
        "PluginId": "my-remote-connector",
        "Endpoint": "https://my-connector.example/api/connector",
        "Secret": "<from-secrets-manager>"
      }
    ]
  }
}
```

### 4.5 Approach B – Remote Connector HTTP API Contract

The remote connector service MUST implement the following endpoints. The host calls these endpoints via `RemoteConnectorAdapter`.

#### `GET /api/connector/metadata`

Returns connector metadata.

**Response `200 OK`:**

```json
{
  "providerKey": "my-connector",
  "displayName": "My Connector",
  "description": "Fetches items from My System.",
  "defaultEmoji": "🔌",
  "configFields": [
    { "key": "baseUrl",   "label": "Base URL",  "inputKind": "text",     "required": true,  "defaultValue": null },
    { "key": "apiToken",  "label": "API Token", "inputKind": "password", "required": true,  "defaultValue": null }
  ]
}
```

#### `POST /api/connector/fetch`

Fetches work items for a single connection.

**Request body:**

```json
{
  "connectionConfig": { /* JsonElement – connector-specific config object */ },
  "oauthToken": "optional-bearer-token-or-null"
}
```

**Response `200 OK`:**

```json
{
  "boardConnections": [ /* BoardConnection[] */ ],
  "workItems":        [ /* WorkItem[] */ ],
  "issues":           [ /* ProviderIssue[] */ ]
}
```

**Error Response `4xx/5xx`:** The `RemoteConnectorAdapter` MUST treat HTTP error responses as a failed fetch, adding a `ProviderIssue` with the HTTP status and response body, and returning a partial `ConnectorResult`.

---

## 5. Acceptance Criteria

### General (Both Approaches)

- **AC-001**: Given no plugin directory and no remote connectors are configured, when the host starts, then only built-in connectors are registered and the application starts without errors.
- **AC-002**: Given two plugins claim the same `ProviderKey`, when the host loads plugins, then the second plugin is skipped, a warning is logged, and the first plugin is available.
- **AC-003**: Given a plugin fails to load (bad DLL / unreachable HTTP endpoint), when the host starts, then the failed plugin is skipped, an error is logged including the plugin's `pluginId`, and all other connectors remain available.
- **AC-004**: Given plugins are loaded, when `GET /api/plugins` is called, then the response lists all connectors with their `source` (`"built-in"` or `"plugin:<pluginId>"`), `providerKey`, `displayName`, and `status` (`"loaded"` or `"failed"`).
- **AC-005**: Given a plugin is loaded, when `ConnectorRegistry.GetAll()` is called, then the plugin's connector is included in the result alongside built-in connectors.
- **AC-006**: Given a plugin is loaded, when `DashboardAggregator.StreamAsync()` executes, then the plugin's connector is invoked for any configured connections matching its `ProviderKey`.

### Approach A – Assembly Loading

- **AC-A-001**: Given a valid plugin directory with a DLL and `plugin.json`, when the host starts, then the connector types listed in `connectorTypes` are instantiated and registered.
- **AC-A-002**: Given a plugin assembly that references a different version of a shared library (e.g., `System.Text.Json`), when the plugin is loaded, then no `TypeLoadException` or version conflict occurs due to isolated `AssemblyLoadContext`.
- **AC-A-003**: Given a `connectorTypes` entry that names a type not implementing `IConnector`, when the host loads the plugin, then that type is skipped with a warning and other valid types in the same plugin are still registered.
- **AC-A-004**: Given a new DLL is dropped into the plugin directory after the host has started, when the host is restarted (SIGTERM + start), then the new connector is available without changing `Program.cs`.

### Approach B – Remote Connector

- **AC-B-001**: Given a remote connector endpoint is configured and `GET /api/connector/metadata` returns valid metadata, when the host starts, then a `RemoteConnectorAdapter` is instantiated and registered for that endpoint.
- **AC-B-002**: Given the remote connector service returns a `200 OK` from `POST /api/connector/fetch`, when `FetchConnectionAsync` is called on the adapter, then the response is deserialized into a `ConnectorResult` and returned to the caller.
- **AC-B-003**: Given the remote connector service returns `401 Unauthorized`, when `FetchConnectionAsync` is called, then the returned `ConnectorResult` contains a `ProviderIssue` with `SyncStatus = "needs-auth"` and an explanatory message.
- **AC-B-004**: Given the `X-Plugin-Secret` header is configured, when the adapter calls the remote service, then the header is present on all outbound requests.
- **AC-B-005**: Given the remote service is unreachable (`HttpRequestException`), when `FetchConnectionAsync` is called, then the exception is caught, a `ProviderIssue` is returned, and no unhandled exception propagates to `DashboardAggregator`.
- **AC-B-006**: Given the remote connector response body exceeds `RemoteConnectorMaxResponseBytes`, when the adapter reads the response, then an error is logged and an error `ProviderIssue` is returned without loading the full body into memory.

---

## 6. Test Automation Strategy

- **Test Levels**: Unit tests for loader/adapter classes; integration tests for the plugin pipeline end-to-end.
- **Frameworks**: MSTest, FluentAssertions, Moq (matching existing test projects `PriorityHub.Api.Tests`, `PriorityHub.Ui.Tests`).
- **Unit Tests (Approach A)**:
  - `PluginConnectorLoader` correctly reads `plugin.json`, loads the assembly, instantiates connector types, and handles invalid manifests / missing assemblies / non-`IConnector` types.
  - Duplicate `ProviderKey` detection logic.
  - `ProviderKey` validation regex.
- **Unit Tests (Approach B)**:
  - `RemoteConnectorAdapter` correctly serializes the `POST /fetch` request and deserializes the response into `ConnectorResult`.
  - Error path: non-2xx responses produce the correct `ProviderIssue`.
  - `X-Plugin-Secret` header injection is verified on all outbound calls using `HttpMessageHandler` mocks.
  - Response size enforcement.
- **Integration Tests**:
  - `WebApplicationFactory<Program>` test that loads a test plugin assembly (Approach A) from a temp directory and verifies it appears in `GET /api/plugins` and the `ConnectorRegistry`.
  - `WebApplicationFactory<Program>` test with a mock HTTP server (Approach B) verifies `RemoteConnectorAdapter` is registered and returns expected data.
- **Test Data Management**: Test plugin assemblies are compiled as part of the test project (as embedded resources or project references). Remote connector tests use `MockHttpMessageHandler` to avoid real network calls.
- **CI/CD Integration**: All tests run in GitHub Actions via `dotnet test PriorityHub.sln` on every PR.
- **Coverage Requirements**: New loader/adapter code SHOULD achieve ≥ 80% line coverage.
- **Performance Testing**: Not required for the initial implementation; plugin loading is a startup-time operation.

---

## 7. Rationale & Context

### Why two approaches?

The two approaches represent a fundamental trade-off between **deployment simplicity** and **isolation/security**:

| Dimension | Approach A (Assembly Loading) | Approach B (Remote HTTP) |
|-----------|-------------------------------|--------------------------|
| **Isolation** | Process-level (shared memory, crash affects host) | Full process isolation (independent crash domain) |
| **Latency** | Negligible (in-process call) | Network round-trip per fetch |
| **Deployment** | Drop DLL into directory; restart host | Deploy independent service; no host restart needed |
| **Security** | Plugin code runs in host process | Plugin code is fully sandboxed |
| **Suitable for** | Trusted internal teams | Third-party / untrusted authors, cloud-native |
| **Dependency conflicts** | Mitigated by ALC, but still same process | None (each service has its own runtime) |
| **Operational complexity** | Low (single process) | Higher (service discovery, TLS, secrets) |

### Why preserve `IConnector` unchanged?

The existing `IConnector` interface is already well-designed as a plugin contract: it is small, dependency-free (only `System.Text.Json` and project models), and stateless per call. Changing it would break all six existing built-in connectors and require a major version bump. Both approaches work within this constraint.

### Why `AssemblyLoadContext` over MEF?

MEF (Managed Extensibility Framework) is a higher-level composition framework but adds complexity and is less commonly used in modern .NET 5+ applications. `AssemblyLoadContext` is the .NET platform primitive for plugin isolation, is well-documented, and gives explicit control over assembly resolution. It is the approach recommended in official .NET documentation for plugin scenarios.

### Why reject dynamic Roslyn compilation?

A third approach – compiling connector source code at runtime using Roslyn – was considered but rejected for this specification because: it introduces a large compilation toolchain into the production host, it widens the attack surface significantly (arbitrary code execution risk), and it does not align with Priority-Hub's security posture.

---

## 8. Dependencies & External Integrations

### External Systems
- None for Approach A.
- **EXT-001** (Approach B): Remote connector HTTP services – independently deployed services exposing the HTTP contract defined in Section 4.5.

### Third-Party Services
- None beyond what individual connector implementations already use.

### Infrastructure Dependencies
- **INF-001**: A writable `plugins/` directory on the host filesystem (Approach A). Must be mounted if running in a container.
- **INF-002**: Network access from the host to remote connector endpoints (Approach B). Firewall/egress rules must permit this.

### Technology Platform Dependencies
- **PLT-001**: .NET 10 runtime – required for `AssemblyLoadContext` features and host compatibility.
- **PLT-002** (Approach B): HTTPS termination – TLS 1.2 minimum for remote connector endpoints in non-development environments.

### Data Dependencies
- None. Plugin loading does not interact with `IConfigStore`.

### Compliance Dependencies
- **COM-001**: Connector plugin code from third parties MAY access user-provided credentials (API keys, PATs) passed via `connectionConfig`. Plugin authors MUST be made aware of this and the host MUST document this data flow in its plugin authoring guide.

---

## 9. Examples & Edge Cases

### Example: Approach A – Minimal Plugin Project

A plugin author creates a .NET class library:

```xml
<!-- MyOrg.MyConnector/MyOrg.MyConnector.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- Exclude the shared contract assembly from the plugin output –
         the host already provides it via the shared SDK package.        -->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="PriorityHub.Connector.Sdk" Version="1.0.0" />
  </ItemGroup>
</Project>
```

```csharp
// MyOrg.MyConnector/MyConnector.cs
using System.Text.Json;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services.Connectors;

namespace MyOrg.MyConnector;

public sealed class MyConnector(HttpClient httpClient) : IConnector
{
    public string ProviderKey   => "my-connector";
    public string DisplayName   => "My Connector";
    public string Description   => "Fetches items from My System.";
    public string DefaultEmoji  => "🔌";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("baseUrl",  "Base URL",  "text",     Required: true),
        new("apiToken", "API Token", "password", Required: true),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(
        JsonElement connectionConfig,
        string? oauthToken,
        CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        // ... implementation ...
        return result;
    }
}
```

Plugin directory layout after publishing:

```
plugins/
└── my-connector/
    ├── plugin.json
    ├── MyOrg.MyConnector.dll
    └── MyOrg.MyConnector.deps.json
```

`plugin.json`:

```json
{
  "schemaVersion": "1",
  "pluginId": "myorg-my-connector",
  "displayName": "My Connector",
  "version": "1.0.0",
  "assembly": "MyOrg.MyConnector.dll",
  "connectorTypes": ["MyOrg.MyConnector.MyConnector"]
}
```

### Example: Approach B – Host Configuration for Remote Connector

```jsonc
// appsettings.json
{
  "Plugins": {
    "RemoteConnectors": [
      {
        "PluginId": "myorg-remote-connector",
        "Endpoint": "https://my-connector.internal/api/connector",
        "Secret": "s3cr3t-shared-key"
      }
    ]
  }
}
```

### Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| `plugin.json` is missing | Host logs error, skips directory, continues. |
| `plugin.json` has invalid JSON | Host logs parsing error, skips plugin. |
| DLL listed in `assembly` does not exist | Host logs error, skips plugin. |
| `connectorTypes` lists a class that has no public constructor | Host logs error for that type, skips it; other types in the manifest are still loaded. |
| `ProviderKey` returned by loaded connector does not match manifest's `providerKeys` | Host logs warning; connector is still registered (manifest hint is advisory for pre-validation only). |
| Remote connector's `GET /metadata` returns `503 Service Unavailable` at startup | Host logs warning, marks plugin as `"failed"` in diagnostics, continues startup. |
| Remote connector takes longer than configured `HttpClient` timeout during `FetchConnectionAsync` | `TaskCanceledException` is caught, `ProviderIssue` is added to result. |
| Two plugin directories declare the same `pluginId` | Host logs warning, first-loaded wins. |
| Plugin assembly targets `netstandard2.1` on a `net10.0` host | Supported. The ALC resolves shared assemblies from the host's runtime. |

---

## 10. Validation Criteria

The following criteria MUST be satisfied before either approach is considered production-ready:

- **VAL-001**: `dotnet build PriorityHub.sln` succeeds with zero errors and zero warnings related to the plugin loader/adapter code.
- **VAL-002**: `dotnet test PriorityHub.sln` passes with all existing tests green plus new plugin system tests.
- **VAL-003**: A manual smoke test confirms that dropping a test connector DLL into `plugins/` and restarting the host causes the connector to appear in the UI Settings page under "Add Connection".
- **VAL-004** (Approach B): A manual smoke test confirms that configuring a remote connector endpoint and starting a minimal HTTP stub returns work items visible in the Priority-Hub dashboard.
- **VAL-005**: `GET /api/plugins` returns a JSON array that correctly lists all loaded connectors (built-in and plugin) with their status.
- **VAL-006**: The load time for 10 plugin assemblies at startup does not exceed 5 seconds on reference hardware (2-core, 4 GB RAM container).
- **VAL-007**: A plugin that throws an unhandled exception from `FetchConnectionAsync` does NOT crash the host process or affect other connectors' fetch results.

---

## 11. Related Specifications / Further Reading

- [Current `IConnector` interface](../backend/PriorityHub.Api/Services/Connectors/IConnector.cs)
- [Current `ConnectorRegistry`](../backend/PriorityHub.Api/Services/ConnectorRegistry.cs)
- [Current `DashboardAggregator`](../backend/PriorityHub.Api/Services/DashboardAggregator.cs)
- [Microsoft .NET Plugin Architecture with AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- [AssemblyLoadContext API Reference](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext)
- [IOptions pattern in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- [Typed HttpClient in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests#typed-clients)
