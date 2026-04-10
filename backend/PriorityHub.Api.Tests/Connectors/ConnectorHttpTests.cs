using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

/// <summary>
/// Tests the HTTP fetch path for each connector using a mock HttpMessageHandler.
/// Each test provides a minimal valid JSON response and asserts the ConnectorResult shape.
/// </summary>
public sealed class ConnectorHttpTests
{
    private static HttpClient ClientWith(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        return new HttpClient(new QueuedHttpMessageHandler(queue));
    }

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Unauthorized() =>
        new(HttpStatusCode.Unauthorized) { Content = new StringContent("""{"error":{"message":"Unauthorized"}}""", Encoding.UTF8, "application/json") };

    private static JsonElement ConnectionJson(object connectionObj) =>
        TestHelpers.JsonObject(connectionObj);

    // ── AzureDevOps ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AzureDevOps_SuccessResponse_ReturnsOneWorkItem()
    {
        var wiqlResponse = OkJson("""{"workItems":[{"id":42}]}""");
        var batchResponse = OkJson("""
        {
          "value": [{
            "id": 42,
            "fields": {
              "System.Title": "Build the thing",
              "System.State": "Active"
            }
          }]
        }
        """);

        var connector = new AzureDevOpsConnector(ClientWith(wiqlResponse, batchResponse), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.WorkItems);
        Assert.Equal("Build the thing", result.WorkItems[0].Title);
        Assert.Single(result.BoardConnections);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
        Assert.Equal(1, result.BoardConnections[0].FetchedItemCount);
    }

    [Fact]
    public async Task AzureDevOps_EmptyWiqlResult_SetsFetchedItemCountToZero()
    {
        var wiqlResponse = OkJson("""{"workItems":[]}""");

        var connector = new AzureDevOpsConnector(ClientWith(wiqlResponse), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "pat", wiql = "SELECT [System.Id] FROM WorkItems WHERE [State] = 'Never'", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Empty(result.WorkItems);
        Assert.Single(result.BoardConnections);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
        Assert.Equal(0, result.BoardConnections[0].FetchedItemCount);
    }

    [Fact]
    public async Task AzureDevOps_UnauthorizedResponse_ReturnsNeedsAuth()
    {
        var connector = new AzureDevOpsConnector(ClientWith(Unauthorized()), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "bad-pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public async Task AzureDevOps_MissingToken_ReturnsNeedsAuthWithoutHttpCall()
    {
        var connector = new AzureDevOpsConnector(new HttpClient(new NeverCalledHttpMessageHandler()), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, issue => issue.Message.Contains("token") || issue.Message.Contains("access"));
    }

    [Fact]
    public async Task AzureDevOps_HtmlResponseInsteadOfJson_ReturnsNeedsAuthWithHelpfulMessage()
    {
        var htmlResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("<html><body>Sign in to your account</body></html>", Encoding.UTF8, "text/html")
        };

        var connector = new AzureDevOpsConnector(ClientWith(htmlResponse), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "expired-token", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, issue =>
            issue.Message.Contains("sign-in", StringComparison.OrdinalIgnoreCase) ||
            issue.Message.Contains("not valid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AzureDevOps_404NotFound_ReportsOrgProjectError()
    {
        var htmlResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("<html><body>The resource cannot be found.</body></html>", Encoding.UTF8, "text/html")
        };

        var connector = new AzureDevOpsConnector(ClientWith(htmlResponse), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "badorg", project = "badproj", personalAccessToken = "", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, "valid-bearer", CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, issue =>
            issue.Message.Contains("404", StringComparison.OrdinalIgnoreCase) &&
            issue.Message.Contains("organization", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AzureDevOps_OauthTokenPreferredOverPat()
    {
        AuthenticationHeaderValue? capturedAuth = null;
        var wiqlResponse = OkJson("""{"workItems":[]}""");
        var handler = new CapturingHttpMessageHandler(wiqlResponse, r => capturedAuth = r.Headers.Authorization);

        var connector = new AzureDevOpsConnector(new HttpClient(handler), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "my-pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        await connector.FetchConnectionAsync(config, "oauth-bearer-token", CancellationToken.None);

        Assert.NotNull(capturedAuth);
        Assert.Equal("Bearer", capturedAuth!.Scheme);
        Assert.Equal("oauth-bearer-token", capturedAuth.Parameter);
    }

    [Fact]
    public async Task AzureDevOps_PatUsedWhenNoOauthToken()
    {
        AuthenticationHeaderValue? capturedAuth = null;
        var wiqlResponse = OkJson("""{"workItems":[]}""");
        var handler = new CapturingHttpMessageHandler(wiqlResponse, r => capturedAuth = r.Headers.Authorization);

        var connector = new AzureDevOpsConnector(new HttpClient(handler), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "my-pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.NotNull(capturedAuth);
        Assert.Equal("Basic", capturedAuth!.Scheme);
    }

    [Fact]
    public async Task AzureDevOps_WhenBearerTokenInvalid_FallsBackToPat()
    {
        var htmlAuthFailure = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("<html><body>Sign in</body></html>", Encoding.UTF8, "text/html")
        };

        var wiqlSuccess = OkJson("""{"workItems":[]}""");
        var capturedSchemes = new List<string>();
        var handler = new CapturingQueuedHttpMessageHandler(
            new Queue<HttpResponseMessage>([htmlAuthFailure, wiqlSuccess]),
            request => capturedSchemes.Add(request.Headers.Authorization?.Scheme ?? string.Empty));

        var connector = new AzureDevOpsConnector(new HttpClient(handler), NullLogger<AzureDevOpsConnector>.Instance);
        var config = ConnectionJson(new
        {
            id = "x",
            name = "Test",
            organization = "myorg",
            project = "myproj",
            personalAccessToken = "my-pat",
            wiql = "SELECT [System.Id] FROM WorkItems",
            enabled = true
        });

        var result = await connector.FetchConnectionAsync(config, "invalid-oauth-token", CancellationToken.None);

        Assert.Equal(["Bearer", "Basic"], capturedSchemes);
        Assert.Single(result.BoardConnections);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
        Assert.Empty(result.Issues);
    }

    // ── GitHub ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GitHub_SuccessResponse_ReturnsOneWorkItem()
    {
        var searchResponse = OkJson("""
        {
          "total_count": 1,
          "items": [{
            "number": 7,
            "html_url": "https://github.com/owner/repo/issues/7",
            "title": "Fix login",
            "state": "open",
            "labels": [],
            "assignees": []
          }]
        }
        """);

        var connector = new GitHubIssuesConnector(ClientWith(searchResponse));
        var config = ConnectionJson(new { id = "gh1", name = "My Repo", owner = "myowner", repository = "myrepo", personalAccessToken = "ghp_test", query = "is:open", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.WorkItems);
        Assert.Equal("Fix login", result.WorkItems[0].Title);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
    }

    [Fact]
    public async Task GitHub_UnauthorizedResponse_ReturnsNeedsAuth()
    {
        var connector = new GitHubIssuesConnector(ClientWith(Unauthorized()));
        var config = ConnectionJson(new { id = "gh1", name = "My Repo", owner = "myowner", repository = "myrepo", personalAccessToken = "bad", query = "is:open", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
    }

    // ── Jira ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Jira_SuccessResponse_ReturnsOneWorkItem()
    {
        var jiraResponse = OkJson("""
        {
          "issues": [{
            "key": "PROJ-1",
            "fields": {
              "summary": "Do the thing",
              "status": {"name": "In Progress"},
              "assignee": null,
              "labels": [],
              "priority": {"name": "High"},
              "duedate": null,
              "created": "2024-01-01T00:00:00Z",
              "updated": "2024-01-10T00:00:00Z",
              "issuelinks": []
            }
          }]
        }
        """);

        var connector = new JiraConnector(ClientWith(jiraResponse));
        var config = ConnectionJson(new { id = "j1", name = "Jira", baseUrl = "https://myorg.atlassian.net", email = "me@example.com", apiToken = "token", jql = "project = PROJ", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.WorkItems);
        Assert.Equal("Do the thing", result.WorkItems[0].Title);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
    }

    // ── MicrosoftTasks — missing oauthToken ───────────────────────────────────

    [Fact]
    public async Task MicrosoftTasks_NullOauthToken_ReturnsNeedsAuthWithoutHttp()
    {
        var connector = new MicrosoftTasksConnector(new HttpClient(new NeverCalledHttpMessageHandler()));
        var config = ConnectionJson(new { id = "mt1", name = "Tasks", taskListName = "", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, i => i.Message.Contains("Microsoft sign-in"));
    }

        [Fact]
        public async Task MicrosoftTasks_SuccessResponse_ExcludesCompletedTasks_AndAddsFallbackSourceUrl()
        {
                var listsResponse = OkJson("""
                {
                    "value": [
                        { "id": "list-1", "displayName": "Tasks" }
                    ]
                }
                """);

                var tasksResponse = OkJson("""
                {
                    "value": [
                        {
                            "id": "task-1",
                            "title": "Active task",
                            "status": "notStarted",
                            "importance": "normal",
                            "createdDateTime": "2024-01-01T00:00:00Z",
                            "lastModifiedDateTime": "2024-01-02T00:00:00Z",
                            "categories": []
                        },
                        {
                            "id": "task-2",
                            "title": "Completed task",
                            "status": "completed",
                            "importance": "high",
                            "createdDateTime": "2024-01-01T00:00:00Z",
                            "lastModifiedDateTime": "2024-01-02T00:00:00Z",
                            "categories": []
                        }
                    ]
                }
                """);

                var connector = new MicrosoftTasksConnector(ClientWith(listsResponse, tasksResponse));
                var config = ConnectionJson(new { id = "mt1", name = "Tasks", taskListName = "", enabled = true });

                var result = await connector.FetchConnectionAsync(config, "oauth-token", CancellationToken.None);

                Assert.Single(result.WorkItems);
                Assert.Equal("Active task", result.WorkItems[0].Title);
                Assert.Equal("https://to-do.office.com/tasks/", result.WorkItems[0].SourceUrl);
                Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
        }

    // ── OutlookFlaggedMail — missing oauthToken ───────────────────────────────

    [Fact]
    public async Task OutlookFlaggedMail_NullOauthToken_ReturnsNeedsAuthWithoutHttp()
    {
        var connector = new OutlookFlaggedMailConnector(new HttpClient(new NeverCalledHttpMessageHandler()));
        var config = ConnectionJson(new { id = "om1", name = "Mail", folderId = "", maxResults = "100", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, i => i.Message.Contains("Microsoft sign-in"));
    }

    // ── Trello — FilterMyCards ────────────────────────────────────────────────

    [Fact]
    public async Task Trello_FilterMyCardsTrue_OnlyReturnsMatchingCards()
    {
        var memberResponse = OkJson("""{"id":"member-abc"}""");
        var listsResponse = OkJson("""[{"id":"list1","name":"To Do","closed":false}]""");
        var cardsResponse = OkJson("""
        [
          {"id":"card1","name":"My card","idList":"list1","closed":false,"labels":[],"idMembers":["member-abc"],"dateLastActivity":"2024-01-01T00:00:00Z"},
          {"id":"card2","name":"Not mine","idList":"list1","closed":false,"labels":[],"idMembers":["other-id"],"dateLastActivity":"2024-01-01T00:00:00Z"},
          {"id":"card3","name":"Unassigned","idList":"list1","closed":false,"labels":[],"idMembers":[],"dateLastActivity":"2024-01-01T00:00:00Z"}
        ]
        """);

        var connector = new TrelloConnector(ClientWith(memberResponse, listsResponse, cardsResponse), NullLogger<TrelloConnector>.Instance);
        var config = ConnectionJson(new { id = "t1", name = "Board", boardId = "board1", apiKey = "key", token = "tok", filterMyCards = true, enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.WorkItems);
        Assert.Equal("My card", result.WorkItems[0].Title);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
    }

    [Fact]
    public async Task Trello_FilterMyCardsFalse_ReturnsAllCards()
    {
        var listsResponse = OkJson("""[{"id":"list1","name":"To Do","closed":false}]""");
        var cardsResponse = OkJson("""
        [
          {"id":"card1","name":"My card","idList":"list1","closed":false,"labels":[],"idMembers":["member-abc"],"dateLastActivity":"2024-01-01T00:00:00Z"},
          {"id":"card2","name":"Not mine","idList":"list1","closed":false,"labels":[],"idMembers":["other-id"],"dateLastActivity":"2024-01-01T00:00:00Z"}
        ]
        """);

        var connector = new TrelloConnector(ClientWith(listsResponse, cardsResponse), NullLogger<TrelloConnector>.Instance);
        var config = ConnectionJson(new { id = "t1", name = "Board", boardId = "board1", apiKey = "key", token = "tok", filterMyCards = false, enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal(2, result.WorkItems.Count);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
    }

    [Fact]
    public async Task Trello_FilterMyCardsTrue_MemberApiFails_ReturnsAllCards()
    {
        var memberFailure = Unauthorized();
        var listsResponse = OkJson("""[{"id":"list1","name":"To Do","closed":false}]""");
        var cardsResponse = OkJson("""
        [
          {"id":"card1","name":"My card","idList":"list1","closed":false,"labels":[],"idMembers":["member-abc"],"dateLastActivity":"2024-01-01T00:00:00Z"},
          {"id":"card2","name":"Not mine","idList":"list1","closed":false,"labels":[],"idMembers":["other-id"],"dateLastActivity":"2024-01-01T00:00:00Z"}
        ]
        """);

        var connector = new TrelloConnector(ClientWith(memberFailure, listsResponse, cardsResponse), NullLogger<TrelloConnector>.Instance);
        var config = ConnectionJson(new { id = "t1", name = "Board", boardId = "board1", apiKey = "key", token = "tok", filterMyCards = true, enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        // Fallback: all cards returned when member resolution fails
        Assert.Equal(2, result.WorkItems.Count);
        Assert.Equal("connected", result.BoardConnections[0].SyncStatus);
    }

    [Fact]
    public async Task Trello_FilterMyCardsTrue_MemberApiCalled_BeforeCardsFetch()
    {
        var capturedUrls = new List<string>();
        var memberResponse = OkJson("""{"id":"member-abc"}""");
        var listsResponse = OkJson("""[{"id":"list1","name":"To Do","closed":false}]""");
        var cardsResponse = OkJson("""[{"id":"card1","name":"Card","idList":"list1","closed":false,"labels":[],"idMembers":["member-abc"],"dateLastActivity":"2024-01-01T00:00:00Z"}]""");

        var handler = new CapturingQueuedHttpMessageHandler(
            new Queue<HttpResponseMessage>([memberResponse, listsResponse, cardsResponse]),
            request => capturedUrls.Add(request.RequestUri?.AbsolutePath ?? ""));

        var connector = new TrelloConnector(new HttpClient(handler), NullLogger<TrelloConnector>.Instance);
        var config = ConnectionJson(new { id = "t1", name = "Board", boardId = "board1", apiKey = "key", token = "tok", filterMyCards = true, enabled = true });

        await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Equal(3, capturedUrls.Count);
        Assert.Contains("/tokens/", capturedUrls[0]); // member resolution first
    }

    // ── Jira — Project scoping ───────────────────────────────────────────────

    [Fact]
    public async Task Jira_ProjectSet_PrependsProjectToJql()
    {
        string? capturedUrl = null;
        var jiraResponse = OkJson("""{"issues":[]}""");
        var handler = new CapturingHttpMessageHandler(jiraResponse, r => capturedUrl = r.RequestUri?.ToString());

        var connector = new JiraConnector(new HttpClient(handler));
        var config = ConnectionJson(new { id = "j1", name = "Jira", baseUrl = "https://myorg.atlassian.net", project = "GROWTH", email = "me@example.com", apiToken = "token", jql = "assignee = currentUser()", enabled = true });

        await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.NotNull(capturedUrl);
        var decodedUrl = Uri.UnescapeDataString(capturedUrl!);
        Assert.Contains("project = \"GROWTH\" AND assignee = currentUser()", decodedUrl);
    }

    [Fact]
    public async Task Jira_ProjectEmpty_JqlUnchanged()
    {
        string? capturedUrl = null;
        var jiraResponse = OkJson("""{"issues":[]}""");
        var handler = new CapturingHttpMessageHandler(jiraResponse, r => capturedUrl = r.RequestUri?.ToString());

        var connector = new JiraConnector(new HttpClient(handler));
        var config = ConnectionJson(new { id = "j1", name = "Jira", baseUrl = "https://myorg.atlassian.net", project = "", email = "me@example.com", apiToken = "token", jql = "assignee = currentUser()", enabled = true });

        await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.NotNull(capturedUrl);
        var decodedUrl = Uri.UnescapeDataString(capturedUrl!);
        Assert.Contains("assignee = currentUser()", decodedUrl);
        Assert.DoesNotContain("project =", decodedUrl);
    }

    [Fact]
    public async Task Jira_ProjectSet_SetsProjectNameOnBoardConnection()
    {
        var jiraResponse = OkJson("""{"issues":[]}""");

        var connector = new JiraConnector(ClientWith(jiraResponse));
        var config = ConnectionJson(new { id = "j1", name = "Jira", baseUrl = "https://myorg.atlassian.net", project = "GROWTH", email = "me@example.com", apiToken = "token", jql = "assignee = currentUser()", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("GROWTH", result.BoardConnections[0].ProjectName);
    }

    [Fact]
    public async Task Jira_ProjectEmpty_ProjectNameDefaultsToHost()
    {
        var jiraResponse = OkJson("""{"issues":[]}""");

        var connector = new JiraConnector(ClientWith(jiraResponse));
        var config = ConnectionJson(new { id = "j1", name = "Jira", baseUrl = "https://myorg.atlassian.net", project = "", email = "me@example.com", apiToken = "token", jql = "assignee = currentUser()", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("myorg.atlassian.net", result.BoardConnections[0].ProjectName);
    }

    // ── Azure DevOps — Default WIQL contains @Me ─────────────────────────────

    [Fact]
    public void AzureDevOps_DefaultWiql_ContainsAtMe()
    {
        var connector = new AzureDevOpsConnector(new HttpClient(), NullLogger<AzureDevOpsConnector>.Instance);
        var wiqlField = connector.ConfigFields.First(f => f.Key == "wiql");
        Assert.Contains("@Me", wiqlField.DefaultValue);
        Assert.Contains("[System.AssignedTo] = @Me", wiqlField.DefaultValue);
    }

    [Fact]
    public void AzureDevOps_ModelDefaultWiql_ContainsAtMe()
    {
        var connection = new AzureDevOpsConnection();
        Assert.Contains("@Me", connection.Wiql);
        Assert.Contains("[System.AssignedTo] = @Me", connection.Wiql);
    }

    // ── GitHub — Default query still contains assignee:@me ───────────────────

    [Fact]
    public void GitHub_DefaultQuery_ContainsAssigneeSelf()
    {
        var connector = new GitHubIssuesConnector(new HttpClient());
        var queryField = connector.ConfigFields.First(f => f.Key == "query");
        Assert.Contains("assignee:@me", queryField.DefaultValue);
    }

    // ── Trello — ConfigFields includes filterMyCards checkbox ─────────────────

    [Fact]
    public void Trello_ConfigFields_IncludesFilterMyCardsCheckbox()
    {
        var connector = new TrelloConnector(new HttpClient(), NullLogger<TrelloConnector>.Instance);
        var field = connector.ConfigFields.FirstOrDefault(f => f.Key == "filterMyCards");
        Assert.NotNull(field);
        Assert.Equal("checkbox", field!.InputKind);
        Assert.Equal("true", field.DefaultValue);
        Assert.False(field.Required);
    }

    // ── Jira — ConfigFields includes project field ───────────────────────────

    [Fact]
    public void Jira_ConfigFields_IncludesProjectField()
    {
        var connector = new JiraConnector(new HttpClient());
        var field = connector.ConfigFields.FirstOrDefault(f => f.Key == "project");
        Assert.NotNull(field);
        Assert.Equal("text", field!.InputKind);
        Assert.False(field.Required);
        Assert.Null(field.DefaultValue);
    }
}

/// <summary>Replays a queue of pre-built responses in order.</summary>
internal sealed class QueuedHttpMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (responses.Count == 0)
        {
            throw new InvalidOperationException("No more queued HTTP responses.");
        }

        return Task.FromResult(responses.Dequeue());
    }
}

/// <summary>Throws if any HTTP request is actually made — use to assert no outbound call occurs.</summary>
internal sealed class NeverCalledHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Unexpected HTTP call to {request.RequestUri}");
    }
}

/// <summary>Returns a fixed response while capturing the request for assertions.</summary>
internal sealed class CapturingHttpMessageHandler(HttpResponseMessage response, Action<HttpRequestMessage> onRequest) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        onRequest(request);
        return Task.FromResult(response);
    }
}

internal sealed class CapturingQueuedHttpMessageHandler(Queue<HttpResponseMessage> responses, Action<HttpRequestMessage> onRequest) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        onRequest(request);

        if (responses.Count == 0)
        {
            throw new InvalidOperationException("No more queued HTTP responses.");
        }

        return Task.FromResult(responses.Dequeue());
    }
}
