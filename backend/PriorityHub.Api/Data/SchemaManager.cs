using System.Reflection;
using Npgsql;

namespace PriorityHub.Api.Data;

/// <summary>
/// Applies SQL migrations embedded in the assembly to bring the database schema up to date.
/// </summary>
/// <remarks>
/// In Development the manager auto-applies any pending migrations.
/// In non-Development environments it fails fast if unapplied migrations are detected,
/// so the deployment process must run migrations before starting the application.
/// </remarks>
public sealed class SchemaManager(NpgsqlDataSource dataSource, IHostEnvironment environment, ILogger<SchemaManager> logger)
{
    private const string MigrationsResourcePrefix = "PriorityHub.Api.Data.Migrations.";

    /// <summary>
    /// Ensures the database schema is current.  Call once during application startup.
    /// </summary>
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMigrationsTableAsync(cancellationToken);

        var applied = await GetAppliedVersionsAsync(cancellationToken);
        var pending = GetPendingMigrations(applied);

        if (pending.Count == 0)
        {
            logger.LogInformation("Database schema is up to date.");
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"The database has {pending.Count} unapplied migration(s): {string.Join(", ", pending.Select(m => m.Version))}. " +
                "Run migrations before starting the application in non-Development environments.");
        }

        foreach (var migration in pending)
        {
            logger.LogInformation("Applying migration {Version}: {Name}.", migration.Version, migration.Name);
            await ApplyMigrationAsync(migration, cancellationToken);
        }

        logger.LogInformation("All migrations applied successfully.");
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task EnsureMigrationsTableAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version     INTEGER     PRIMARY KEY,
                applied_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """;

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<HashSet<int>> GetAppliedVersionsAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT version FROM schema_migrations ORDER BY version";
        var versions = new HashSet<int>();

        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private static List<MigrationScript> GetPendingMigrations(HashSet<int> applied)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(MigrationsResourcePrefix, StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .ToList();

        var pending = new List<MigrationScript>();

        foreach (var resource in resources)
        {
            var fileName = resource[MigrationsResourcePrefix.Length..];
            if (!TryParseVersion(fileName, out var version)) continue;
            if (applied.Contains(version)) continue;

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            pending.Add(new MigrationScript(version, fileName, sql));
        }

        return pending;
    }

    private async Task ApplyMigrationAsync(MigrationScript migration, CancellationToken cancellationToken)
    {
        await using var conn = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        await using var migrationCmd = new NpgsqlCommand(migration.Sql, conn, tx);
        await migrationCmd.ExecuteNonQueryAsync(cancellationToken);

        await using var trackCmd = new NpgsqlCommand(
            "INSERT INTO schema_migrations (version) VALUES ($1) ON CONFLICT DO NOTHING",
            conn, tx);
        trackCmd.Parameters.AddWithValue(migration.Version);
        await trackCmd.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    private static bool TryParseVersion(string fileName, out int version)
    {
        version = 0;
        var underscore = fileName.IndexOf('_');
        if (underscore <= 0) return false;
        return int.TryParse(fileName[..underscore], out version);
    }

    private sealed record MigrationScript(int Version, string Name, string Sql);
}
