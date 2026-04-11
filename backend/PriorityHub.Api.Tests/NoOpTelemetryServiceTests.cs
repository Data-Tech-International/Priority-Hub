using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class NoOpTelemetryServiceTests
{
    private readonly StubActiveUserTracker _tracker = new();
    private readonly NoOpTelemetryService _sut;

    public NoOpTelemetryServiceTests()
    {
        _sut = new NoOpTelemetryService(_tracker);
    }

    [Fact]
    public void RecordUserActivity_DelegatesToActiveUserTrackerWithHashedId()
    {
        const string rawIdentity = "user@example.com";
        var expectedHash = UserIdentityHasher.Hash(rawIdentity);

        _sut.RecordUserActivity(rawIdentity);

        Assert.Single(_tracker.RecordedIds);
        Assert.Equal(expectedHash, _tracker.RecordedIds[0]);
    }

    [Fact]
    public void RecordUserActivity_DoesNotPassRawIdentity()
    {
        const string rawIdentity = "user@example.com";

        _sut.RecordUserActivity(rawIdentity);

        Assert.DoesNotContain(rawIdentity, _tracker.RecordedIds);
    }

    [Fact]
    public void RecordSignIn_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordSignIn("google", "user@example.com"));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordSignOut_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordSignOut("user@example.com"));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordConnectorFetch_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            _sut.RecordConnectorFetch("azure-devops", "conn-1", "user@example.com", 10, 250.5, true));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordConnectorException_DoesNotThrow()
    {
        var exception = Record.Exception(() =>
            _sut.RecordConnectorException(new InvalidOperationException("test"), "jira", "conn-2", "user@example.com"));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordPageView_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordPageView("Dashboard", "user@example.com"));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordPageView_NullUser_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordPageView("Dashboard", null));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordConfigSave_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordConfigSave("user@example.com", 3));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordLinkedAccountOperation_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordLinkedAccountOperation("added", "user@example.com"));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordUserCounts_DoesNotThrow()
    {
        var exception = Record.Exception(() => _sut.RecordUserCounts(5, 100));

        Assert.Null(exception);
    }

    private sealed class StubActiveUserTracker : IActiveUserTracker
    {
        public List<string> RecordedIds { get; } = [];

        public void RecordActivity(string hashedUserId) => RecordedIds.Add(hashedUserId);

        public int GetActiveUserCount() => RecordedIds.Count;
    }
}
