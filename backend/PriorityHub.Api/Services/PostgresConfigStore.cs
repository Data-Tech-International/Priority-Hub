using System.Text.Json;
using Npgsql;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

/// <summary>
/// Stores per-user provider configuration in a PostgreSQL JSONB column.
/// Each row holds the entire <see cref="ProviderConfiguration"/> document for one user.
/// A monotonically increasing <c>version</c> column is incremented on every write
/// to support audit and future optimistic-concurrency upgrades.
/// </summary>
public sealed class PostgresConfigStore(NpgsqlDataSource dataSource) : IConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public async Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT config FROM user_config WHERE user_id = $1";

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(userId);

        var raw = await cmd.ExecuteScalarAsync(cancellationToken) as string;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return LocalConfigStore.Normalize(new ProviderConfiguration());
        }

        var config = JsonSerializer.Deserialize<ProviderConfiguration>(raw, JsonOptions);
        return LocalConfigStore.Normalize(config ?? new ProviderConfiguration());
    }

    /// <inheritdoc/>
    public async Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        var normalized = LocalConfigStore.Normalize(configuration);
        var json = JsonSerializer.Serialize(normalized, JsonOptions);

        const string sql = """
            INSERT INTO user_config (user_id, config, version, updated_at)
            VALUES ($1, $2::jsonb, 1, NOW())
            ON CONFLICT (user_id) DO UPDATE
                SET config     = EXCLUDED.config,
                    version    = user_config.version + 1,
                    updated_at = NOW()
            """;

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue(userId);
        cmd.Parameters.AddWithValue(json);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GetRegisteredUserCountAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM user_config";

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);

        var value = await cmd.ExecuteScalarAsync(cancellationToken);
        return value is long count ? checked((int)count) : 0;
    }
}
