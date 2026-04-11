using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class ApplicationInsightsTelemetryServiceTests : IDisposable
{
    private readonly InMemoryChannel _channel = new();
    private readonly TelemetryConfiguration _config;
    private readonly TelemetryClient _client;
    private readonly StubActiveUserTracker _tracker = new();
    private readonly ApplicationInsightsTelemetryService _sut;

    public ApplicationInsightsTelemetryServiceTests()
    {
        _config = new TelemetryConfiguration { TelemetryChannel = _channel };
        _client = new TelemetryClient(_config);
        _sut = new ApplicationInsightsTelemetryService(_client, _tracker);
    }

    public void Dispose()
    {
        _config.Dispose();
        _channel.Dispose();
    }

    // --- RecordSignIn ---

    [Fact]
    public void RecordSignIn_EmitsAuthenticationSignInEvent()
    {
        _sut.RecordSignIn("google", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("AuthenticationSignIn", evt.Name);
    }

    [Fact]
    public void RecordSignIn_IncludesProviderProperty()
    {
        _sut.RecordSignIn("google", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("google", evt.Properties["provider"]);
    }

    [Fact]
    public void RecordSignIn_IncludesHashedUserId()
    {
        const string rawEmail = "user@example.com";
        var expectedHash = UserIdentityHasher.Hash(rawEmail);

        _sut.RecordSignIn("google", rawEmail);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal(expectedHash, evt.Properties["userId"]);
    }

    [Fact]
    public void RecordSignIn_DoesNotIncludeRawEmail()
    {
        const string rawEmail = "user@example.com";

        _sut.RecordSignIn("google", rawEmail);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.DoesNotContain(rawEmail, evt.Properties.Values);
    }

    // --- RecordSignOut ---

    [Fact]
    public void RecordSignOut_EmitsAuthenticationSignOutEvent()
    {
        _sut.RecordSignOut("user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("AuthenticationSignOut", evt.Name);
    }

    [Fact]
    public void RecordSignOut_IncludesHashedUserId()
    {
        const string rawEmail = "user@example.com";
        var expectedHash = UserIdentityHasher.Hash(rawEmail);

        _sut.RecordSignOut(rawEmail);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal(expectedHash, evt.Properties["userId"]);
    }

    // --- RecordConnectorFetch ---

    [Fact]
    public void RecordConnectorFetch_EmitsConnectorFetchEvent()
    {
        _sut.RecordConnectorFetch("jira", "conn-1", "user@example.com", 5, 123.4, true);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("ConnectorFetch", evt.Name);
    }

    [Fact]
    public void RecordConnectorFetch_IncludesCorrectProperties()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordConnectorFetch("jira", "conn-1", "user@example.com", 5, 123.4, true);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("jira", evt.Properties["provider"]);
        Assert.Equal("conn-1", evt.Properties["connectionId"]);
        Assert.Equal("5", evt.Properties["itemCount"]);
        Assert.Equal("123", evt.Properties["durationMs"]);
        Assert.Equal(expectedHash, evt.Properties["userId"]);
    }

    [Fact]
    public void RecordConnectorFetch_SuccessTrue_StatusIsSuccess()
    {
        _sut.RecordConnectorFetch("jira", "conn-1", "user@example.com", 5, 100.0, success: true);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("success", evt.Properties["status"]);
    }

    [Fact]
    public void RecordConnectorFetch_SuccessFalse_StatusIsError()
    {
        _sut.RecordConnectorFetch("jira", "conn-1", "user@example.com", 0, 50.0, success: false);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("error", evt.Properties["status"]);
    }

    // --- RecordConnectorException ---

    [Fact]
    public void RecordConnectorException_EmitsExceptionTelemetry()
    {
        var exception = new InvalidOperationException("test error");

        _sut.RecordConnectorException(exception, "github", "conn-2", "user@example.com");

        var ex = Assert.Single(_channel.Items.OfType<ExceptionTelemetry>());
        Assert.Equal(exception, ex.Exception);
    }

    [Fact]
    public void RecordConnectorException_IncludesCorrectProperties()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordConnectorException(new Exception("fail"), "github", "conn-2", "user@example.com");

        var ex = Assert.Single(_channel.Items.OfType<ExceptionTelemetry>());
        Assert.Equal("github", ex.Properties["provider"]);
        Assert.Equal("conn-2", ex.Properties["connectionId"]);
        Assert.Equal(expectedHash, ex.Properties["userId"]);
    }

    // --- RecordPageView ---

    [Fact]
    public void RecordPageView_EmitsPageViewWithCorrectName()
    {
        _sut.RecordPageView("Dashboard", "user@example.com");

        var pv = Assert.Single(_channel.Items.OfType<PageViewTelemetry>());
        Assert.Equal("Dashboard", pv.Name);
    }

    [Fact]
    public void RecordPageView_AuthenticatedUser_IncludesHashedUserId()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordPageView("Dashboard", "user@example.com");

        var pv = Assert.Single(_channel.Items.OfType<PageViewTelemetry>());
        Assert.Equal(expectedHash, pv.Properties["userId"]);
    }

    [Fact]
    public void RecordPageView_NullUser_DoesNotIncludeUserIdProperty()
    {
        _sut.RecordPageView("Dashboard", null);

        var pv = Assert.Single(_channel.Items.OfType<PageViewTelemetry>());
        Assert.False(pv.Properties.ContainsKey("userId"));
    }

    [Fact]
    public void RecordPageView_WhitespaceUser_DoesNotIncludeUserIdProperty()
    {
        _sut.RecordPageView("Dashboard", "   ");

        var pv = Assert.Single(_channel.Items.OfType<PageViewTelemetry>());
        Assert.False(pv.Properties.ContainsKey("userId"));
    }

    // --- RecordConfigSave ---

    [Fact]
    public void RecordConfigSave_EmitsConfigurationSavedEvent()
    {
        _sut.RecordConfigSave("user@example.com", 3);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("ConfigurationSaved", evt.Name);
    }

    [Fact]
    public void RecordConfigSave_IncludesHashedUserIdAndConnectorCount()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordConfigSave("user@example.com", 3);

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal(expectedHash, evt.Properties["userId"]);
        Assert.Equal("3", evt.Properties["connectorCount"]);
    }

    // --- RecordLinkedAccountOperation ---

    [Fact]
    public void RecordLinkedAccountOperation_Added_EmitsLinkedAccountAddedEvent()
    {
        _sut.RecordLinkedAccountOperation("added", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("LinkedAccountAdded", evt.Name);
    }

    [Fact]
    public void RecordLinkedAccountOperation_Removed_EmitsLinkedAccountRemovedEvent()
    {
        _sut.RecordLinkedAccountOperation("removed", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("LinkedAccountRemoved", evt.Name);
    }

    [Fact]
    public void RecordLinkedAccountOperation_RemovedCaseInsensitive_EmitsLinkedAccountRemovedEvent()
    {
        _sut.RecordLinkedAccountOperation("Removed", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal("LinkedAccountRemoved", evt.Name);
    }

    [Fact]
    public void RecordLinkedAccountOperation_IncludesHashedUserId()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordLinkedAccountOperation("added", "user@example.com");

        var evt = Assert.Single(_channel.Items.OfType<EventTelemetry>());
        Assert.Equal(expectedHash, evt.Properties["userId"]);
    }

    // --- RecordUserActivity ---

    [Fact]
    public void RecordUserActivity_DelegatesToActiveUserTrackerWithHashedId()
    {
        var expectedHash = UserIdentityHasher.Hash("user@example.com");

        _sut.RecordUserActivity("user@example.com");

        Assert.Single(_tracker.RecordedIds);
        Assert.Equal(expectedHash, _tracker.RecordedIds[0]);
    }

    // --- Stubs ---

    private sealed class StubActiveUserTracker : IActiveUserTracker
    {
        public List<string> RecordedIds { get; } = [];
        public void RecordActivity(string hashedUserId) => RecordedIds.Add(hashedUserId);
        public int GetActiveUserCount() => RecordedIds.Count;
    }

    private sealed class InMemoryChannel : ITelemetryChannel
    {
        public List<ITelemetry> Items { get; } = [];
        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; } = string.Empty;
        public void Send(ITelemetry item) => Items.Add(item);
        public void Flush() { }
        public void Dispose() { }
    }
}
