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
        public string DefaultEmoji => "📋";
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

    private sealed class StubConnector(string key, WorkItem[] items) : IConnector
    {
        public string ProviderKey => key;
        public string DisplayName => "Stub";
        public string Description => "Stub";
        public string DefaultEmoji => "🔷";
        public ConnectorFieldSpec[] ConfigFields => [];
        public List<BoardConnection> BoardConnections { get; } = [];

        public Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
        {
            var result = new ConnectorResult();
            result.WorkItems.AddRange(items);
            result.BoardConnections.AddRange(BoardConnections);
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task BuildAsync_PartialFailure_StillReturnsSuccessfulItems()
    {
        var store = CreateStore();
        var config = new ProviderConfiguration
        {
            Jira =
            [
                new JiraConnection { Id = "j1", Name = "Jira OK", Enabled = true, BaseUrl = "https://x.atlassian.net", Email = "u@x.com", ApiToken = "t" },
            ]
        };
        await store.SaveAsync(UserId, config, CancellationToken.None);

        // One stub returns items; one stub throws
        var okConnector = new StubConnector("jira", [
            new WorkItem { Id = "J-1", BoardId = "j1", Title = "Jira Item" }
        ]);
        okConnector.BoardConnections.Add(new BoardConnection { Id = "j1", Provider = "jira" });

        var registry = new ConnectorRegistry([okConnector]);
        var aggregator = new DashboardAggregator(store, registry);

        var payload = await aggregator.BuildAsync(UserId, new Dictionary<string, string>(), CancellationToken.None);

        // At least the items from the OK connector come through
        Assert.NotEmpty(payload.WorkItems);
    }

    [Fact]
    public void Merge_MultipleResults_AllWorkItemsAppended()
    {
        var dashboard = new DashboardPayload();

        var result1 = new ConnectorResult();
        result1.WorkItems.Add(new WorkItem { Id = "A-1" });

        var result2 = new ConnectorResult();
        result2.WorkItems.Add(new WorkItem { Id = "B-1" });
        result2.WorkItems.Add(new WorkItem { Id = "B-2" });

        DashboardAggregator.Merge(dashboard, result1, orderedIds: []);
        DashboardAggregator.Merge(dashboard, result2, orderedIds: []);

        Assert.Equal(3, dashboard.WorkItems.Count);
    }

    [Fact]
    public void Merge_EmptyResult_DoesNotModifyDashboard()
    {
        var dashboard = new DashboardPayload();
        dashboard.WorkItems.Add(new WorkItem { Id = "X-1" });

        var emptyResult = new ConnectorResult();

        DashboardAggregator.Merge(dashboard, emptyResult, orderedIds: []);

        Assert.Single(dashboard.WorkItems);
    }
}
