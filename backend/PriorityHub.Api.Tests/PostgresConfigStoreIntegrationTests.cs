using DotNet.Testcontainers.Builders;
using Npgsql;
using PriorityHub.Api.Data;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;
using Testcontainers.PostgreSql;

namespace PriorityHub.Api.Tests;

/// <summary>
/// Integration tests for <see cref="PostgresConfigStore"/> using an ephemeral PostgreSQL container.
/// These tests require Docker to be available at runtime.
/// </summary>
[Collection("PostgresIntegration")]
public sealed class PostgresConfigStoreIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithDatabase("priorityhub_test")
        .WithUsername("test")
        .WithPassword("test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    private NpgsqlDataSource _dataSource = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = new NpgsqlDataSourceBuilder(_container.GetConnectionString()).Build();

        var env = new TestHostEnvironment(Path.GetTempPath()) { EnvironmentName = "Development" };
        var logger = new TestLogger<SchemaManager>();
        var schemaManager = new SchemaManager(_dataSource, env, logger);
        await schemaManager.ApplyAsync();
    }

    public async Task DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    private PostgresConfigStore CreateStore() => new(_dataSource);

    // ── LoadAsync ────────────────────────────────────────────────────────────

    [SkipIfNoDockerFact]
    public async Task LoadAsync_UnknownUser_ReturnsNormalizedEmptyConfig()
    {
        var store = CreateStore();
        var config = await store.LoadAsync("nobody@example.com", CancellationToken.None);

        Assert.NotNull(config);
        Assert.Empty(config.AzureDevOps);
        Assert.Empty(config.GitHub);
        Assert.Empty(config.Jira);
        Assert.Empty(config.Trello);
        Assert.NotNull(config.Preferences);
        Assert.Empty(config.Preferences.OrderedItemIds);
    }

    // ── SaveAsync / LoadAsync round-trip ─────────────────────────────────────

    [SkipIfNoDockerFact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsSameData()
    {
        var store = CreateStore();
        const string userId = "roundtrip@test.com";

        var saved = new ProviderConfiguration
        {
            AzureDevOps =
            [
                new AzureDevOpsConnection { Name = "My ADO", Organization = "myorg", Project = "myproject" }
            ],
            Preferences = new UserPreferences { OrderedItemIds = ["ADO-1", "ADO-2"] }
        };

        await store.SaveAsync(userId, saved, CancellationToken.None);
        var loaded = await store.LoadAsync(userId, CancellationToken.None);

        Assert.Single(loaded.AzureDevOps);
        Assert.Equal("My ADO", loaded.AzureDevOps[0].Name);
        Assert.Equal("myorg", loaded.AzureDevOps[0].Organization);
        Assert.Equal(2, loaded.Preferences.OrderedItemIds.Count);
        Assert.Contains("ADO-1", loaded.Preferences.OrderedItemIds);
    }

    [SkipIfNoDockerFact]
    public async Task SaveAsync_CalledTwice_OverwritesData()
    {
        var store = CreateStore();
        const string userId = "overwrite@test.com";

        var first = new ProviderConfiguration
        {
            Trello = [new TrelloConnection { Name = "First Board" }]
        };
        await store.SaveAsync(userId, first, CancellationToken.None);

        var second = new ProviderConfiguration
        {
            Trello = [new TrelloConnection { Name = "Second Board" }]
        };
        await store.SaveAsync(userId, second, CancellationToken.None);

        var loaded = await store.LoadAsync(userId, CancellationToken.None);

        Assert.Single(loaded.Trello);
        Assert.Equal("Second Board", loaded.Trello[0].Name);
    }

    [SkipIfNoDockerFact]
    public async Task SaveAsync_IncrementsVersion()
    {
        var store = CreateStore();
        const string userId = "version@test.com";

        await store.SaveAsync(userId, new ProviderConfiguration(), CancellationToken.None);
        await store.SaveAsync(userId, new ProviderConfiguration(), CancellationToken.None);

        const string sql = "SELECT version FROM user_config WHERE user_id = $1";
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(userId);
        var version = (int)(await cmd.ExecuteScalarAsync())!;

        Assert.Equal(2, version);
    }

    [SkipIfNoDockerFact]
    public async Task SaveAsync_IsolatesByUserId()
    {
        var store = CreateStore();

        var configA = new ProviderConfiguration
        {
            GitHub = [new GitHubConnection { Name = "User A Repo" }]
        };
        var configB = new ProviderConfiguration
        {
            GitHub = [new GitHubConnection { Name = "User B Repo" }, new GitHubConnection { Name = "User B Repo 2" }]
        };

        await store.SaveAsync("userA@test.com", configA, CancellationToken.None);
        await store.SaveAsync("userB@test.com", configB, CancellationToken.None);

        var loadedA = await store.LoadAsync("userA@test.com", CancellationToken.None);
        var loadedB = await store.LoadAsync("userB@test.com", CancellationToken.None);

        Assert.Single(loadedA.GitHub);
        Assert.Equal("User A Repo", loadedA.GitHub[0].Name);

        Assert.Equal(2, loadedB.GitHub.Count);
    }

    // ── SchemaManager ────────────────────────────────────────────────────────

    [SkipIfNoDockerFact]
    public async Task SchemaManager_SecondApply_IsIdempotent()
    {
        var env = new TestHostEnvironment(Path.GetTempPath()) { EnvironmentName = "Development" };
        var logger = new TestLogger<SchemaManager>();
        var schemaManager = new SchemaManager(_dataSource, env, logger);

        // Should not throw even though migrations were already applied in InitializeAsync
        await schemaManager.ApplyAsync();
    }
}
