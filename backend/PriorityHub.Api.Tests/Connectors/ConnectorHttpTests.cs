using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

        var connector = new AzureDevOpsConnector(ClientWith(wiqlResponse, batchResponse));
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

        var connector = new AzureDevOpsConnector(ClientWith(wiqlResponse));
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
        var connector = new AzureDevOpsConnector(ClientWith(Unauthorized()));
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "bad-pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public async Task AzureDevOps_MissingToken_ReturnsNeedsAuthWithoutHttpCall()
    {
        var connector = new AzureDevOpsConnector(new HttpClient(new NeverCalledHttpMessageHandler()));
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

        var connector = new AzureDevOpsConnector(ClientWith(htmlResponse));
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "expired-token", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        var result = await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("needs-auth", result.BoardConnections[0].SyncStatus);
        Assert.Contains(result.Issues, issue =>
            issue.Message.Contains("HTML") &&
            issue.Message.Contains("sign-in", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AzureDevOps_OauthTokenPreferredOverPat()
    {
        AuthenticationHeaderValue? capturedAuth = null;
        var wiqlResponse = OkJson("""{"workItems":[]}""");
        var handler = new CapturingHttpMessageHandler(wiqlResponse, r => capturedAuth = r.Headers.Authorization);

        var connector = new AzureDevOpsConnector(new HttpClient(handler));
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

        var connector = new AzureDevOpsConnector(new HttpClient(handler));
        var config = ConnectionJson(new { id = "x", name = "Test", organization = "myorg", project = "myproj", personalAccessToken = "my-pat", wiql = "SELECT [System.Id] FROM WorkItems", enabled = true });

        await connector.FetchConnectionAsync(config, null, CancellationToken.None);

        Assert.NotNull(capturedAuth);
        Assert.Equal("Basic", capturedAuth!.Scheme);
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
