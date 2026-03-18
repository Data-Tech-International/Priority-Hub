using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class TrelloConnectorTests
{
    // ── MapStatus ────────────────────────────────────────────────────────────

    [Fact]
    public void MapStatus_ClosedCard_ReturnsDone()
    {
        Assert.Equal("done", TrelloConnector.MapStatus("Backlog", closed: true));
    }

    [Theory]
    [InlineData("Done", "done")]
    [InlineData("Completed", "done")]
    [InlineData("Complete", "done")]
    [InlineData("In Review", "review")]
    [InlineData("QA", "review")]
    [InlineData("Blocked", "blocked")]
    [InlineData("Doing", "in-progress")]
    [InlineData("In Progress", "in-progress")]
    [InlineData("Active", "in-progress")]
    [InlineData("To Do", "planned")]
    [InlineData("Backlog", "planned")]
    [InlineData(null, "planned")]
    public void MapStatus_OpenCard_ReturnsExpectedValue(string? listName, string expected)
    {
        Assert.Equal(expected, TrelloConnector.MapStatus(listName, closed: false));
    }

    // ── DaysSince ────────────────────────────────────────────────────────────

    [Fact]
    public void DaysSince_TwentyDaysAgo_ReturnsApproximateDays()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-20).ToString("O");
        Assert.InRange(TrelloConnector.DaysSince(date), 19, 21);
    }

    [Fact]
    public void DaysSince_Null_ReturnsZero()
    {
        Assert.Equal(0, TrelloConnector.DaysSince(null));
    }

    [Fact]
    public void DaysSince_Garbage_ReturnsZero()
    {
        Assert.Equal(0, TrelloConnector.DaysSince("not-a-date"));
    }
}
