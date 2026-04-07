using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace PriorityHub.Api.Services.Telemetry;

public sealed class ActiveUserTracker(IOptions<TelemetryOptions> options) : IActiveUserTracker
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSeenByUser = new(StringComparer.Ordinal);
    private readonly TimeSpan _activeWindow = TimeSpan.FromMinutes(Math.Max(1, options.Value.ActiveUserWindowMinutes));
    private readonly int _maxEntries = Math.Max(1_000, options.Value.MaxActiveUserEntries);

    public void RecordActivity(string hashedUserId)
    {
        if (string.IsNullOrWhiteSpace(hashedUserId))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _lastSeenByUser[hashedUserId] = now;

        if (_lastSeenByUser.Count > _maxEntries)
        {
            Prune(now, forceCapacityTrim: true);
        }
    }

    public int GetActiveUserCount()
    {
        var now = DateTimeOffset.UtcNow;
        Prune(now, forceCapacityTrim: false);

        var threshold = now - _activeWindow;
        return _lastSeenByUser.Values.Count(ts => ts >= threshold);
    }

    private void Prune(DateTimeOffset now, bool forceCapacityTrim)
    {
        var threshold = now - _activeWindow;

        foreach (var kvp in _lastSeenByUser)
        {
            if (kvp.Value < threshold)
            {
                _lastSeenByUser.TryRemove(kvp.Key, out _);
            }
        }

        if (!forceCapacityTrim || _lastSeenByUser.Count <= _maxEntries)
        {
            return;
        }

        var overflow = _lastSeenByUser.Count - _maxEntries;
        foreach (var kvp in _lastSeenByUser.OrderBy(pair => pair.Value).Take(overflow))
        {
            _lastSeenByUser.TryRemove(kvp.Key, out _);
        }
    }
}
