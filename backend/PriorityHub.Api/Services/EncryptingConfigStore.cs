using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

/// <summary>
/// Decorator over <see cref="IConfigStore"/> that transparently encrypts sensitive
/// credential fields before saving and decrypts them after loading.
/// The underlying store interface signature is unchanged (CON-ENC-002).
/// </summary>
public sealed class EncryptingConfigStore(
    IConfigStore inner,
    ICredentialProtector protector,
    ILogger<EncryptingConfigStore> logger) : IConfigStore
{
    /// <inheritdoc/>
    public async Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken)
    {
        var config = await inner.LoadAsync(userId, cancellationToken);
        ConfigEncryptionHelper.Decrypt(config, protector, logger);
        return config;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        // Work on a deep clone so the caller's in-memory config keeps plaintext values.
        var clone = DeepClone(configuration);
        ConfigEncryptionHelper.Encrypt(clone, protector);
        await inner.SaveAsync(userId, clone, cancellationToken);
    }

    private static ProviderConfiguration DeepClone(ProviderConfiguration config)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(config, System.Text.Json.JsonSerializerOptions.Web);
        return System.Text.Json.JsonSerializer.Deserialize<ProviderConfiguration>(json, System.Text.Json.JsonSerializerOptions.Web)
               ?? new ProviderConfiguration();
    }
}
