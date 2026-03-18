using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Services;

/// <summary>
/// Holds all registered IConnector implementations.
/// Inject IEnumerable&lt;IConnector&gt; to register multiple connectors,
/// then use this registry in DashboardAggregator for provider-agnostic aggregation.
/// </summary>
public sealed class ConnectorRegistry
{
    private readonly IReadOnlyList<IConnector> _all;
    private readonly IReadOnlyDictionary<string, IConnector> _byKey;

    public ConnectorRegistry(IEnumerable<IConnector> connectors)
    {
        _all = connectors.ToList();
        _byKey = _all.ToDictionary(c => c.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IConnector> GetAll() => _all;

    public IConnector? GetByKey(string providerKey) =>
        _byKey.GetValueOrDefault(providerKey);
}
