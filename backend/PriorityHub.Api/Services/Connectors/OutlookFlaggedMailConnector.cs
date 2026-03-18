using System.Net.Http.Headers;
using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

public sealed class OutlookFlaggedMailConnector(HttpClient httpClient) : IConnector
{
    private const int DefaultMaxResults = 100;
    private const int MaximumScanPages = 10;

    public string ProviderKey => "outlook-flagged-mail";
    public string DisplayName => "Outlook Flagged Mail";
    public string Description => "Aggregate flagged Outlook email from Microsoft Graph.";
    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("folderId", "Folder ID (optional)", "text", false),
        new("maxResults", "Max results", "text", false, "100"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(JsonElement connectionConfig, string? oauthToken, CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<OutlookFlaggedMailConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid Outlook Flagged Mail connection configuration.");

        return await FetchConnectionAsync(connection, oauthToken, cancellationToken);
    }

    private async Task<ConnectorResult> FetchConnectionAsync(OutlookFlaggedMailConnection connection, string? oauthToken, CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = ProviderKey,
            WorkspaceName = "Microsoft 365",
            BoardName = string.IsNullOrWhiteSpace(connection.Name) ? "Flagged Mail" : connection.Name,
            ProjectName = string.IsNullOrWhiteSpace(connection.FolderId) ? "All mail folders" : connection.FolderId,
            Owner = "Me",
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            if (string.IsNullOrWhiteSpace(oauthToken))
            {
                throw new InvalidOperationException("Missing Microsoft sign-in token. Sign in with Microsoft to load flagged mail.");
            }

            var maxResults = ParseMaxResults(connection.MaxResults);
            var messages = await FetchFlaggedMessagesAsync(connection, oauthToken, maxResults, cancellationToken);
            foreach (var message in messages)
            {
                result.WorkItems.Add(MapMessage(connection, message));
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

    private async Task<List<JsonElement>> FetchFlaggedMessagesAsync(OutlookFlaggedMailConnection connection, string accessToken, int maxResults, CancellationToken cancellationToken)
    {
        var messages = new List<JsonElement>(maxResults);
        var baseUrl = string.IsNullOrWhiteSpace(connection.FolderId)
            ? "https://graph.microsoft.com/v1.0/me/messages"
            : $"https://graph.microsoft.com/v1.0/me/mailFolders/{Uri.EscapeDataString(connection.FolderId)}/messages";
        var nextUrl = $"{baseUrl}?$top=50&$select=id,subject,webLink,receivedDateTime,from,flag,categories,importance,isRead&$orderby=receivedDateTime desc";
        var pageCount = 0;

        while (!string.IsNullOrWhiteSpace(nextUrl) && messages.Count < maxResults && pageCount < MaximumScanPages)
        {
            pageCount += 1;
            using var document = await SendGraphRequestAsync(nextUrl, accessToken, cancellationToken);
            if (!document.RootElement.TryGetProperty("value", out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            foreach (var message in valuesElement.EnumerateArray())
            {
                if (!IsFlagged(message))
                {
                    continue;
                }

                messages.Add(message.Clone());
                if (messages.Count >= maxResults)
                {
                    break;
                }
            }

            nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement)
                ? nextLinkElement.GetString() ?? string.Empty
                : string.Empty;
        }

        return messages;
    }

    private async Task<JsonDocument> SendGraphRequestAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Prefer", "outlook.body-content-type=\"text\"");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(FormatGraphError("Outlook flagged mail request failed", response.StatusCode, body));
        }

        return JsonDocument.Parse(body);
    }

    private static WorkItem MapMessage(OutlookFlaggedMailConnection connection, JsonElement message)
    {
        var receivedAt = message.TryGetProperty("receivedDateTime", out var receivedElement)
            ? receivedElement.GetString()
            : null;
        var importance = message.TryGetProperty("importance", out var importanceElement)
            ? importanceElement.GetString()
            : null;
        var isRead = message.TryGetProperty("isRead", out var isReadElement) && isReadElement.GetBoolean();

        var tags = new List<string> { "email", "flagged" };
        if (message.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
        {
            tags.AddRange(categoriesElement.EnumerateArray()
                .Select(category => category.GetString())
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Cast<string>());
        }

        return new WorkItem
        {
            Id = $"OUTLOOK-{message.GetProperty("id").GetString()}",
            Provider = "outlook-flagged-mail",
            BoardId = connection.Id,
            SourceUrl = message.TryGetProperty("webLink", out var webLinkElement) ? webLinkElement.GetString() ?? string.Empty : string.Empty,
            Title = message.TryGetProperty("subject", out var subjectElement) && !string.IsNullOrWhiteSpace(subjectElement.GetString())
                ? subjectElement.GetString()!
                : "Untitled email",
            Status = MapStatus(message, isRead),
            Assignee = ReadSender(message),
            Effort = 2,
            Impact = MapImpact(importance),
            Urgency = MapUrgency(importance, isRead),
            Confidence = 6,
            AgeDays = DaysSince(receivedAt),
            BlockerCount = 0,
            DueInDays = null,
            Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    internal static bool IsFlagged(JsonElement message)
    {
        if (!message.TryGetProperty("flag", out var flagElement) || flagElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!flagElement.TryGetProperty("flagStatus", out var flagStatusElement))
        {
            return false;
        }

        var flagStatus = flagStatusElement.GetString();
        return string.Equals(flagStatus, "flagged", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(flagStatus, "complete", StringComparison.OrdinalIgnoreCase);
    }

    internal static string MapStatus(JsonElement message, bool isRead)
    {
        var flagStatus = message.TryGetProperty("flag", out var flagElement) &&
            flagElement.TryGetProperty("flagStatus", out var flagStatusElement)
            ? flagStatusElement.GetString()
            : null;

        if (string.Equals(flagStatus, "complete", StringComparison.OrdinalIgnoreCase))
        {
            return "done";
        }

        return isRead ? "planned" : "review";
    }

    private static string ReadSender(JsonElement message)
    {
        if (!message.TryGetProperty("from", out var fromElement) || fromElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (!fromElement.TryGetProperty("emailAddress", out var emailAddressElement) || emailAddressElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (emailAddressElement.TryGetProperty("name", out var nameElement) && !string.IsNullOrWhiteSpace(nameElement.GetString()))
        {
            return nameElement.GetString()!;
        }

        return emailAddressElement.TryGetProperty("address", out var addressElement) ? addressElement.GetString() ?? string.Empty : string.Empty;
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

    internal static int MapUrgency(string? importance, bool isRead)
    {
        if (string.Equals(importance, "high", StringComparison.OrdinalIgnoreCase))
        {
            return isRead ? 8 : 9;
        }

        if (string.Equals(importance, "low", StringComparison.OrdinalIgnoreCase))
        {
            return isRead ? 3 : 4;
        }

        return isRead ? 5 : 7;
    }

    internal static int ParseMaxResults(string? value)
    {
        return int.TryParse(value, out var parsed)
            ? Math.Clamp(parsed, 1, 200)
            : DefaultMaxResults;
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
}
