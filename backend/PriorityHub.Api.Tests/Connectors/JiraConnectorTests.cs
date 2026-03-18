using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class JiraConnectorTests
{
    // ── MapStatus ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Done", "done")]
    [InlineData("Closed", "done")]
    [InlineData("Resolved", "done")]
    [InlineData("In Review", "review")]
    [InlineData("QA", "review")]
    [InlineData("Verify", "review")]
    [InlineData("Blocked", "blocked")]
    [InlineData("In Progress", "in-progress")]
    [InlineData("Doing", "in-progress")]
    [InlineData("Active", "in-progress")]
    [InlineData("To Do", "planned")]
    [InlineData("Open", "planned")]
    [InlineData(null, "planned")]
    public void MapStatus_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(expected, JiraConnector.MapStatus(input));
    }

    // ── PriorityToScore ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("Highest", 10)]
    [InlineData("Critical", 10)]
    [InlineData("Blocker", 10)]
    [InlineData("High", 8)]
    [InlineData("Low", 4)]
    [InlineData("Medium", 6)]
    [InlineData("Normal", 6)]
    [InlineData(null, 6)]
    [InlineData("", 6)]
    public void PriorityToScore_ReturnsExpectedScore(string? input, int expected)
    {
        Assert.Equal(expected, JiraConnector.PriorityToScore(input));
    }

    // ── BuildIssueUrl ────────────────────────────────────────────────────────

    [Fact]
    public void BuildIssueUrl_ValidKey_ReturnsCorrectUrl()
    {
        var url = JiraConnector.BuildIssueUrl("https://myorg.atlassian.net", "PROJ-123");
        Assert.Equal("https://myorg.atlassian.net/browse/PROJ-123", url);
    }

    [Fact]
    public void BuildIssueUrl_TrailingSlashInBase_RemovesSlash()
    {
        var url = JiraConnector.BuildIssueUrl("https://myorg.atlassian.net/", "PROJ-1");
        Assert.Equal("https://myorg.atlassian.net/browse/PROJ-1", url);
    }

    [Fact]
    public void BuildIssueUrl_NullKey_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, JiraConnector.BuildIssueUrl("https://myorg.atlassian.net", null));
    }

    [Fact]
    public void BuildIssueUrl_EmptyKey_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, JiraConnector.BuildIssueUrl("https://myorg.atlassian.net", string.Empty));
    }

    // ── DaysSince ────────────────────────────────────────────────────────────

    [Fact]
    public void DaysSince_ValidPastDate_ReturnsPositiveDays()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-7).ToString("O");
        Assert.InRange(JiraConnector.DaysSince(date), 6, 8);
    }

    [Fact]
    public void DaysSince_Unparseable_ReturnsZero()
    {
        Assert.Equal(0, JiraConnector.DaysSince("not-a-date"));
        Assert.Equal(0, JiraConnector.DaysSince(null));
    }

    // ── DueInDays ────────────────────────────────────────────────────────────

    [Fact]
    public void DueInDays_FutureDate_ReturnsPositive()
    {
        var future = DateTimeOffset.UtcNow.AddDays(5).ToString("O");
        Assert.True(JiraConnector.DueInDays(future) > 0);
    }

    [Fact]
    public void DueInDays_PastDate_ReturnsNegative()
    {
        var past = DateTimeOffset.UtcNow.AddDays(-3).ToString("O");
        Assert.True(JiraConnector.DueInDays(past) < 0);
    }

    [Fact]
    public void DueInDays_Null_ReturnsNull()
    {
        Assert.Null(JiraConnector.DueInDays(null));
    }
}
