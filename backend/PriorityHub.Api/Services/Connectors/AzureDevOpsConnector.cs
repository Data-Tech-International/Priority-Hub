using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class AzureDevOpsConnector(HttpClient httpClient) : IConnector
{
    private const int WorkItemBatchSize = 100;
    private static readonly string[] RequestedFields =
    [
        "System.Title",
        "System.State",
        "System.AssignedTo",
        "Microsoft.VSTS.Scheduling.StoryPoints",
        "Microsoft.VSTS.Common.Priority",
        "Microsoft.VSTS.Common.Severity",
        "System.ChangedDate",
        "System.CreatedDate",
        "Microsoft.VSTS.Scheduling.TargetDate",
        "System.Tags"
    ];

    // IConnector metadata
    public string ProviderKey => "azure-devops";
    public string DisplayName => "Azure DevOps";
    public string Description => "Aggregate work items from an Azure DevOps project using WIQL.";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("organization", "Organization"),
        new("project", "Project"),
        new("personalAccessToken", "PAT (optional with Microsoft sign-in)", "password", false),
        new("wiql", "WIQL", "textarea", true,
            "Select [System.Id] From WorkItems Where [System.TeamProject] = @project And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<AzureDevOpsConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid Azure DevOps connection configuration.");
        return await FetchConnectionAsync(connection, oauthToken, cancellationToken);
    }

    public async Task<ConnectorResult> FetchAsync(IEnumerable<AzureDevOpsConnection> connections, CancellationToken cancellationToken)
    {
        return await FetchAsync(connections, null, cancellationToken);
    }

    public async Task<ConnectorResult> FetchAsync(IEnumerable<AzureDevOpsConnection> connections, string? bearerToken, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();

        foreach (var connection in connections.Where(item => item.Enabled))
        {
            var connectionResult = await FetchConnectionAsync(connection, bearerToken, cancellationToken);
            result.BoardConnections.AddRange(connectionResult.BoardConnections);
            result.WorkItems.AddRange(connectionResult.WorkItems);
            result.Issues.AddRange(connectionResult.Issues);
        }

        return result;
    }

    public async Task<ConnectorResult> FetchConnectionAsync(AzureDevOpsConnection connection, string? bearerToken, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = "azure-devops",
            WorkspaceName = connection.Organization,
            BoardName = connection.Name,
            ProjectName = connection.Project,
            Owner = string.Empty,
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            var authHeader = BuildAuthorizationHeader(connection, bearerToken);
            using var wiqlRequest = new HttpRequestMessage(HttpMethod.Post, BuildWiqlUrl(connection))
            {
                Content = JsonContent.Create(new { query = connection.Wiql })
            };
            wiqlRequest.Headers.Authorization = authHeader;

            using var wiqlResponse = await httpClient.SendAsync(wiqlRequest, cancellationToken);
            using var wiqlDocument = await ReadJsonResponseAsync(wiqlResponse, "Azure DevOps WIQL request failed", cancellationToken);
            var ids = wiqlDocument.RootElement.TryGetProperty("workItems", out var workItemsElement)
                ? workItemsElement.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ToArray()
                : [];

            if (ids.Length == 0)
            {
                boardConnection.FetchedItemCount = 0;
                result.BoardConnections.Add(boardConnection);
                return result;
            }

            foreach (var item in await FetchWorkItemsAsync(connection, authHeader, ids, cancellationToken))
            {
                var workItemId = item.GetProperty("id").GetInt32();
                var fields = item.GetProperty("fields");
                result.WorkItems.Add(new WorkItem
                {
                    Id = $"ADO-{workItemId}",
                    Provider = "azure-devops",
                    BoardId = connection.Id,
                    SourceUrl = BuildWorkItemUrl(connection, workItemId),
                    Title = ReadString(fields, "System.Title") ?? "Untitled work item",
                    Status = MapStatus(ReadString(fields, "System.State")),
                    Assignee = ReadNestedString(fields, "System.AssignedTo", "displayName") ?? string.Empty,
                    Effort = ReadInt(fields, "Microsoft.VSTS.Scheduling.StoryPoints", 3),
                    Impact = Math.Clamp(11 - ReadInt(fields, "Microsoft.VSTS.Common.Priority", 5), 1, 10),
                    Urgency = Math.Clamp(11 - ReadInt(fields, "Microsoft.VSTS.Common.Severity", 5), 1, 10),
                    Confidence = 7,
                    AgeDays = DaysSince(ReadString(fields, "System.ChangedDate") ?? ReadString(fields, "System.CreatedDate")),
                    BlockerCount = item.TryGetProperty("relations", out var relationsElement)
                        ? relationsElement.EnumerateArray().Count(relation => relation.TryGetProperty("rel", out var rel) && rel.GetString()?.Contains("dependency", StringComparison.OrdinalIgnoreCase) == true)
                        : 0,
                    DueInDays = DueInDays(ReadString(fields, "Microsoft.VSTS.Scheduling.TargetDate")),
                    Tags = ParseTags(ReadString(fields, "System.Tags"))
                });
            }

            boardConnection.FetchedItemCount = result.WorkItems.Count;
            result.BoardConnections.Add(boardConnection);
        }
        catch (Exception exception)
        {
            boardConnection.SyncStatus = "needs-auth";
            boardConnection.LastSyncMinutesAgo = 999;
            result.BoardConnections.Add(boardConnection);
            result.Issues.Add(new ProviderIssue
            {
                Provider = "azure-devops",
                ConnectionId = connection.Id,
                Message = exception.Message
            });
        }

        return result;
    }

    private static string BuildWiqlUrl(AzureDevOpsConnection connection)
    {
        var baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(connection.Organization)}/{Uri.EscapeDataString(connection.Project)}";
        return $"{baseUrl}/_apis/wit/wiql?api-version=7.1-preview.2";
    }

    private static string BuildWorkItemsBatchUrl(AzureDevOpsConnection connection)
    {
        var baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(connection.Organization)}/{Uri.EscapeDataString(connection.Project)}";
        return $"{baseUrl}/_apis/wit/workitemsbatch?api-version=7.1-preview.1";
    }

    private static string BuildWorkItemUrl(AzureDevOpsConnection connection, int workItemId)
    {
        return $"https://dev.azure.com/{Uri.EscapeDataString(connection.Organization)}/{Uri.EscapeDataString(connection.Project)}/_workitems/edit/{workItemId}";
    }

    private async Task<List<JsonElement>> FetchWorkItemsAsync(
        AzureDevOpsConnection connection,
        AuthenticationHeaderValue authHeader,
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken)
    {
        var items = new List<JsonElement>(ids.Count);

        foreach (var batch in ids.Chunk(WorkItemBatchSize))
        {
            using var workItemsRequest = new HttpRequestMessage(HttpMethod.Post, BuildWorkItemsBatchUrl(connection))
            {
                Content = JsonContent.Create(new
                {
                    ids = batch,
                    fields = RequestedFields,
                    expand = "relations",
                    errorPolicy = "Omit"
                })
            };
            workItemsRequest.Headers.Authorization = authHeader;

            using var workItemsResponse = await httpClient.SendAsync(workItemsRequest, cancellationToken);
            using var workItemsDocument = await ReadJsonResponseAsync(workItemsResponse, "Azure DevOps work items request failed", cancellationToken);
            items.AddRange(workItemsDocument.RootElement.GetProperty("value").EnumerateArray().Select(item => item.Clone()));
        }

        return items;
    }

    private static AuthenticationHeaderValue BuildAuthorizationHeader(AzureDevOpsConnection connection, string? bearerToken)
    {
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            return new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (!string.IsNullOrWhiteSpace(connection.PersonalAccessToken))
        {
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{connection.PersonalAccessToken}"));
            return new AuthenticationHeaderValue("Basic", encodedToken);
        }

        throw new InvalidOperationException("Azure DevOps authentication required. Sign in with Microsoft to grant access, or add a Personal Access Token for this connection.");
    }

    internal static string MapStatus(string? value)
    {
        var normalized = value?.ToLowerInvariant() ?? string.Empty;
        if (normalized.Contains("done") || normalized.Contains("closed") || normalized.Contains("resolved")) return "done";
        if (normalized.Contains("review") || normalized.Contains("validate")) return "review";
        if (normalized.Contains("block")) return "blocked";
        if (normalized.Contains("active") || normalized.Contains("progress") || normalized.Contains("implement")) return "in-progress";
        return "planned";
    }

    private static string? ReadString(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static string? ReadNestedString(JsonElement element, string name, string nestedName)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return value.TryGetProperty(nestedName, out var nested) ? nested.GetString() : null;
    }

    private static int ReadInt(JsonElement element, string name, int fallback)
    {
        if (!element.TryGetProperty(name, out var value)) return fallback;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue)) return intValue;
        return int.TryParse(value.GetString(), out var parsed) ? parsed : fallback;
    }

    internal static int DaysSince(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return 0;
        return Math.Max(0, (int)Math.Round((DateTimeOffset.UtcNow - parsed).TotalDays));
    }

    private static async Task<JsonDocument> ReadJsonResponseAsync(HttpResponseMessage response, string prefix, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(FormatAzureError(prefix, response.StatusCode, body));
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(FormatAzureError(prefix, response.StatusCode, body));
        }
    }

    private static string FormatAzureError(string prefix, HttpStatusCode statusCode, string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var messageElement) &&
                !string.IsNullOrWhiteSpace(messageElement.GetString()))
            {
                return $"{prefix}: {messageElement.GetString()}";
            }

            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(errorElement.GetString()))
                {
                    return $"{prefix}: {errorElement.GetString()}";
                }

                if (errorElement.ValueKind == JsonValueKind.Object &&
                    errorElement.TryGetProperty("message", out var nestedMessage) &&
                    !string.IsNullOrWhiteSpace(nestedMessage.GetString()))
                {
                    return $"{prefix}: {nestedMessage.GetString()}";
                }
            }
        }
        catch
        {
        }

        var trimmed = (body ?? string.Empty).TrimStart();
        if (trimmed.StartsWith("<", StringComparison.Ordinal))
        {
            return $"{prefix}: Azure DevOps returned HTML instead of JSON. The sign-in token is not valid for Azure DevOps. Re-sign in with Microsoft to refresh access, or add a Personal Access Token for this connection.";
        }

        var snippet = string.IsNullOrWhiteSpace(body)
            ? statusCode.ToString()
            : body.Length > 200 ? body[..200] : body;
        return $"{prefix}: {snippet}";
    }

    private static int? DueInDays(string? value)
    {
        if (!DateTimeOffset.TryParse(value, out var parsed)) return null;
        return (int)Math.Round((parsed - DateTimeOffset.UtcNow).TotalDays);
    }

    internal static List<string> ParseTags(string? value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}