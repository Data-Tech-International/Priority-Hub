using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace PriorityHub.Api.Services.Telemetry;

public sealed class ApplicationInsightsTelemetryService(
    TelemetryClient telemetryClient,
    IActiveUserTracker activeUserTracker) : ITelemetryService
{
    private readonly Metric _authSignInCount = telemetryClient.GetMetric("AuthSignInCount", "Provider");
    private readonly Metric _connectorFetchCount = telemetryClient.GetMetric("ConnectorFetchCount", "Provider", "Status");
    private readonly Metric _connectorFetchDuration = telemetryClient.GetMetric("ConnectorFetchDuration", "Provider");
    private readonly Metric _connectorItemsCount = telemetryClient.GetMetric("ConnectorItemsCount", "Provider");
    private readonly Metric _activeUsers = telemetryClient.GetMetric("ActiveUsers");
    private readonly Metric _registeredUsers = telemetryClient.GetMetric("RegisteredUsers");

    public void RecordSignIn(string provider, string userIdentityKey)
    {
        var hashedUserId = UserIdentityHasher.Hash(userIdentityKey);
        telemetryClient.TrackEvent("AuthenticationSignIn", new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["userId"] = hashedUserId
        });

        _authSignInCount.TrackValue(1, provider);
    }

    public void RecordSignOut(string userIdentityKey)
    {
        telemetryClient.TrackEvent("AuthenticationSignOut", new Dictionary<string, string>
        {
            ["userId"] = UserIdentityHasher.Hash(userIdentityKey)
        });
    }

    public void RecordConnectorFetch(string provider, string connectionId, string userIdentityKey, int itemCount, double durationMs, bool success)
    {
        var status = success ? "success" : "error";
        telemetryClient.TrackEvent("ConnectorFetch", new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["connectionId"] = connectionId,
            ["status"] = status,
            ["itemCount"] = itemCount.ToString(),
            ["durationMs"] = durationMs.ToString("F0"),
            ["userId"] = UserIdentityHasher.Hash(userIdentityKey)
        });

        _connectorFetchCount.TrackValue(1, provider, status);
        _connectorFetchDuration.TrackValue(durationMs, provider);
        _connectorItemsCount.TrackValue(itemCount, provider);
    }

    public void RecordConnectorException(Exception exception, string provider, string connectionId, string userIdentityKey)
    {
        telemetryClient.TrackException(exception, new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["connectionId"] = connectionId,
            ["userId"] = UserIdentityHasher.Hash(userIdentityKey)
        });
    }

    public void RecordUserActivity(string userIdentityKey)
    {
        activeUserTracker.RecordActivity(UserIdentityHasher.Hash(userIdentityKey));
    }

    public void RecordPageView(string pageName, string? userIdentityKey)
    {
        var telemetry = new PageViewTelemetry(pageName);
        if (!string.IsNullOrWhiteSpace(userIdentityKey))
        {
            telemetry.Properties["userId"] = UserIdentityHasher.Hash(userIdentityKey);
        }

        telemetryClient.TrackPageView(telemetry);
    }

    public void RecordConfigSave(string userIdentityKey, int connectorCount)
    {
        telemetryClient.TrackEvent("ConfigurationSaved", new Dictionary<string, string>
        {
            ["userId"] = UserIdentityHasher.Hash(userIdentityKey),
            ["connectorCount"] = connectorCount.ToString()
        });
    }

    public void RecordLinkedAccountOperation(string operation, string userIdentityKey)
    {
        var eventName = operation.Equals("removed", StringComparison.OrdinalIgnoreCase)
            ? "LinkedAccountRemoved"
            : "LinkedAccountAdded";

        telemetryClient.TrackEvent(eventName, new Dictionary<string, string>
        {
            ["userId"] = UserIdentityHasher.Hash(userIdentityKey)
        });
    }

    public void RecordUserCounts(int activeUsers, int registeredUsers)
    {
        _activeUsers.TrackValue(activeUsers);
        _registeredUsers.TrackValue(registeredUsers);
    }
}
