using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

/// <summary>
/// Implemented by every connector type.
/// Adding a new connector = implement this interface, register with AddHttpClient in DI.
/// No changes to DashboardAggregator or ProviderConfiguration are required.
/// </summary>
public interface IConnector
{
    string ProviderKey { get; }
    string DisplayName { get; }
    string Description { get; }
    ConnectorFieldSpec[] ConfigFields { get; }

    Task<ConnectorResult> FetchConnectionAsync(
        JsonElement connectionConfig,
        string? oauthToken,
        CancellationToken cancellationToken);
}

/// <summary>Describes a single configuration field for a connector.</summary>
public sealed record ConnectorFieldSpec(
    string Key,
    string Label,
    string InputKind = "text",
    bool Required = true,
    string? DefaultValue = null);
