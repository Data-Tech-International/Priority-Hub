using Microsoft.Extensions.Options;

namespace PriorityHub.Api.Services.Telemetry;

public sealed class RegisteredUserMetricsBackgroundService(
    ITelemetryService telemetryService,
    IActiveUserTracker activeUserTracker,
    IConfigStore configStore,
    IOptions<TelemetryOptions> telemetryOptions,
    ILogger<RegisteredUserMetricsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(60, telemetryOptions.Value.RegisteredUserRefreshIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var activeUsers = activeUserTracker.GetActiveUserCount();
                var registeredUsers = await configStore.GetRegisteredUserCountAsync(stoppingToken);
                telemetryService.RecordUserCounts(activeUsers, registeredUsers);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to emit telemetry user counts.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }
}
