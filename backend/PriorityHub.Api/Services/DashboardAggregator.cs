using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

public sealed class DashboardAggregator(
    IConfigStore configStore,
    ConnectorRegistry connectorRegistry)
{
    public async Task<DashboardPayload> BuildAsync(string userId, IReadOnlyDictionary<string, string> oauthTokensByProvider, CancellationToken cancellationToken)
        => await BuildAsync(userId, oauthTokensByProvider, new Dictionary<string, string>(), cancellationToken);

    public async Task<DashboardPayload> BuildAsync(string userId, IReadOnlyDictionary<string, string> oauthTokensByProvider, IReadOnlyDictionary<string, string> oauthTokensByConnectionId, CancellationToken cancellationToken)
    {
        DashboardPayload? latestSnapshot = null;

        await foreach (var update in StreamAsync(userId, oauthTokensByProvider, oauthTokensByConnectionId, cancellationToken))
        {
            latestSnapshot = update.Dashboard;
        }

        return latestSnapshot ?? new DashboardPayload();
    }

    public IAsyncEnumerable<DashboardStreamEvent> StreamAsync(string userId, IReadOnlyDictionary<string, string> oauthTokensByProvider, CancellationToken cancellationToken)
        => StreamAsync(userId, oauthTokensByProvider, new Dictionary<string, string>(), cancellationToken);

    public async IAsyncEnumerable<DashboardStreamEvent> StreamAsync(string userId, IReadOnlyDictionary<string, string> oauthTokensByProvider, IReadOnlyDictionary<string, string> oauthTokensByConnectionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = await configStore.LoadAsync(userId, cancellationToken);
        var orderedIds = config.Preferences.OrderedItemIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var workingDashboard = new DashboardPayload
        {
            Preferences = config.Preferences,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var pendingFetches = BuildPendingFetches(config, oauthTokensByProvider, oauthTokensByConnectionId, cancellationToken);
        var totalConnections = pendingFetches.Count;
        var completedConnections = 0;

        yield return CreateStreamEvent(
            workingDashboard,
            totalConnections,
            completedConnections,
            string.Empty,
            string.Empty,
            string.Empty,
            totalConnections == 0 ? "No enabled connectors configured." : "Fetching live work items.",
            totalConnections == 0);

        while (pendingFetches.Count > 0)
        {
            var completedTask = await Task.WhenAny(pendingFetches.Select(item => item.Task));
            var completedFetch = pendingFetches.Single(item => item.Task == completedTask);
            pendingFetches.Remove(completedFetch);

            var result = await completedFetch.Task;
            Merge(workingDashboard, result, orderedIds);
            completedConnections += 1;

            yield return CreateStreamEvent(
                workingDashboard,
                totalConnections,
                completedConnections,
                completedFetch.Provider,
                completedFetch.ConnectionId,
                completedFetch.ConnectionName,
                $"Fetched {completedFetch.ConnectionName} from {completedFetch.Provider}.",
                completedConnections == totalConnections);
        }
    }

    private List<PendingFetch> BuildPendingFetches(ProviderConfiguration config, IReadOnlyDictionary<string, string> oauthTokensByProvider, IReadOnlyDictionary<string, string> oauthTokensByConnectionId, CancellationToken cancellationToken)
    {
        var pendingFetches = new List<PendingFetch>();

        foreach (var connector in connectorRegistry.GetAll())
        {
            foreach (var connectionConfig in config.GetConnections(connector.ProviderKey))
            {
                // Default for 'enabled' is true when the property is absent
                var enabled = !connectionConfig.TryGetProperty("enabled", out var enabledProp) || enabledProp.GetBoolean();
                if (!enabled) continue;

                var id = connectionConfig.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
                var name = connectionConfig.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty;

                // Phase 3: resolve per-connection token for linked accounts first,
                // then fall back to provider-level token.
                string? oauthToken;
                var linkedAccountId = connectionConfig.TryGetProperty("linkedAccountId", out var lacProp)
                    ? lacProp.GetString()
                    : null;

                if (!string.IsNullOrWhiteSpace(linkedAccountId) && oauthTokensByConnectionId.TryGetValue(id, out var connToken))
                {
                    oauthToken = connToken;
                }
                else
                {
                    oauthToken = oauthTokensByProvider.TryGetValue(connector.ProviderKey, out var providerToken)
                        ? providerToken
                        : null;
                }

                pendingFetches.Add(new PendingFetch(
                    connector.ProviderKey, id, name,
                    connector.FetchConnectionAsync(connectionConfig, oauthToken, cancellationToken)));
            }
        }

        return pendingFetches;
    }

    internal static void Merge(DashboardPayload dashboard, ConnectorResult result, HashSet<string> orderedIds)
    {
        dashboard.BoardConnections.AddRange(result.BoardConnections);

        foreach (var workItem in result.WorkItems)
        {
            workItem.IsNew = !orderedIds.Contains(workItem.Id);
        }

        dashboard.WorkItems.AddRange(result.WorkItems);
        dashboard.Issues.AddRange(result.Issues);
        dashboard.GeneratedAt = DateTimeOffset.UtcNow.ToString("O");
    }

    private static DashboardStreamEvent CreateStreamEvent(
        DashboardPayload dashboard,
        int totalConnections,
        int completedConnections,
        string activeProvider,
        string activeConnectionId,
        string activeConnectionName,
        string message,
        bool isComplete)
    {
        return new DashboardStreamEvent
        {
            Dashboard = new DashboardPayload
            {
                BoardConnections = [.. dashboard.BoardConnections],
                WorkItems = [.. dashboard.WorkItems],
                Issues = [.. dashboard.Issues],
                Preferences = new UserPreferences
                {
                    OrderedItemIds = [.. dashboard.Preferences.OrderedItemIds]
                },
                GeneratedAt = dashboard.GeneratedAt
            },
            Progress = new DashboardProgress
            {
                TotalConnections = totalConnections,
                CompletedConnections = completedConnections,
                ActiveProvider = activeProvider,
                ActiveConnectionId = activeConnectionId,
                ActiveConnectionName = activeConnectionName,
                Message = message,
                IsComplete = isComplete
            }
        };
    }

    private sealed record PendingFetch(string Provider, string ConnectionId, string ConnectionName, Task<ConnectorResult> Task);
}