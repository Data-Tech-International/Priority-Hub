using System.Text.Json;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class OutlookFlaggedMailConnectorTests
{
    // ── IsFlagged ────────────────────────────────────────────────────────────

    [Fact]
    public void IsFlagged_FlaggedStatus_ReturnsTrue()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"flagged"}}""");
        Assert.True(OutlookFlaggedMailConnector.IsFlagged(message));
    }

    [Fact]
    public void IsFlagged_CompleteStatus_ReturnsTrue()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"complete"}}""");
        Assert.True(OutlookFlaggedMailConnector.IsFlagged(message));
    }

    [Fact]
    public void IsFlagged_NotFlaggedStatus_ReturnsFalse()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"notFlagged"}}""");
        Assert.False(OutlookFlaggedMailConnector.IsFlagged(message));
    }

    [Fact]
    public void IsFlagged_MissingFlagProperty_ReturnsFalse()
    {
        var message = TestHelpers.JsonOf("""{"subject":"No flag"}""");
        Assert.False(OutlookFlaggedMailConnector.IsFlagged(message));
    }

    [Fact]
    public void IsFlagged_FlagPropertyNull_ReturnsFalse()
    {
        var message = TestHelpers.JsonOf("""{"flag":null}""");
        Assert.False(OutlookFlaggedMailConnector.IsFlagged(message));
    }

    // ── MapStatus ────────────────────────────────────────────────────────────

    [Fact]
    public void MapStatus_CompleteFlagStatus_ReturnsDone()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"complete"}}""");
        Assert.Equal("done", OutlookFlaggedMailConnector.MapStatus(message, isRead: false));
    }

    [Fact]
    public void MapStatus_FlaggedAndUnread_ReturnsReview()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"flagged"}}""");
        Assert.Equal("review", OutlookFlaggedMailConnector.MapStatus(message, isRead: false));
    }

    [Fact]
    public void MapStatus_FlaggedAndRead_ReturnsPlanned()
    {
        var message = TestHelpers.JsonOf("""{"flag":{"flagStatus":"flagged"}}""");
        Assert.Equal("planned", OutlookFlaggedMailConnector.MapStatus(message, isRead: true));
    }

    // ── MapImpact ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("high", 8)]
    [InlineData("High", 8)]
    [InlineData("low", 4)]
    [InlineData("normal", 6)]
    [InlineData(null, 6)]
    public void MapImpact_ReturnsExpectedScore(string? importance, int expected)
    {
        Assert.Equal(expected, OutlookFlaggedMailConnector.MapImpact(importance));
    }

    // ── MapUrgency ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("high", false, 9)]   // high importance, unread
    [InlineData("high", true, 8)]    // high importance, read
    [InlineData("low", false, 4)]    // low importance, unread
    [InlineData("low", true, 3)]     // low importance, read
    [InlineData("normal", false, 7)] // normal importance, unread
    [InlineData("normal", true, 5)]  // normal importance, read
    [InlineData(null, false, 7)]     // no importance, unread
    [InlineData(null, true, 5)]      // no importance, read
    public void MapUrgency_ReturnsExpectedScore(string? importance, bool isRead, int expected)
    {
        Assert.Equal(expected, OutlookFlaggedMailConnector.MapUrgency(importance, isRead));
    }

    // ── ParseMaxResults ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("50", 50)]
    [InlineData("200", 200)]
    [InlineData("1", 1)]
    [InlineData("0", 1)]      // clamped to min 1
    [InlineData("300", 200)]  // clamped to max 200
    [InlineData(null, 100)]   // default 100
    [InlineData("abc", 100)]  // unparseable → default 100
    public void ParseMaxResults_ReturnsExpectedValue(string? input, int expected)
    {
        Assert.Equal(expected, OutlookFlaggedMailConnector.ParseMaxResults(input));
    }
}
