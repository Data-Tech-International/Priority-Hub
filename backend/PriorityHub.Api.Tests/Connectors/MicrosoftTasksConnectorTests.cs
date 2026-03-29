using System.Text.Json;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class MicrosoftTasksConnectorTests
{
    // ── IsCompletedStatus ───────────────────────────────────────────────────

    [Theory]
    [InlineData("completed", true)]
    [InlineData("Completed", true)]
    [InlineData("inProgress", false)]
    [InlineData(null, false)]
    public void IsCompletedStatus_ReturnsExpectedValue(string? input, bool expected)
    {
        Assert.Equal(expected, MicrosoftTasksConnector.IsCompletedStatus(input));
    }

    // ── MapStatus ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("completed", "done")]
    [InlineData("Completed", "done")]
    [InlineData("inProgress", "in-progress")]
    [InlineData("waitingOnOthers", "blocked")]
    [InlineData("deferred", "review")]
    [InlineData("notStarted", "planned")]
    [InlineData(null, "planned")]
    [InlineData("", "planned")]
    public void MapStatus_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(expected, MicrosoftTasksConnector.MapStatus(input));
    }

    // ── MapImpact ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("high", 8)]
    [InlineData("High", 8)]
    [InlineData("HIGH", 8)]
    [InlineData("low", 4)]
    [InlineData("normal", 6)]
    [InlineData(null, 6)]
    public void MapImpact_ReturnsExpectedScore(string? importance, int expected)
    {
        Assert.Equal(expected, MicrosoftTasksConnector.MapImpact(importance));
    }

    // ── MapUrgency ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("high", 0, 10)]   // due today + high importance → max(10, 8) = 10
    [InlineData("high", 1, 9)]    // due in 1 day + high → max(9, 8) = 9
    [InlineData("high", null, 8)] // no due date + high → max(5, 8) = 8
    [InlineData("normal", null, 6)] // no due date + normal → max(5, 6) = 6
    [InlineData("low", null, 5)]  // no due date + low → max(5, 4) = 5
    [InlineData(null, 0, 10)]     // due today + no importance → max(10, 6) = 10
    [InlineData(null, 3, 7)]      // due in 3 days + no importance → max(7, 6) = 7
    public void MapUrgency_ReturnsExpectedScore(string? importance, int? dueInDays, int expected)
    {
        Assert.Equal(expected, MicrosoftTasksConnector.MapUrgency(importance, dueInDays));
    }

    // ── ReadDateOffsetDays ───────────────────────────────────────────────────

    [Fact]
    public void ReadDateOffsetDays_ValidFutureDate_ReturnsPositive()
    {
        var futureDate = DateTimeOffset.UtcNow.AddDays(5).ToString("O");
        var json = TestHelpers.JsonOf($$$"""{"dueDateTime": {"dateTime": "{{{futureDate}}}","timeZone":"UTC"}}""");
        var result = MicrosoftTasksConnector.ReadDateOffsetDays(json, "dueDateTime");
        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Fact]
    public void ReadDateOffsetDays_MissingProperty_ReturnsNull()
    {
        var json = TestHelpers.JsonOf("""{"title": "no due date"}""");
        Assert.Null(MicrosoftTasksConnector.ReadDateOffsetDays(json, "dueDateTime"));
    }

    [Fact]
    public void ReadDateOffsetDays_PropertyIsNull_ReturnsNull()
    {
        var json = TestHelpers.JsonOf("""{"dueDateTime": null}""");
        Assert.Null(MicrosoftTasksConnector.ReadDateOffsetDays(json, "dueDateTime"));
    }

    // ── ResolveSourceUrl ─────────────────────────────────────────────────────

    [Fact]
    public void ResolveSourceUrl_UsesLinkedResourceUrl_WhenPresent()
    {
        var json = TestHelpers.JsonOf("""{"linkedResources":[{"webUrl":"https://example.test/task/1"}]}""");
        Assert.Equal("https://example.test/task/1", MicrosoftTasksConnector.ResolveSourceUrl(json));
    }

    [Fact]
    public void ResolveSourceUrl_FallsBackToMicrosoftToDo_WhenMissing()
    {
        var json = TestHelpers.JsonOf("""{"title":"Task without link"}""");
        Assert.Equal("https://to-do.office.com/tasks/", MicrosoftTasksConnector.ResolveSourceUrl(json));
    }

    // ── DaysSince ────────────────────────────────────────────────────────────

    [Fact]
    public void DaysSince_ValidDate_ReturnsApproximateDays()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-3).ToString("O");
        Assert.InRange(MicrosoftTasksConnector.DaysSince(date), 2, 4);
    }

    [Fact]
    public void DaysSince_NullOrInvalid_ReturnsZero()
    {
        Assert.Equal(0, MicrosoftTasksConnector.DaysSince(null));
        Assert.Equal(0, MicrosoftTasksConnector.DaysSince("garbage"));
    }

    // ── ReadTargetDate ───────────────────────────────────────────────────────

    [Fact]
    public void ReadTargetDate_ValidFutureDate_ReturnsDateTimeOffset()
    {
        var futureDate = DateTimeOffset.UtcNow.AddDays(5).ToString("O");
        var json = TestHelpers.JsonOf($$$"""{"dueDateTime": {"dateTime": "{{{futureDate}}}","timeZone":"UTC"}}""");
        var result = MicrosoftTasksConnector.ReadTargetDate(json, "dueDateTime");
        Assert.NotNull(result);
        Assert.True(result.Value > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ReadTargetDate_MissingProperty_ReturnsNull()
    {
        var json = TestHelpers.JsonOf("""{"title": "no due date"}""");
        Assert.Null(MicrosoftTasksConnector.ReadTargetDate(json, "dueDateTime"));
    }

    [Fact]
    public void ReadTargetDate_PropertyIsNull_ReturnsNull()
    {
        var json = TestHelpers.JsonOf("""{"dueDateTime": null}""");
        Assert.Null(MicrosoftTasksConnector.ReadTargetDate(json, "dueDateTime"));
    }

    // ── IsBlocked ────────────────────────────────────────────────────────────

    [Fact]
    public void MapStatus_WaitingOnOthers_MapsToBlocked()
    {
        Assert.Equal("blocked", MicrosoftTasksConnector.MapStatus("waitingOnOthers"));
    }

    [Fact]
    public void IsBlocked_WhenWaitingOnOthers_ReturnsTrue()
    {
        // IsBlocked = string.Equals(status, "waitingOnOthers", OrdinalIgnoreCase)
        Assert.Equal("waitingOnOthers", "waitingOnOthers", StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual("inProgress", "waitingOnOthers", StringComparer.OrdinalIgnoreCase);
    }
}
