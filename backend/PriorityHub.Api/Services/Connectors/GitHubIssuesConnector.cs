using System.Net.Http.Headers;
using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class GitHubIssuesConnector(HttpClient httpClient) : IConnector
{
    public string ProviderKey => "github";
    public string DisplayName => "GitHub Issues";
    public string Description => "Aggregate issues from a GitHub repository using a query filter.";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("owner", "Owner or organization"),
        new("repository", "Repository"),
        new("personalAccessToken", "Personal access token (optional with GitHub sign-in)", "password", false),
        new("query", "Issue query", "textarea", false, "is:open assignee:@me"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<GitHubConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid GitHub connection configuration.");
        return await FetchConnectionAsync(connection, oauthToken, cancellationToken);
    }

    public async Task<ConnectorResult> FetchConnectionAsync(GitHubConnection connection, string? oauthToken, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = ProviderKey,
            WorkspaceName = connection.Owner,
            BoardName = connection.Name,
            ProjectName = connection.Repository,
            Owner = connection.Owner,
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            if (string.IsNullOrWhiteSpace(connection.Owner) || string.IsNullOrWhiteSpace(connection.Repository))
            {
                throw new InvalidOperationException("Missing GitHub owner or repository.");
            }

            var accessToken = ResolveAccessToken(connection, oauthToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("Missing GitHub token. Provide a PAT or sign in with GitHub.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildSearchUrl(connection));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            request.Headers.UserAgent.ParseAdd("PriorityHub");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("items", out var issuesElement) || issuesElement.ValueKind != JsonValueKind.Array)
            {
                result.BoardConnections.Add(boardConnection);
                return result;
            }

            foreach (var issue in issuesElement.EnumerateArray())
            {
                if (issue.TryGetProperty("pull_request", out _))
                {
                    continue;
                }

                var number = issue.TryGetProperty("number", out var numberElement) ? numberElement.GetInt32() : 0;
                var tags = ParseLabels(issue);
                result.WorkItems.Add(new WorkItem
                {
                    Id = number > 0 ? $"GH-{connection.Owner}/{connection.Repository}#{number}" : issue.GetProperty("node_id").GetString() ?? string.Empty,
                    Provider = ProviderKey,
                    BoardId = connection.Id,
                    SourceUrl = issue.TryGetProperty("html_url", out var urlElement) ? urlElement.GetString() ?? string.Empty : string.Empty,
                    Title = issue.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "Untitled issue" : "Untitled issue",
                    Status = MapStatus(issue),
                    Assignee = ReadPrimaryAssignee(issue),
                    Effort = 3,
                    Impact = CalculateImpact(tags),
                    Urgency = CalculateUrgency(tags),
                    Confidence = 7,
                    AgeDays = DaysSince(issue.TryGetProperty("updated_at", out var updatedElement) ? updatedElement.GetString() : null),
                    BlockerCount = tags.Any(label => label.Equals("blocked", StringComparison.OrdinalIgnoreCase)) ? 1 : 0,
                    DueInDays = null,
                    TargetDate = null,
                    IsBlocked = tags.Any(label => label.Equals("blocked", StringComparison.OrdinalIgnoreCase)),
                    Tags = tags
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
                Provider = ProviderKey,
                ConnectionId = connection.Id,
                Message = exception.Message
            });
        }

        return result;
    }

    internal static string ResolveAccessToken(GitHubConnection connection, string? oauthToken)
    {
        if (!string.IsNullOrWhiteSpace(connection.PersonalAccessToken))
        {
            return connection.PersonalAccessToken;
        }

        return oauthToken ?? string.Empty;
    }

    internal static string BuildSearchUrl(GitHubConnection connection)
    {
        var queryParts = new List<string>();
        var trimmedQuery = connection.Query?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(trimmedQuery))
        {
            queryParts.Add(trimmedQuery);
        }
        else
        {
            queryParts.Add("is:open");
        }

        if (!trimmedQuery.Contains("repo:", StringComparison.OrdinalIgnoreCase))
        {
            queryParts.Add($"repo:{connection.Owner}/{connection.Repository}");
        }

        if (!trimmedQuery.Contains("is:issue", StringComparison.OrdinalIgnoreCase) && !trimmedQuery.Contains("type:issue", StringComparison.OrdinalIgnoreCase))
        {
            queryParts.Add("is:issue");
        }

        var query = string.Join(' ', queryParts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return $"https://api.github.com/search/issues?per_page=100&q={Uri.EscapeDataString(query)}";
    }

    private static string ReadPrimaryAssignee(JsonElement issue)
    {
        if (!issue.TryGetProperty("assignees", out var assigneesElement) || assigneesElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var assignee in assigneesElement.EnumerateArray())
        {
            if (assignee.TryGetProperty("login", out var loginElement) && !string.IsNullOrWhiteSpace(loginElement.GetString()))
            {
                return loginElement.GetString()!;
            }
        }

        return string.Empty;
    }

    private static List<string> ParseLabels(JsonElement issue)
    {
        if (!issue.TryGetProperty("labels", out var labelsElement) || labelsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return labelsElement.EnumerateArray()
            .Select(label => label.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Cast<string>()
            .ToList();
    }

    internal static string MapStatus(JsonElement issue)
    {
        var state = issue.TryGetProperty("state", out var stateElement)
            ? stateElement.GetString()?.ToLowerInvariant() ?? string.Empty
            : string.Empty;
        var labels = ParseLabels(issue).Select(label => label.ToLowerInvariant()).ToList();

        if (state == "closed") return "done";
        if (labels.Any(label => label.Contains("block"))) return "blocked";
        if (labels.Any(label => label.Contains("review") || label.Contains("qa"))) return "review";
        if (labels.Any(label => label.Contains("in progress") || label.Contains("doing") || label.Contains("active"))) return "in-progress";
        return "planned";
    }

    internal static int CalculateImpact(IEnumerable<string> tags)
    {
        var labels = tags.Select(tag => tag.ToLowerInvariant()).ToList();
        if (labels.Any(label => label.Contains("critical") || label.Contains("high"))) return 9;
        if (labels.Any(label => label.Contains("low"))) return 4;
        return 6;
    }

    internal static int CalculateUrgency(IEnumerable<string> tags)
    {
        var labels = tags.Select(tag => tag.ToLowerInvariant()).ToList();
        if (labels.Any(label => label.Contains("urgent") || label.Contains("p0") || label.Contains("p1"))) return 9;
        if (labels.Any(label => label.Contains("p2") || label.Contains("p3"))) return 6;
        return 5;
    }

    internal static int DaysSince(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return 0;
        return Math.Max(0, (int)Math.Round((DateTimeOffset.UtcNow - parsed).TotalDays));
    }
}