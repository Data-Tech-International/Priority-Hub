namespace PriorityHub.Api.Services.Telemetry;

public sealed class NoOpTelemetryService(IActiveUserTracker activeUserTracker) : ITelemetryService
{
    public void RecordSignIn(string provider, string userIdentityKey) { }
    public void RecordSignOut(string userIdentityKey) { }
    public void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success) { }
    public void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey) { }

    public void RecordUserActivity(string userIdentityKey)
    {
        activeUserTracker.RecordActivity(UserIdentityHasher.Hash(userIdentityKey));
    }

    public void RecordPageView(string pageName, string? userIdentityKey) { }
    public void RecordConfigSave(string userIdentityKey, int connectorCount) { }
    public void RecordLinkedAccountOperation(string operation, string userIdentityKey) { }
    public void RecordUserCounts(int activeUsers, int registeredUsers) { }
}
