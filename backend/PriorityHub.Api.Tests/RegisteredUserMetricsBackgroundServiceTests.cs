using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class RegisteredUserMetricsBackgroundServiceTests
{
    [Fact]
    public async Task ExecuteAsync_CallsRecordUserCountsWithCorrectValues()
    {
        var telemetryService = new RecordingTelemetryService();
        var tracker = new StubActiveUserTracker { ActiveUserCount = 7 };
        var configStore = new StubConfigStore { RegisteredUserCount = 42 };
        var options = Options.Create(new TelemetryOptions { RegisteredUserRefreshIntervalSeconds = 60 });
        var logger = new TestLogger<RegisteredUserMetricsBackgroundService>();

        var service = new RegisteredUserMetricsBackgroundService(
            telemetryService, tracker, configStore, options, logger);

        using var cts = new CancellationTokenSource();

        // Start the service; it emits counts immediately before waiting on the timer.
        var executeTask = service.StartAsync(cts.Token);

        // Give the service time to complete its first iteration.
        await Task.Delay(500);

        // Cancel to stop the background loop.
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.NotEmpty(telemetryService.RecordedCounts);
        var (active, registered) = telemetryService.RecordedCounts[0];
        Assert.Equal(7, active);
        Assert.Equal(42, registered);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConfigStoreThrows_DoesNotCrash()
    {
        var telemetryService = new RecordingTelemetryService();
        var tracker = new StubActiveUserTracker { ActiveUserCount = 1 };
        var configStore = new ThrowingConfigStore();
        var options = Options.Create(new TelemetryOptions { RegisteredUserRefreshIntervalSeconds = 60 });
        var logger = new TestLogger<RegisteredUserMetricsBackgroundService>();

        var service = new RegisteredUserMetricsBackgroundService(
            telemetryService, tracker, configStore, options, logger);

        using var cts = new CancellationTokenSource();

        var executeTask = service.StartAsync(cts.Token);

        // Give the service time to hit the exception and continue.
        await Task.Delay(500);

        await cts.CancelAsync();

        // The service should stop gracefully without throwing.
        var exception = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));
        Assert.Null(exception);

        // RecordUserCounts should NOT have been called since GetRegisteredUserCountAsync threw.
        Assert.Empty(telemetryService.RecordedCounts);
    }

    // --- Stubs ---

    private sealed class RecordingTelemetryService : ITelemetryService
    {
        public List<(int Active, int Registered)> RecordedCounts { get; } = [];

        public void RecordSignIn(string provider, string userIdentityKey) { }
        public void RecordSignOut(string userIdentityKey) { }
        public void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success) { }
        public void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey) { }
        public void RecordUserActivity(string userIdentityKey) { }
        public void RecordPageView(string pageName, string? userIdentityKey) { }
        public void RecordConfigSave(string userIdentityKey, int connectorCount) { }
        public void RecordLinkedAccountOperation(string operation, string userIdentityKey) { }
        public void RecordUserCounts(int activeUsers, int registeredUsers) => RecordedCounts.Add((activeUsers, registeredUsers));
    }

    private sealed class StubActiveUserTracker : IActiveUserTracker
    {
        public int ActiveUserCount { get; set; }
        public void RecordActivity(string hashedUserId) { }
        public int GetActiveUserCount() => ActiveUserCount;
    }

    private sealed class StubConfigStore : IConfigStore
    {
        public int RegisteredUserCount { get; set; }
        public Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(new ProviderConfiguration());
        public Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> GetRegisteredUserCountAsync(CancellationToken cancellationToken) => Task.FromResult(RegisteredUserCount);
    }

    private sealed class ThrowingConfigStore : IConfigStore
    {
        public Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(new ProviderConfiguration());
        public Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> GetRegisteredUserCountAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Database unavailable");
    }
}
