using PriorityHub.Api.Models;

namespace PriorityHub.Ui.Services;

/// <summary>
/// Scores and ranks work items using the same formula as the React frontend's priorities.js.
/// Score = Impact×4 + Urgency×3 + Confidence×1.5 + Age(cap 10) + Blockers×4 + DueDateWeight − Effort×2, clamped 0–100.
/// Bands: ≥82 critical, ≥60 focus, else maintain.
/// </summary>
public sealed class WorkItemRanker
{
    public List<RankedWorkItem> Rank(
        IReadOnlyList<WorkItem> items,
        IReadOnlyList<BoardConnection> boardConnections,
        IReadOnlyList<string> orderedItemIds)
    {
        var orderIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < orderedItemIds.Count; i++)
        {
            orderIndex.TryAdd(orderedItemIds[i], i);
        }

        var boardLookup = boardConnections.ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

        var enriched = new List<RankedWorkItem>();
        foreach (var item in items)
        {
            if (!boardLookup.TryGetValue(item.BoardId, out var board))
            {
                continue;
            }

            var score = Clamp(
                item.Impact * 4
                + item.Urgency * 3
                + item.Confidence * 1.5
                + Math.Min(item.AgeDays, 10)
                + item.BlockerCount * 4
                + DueDateWeight(item.DueInDays)
                - item.Effort * 2,
                0, 100);

            var band = score >= 82 ? "critical" : score >= 60 ? "focus" : "maintain";
            var order = orderIndex.TryGetValue(item.Id, out var idx) ? idx : int.MaxValue;

            enriched.Add(new RankedWorkItem(item, score, band, board.BoardName, board.ProjectName, order));
        }

        var newItems = enriched
            .Where(i => i.Item.IsNew)
            .OrderByDescending(i => i.Score)
            .ToList();

        var existingItems = enriched
            .Where(i => !i.Item.IsNew)
            .OrderBy(i => i.OrderIndex)
            .ThenByDescending(i => i.Score)
            .ToList();

        newItems.AddRange(existingItems);
        return newItems;
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Min(max, Math.Max(min, value));

    private static double DueDateWeight(int? dueInDays)
    {
        if (dueInDays is null) return 4;
        if (dueInDays <= 1) return 14;
        if (dueInDays <= 3) return 10;
        if (dueInDays <= 7) return 6;
        return 2;
    }

    public static string FormatProviderName(string provider) => provider switch
    {
        "microsoft" => "Microsoft",
        "github" => "GitHub",
        "azure-devops" => "Azure DevOps",
        "jira" => "Jira",
        "microsoft-tasks" => "Microsoft Tasks",
        "outlook-flagged-mail" => "Outlook Flagged Mail",
        "trello" => "Trello",
        _ => provider
    };

    public static string FormatPriorityBand(string band) => band switch
    {
        "critical" => "Critical now",
        "focus" => "Focus next",
        "maintain" => "Maintain",
        _ => band
    };
}

public sealed record RankedWorkItem(
    WorkItem Item,
    double Score,
    string Band,
    string BoardName,
    string ProjectName,
    int OrderIndex);
