using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

/// <summary>
/// Abstraction for loading and saving per-user provider configuration.
/// </summary>
public interface IConfigStore
{
    /// <summary>Loads the provider configuration for the given user.</summary>
    Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Persists the provider configuration for the given user.</summary>
    Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>Returns the total number of users with persisted configuration entries.</summary>
    Task<int> GetRegisteredUserCountAsync(CancellationToken cancellationToken);
}
