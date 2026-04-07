namespace PriorityHub.Api.Services.Telemetry;

public interface IActiveUserTracker
{
    void RecordActivity(string hashedUserId);
    int GetActiveUserCount();
}
