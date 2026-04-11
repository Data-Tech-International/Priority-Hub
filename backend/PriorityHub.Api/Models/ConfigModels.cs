using System.Text.Json;
using System.Text.Json.Serialization;

namespace PriorityHub.Api.Models;

public sealed class ProviderConfiguration
{
    public List<AzureDevOpsConnection> AzureDevOps { get; set; } = [];
    [JsonPropertyName("github")]
    public List<GitHubConnection> GitHub { get; set; } = [];
    public List<JiraConnection> Jira { get; set; } = [];
    public List<MicrosoftTasksConnection> MicrosoftTasks { get; set; } = [];
    public List<OutlookFlaggedMailConnection> OutlookFlaggedMail { get; set; } = [];
    public List<TrelloConnection> Trello { get; set; } = [];
    public List<ImapFlaggedMailConnection> ImapFlaggedMail { get; set; } = [];
    public List<LinkedMicrosoftAccount> LinkedMicrosoftAccounts { get; set; } = [];
    public UserPreferences Preferences { get; set; } = new();

    /// <summary>
    /// Returns true if any connector list contains at least one connection.
    /// </summary>
    public bool HasAnyConnections() =>
        AzureDevOps.Count > 0 || GitHub.Count > 0 || Jira.Count > 0 ||
        MicrosoftTasks.Count > 0 || OutlookFlaggedMail.Count > 0 ||
        Trello.Count > 0 || ImapFlaggedMail.Count > 0;

    /// <summary>
    /// Returns the connections for a given provider key as serialized JsonElements
    /// so DashboardAggregator can pass them to IConnector.FetchConnectionAsync without
    /// knowing the concrete connection type.
    /// </summary>
    public IEnumerable<JsonElement> GetConnections(string providerKey) => providerKey switch
    {
        "azure-devops" => AzureDevOps.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "github" => GitHub.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "jira" => Jira.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "microsoft-tasks" => MicrosoftTasks.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "outlook-flagged-mail" => OutlookFlaggedMail.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "trello" => Trello.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        "imap-flagged-mail" => ImapFlaggedMail.Select(c => JsonSerializer.SerializeToElement(c, JsonSerializerOptions.Web)),
        _ => []
    };
}

public sealed class UserPreferences
{
    public List<string> OrderedItemIds { get; set; } = [];
}

public sealed class AzureDevOpsConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🔷";
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    [SensitiveField]
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string Wiql { get; set; } = "Select [System.Id] From WorkItems Where [System.AssignedTo] = @me And [System.TeamProject] = @project And [System.State] <> 'Closed' Order By [System.ChangedDate] Desc";
    public bool Enabled { get; set; } = true;
}

public sealed class JiraConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📋";
    public string BaseUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [SensitiveField]
    public string ApiToken { get; set; } = string.Empty;
    public string Jql { get; set; } = "assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC";
    public bool Enabled { get; set; } = true;
}

public sealed class GitHubConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🐙";
    public string Owner { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    [SensitiveField]
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string Query { get; set; } = "is:open assignee:@me";
    public bool Enabled { get; set; } = true;
}

public sealed class TrelloConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📌";
    public string BoardId { get; set; } = string.Empty;
    [SensitiveField]
    public string ApiKey { get; set; } = string.Empty;
    [SensitiveField]
    public string Token { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public sealed class MicrosoftTasksConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "✅";
    public string TaskListName { get; set; } = string.Empty;
    public string LinkedAccountId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public sealed class OutlookFlaggedMailConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📧";
    public string FolderId { get; set; } = string.Empty;
    public string MaxResults { get; set; } = "100";
    public string LinkedAccountId { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

public sealed class ImapFlaggedMailConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "📨";
    public bool Enabled { get; set; } = true;
    public string ImapServer { get; set; } = string.Empty;
    public string Port { get; set; } = "993";
    public string Email { get; set; } = string.Empty;
    [SensitiveField]
    public string Password { get; set; } = string.Empty;
    public string FolderPath { get; set; } = "INBOX";
    public string Keywords { get; set; } = string.Empty;
    public string MaxResults { get; set; } = "100";
}

public sealed class LinkedMicrosoftAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [SensitiveField]
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
}