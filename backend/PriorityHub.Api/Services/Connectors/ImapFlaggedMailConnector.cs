using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services.Connectors;

/// <summary>
/// Fetches flagged and keyword-tagged email from any IMAP server using implicit TLS (port 993).
/// </summary>
public sealed class ImapFlaggedMailConnector(IImapClientFactory clientFactory) : IConnector
{
    private const int DefaultMaxResults = 100;

    public string ProviderKey => "imap-flagged-mail";
    public string DisplayName => "IMAP Flagged Mail";
    public string Description => "Aggregate flagged email from any IMAP server (Gmail, Outlook.com, Yahoo, and others) over TLS.";
    public string DefaultEmoji => "📨";

    public ConnectorFieldSpec[] ConfigFields =>
    [
        new("name", "Connection name"),
        new("imapServer", "IMAP server", "text", true),
        new("port", "Port", "text", false, "993"),
        new("email", "Email address", "text", true),
        new("password", "Password / App password", "password", true),
        new("folderPath", "Folder path", "text", false, "INBOX"),
        new("keywords", "Custom IMAP keywords (comma-separated)", "text", false),
        new("maxResults", "Max results", "text", false, "100"),
    ];

    public async Task<ConnectorResult> FetchConnectionAsync(
        JsonElement connectionConfig,
        string? oauthToken,
        CancellationToken cancellationToken)
    {
        var connection = JsonSerializer.Deserialize<ImapFlaggedMailConnection>(connectionConfig, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("Invalid IMAP Flagged Mail connection configuration.");

        return await FetchConnectionAsync(connection, cancellationToken);
    }

    internal async Task<ConnectorResult> FetchConnectionAsync(
        ImapFlaggedMailConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new ConnectorResult();
        var boardConnection = new BoardConnection
        {
            Id = connection.Id,
            Provider = ProviderKey,
            Emoji = string.IsNullOrWhiteSpace(connection.Emoji) ? DefaultEmoji : connection.Emoji,
            WorkspaceName = connection.ImapServer,
            BoardName = string.IsNullOrWhiteSpace(connection.Name) ? "IMAP Flagged Mail" : connection.Name,
            ProjectName = string.IsNullOrWhiteSpace(connection.FolderPath) ? "INBOX" : connection.FolderPath,
            Owner = connection.Email,
            SyncStatus = "connected",
            LastSyncMinutesAgo = 0
        };

        try
        {
            ValidateTlsRequirement(connection);

            var port = ParsePort(connection.Port);
            var maxResults = ParseMaxResults(connection.MaxResults);
            var keywords = ParseKeywords(connection.Keywords);
            var folderPath = string.IsNullOrWhiteSpace(connection.FolderPath) ? "INBOX" : connection.FolderPath.Trim();

            var messages = await FetchMessagesAsync(connection, port, folderPath, keywords, maxResults, cancellationToken);
            foreach (var message in messages)
            {
                result.WorkItems.Add(MapMessage(connection, message, folderPath, keywords));
            }

            boardConnection.FetchedItemCount = result.WorkItems.Count;
            result.BoardConnections.Add(boardConnection);
        }
        catch (Exception ex)
        {
            var syncStatus = ex is AuthenticationException
                ? "needs-auth"
                : "error";

            boardConnection.SyncStatus = syncStatus;
            boardConnection.LastSyncMinutesAgo = 999;
            result.BoardConnections.Add(boardConnection);
            result.Issues.Add(new ProviderIssue
            {
                Provider = ProviderKey,
                ConnectionId = connection.Id,
                Message = FormatError(ex)
            });
        }

        return result;
    }

    private async Task<List<MimeMessage>> FetchMessagesAsync(
        ImapFlaggedMailConnection connection,
        int port,
        string folderPath,
        IReadOnlyList<string> keywords,
        int maxResults,
        CancellationToken cancellationToken)
    {
        using var client = clientFactory.CreateClient();

        await client.ConnectAsync(connection.ImapServer, port, MailKit.Security.SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(connection.Email, connection.Password, cancellationToken);

        var folder = await OpenFolderAsync(client, folderPath, cancellationToken);

        var query = BuildSearchQuery(keywords);
        var uids = await folder.SearchAsync(query, cancellationToken);

        // Take the most recent N messages (UIDs are in ascending order, take from the end).
        var selected = uids.Count > maxResults
            ? uids.Skip(uids.Count - maxResults).ToList()
            : uids.ToList();

        var messages = new List<MimeMessage>(selected.Count);
        foreach (var uid in selected)
        {
            var message = await folder.GetMessageAsync(uid, cancellationToken);
            messages.Add(message);
        }

        await client.DisconnectAsync(quit: true, cancellationToken);
        return messages;
    }

    private static async Task<IMailFolder> OpenFolderAsync(IImapClient client, string folderPath, CancellationToken cancellationToken)
    {
        IMailFolder folder;
        try
        {
            folder = await client.GetFolderAsync(folderPath, cancellationToken);
        }
        catch (FolderNotFoundException)
        {
            throw new InvalidOperationException($"IMAP folder '{folderPath}' not found on server.");
        }

        await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        return folder;
    }

    internal static SearchQuery BuildSearchQuery(IReadOnlyList<string> keywords)
    {
        SearchQuery query = SearchQuery.Flagged;

        foreach (var keyword in keywords)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Or(SearchQuery.HasKeyword(keyword.Trim()));
            }
        }

        return query;
    }

    private static WorkItem MapMessage(
        ImapFlaggedMailConnection connection,
        MimeMessage message,
        string folderPath,
        IReadOnlyList<string> keywords)
    {
        var subject = string.IsNullOrWhiteSpace(message.Subject) ? "Untitled email" : message.Subject;

        var from = message.From.OfType<MailboxAddress>().FirstOrDefault();
        var assignee = from is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(from.Name) ? from.Address : $"{from.Name} <{from.Address}>";

        var ageDays = (int)(DateTimeOffset.UtcNow - message.Date).TotalDays;
        if (ageDays < 0) ageDays = 0;

        var tags = new List<string> { "email", "flagged" };
        var folder = folderPath.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(folder) && folder != "inbox")
            tags.Add(folder);
        foreach (var kw in keywords)
        {
            if (!string.IsNullOrWhiteSpace(kw))
                tags.Add(kw.Trim().ToLowerInvariant());
        }

        // Generate a stable ID from connection ID + Message-ID header.
        var messageId = message.MessageId ?? message.Subject ?? Guid.NewGuid().ToString();
        var idHash = Math.Abs(HashCode.Combine(connection.Id, messageId));

        return new WorkItem
        {
            Id = $"IMAP-{idHash}",
            Provider = "imap-flagged-mail",
            BoardId = connection.Id,
            SourceUrl = string.Empty,
            Title = subject,
            Status = "to-do",
            Assignee = assignee,
            Effort = 2,
            Impact = 5,
            Urgency = 5,
            Confidence = 6,
            AgeDays = ageDays,
            Tags = tags,
        };
    }

    internal static void ValidateTlsRequirement(ImapFlaggedMailConnection connection)
    {
        var port = ParsePort(connection.Port);
        if (port != 993)
        {
            throw new InvalidOperationException(
                $"IMAP connections require implicit TLS on port 993. Port {port} is not supported.");
        }
    }

    internal static int ParsePort(string? value)
    {
        if (int.TryParse(value, out var port) && port > 0)
            return port;
        return 993;
    }

    internal static int ParseMaxResults(string? value)
    {
        if (int.TryParse(value, out var max) && max > 0)
            return max;
        return DefaultMaxResults;
    }

    internal static List<string> ParseKeywords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();
    }

    private static string FormatError(Exception ex) => ex switch
    {
        MailKit.Security.AuthenticationException => "IMAP authentication failed. Check your email address and password (use an app password if 2FA is enabled).",
        InvalidOperationException ioe when ioe.Message.Contains("not found") => ex.Message,
        InvalidOperationException ioe when ioe.Message.Contains("TLS") => ex.Message,
        _ => $"IMAP connection error: {ex.Message}"
    };
}
