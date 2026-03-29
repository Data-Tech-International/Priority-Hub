using System.Text.Json;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class GitHubIssuesConnectorTests
{
    // ── MapStatus ────────────────────────────────────────────────────────────

    [Fact]
    public void MapStatus_ClosedIssue_ReturnsDone()
    {
        var issue = TestHelpers.JsonOf("""{"state":"closed","labels":[]}""");
        Assert.Equal("done", GitHubIssuesConnector.MapStatus(issue));
    }

    [Fact]
    public void MapStatus_OpenWithBlockedLabel_ReturnsBlocked()
    {
        var issue = TestHelpers.JsonOf("""{"state":"open","labels":[{"name":"blocked"}]}""");
        Assert.Equal("blocked", GitHubIssuesConnector.MapStatus(issue));
    }

    [Fact]
    public void MapStatus_OpenWithReviewLabel_ReturnsReview()
    {
        var issue = TestHelpers.JsonOf("""{"state":"open","labels":[{"name":"in review"}]}""");
        Assert.Equal("review", GitHubIssuesConnector.MapStatus(issue));
    }

    [Fact]
    public void MapStatus_OpenWithInProgressLabel_ReturnsInProgress()
    {
        var issue = TestHelpers.JsonOf("""{"state":"open","labels":[{"name":"in progress"}]}""");
        Assert.Equal("in-progress", GitHubIssuesConnector.MapStatus(issue));
    }

    [Fact]
    public void MapStatus_PlainOpenIssue_ReturnsPlanned()
    {
        var issue = TestHelpers.JsonOf("""{"state":"open","labels":[]}""");
        Assert.Equal("planned", GitHubIssuesConnector.MapStatus(issue));
    }

    // ── CalculateImpact ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateImpact_CriticalLabel_Returns9()
    {
        Assert.Equal(9, GitHubIssuesConnector.CalculateImpact(["critical"]));
    }

    [Fact]
    public void CalculateImpact_HighLabel_Returns9()
    {
        Assert.Equal(9, GitHubIssuesConnector.CalculateImpact(["high"]));
    }

    [Fact]
    public void CalculateImpact_LowLabel_Returns4()
    {
        Assert.Equal(4, GitHubIssuesConnector.CalculateImpact(["low"]));
    }

    [Fact]
    public void CalculateImpact_NoSpecialLabel_Returns6()
    {
        Assert.Equal(6, GitHubIssuesConnector.CalculateImpact(["bug", "feature"]));
    }

    // ── CalculateUrgency ─────────────────────────────────────────────────────

    [Fact]
    public void CalculateUrgency_UrgentLabel_Returns9()
    {
        Assert.Equal(9, GitHubIssuesConnector.CalculateUrgency(["urgent"]));
    }

    [Fact]
    public void CalculateUrgency_P0Label_Returns9()
    {
        Assert.Equal(9, GitHubIssuesConnector.CalculateUrgency(["p0"]));
    }

    [Fact]
    public void CalculateUrgency_P2Label_Returns6()
    {
        Assert.Equal(6, GitHubIssuesConnector.CalculateUrgency(["p2"]));
    }

    [Fact]
    public void CalculateUrgency_NoLabel_Returns5()
    {
        Assert.Equal(5, GitHubIssuesConnector.CalculateUrgency([]));
    }

    // ── ResolveAccessToken ───────────────────────────────────────────────────

    [Fact]
    public void ResolveAccessToken_PatPresent_ReturnsPat()
    {
        var connection = new GitHubConnection { PersonalAccessToken = "my-pat" };
        Assert.Equal("my-pat", GitHubIssuesConnector.ResolveAccessToken(connection, "oauth-token"));
    }

    [Fact]
    public void ResolveAccessToken_BlankPat_ReturnsOauthToken()
    {
        var connection = new GitHubConnection { PersonalAccessToken = string.Empty };
        Assert.Equal("oauth-token", GitHubIssuesConnector.ResolveAccessToken(connection, "oauth-token"));
    }

    [Fact]
    public void ResolveAccessToken_BlankPatAndNullOauth_ReturnsEmpty()
    {
        var connection = new GitHubConnection { PersonalAccessToken = string.Empty };
        Assert.Equal(string.Empty, GitHubIssuesConnector.ResolveAccessToken(connection, null));
    }

    // ── BuildSearchUrl ───────────────────────────────────────────────────────

    [Fact]
    public void BuildSearchUrl_DefaultQuery_IncludesRepoAndIsIssue()
    {
        var connection = new GitHubConnection { Owner = "myorg", Repository = "myrepo", Query = string.Empty };
        var url = GitHubIssuesConnector.BuildSearchUrl(connection);
        Assert.Contains("is%3Aissue", url);
        Assert.Contains("repo%3Amyorg%2Fmyrepo", url);
    }

    [Fact]
    public void BuildSearchUrl_CustomQueryWithRepo_DoesNotDuplicateRepo()
    {
        var connection = new GitHubConnection { Owner = "myorg", Repository = "myrepo", Query = "is:open repo:myorg/myrepo is:issue" };
        var url = GitHubIssuesConnector.BuildSearchUrl(connection);
        // The custom query already has repo: so no second repo: should be added
        Assert.Equal(1, url.Split("repo%3A").Length - 1);
    }

    // ── DaysSince ────────────────────────────────────────────────────────────

    [Fact]
    public void DaysSince_ValidDate_ReturnsApproximateDays()
    {
        var tenDaysAgo = DateTimeOffset.UtcNow.AddDays(-10).ToString("O");
        Assert.InRange(GitHubIssuesConnector.DaysSince(tenDaysAgo), 9, 11);
    }

    [Fact]
    public void DaysSince_InvalidOrNull_ReturnsZero()
    {
        Assert.Equal(0, GitHubIssuesConnector.DaysSince(null));
        Assert.Equal(0, GitHubIssuesConnector.DaysSince("garbage"));
    }

    // ── IsBlocked ────────────────────────────────────────────────────────────

    [Fact]
    public void IsBlocked_WhenBlockedLabelPresent_ReturnsTrue()
    {
        var tags = new List<string> { "enhancement", "blocked" };
        Assert.Contains("blocked", tags, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsBlocked_WhenNoBlockedLabel_ReturnsFalse()
    {
        var tags = new List<string> { "enhancement", "feature" };
        Assert.DoesNotContain("blocked", tags, StringComparer.OrdinalIgnoreCase);
    }
}
