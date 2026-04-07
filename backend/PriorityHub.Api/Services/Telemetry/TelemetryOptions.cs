namespace PriorityHub.Api.Services.Telemetry;

public sealed class TelemetryOptions
{
    public int ActiveUserWindowMinutes { get; set; } = 1440;
    public int RegisteredUserRefreshIntervalSeconds { get; set; } = 300;
    public int MaxActiveUserEntries { get; set; } = 100_000;
}
