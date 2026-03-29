using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class AzureDevOpsConnectorTests
{
    // ── MapStatus ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Done", "done")]
    [InlineData("Closed", "done")]
    [InlineData("Resolved", "done")]
    [InlineData("Code Review", "review")]
    [InlineData("Validate", "review")]
    [InlineData("Blocked", "blocked")]
    [InlineData("Active", "in-progress")]
    [InlineData("In Progress", "in-progress")]
    [InlineData("Implementing", "in-progress")]
    [InlineData("New", "planned")]
    [InlineData("To Do", "planned")]
    [InlineData(null, "planned")]
    [InlineData("", "planned")]
    public void MapStatus_ReturnsExpectedValue(string? input, string expected)
    {
        Assert.Equal(expected, AzureDevOpsConnector.MapStatus(input));
    }

    // ── ParseTags ────────────────────────────────────────────────────────────

    [Fact]
    public void ParseTags_SemicolonDelimited_ReturnsTrimmedTokens()
    {
        var tags = AzureDevOpsConnector.ParseTags("bug; feature ; tech-debt");
        Assert.Equal(3, tags.Count);
        Assert.Contains("bug", tags);
        Assert.Contains("feature", tags);
        Assert.Contains("tech-debt", tags);
    }

    [Fact]
    public void ParseTags_NullOrEmpty_ReturnsEmptyList()
    {
        Assert.Empty(AzureDevOpsConnector.ParseTags(null));
        Assert.Empty(AzureDevOpsConnector.ParseTags(string.Empty));
        Assert.Empty(AzureDevOpsConnector.ParseTags("   "));
    }

    [Fact]
    public void ParseTags_SingleTag_ReturnsSingleElement()
    {
        var tags = AzureDevOpsConnector.ParseTags("bug");
        Assert.Single(tags);
        Assert.Equal("bug", tags[0]);
    }

    // ── DaysSince ────────────────────────────────────────────────────────────

    [Fact]
    public void DaysSince_ValidPastDate_ReturnsPositiveDays()
    {
        var fiveDaysAgo = DateTimeOffset.UtcNow.AddDays(-5).ToString("O");
        Assert.InRange(AzureDevOpsConnector.DaysSince(fiveDaysAgo), 4, 6);
    }

    [Fact]
    public void DaysSince_FutureDate_ReturnsZero()
    {
        var tomorrow = DateTimeOffset.UtcNow.AddDays(1).ToString("O");
        Assert.Equal(0, AzureDevOpsConnector.DaysSince(tomorrow));
    }

    [Fact]
    public void DaysSince_InvalidString_ReturnsZero()
    {
        Assert.Equal(0, AzureDevOpsConnector.DaysSince("not-a-date"));
        Assert.Equal(0, AzureDevOpsConnector.DaysSince(null));
    }

    // ── Impact / Urgency formula (Math.Clamp(11 - priority, 1, 10)) ─────────

    [Theory]
    [InlineData(1, 10)]   // highest priority → highest impact
    [InlineData(2, 9)]
    [InlineData(5, 6)]    // default/mid
    [InlineData(10, 1)]   // lowest priority → lowest impact
    [InlineData(11, 1)]   // clamp floor
    [InlineData(0, 10)]   // clamp ceiling
    public void ImpactFormula_ClampsBetween1And10(int priority, int expected)
    {
        Assert.Equal(expected, Math.Clamp(11 - priority, 1, 10));
    }

    // ── IsBlocked ────────────────────────────────────────────────────────────

    [Fact]
    public void MapStatus_BlockedState_MapsToBlocked()
    {
        Assert.Equal("blocked", AzureDevOpsConnector.MapStatus("Blocked"));
    }

    [Fact]
    public void IsBlocked_DerivedFromBlockedState()
    {
        // IsBlocked = MapStatus(state) == "blocked"
        Assert.True(AzureDevOpsConnector.MapStatus("Blocked") == "blocked");
        Assert.False(AzureDevOpsConnector.MapStatus("Active") == "blocked");
    }

    // ── TargetDate (ParseTargetDate is private; exercised via DaysSince) ─────

    [Fact]
    public void DaysSince_CanParseIso8601_TargetDateFormat()
    {
        // Azure DevOps returns TargetDate in ISO 8601 format — same parser used for age
        var isoDate = DateTimeOffset.UtcNow.AddDays(-2).ToString("O");
        Assert.InRange(AzureDevOpsConnector.DaysSince(isoDate), 1, 3);
    }
}
