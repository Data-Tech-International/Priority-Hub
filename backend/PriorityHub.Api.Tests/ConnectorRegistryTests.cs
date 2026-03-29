using System.Text.Json;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests;

public sealed class ConnectorRegistryTests
{
    private static IConnector MakeConnector(string key, string display = "Test") =>
        new StubConnector(key, display);

    [Fact]
    public void GetAll_ReturnsAllRegisteredConnectors()
    {
        var c1 = MakeConnector("jira");
        var c2 = MakeConnector("trello");
        var registry = new ConnectorRegistry([c1, c2]);

        var all = registry.GetAll();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetByKey_ReturnsConnector_WhenKeyMatches()
    {
        var connector = MakeConnector("jira");
        var registry = new ConnectorRegistry([connector]);

        var found = registry.GetByKey("jira");

        Assert.Same(connector, found);
    }

    [Fact]
    public void GetByKey_IsCaseInsensitive()
    {
        var connector = MakeConnector("azure-devops");
        var registry = new ConnectorRegistry([connector]);

        Assert.Same(connector, registry.GetByKey("AZURE-DEVOPS"));
        Assert.Same(connector, registry.GetByKey("Azure-DevOps"));
    }

    [Fact]
    public void GetByKey_ReturnsNull_WhenKeyNotFound()
    {
        var registry = new ConnectorRegistry([MakeConnector("jira")]);

        var result = registry.GetByKey("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsEmptyList_WhenNoConnectors()
    {
        var registry = new ConnectorRegistry([]);

        Assert.Empty(registry.GetAll());
    }

    private sealed class StubConnector(string key, string display) : IConnector
    {
        public string ProviderKey => key;
        public string DisplayName => display;
        public string Description => "Stub";
        public string DefaultEmoji => "🔷";
        public ConnectorFieldSpec[] ConfigFields => [];

        public Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken) =>
            Task.FromResult(new ConnectorResult());
    }
}
