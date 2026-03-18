using System.Text.Json;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests;

public sealed class DashboardAggregatorTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private const string UserId = "test@example.com";

    private LocalConfigStore CreateStore() =>
        new(new TestHostEnvironment(Path.Combine(_tempRoot, "api", "bin")));

    // ── Merge (direct) ───────────────────────────────────────────────────────

    [Fact]
    public void Merge_NewItem_SetsIsNewTrue()
    {
        var dashboard = new DashboardPayload();
        var result = new ConnectorResult();
        result.WorkItems.Add(new WorkItem { Id = "ITEM-999" });

        DashboardAggregator.Merge(dashboard, result, orderedIds: []);

        Assert.True(dashboard.WorkItems[0].IsNew);
    }

    [Fact]
    public void Merge_KnownItem_SetsIsNewFalse()
    {
        var dashboard = new DashboardPayload();
        var result = new ConnectorResult();
        result.WorkItems.Add(new WorkItem { Id = "ITEM-1" });

        DashboardAggregator.Merge(dashboard, result, orderedIds: new HashSet<string>(["ITEM-1"], StringComparer.OrdinalIgnoreCase));

        Assert.False(dashboard.WorkItems[0].IsNew);
    }

    [Fact]
    public void Merge_AppendsBoardConnections()
    {
        var dashboard = new DashboardPayload();
        var result = new ConnectorResult();
        result.BoardConnections.Add(new BoardConnection { Id = "board-1" });
        result.BoardConnections.Add(new BoardConnection { Id = "board-2" });

        DashboardAggregator.Merge(dashboard, result, orderedIds: []);

        Assert.Equal(2, dashboard.BoardConnections.Count);
    }

    [Fact]
    public void Merge_AppendsIssues()
    {
        var dashboard = new DashboardPayload();
        var result = new ConnectorResult();
        result.Issues.Add(new ProviderIssue { Message = "fault" });

        DashboardAggregator.Merge(dashboard, result, orderedIds: []);

        Assert.Single(dashboard.Issues);
    }

    // ── BuildAsync (integration with stub connectors) ────────────────────────

    [Fact]
    public async Task BuildAsync_NoConnectors_ReturnsEmptyDashboard()
    {
        var store = CreateStore();
        await store.SaveAsync(UserId, new ProviderConfiguration(), CancellationToken.None);

        var registry = new ConnectorRegistry([]);
        var aggregator = new DashboardAggregator(store, registry);

        var payload = await aggregator.BuildAsync(UserId, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Empty(payload.WorkItems);
        Assert.Empty(payload.BoardConnections);
    }

    [Fact]
    public async Task BuildAsync_TwoStubConnectors_UnionsWorkItems()
    {
        var store = CreateStore();

        // Pre-populate a Jira connection so BuildPendingFetches finds work to do
        var config = new ProviderConfiguration
        {
            Jira =
            [
                new JiraConnection { Id = "j1", Name = "Jira", Enabled = true, BaseUrl = "https://x.atlassian.net", Email = "u@x.com", ApiToken = "t" },
                new JiraConnection { Id = "j2", Name = "Jira 2", Enabled = true, BaseUrl = "https://y.atlassian.net", Email = "u@y.com", ApiToken = "t" }
            ]
        };
        await store.SaveAsync(UserId, config, CancellationToken.None);

        // Stub connector returns 2 items for every connection
        var registry = new ConnectorRegistry([new StubJiraConnector(itemsPerConnection: 2)]);
        var aggregator = new DashboardAggregator(store, registry);

        var payload = await aggregator.BuildAsync(UserId, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal(4, payload.WorkItems.Count); // 2 connections × 2 items
        Assert.Equal(2, payload.BoardConnections.Count);
    }

    [Fact]
    public async Task StreamAsync_EmitsProgressEvents()
    {
        var store = CreateStore();
        await store.SaveAsync(UserId, new ProviderConfiguration(), CancellationToken.None);

        var registry = new ConnectorRegistry([]);
        var aggregator = new DashboardAggregator(store, registry);

        var events = new List<DashboardStreamEvent>();
        await foreach (var ev in aggregator.StreamAsync(UserId, new Dictionary<string, string>(), CancellationToken.None))
        {
            events.Add(ev);
        }

        Assert.NotEmpty(events);
        Assert.True(events[^1].Progress.IsComplete);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    // ── Stub connector ───────────────────────────────────────────────────────

    private sealed class StubJiraConnector(int itemsPerConnection) : IConnector
    {
        public string ProviderKey => "jira";
        public string DisplayName => "Stub Jira";
        public string Description => "Test stub";
        public ConnectorFieldSpec[] ConfigFields => [];

        public Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
        {
            var result = new ConnectorResult();
            result.BoardConnections.Add(new BoardConnection { SyncStatus = "connected" });
            for (var i = 0; i < itemsPerConnection; i++)
            {
                result.WorkItems.Add(new WorkItem { Id = $"STUB-{Guid.NewGuid():N}", Title = $"Item {i}" });
            }

            return Task.FromResult(result);
        }
    }
}
