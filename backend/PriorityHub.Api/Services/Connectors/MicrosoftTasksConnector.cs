using System.Net.Http.Headers;
using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class MicrosoftTasksConnector(HttpClient httpClient) : IConnector
{
    private const int MaximumTasksPerConnection = 200;
    private const string MicrosoftToDoWebUrl = "https://to-do.office.com/tasks/";

    public string ProviderKey => "microsoft-tasks";
    public string DisplayName => "Microsoft Tasks";
    public string Description => "Aggregate Microsoft To Do tasks from Microsoft Graph.";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("taskListName", "Task list name (optional)", "text", false),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<MicrosoftTasksConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid Microsoft Tasks connection configuration.");

        return await FetchConnectionAsync(connection, oauthToken, cancellationToken);
    }

    private async Task<ConnectorResult> FetchConnectionAsync(MicrosoftTasksConnection connection, string? oauthToken, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = ProviderKey,
            WorkspaceName = "Microsoft 365",
            BoardName = string.IsNullOrWhiteSpace(connection.Name) ? "Microsoft Tasks" : connection.Name,
            ProjectName = string.IsNullOrWhiteSpace(connection.TaskListName) ? "All task lists" : connection.TaskListName,
            Owner = "Me",
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            if (string.IsNullOrWhiteSpace(oauthToken))
            {
                throw new InvalidOperationException("Missing Microsoft sign-in token. Sign in with Microsoft to load tasks.");
            }

            var taskLists = await FetchTaskListsAsync(oauthToken, cancellationToken);
            if (!string.IsNullOrWhiteSpace(connection.TaskListName))
            {
                taskLists = taskLists
                    .Where(list => string.Equals(list.DisplayName, connection.TaskListName.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (taskLists.Count == 0)
                {
                    throw new InvalidOperationException($"No Microsoft To Do list matched '{connection.TaskListName}'.");
                }
            }

            var fetchedCount = 0;
            foreach (var taskList in taskLists)
            {
                var remaining = MaximumTasksPerConnection - fetchedCount;
                if (remaining <= 0)
                {
                    break;
                }

                var tasks = await FetchTasksAsync(taskList.Id, oauthToken, remaining, cancellationToken);
                foreach (var task in tasks)
                {
                    if (IsCompletedStatus(task.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null))
                    {
                        continue;
                    }

                    fetchedCount += 1;
                    result.WorkItems.Add(MapTask(connection, taskList.DisplayName, task));
                }
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

    private async Task<List<TaskListRecord>> FetchTaskListsAsync(string accessToken, CancellationToken cancellationToken)
    {
        var url = "https://graph.microsoft.com/v1.0/me/todo/lists";
        var lists = await FetchGraphCollectionAsync(url, accessToken, cancellationToken, 100);
        return lists.Select(list => new TaskListRecord(
                list.TryGetProperty("id", out var idElement) ? idElement.GetString() ?? string.Empty : string.Empty,
                list.TryGetProperty("displayName", out var displayNameElement) ? displayNameElement.GetString() ?? "Tasks" : "Tasks"))
            .Where(list => !string.IsNullOrWhiteSpace(list.Id))
            .ToList();
    }

    private Task<List<JsonElement>> FetchTasksAsync(string taskListId, string accessToken, int limit, CancellationToken cancellationToken)
    {
        var encodedTaskListId = Uri.EscapeDataString(taskListId);
        var statusFilter = Uri.EscapeDataString("status ne 'completed'");
        var url = $"https://graph.microsoft.com/v1.0/me/todo/lists/{encodedTaskListId}/tasks?$filter={statusFilter}";
        return FetchGraphCollectionAsync(url, accessToken, cancellationToken, limit);
    }

    private async Task<List<JsonElement>> FetchGraphCollectionAsync(string initialUrl, string accessToken, CancellationToken cancellationToken, int limit)
    {
        var items = new List<JsonElement>(Math.Min(limit, 100));
        var nextUrl = initialUrl;

        while (!string.IsNullOrWhiteSpace(nextUrl) && items.Count < limit)
        {
            using var document = await SendGraphRequestAsync(nextUrl, accessToken, cancellationToken);
            if (!document.RootElement.TryGetProperty("value", out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            foreach (var item in valuesElement.EnumerateArray())
            {
                items.Add(item.Clone());
                if (items.Count >= limit)
                {
                    break;
                }
            }

            nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement)
                ? nextLinkElement.GetString() ?? string.Empty
                : string.Empty;
        }

        return items;
    }

    private async Task<JsonDocument> SendGraphRequestAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(FormatGraphError("Microsoft Tasks request failed", response.StatusCode, body));
        }

        return JsonDocument.Parse(body);
    }

    private static WorkItem MapTask(MicrosoftTasksConnection connection, string taskListName, JsonElement task)
    {
        var title = task.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "Untitled task" : "Untitled task";
        var importance = task.TryGetProperty("importance", out var importanceElement) ? importanceElement.GetString() : null;
        var status = task.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
        var ageSource = task.TryGetProperty("lastModifiedDateTime", out var modifiedElement)
            ? modifiedElement.GetString()
            : task.TryGetProperty("createdDateTime", out var createdElement)
                ? createdElement.GetString()
                : null;
        var dueInDays = ReadDateOffsetDays(task, "dueDateTime");

        var tags = new List<string> { taskListName };
        if (task.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
        {
            tags.AddRange(categoriesElement.EnumerateArray()
                .Select(category => category.GetString())
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Cast<string>());
        }

        return new WorkItem
        {
            Id = $"MSTODO-{task.GetProperty("id").GetString()}",
            Provider = "microsoft-tasks",
            BoardId = connection.Id,
            SourceUrl = ResolveSourceUrl(task),
            Title = title,
            Status = MapStatus(status),
            Assignee = "Me",
            Effort = 2,
            Impact = MapImpact(importance),
            Urgency = MapUrgency(importance, dueInDays),
            Confidence = 8,
            AgeDays = DaysSince(ageSource),
            BlockerCount = string.Equals(status, "waitingOnOthers", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            DueInDays = dueInDays,
            Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    internal static string ResolveSourceUrl(JsonElement task)
    {
        var linkedResourceUrl = ReadLinkedResourceUrl(task);
        return string.IsNullOrWhiteSpace(linkedResourceUrl)
            ? MicrosoftToDoWebUrl
            : linkedResourceUrl;
    }

    private static string ReadLinkedResourceUrl(JsonElement task)
    {
        if (!task.TryGetProperty("linkedResources", out var linkedResourcesElement) || linkedResourcesElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var linkedResource in linkedResourcesElement.EnumerateArray())
        {
            if (linkedResource.TryGetProperty("webUrl", out var webUrlElement) && !string.IsNullOrWhiteSpace(webUrlElement.GetString()))
            {
                return webUrlElement.GetString()!;
            }
        }

        return string.Empty;
    }

    internal static bool IsCompletedStatus(string? value)
    {
        return string.Equals(value, "completed", StringComparison.OrdinalIgnoreCase);
    }

    internal static int? ReadDateOffsetDays(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var propertyElement) || propertyElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!propertyElement.TryGetProperty("dateTime", out var dateTimeElement))
        {
            return null;
        }

        return DateTimeOffset.TryParse(dateTimeElement.GetString(), out var parsed)
            ? (int)Math.Round((parsed - DateTimeOffset.UtcNow).TotalDays)
            : null;
    }

    internal static string MapStatus(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "completed" => "done",
            "inprogress" => "in-progress",
            "waitingonothers" => "blocked",
            "deferred" => "review",
            _ => "planned"
        };
    }

    internal static int MapImpact(string? importance)
    {
        return importance?.ToLowerInvariant() switch
        {
            "high" => 8,
            "low" => 4,
            _ => 6
        };
    }

    internal static int MapUrgency(string? importance, int? dueInDays)
    {
        var dueScore = dueInDays switch
        {
            <= 0 => 10,
            <= 2 => 9,
            <= 7 => 7,
            _ => 5,
        };

        return Math.Max(dueScore, importance?.ToLowerInvariant() == "high" ? 8 : importance?.ToLowerInvariant() == "low" ? 4 : 6);
    }

    internal static int DaysSince(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed))
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Round((DateTimeOffset.UtcNow - parsed).TotalDays));
    }

    private static string FormatGraphError(string prefix, System.Net.HttpStatusCode statusCode, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var errorElement) &&
                errorElement.TryGetProperty("message", out var messageElement) &&
                !string.IsNullOrWhiteSpace(messageElement.GetString()))
            {
                return $"{prefix}: {messageElement.GetString()}";
            }
        }
        catch
        {
        }

        var snippet = string.IsNullOrWhiteSpace(body)
            ? statusCode.ToString()
            : body.Length > 200 ? body[..200] : body;
        return $"{prefix}: {snippet}";
    }

    private sealed record TaskListRecord(string Id, string DisplayName);
}
