using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class JiraConnector(HttpClient httpClient) : IConnector
{
    // IConnector metadata
    public string ProviderKey => "jira";
    public string DisplayName => "Jira";
    public string Description => "Aggregate issues from a Jira project using JQL.";
    public string DefaultEmoji => "📋";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("baseUrl", "Base URL"),
        new("project", "Project key (optional)", "text", false),
        new("email", "Email"),
        new("apiToken", "API token", "password"),
        new("jql", "JQL", "textarea", true,
            "assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<JiraConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid Jira connection configuration.");
        return await FetchConnectionAsync(connection, cancellationToken);
    }

    public async Task<ConnectorResult> FetchAsync(IEnumerable<JiraConnection> connections, CancellationToken cancellationToken)
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

    public async Task<ConnectorResult> FetchConnectionAsync(JiraConnection connection, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = "jira",
            Emoji = string.IsNullOrWhiteSpace(connection.Emoji) ? DefaultEmoji : connection.Emoji,
            WorkspaceName = string.Empty,
            BoardName = connection.Name,
            ProjectName = string.Empty,
            Owner = connection.Email,
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            if (string.IsNullOrWhiteSpace(connection.BaseUrl) || string.IsNullOrWhiteSpace(connection.Email) || string.IsNullOrWhiteSpace(connection.ApiToken))
            {
                throw new InvalidOperationException("Missing Jira base URL, email, or API token.");
            }

            var baseUri = new Uri(connection.BaseUrl);
            boardConnection.WorkspaceName = baseUri.Host;
            boardConnection.ProjectName = !string.IsNullOrWhiteSpace(connection.Project) ? connection.Project : baseUri.Host;

            var effectiveJql = connection.Jql;
            if (!string.IsNullOrWhiteSpace(connection.Project))
            {
                effectiveJql = $"project = \"{connection.Project}\" AND {connection.Jql}";
            }

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{connection.Email}:{connection.ApiToken}"));
            var url = $"{connection.BaseUrl.TrimEnd('/')}/rest/api/3/search/jql?maxResults=50&fields=summary,status,assignee,labels,priority,duedate,created,updated,issuelinks&jql={Uri.EscapeDataString(effectiveJql)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("issues", out var issuesElement))
            {
                result.BoardConnections.Add(boardConnection);
                return result;
            }

            foreach (var issue in issuesElement.EnumerateArray())
            {
                var fields = issue.GetProperty("fields");
                var priorityName = fields.TryGetProperty("priority", out var priorityElement) && priorityElement.TryGetProperty("name", out var priorityNameElement)
                    ? priorityNameElement.GetString()
                    : null;

                result.WorkItems.Add(new WorkItem
                {
                    Id = issue.GetProperty("key").GetString() ?? string.Empty,
                    Provider = "jira",
                    BoardId = connection.Id,
                    SourceUrl = BuildIssueUrl(connection.BaseUrl, issue.GetProperty("key").GetString()),
                    Title = ReadString(fields, "summary") ?? "Untitled issue",
                    Status = MapStatus(fields.GetProperty("status").GetProperty("name").GetString()),
                    Assignee = fields.TryGetProperty("assignee", out var assigneeElement) && assigneeElement.ValueKind == JsonValueKind.Object
                        ? assigneeElement.GetProperty("displayName").GetString() ?? connection.Email
                        : connection.Email,
                    Effort = 3,
                    Impact = PriorityToScore(priorityName),
                    Urgency = PriorityToScore(priorityName),
                    Confidence = 7,
                    AgeDays = DaysSince(ReadString(fields, "updated") ?? ReadString(fields, "created")),
                    BlockerCount = fields.TryGetProperty("issuelinks", out var linksElement) && linksElement.ValueKind == JsonValueKind.Array ? linksElement.GetArrayLength() : 0,
                    DueInDays = DueInDays(ReadString(fields, "duedate")),
                    TargetDate = ParseTargetDate(ReadString(fields, "duedate")),
                    IsBlocked = MapStatus(fields.GetProperty("status").GetProperty("name").GetString()) == "blocked",
                    Tags = fields.TryGetProperty("labels", out var labelsElement) && labelsElement.ValueKind == JsonValueKind.Array
                        ? labelsElement.EnumerateArray().Select(label => label.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToList()
                        : []
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
                Provider = "jira",
                ConnectionId = connection.Id,
                Message = exception.Message
            });
        }

        return result;
    }

    internal static string MapStatus(string? value)
    {
        var normalized = value?.ToLowerInvariant() ?? string.Empty;
        if (normalized.Contains("done") || normalized.Contains("closed") || normalized.Contains("resolved")) return "done";
        if (normalized.Contains("review") || normalized.Contains("qa") || normalized.Contains("verify")) return "review";
        if (normalized.Contains("block")) return "blocked";
        if (normalized.Contains("progress") || normalized.Contains("doing") || normalized.Contains("active")) return "in-progress";
        return "planned";
    }

    internal static int PriorityToScore(string? value)
    {
        var normalized = value?.ToLowerInvariant() ?? string.Empty;
        if (normalized.Contains("highest") || normalized.Contains("critical") || normalized.Contains("blocker")) return 10;
        if (normalized.Contains("high")) return 8;
        if (normalized.Contains("low")) return 4;
        return 6;
    }

    private static string? ReadString(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value.GetString() : null;

    internal static string BuildIssueUrl(string baseUrl, string? issueKey)
    {
        return string.IsNullOrWhiteSpace(issueKey) ? string.Empty : $"{baseUrl.TrimEnd('/')}/browse/{Uri.EscapeDataString(issueKey)}";
    }

    internal static int DaysSince(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return 0;
        return Math.Max(0, (int)Math.Round((DateTimeOffset.UtcNow - parsed).TotalDays));
    }

    internal static int? DueInDays(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return null;
        return (int)Math.Round((parsed - DateTimeOffset.UtcNow).TotalDays);
    }

    internal static DateTimeOffset? ParseTargetDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    }
}