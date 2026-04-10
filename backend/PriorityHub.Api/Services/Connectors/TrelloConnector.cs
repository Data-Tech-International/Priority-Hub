using System.Text.Json;
using Microsoft.Extensions.Logging;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class TrelloConnector(HttpClient httpClient, ILogger<TrelloConnector> logger) : IConnector
{
    // IConnector metadata
    public string ProviderKey => "trello";
    public string DisplayName => "Trello";
    public string Description => "Aggregate cards from a Trello board.";
    public string DefaultEmoji => "📌";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("boardId", "Board ID"),
        new("apiKey", "API key", "password"),
        new("token", "Token", "password"),
        new("filterMyCards", "Only show cards assigned to me", "checkbox", false, "true"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<TrelloConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid Trello connection configuration.");
        return await FetchConnectionAsync(connection, cancellationToken);
    }

    public async Task<ConnectorResult> FetchAsync(IEnumerable<TrelloConnection> connections, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();

        foreach (var connection in connections.Where(item => item.Enabled))
        {
            var connectionResult = await FetchConnectionAsync(connection, cancellationToken);
            result.BoardConnections.AddRange(connectionResult.BoardConnections);
            result.WorkItems.AddRange(connectionResult.WorkItems);
            result.Issues.AddRange(connectionResult.Issues);
        }

        return result;
    }

    public async Task<ConnectorResult> FetchConnectionAsync(TrelloConnection connection, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = "trello",
            Emoji = string.IsNullOrWhiteSpace(connection.Emoji) ? DefaultEmoji : connection.Emoji,
            WorkspaceName = "Trello",
            BoardName = connection.Name,
            ProjectName = connection.BoardId,
            Owner = string.Empty,
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            if (string.IsNullOrWhiteSpace(connection.ApiKey) || string.IsNullOrWhiteSpace(connection.Token) || string.IsNullOrWhiteSpace(connection.BoardId))
            {
                throw new InvalidOperationException("Missing Trello board ID, API key, or token.");
            }

            // Resolve the token owner's member ID when filtering is enabled
            string? myMemberId = null;
            if (connection.FilterMyCards)
            {
                myMemberId = await ResolveTokenOwnerMemberIdAsync(connection, cancellationToken);
            }

            var listsUrl = $"https://api.trello.com/1/boards/{Uri.EscapeDataString(connection.BoardId)}/lists?fields=name,closed&key={Uri.EscapeDataString(connection.ApiKey)}&token={Uri.EscapeDataString(connection.Token)}";
            var cardsUrl = $"https://api.trello.com/1/boards/{Uri.EscapeDataString(connection.BoardId)}/cards?fields=name,idList,due,dateLastActivity,closed,labels,idMembers,url,shortLink&key={Uri.EscapeDataString(connection.ApiKey)}&token={Uri.EscapeDataString(connection.Token)}";

            using var listsResponse = await httpClient.GetAsync(listsUrl, cancellationToken);
            using var cardsResponse = await httpClient.GetAsync(cardsUrl, cancellationToken);
            listsResponse.EnsureSuccessStatusCode();
            cardsResponse.EnsureSuccessStatusCode();

            using var listsDocument = JsonDocument.Parse(await listsResponse.Content.ReadAsStringAsync(cancellationToken));
            using var cardsDocument = JsonDocument.Parse(await cardsResponse.Content.ReadAsStringAsync(cancellationToken));

            var listsById = listsDocument.RootElement.EnumerateArray().ToDictionary(
                list => list.GetProperty("id").GetString() ?? string.Empty,
                list => new
                {
                    Name = list.GetProperty("name").GetString() ?? string.Empty,
                    Closed = list.GetProperty("closed").GetBoolean()
                });

            foreach (var card in cardsDocument.RootElement.EnumerateArray())
            {
                // Post-filter by member ID when filtering is active
                if (myMemberId is not null)
                {
                    var memberIds = card.TryGetProperty("idMembers", out var membersEl) && membersEl.ValueKind == JsonValueKind.Array
                        ? membersEl.EnumerateArray().Select(m => m.GetString()).Where(id => id is not null).ToList()
                        : new List<string?>();
                    if (!memberIds.Contains(myMemberId))
                        continue;
                }

                var listId = card.GetProperty("idList").GetString() ?? string.Empty;
                listsById.TryGetValue(listId, out var list);
                var dueValue = card.TryGetProperty("due", out var dueElement) ? dueElement.GetString() : null;
                var dueInDays = DueInDays(dueValue);
                var labels = card.TryGetProperty("labels", out var labelsElement)
                    ? labelsElement.EnumerateArray()
                        .Select(label => label.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Cast<string>()
                        .ToList()
                    : [];

                result.WorkItems.Add(new WorkItem
                {
                    Id = card.GetProperty("id").GetString() ?? string.Empty,
                    Provider = "trello",
                    BoardId = connection.Id,
                    SourceUrl = BuildCardUrl(card),
                    Title = card.GetProperty("name").GetString() ?? "Untitled card",
                    Status = MapStatus(list?.Name, card.GetProperty("closed").GetBoolean() || (list?.Closed ?? false)),
                    Assignee = card.TryGetProperty("idMembers", out var membersElement) && membersElement.ValueKind == JsonValueKind.Array && membersElement.GetArrayLength() > 0
                        ? $"{membersElement.GetArrayLength()} member(s)"
                        : string.Empty,
                    Effort = 2,
                    Impact = labels.Any(label => label.Equals("urgent", StringComparison.OrdinalIgnoreCase)) ? 8 : 6,
                    Urgency = dueInDays is not null && dueInDays <= 2 ? 9 : 6,
                    Confidence = 6,
                    AgeDays = DaysSince(card.TryGetProperty("dateLastActivity", out var activityElement) ? activityElement.GetString() : null),
                    BlockerCount = labels.Any(label => label.Equals("blocked", StringComparison.OrdinalIgnoreCase)) ? 1 : 0,
                    DueInDays = dueInDays,
                    TargetDate = ParseTargetDate(dueValue),
                    IsBlocked = labels.Any(label => label.Equals("blocked", StringComparison.OrdinalIgnoreCase)),
                    Tags = labels
                });
            }

            result.BoardConnections.Add(boardConnection);
        }
        catch (Exception exception)
        {
            boardConnection.SyncStatus = "needs-auth";
            boardConnection.LastSyncMinutesAgo = 999;
            result.BoardConnections.Add(boardConnection);
            result.Issues.Add(new ProviderIssue
            {
                Provider = "trello",
                ConnectionId = connection.Id,
                Message = exception.Message
            });
        }

        return result;
    }

    /// <summary>
    /// Resolves the Trello member ID of the token owner.
    /// Returns null (graceful fallback to unfiltered) on any API failure.
    /// </summary>
    private async Task<string?> ResolveTokenOwnerMemberIdAsync(TrelloConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var memberUrl = $"https://api.trello.com/1/tokens/{Uri.EscapeDataString(connection.Token)}/member?fields=id&key={Uri.EscapeDataString(connection.ApiKey)}";
            using var memberResponse = await httpClient.GetAsync(memberUrl, cancellationToken);
            memberResponse.EnsureSuccessStatusCode();
            using var memberDocument = JsonDocument.Parse(await memberResponse.Content.ReadAsStringAsync(cancellationToken));
            var memberId = memberDocument.RootElement.GetProperty("id").GetString();
            return memberId;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Trello [{ConnectionId}] failed to resolve token owner member ID; falling back to unfiltered cards.", connection.Id);
            return null;
        }
    }

    internal static string MapStatus(string? listName, bool closed)
    {
        if (closed) return "done";
        var normalized = listName?.ToLowerInvariant() ?? string.Empty;
        if (normalized.Contains("done") || normalized.Contains("complete")) return "done";
        if (normalized.Contains("review") || normalized.Contains("qa")) return "review";
        if (normalized.Contains("block")) return "blocked";
        if (normalized.Contains("doing") || normalized.Contains("progress") || normalized.Contains("active")) return "in-progress";
        return "planned";
    }

    internal static int DaysSince(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return 0;
        return Math.Max(0, (int)Math.Round((DateTimeOffset.UtcNow - parsed).TotalDays));
    }

    private static int? DueInDays(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return null;
        return (int)Math.Round((parsed - DateTimeOffset.UtcNow).TotalDays);
    }

    internal static DateTimeOffset? ParseTargetDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }

    private static string BuildCardUrl(JsonElement card)
    {
        if (card.TryGetProperty("url", out var urlElement) && !string.IsNullOrWhiteSpace(urlElement.GetString()))
        {
            return urlElement.GetString()!;
        }

        if (card.TryGetProperty("shortLink", out var shortLinkElement) && !string.IsNullOrWhiteSpace(shortLinkElement.GetString()))
        {
            return $"https://trello.com/c/{shortLinkElement.GetString()}";
        }

        return string.Empty;
    }
}