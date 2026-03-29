using PriorityHub.Api.Models;
using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Services;

public class WorkItemRankerTests
{
    private readonly WorkItemRanker _ranker = new();

    private static WorkItem MakeItem(string id, int impact = 5, int urgency = 5, int confidence = 5,
        int effort = 3, int ageDays = 5, int blockerCount = 0, int? dueInDays = null, bool isNew = false,
        string boardId = "board-1", string status = "planned", bool isBlocked = false, DateTimeOffset? targetDate = null) =>
        new()
        {
            Id = id, BoardId = boardId, Title = $"Item {id}", Status = status,
            Impact = impact, Urgency = urgency, Confidence = confidence,
            Effort = effort, AgeDays = ageDays, BlockerCount = blockerCount,
            DueInDays = dueInDays, IsNew = isNew, IsBlocked = isBlocked, TargetDate = targetDate
        };

    private static BoardConnection MakeBoard(string id = "board-1") =>
        new() { Id = id, BoardName = "Test Board", ProjectName = "Test Project" };

    [Fact]
    public void Rank_SkipsItemsWithoutMatchingBoard()
    {
        var items = new[] { MakeItem("1", boardId: "missing-board") };
        var boards = new[] { MakeBoard("board-1") };

        var result = _ranker.Rank(items, boards, []);

        Assert.Empty(result);
    }

    [Fact]
    public void Rank_ScoreIsClampedTo0Through100()
    {
        // Very low scores (negative before clamping)
        var low = MakeItem("1", impact: 0, urgency: 0, confidence: 0, effort: 10, ageDays: 0, blockerCount: 0, dueInDays: 30);
        // Very high scores (over 100 before clamping)
        var high = MakeItem("2", impact: 10, urgency: 10, confidence: 10, effort: 0, ageDays: 100, blockerCount: 5, dueInDays: 0);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([low, high], boards, []);

        Assert.All(result, item => Assert.InRange(item.Score, 0, 100));
    }

    [Fact]
    public void Rank_HighImpact_SortedAboveLowImpact()
    {
        var highImpact = MakeItem("high", impact: 10, urgency: 5, effort: 3);
        var lowImpact = MakeItem("low", impact: 1, urgency: 5, effort: 3);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([lowImpact, highImpact], boards, []);

        Assert.Equal("high", result[0].Item.Id);
        Assert.Equal("low", result[1].Item.Id);
    }

    [Fact]
    public void Rank_BandAssignment_Critical()
    {
        var item = MakeItem("1", impact: 10, urgency: 10, confidence: 10, effort: 0, ageDays: 10, blockerCount: 2, dueInDays: 1);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([item], boards, []);

        Assert.Equal("critical", result[0].Band);
    }

    [Fact]
    public void Rank_BandAssignment_Focus()
    {
        // Score: 8*4 + 6*3 + 5*1.5 + 5 + 0*4 + 4 - 2*2 = 32+18+7.5+5+0+4-4 = 62.5 => focus
        var item = MakeItem("1", impact: 8, urgency: 6, confidence: 5, effort: 2, ageDays: 5, blockerCount: 0, dueInDays: null);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([item], boards, []);

        Assert.Equal("focus", result[0].Band);
    }

    [Fact]
    public void Rank_BandAssignment_Maintain()
    {
        var item = MakeItem("1", impact: 2, urgency: 2, confidence: 2, effort: 5, ageDays: 1, blockerCount: 0, dueInDays: 30);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([item], boards, []);

        Assert.Equal("maintain", result[0].Band);
    }

    [Fact]
    public void Rank_NewItemsSortedByScore_BeforeExisting()
    {
        var existingHigh = MakeItem("e-high", impact: 10, isNew: false);
        var newLow = MakeItem("n-low", impact: 1, isNew: true);
        var newHigh = MakeItem("n-high", impact: 10, isNew: true);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([existingHigh, newLow, newHigh], boards, ["e-high"]);

        Assert.Equal("n-high", result[0].Item.Id);
        Assert.Equal("n-low", result[1].Item.Id);
        Assert.Equal("e-high", result[2].Item.Id);
    }

    [Fact]
    public void Rank_ExistingItemsUsManualOrder_ThenScore()
    {
        var item1 = MakeItem("1", impact: 10, isNew: false);
        var item2 = MakeItem("2", impact: 5, isNew: false);
        var item3 = MakeItem("3", impact: 1, isNew: false);
        var boards = new[] { MakeBoard() };

        // Manual order puts low-score item first
        var orderedIds = new List<string> { "3", "1", "2" };
        var result = _ranker.Rank([item1, item2, item3], boards, orderedIds);

        Assert.Equal("3", result[0].Item.Id);
        Assert.Equal("1", result[1].Item.Id);
        Assert.Equal("2", result[2].Item.Id);
    }

    [Fact]
    public void Rank_DueDateWeight_OverdueItemsScoreHigher()
    {
        var overdue = MakeItem("overdue", impact: 5, urgency: 5, confidence: 5, effort: 3, dueInDays: 0);
        var farOut = MakeItem("farout", impact: 5, urgency: 5, confidence: 5, effort: 3, dueInDays: 30);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([farOut, overdue], boards, []);

        Assert.True(result.First(i => i.Item.Id == "overdue").Score >
                    result.First(i => i.Item.Id == "farout").Score);
    }

    [Fact]
    public void Rank_AgeCappedAt10()
    {
        var young = MakeItem("young", ageDays: 5);
        var ancient = MakeItem("ancient", ageDays: 500);
        var boards = new[] { MakeBoard() };

        var resultYoung = _ranker.Rank([young], boards, []);
        var resultAncient = _ranker.Rank([ancient], boards, []);

        // Age contribution should be 5 vs 10 (capped) — 5 point difference
        var diff = resultAncient[0].Score - resultYoung[0].Score;
        Assert.Equal(5, diff, 0.01);
    }

    [Fact]
    public void FormatProviderName_ReturnsCorrectNames()
    {
        Assert.Equal("Azure DevOps", WorkItemRanker.FormatProviderName("azure-devops"));
        Assert.Equal("GitHub", WorkItemRanker.FormatProviderName("github"));
        Assert.Equal("Jira", WorkItemRanker.FormatProviderName("jira"));
        Assert.Equal("Microsoft Tasks", WorkItemRanker.FormatProviderName("microsoft-tasks"));
        Assert.Equal("Outlook Flagged Mail", WorkItemRanker.FormatProviderName("outlook-flagged-mail"));
        Assert.Equal("Trello", WorkItemRanker.FormatProviderName("trello"));
        Assert.Equal("custom", WorkItemRanker.FormatProviderName("custom"));
    }

    [Fact]
    public void FormatPriorityBand_ReturnsCorrectLabels()
    {
        Assert.Equal("Critical now", WorkItemRanker.FormatPriorityBand("critical"));
        Assert.Equal("Focus next", WorkItemRanker.FormatPriorityBand("focus"));
        Assert.Equal("Maintain", WorkItemRanker.FormatPriorityBand("maintain"));
        Assert.Equal("custom", WorkItemRanker.FormatPriorityBand("custom"));
    }

    [Fact]
    public void Rank_BlockedItem_ScoresHigherThanUnblocked()
    {
        var blocked = MakeItem("blocked", isBlocked: true);
        var unblocked = MakeItem("unblocked", isBlocked: false);
        var boards = new[] { MakeBoard() };

        var result = _ranker.Rank([blocked, unblocked], boards, []);

        Assert.True(result.First(i => i.Item.Id == "blocked").Score >
                    result.First(i => i.Item.Id == "unblocked").Score);
    }

    [Fact]
    public void Rank_BlockedItem_BandAssignment()
    {
        // Score with isBlocked: 5*4 + 5*3 + 5*1.5 + 5 + 0*4 + 4 + 6 - 3*2 = 20+15+7.5+5+4+6-6 = 51.5 => maintain
        var blocked = MakeItem("blocked", isBlocked: true);
        // Score without: 20+15+7.5+5+4 - 6 = 45.5 => maintain
        var unblocked = MakeItem("unblocked", isBlocked: false);
        var boards = new[] { MakeBoard() };

        var resultBlocked = _ranker.Rank([blocked], boards, []);
        var resultUnblocked = _ranker.Rank([unblocked], boards, []);

        // Both maintain band, but blocked scores higher
        Assert.Equal("maintain", resultBlocked[0].Band);
        Assert.Equal("maintain", resultUnblocked[0].Band);
        Assert.Equal(6.0, resultBlocked[0].Score - resultUnblocked[0].Score, 0.01);
    }

    [Fact]
    public void FormatDaysLeft_NullReturnsEmpty()
    {
        Assert.Equal(string.Empty, WorkItemRanker.FormatDaysLeft(null));
    }

    [Fact]
    public void FormatDaysLeft_OverdueReturnsNegativeMessage()
    {
        var pastDate = DateTimeOffset.UtcNow.AddDays(-3);
        var result = WorkItemRanker.FormatDaysLeft(pastDate);
        Assert.Contains("Overdue", result);
        Assert.Contains("3", result);
    }

    [Fact]
    public void FormatDaysLeft_TodayReturnsDueToday()
    {
        var today = DateTimeOffset.UtcNow;
        var result = WorkItemRanker.FormatDaysLeft(today);
        Assert.Equal("Due today", result);
    }

    [Fact]
    public void FormatDaysLeft_FutureReturnsCountdown()
    {
        var future = DateTimeOffset.UtcNow.AddDays(5);
        var result = WorkItemRanker.FormatDaysLeft(future);
        Assert.Contains("5", result);
        Assert.Contains("left", result);
    }
}
