using Microsoft.Extensions.Options;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class ActiveUserTrackerTests
{
    [Fact]
    public void RecordActivity_CountsDistinctUsers()
    {
        var tracker = new ActiveUserTracker(Options.Create(new TelemetryOptions
        {
            ActiveUserWindowMinutes = 60,
            MaxActiveUserEntries = 100_000
        }));

        tracker.RecordActivity("u1");
        tracker.RecordActivity("u2");
        tracker.RecordActivity("u1");

        Assert.Equal(2, tracker.GetActiveUserCount());
    }

    [Fact]
    public void RecordActivity_IgnoresEmptyIds()
    {
        var tracker = new ActiveUserTracker(Options.Create(new TelemetryOptions()));

        tracker.RecordActivity(string.Empty);
        tracker.RecordActivity("   ");

        Assert.Equal(0, tracker.GetActiveUserCount());
    }
}
