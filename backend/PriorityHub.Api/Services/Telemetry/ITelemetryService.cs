namespace PriorityHub.Api.Services.Telemetry;

public interface ITelemetryService
{
    void RecordSignIn(string provider, string userIdentityKey);
    void RecordSignOut(string userIdentityKey);
    void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success);
    void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey);
    void RecordUserActivity(string userIdentityKey);
    void RecordPageView(string pageName, string? userIdentityKey);
    void RecordConfigSave(string userIdentityKey, int connectorCount);
    void RecordLinkedAccountOperation(string operation, string userIdentityKey);
    void RecordUserCounts(int activeUsers, int registeredUsers);
}
