namespace PriorityHub.Api.Models;

public sealed class DashboardPayload
{
    public List<BoardConnection> BoardConnections { get; set; } = [];
    public List<WorkItem> WorkItems { get; set; } = [];
    public List<ProviderIssue> Issues { get; set; } = [];
    public UserPreferences Preferences { get; set; } = new();
    public string GeneratedAt { get; set; } = DateTimeOffset.UtcNow.ToString("O");
}

public sealed class DashboardProgress
{
    public int TotalConnections { get; set; }
    public int CompletedConnections { get; set; }
    public string ActiveProvider { get; set; } = string.Empty;
    public string ActiveConnectionId { get; set; } = string.Empty;
    public string ActiveConnectionName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

public sealed class DashboardStreamEvent
{
    public string Type { get; set; } = "snapshot";
    public DashboardPayload Dashboard { get; set; } = new();
    public DashboardProgress Progress { get; set; } = new();
}

public sealed class AuthUser
{
    public string Sub { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public sealed class BoardConnection
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string WorkspaceName { get; set; } = string.Empty;
    public string BoardName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = "connected";
    public int LastSyncMinutesAgo { get; set; }
    public int? FetchedItemCount { get; set; }
}

public sealed class WorkItem
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string BoardId { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "planned";
    public string Assignee { get; set; } = string.Empty;
    public int Effort { get; set; }
    public int Impact { get; set; }
    public int Urgency { get; set; }
    public int Confidence { get; set; }
    public int AgeDays { get; set; }
    public int BlockerCount { get; set; }
    public int? DueInDays { get; set; }
    public bool IsNew { get; set; }
    public DateTimeOffset? TargetDate { get; set; }
    public bool IsBlocked { get; set; }
    public List<string> Tags { get; set; } = [];
}

public sealed class ProviderIssue
{
    public string Provider { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ConnectorResult
{
    public List<BoardConnection> BoardConnections { get; } = [];
    public List<WorkItem> WorkItems { get; } = [];
    public List<ProviderIssue> Issues { get; } = [];
}