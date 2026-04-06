using Microsoft.AspNetCore.DataProtection;
using Npgsql;
using PriorityHub.Api.Data;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Extensions;

/// <summary>
/// Extension methods for registering the config store and its dependencies.
/// </summary>
public static class ConfigStoreServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IConfigStore"/> based on the <c>ConfigStore:Provider</c>
    /// configuration value (<c>Postgres</c> or <c>File</c>).
    /// Defaults to <c>File</c> when the value is absent or unrecognised.
    /// The registered store is wrapped with <see cref="EncryptingConfigStore"/> so all
    /// sensitive credential fields are encrypted at rest.
    /// </summary>
    public static IServiceCollection AddConfigStore(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Data Protection with a configurable key ring directory.
        // Default: config/keys/ at the repository root (2 levels above ContentRootPath,
        // consistent with LocalConfigStore which locates config/ the same way).
        // Override with DataProtection:KeyRingPath in appsettings.json or environment variables.
        services.AddDataProtection(options =>
        {
            options.ApplicationDiscriminator = "PriorityHub";
        })
        .PersistKeysToFileSystem(new DirectoryInfo(ResolveKeyRingPath(configuration)))
        .SetApplicationName("PriorityHub");

        services.AddSingleton<ICredentialProtector, DataProtectionCredentialProtector>();

        var provider = configuration["ConfigStore:Provider"] ?? "File";

        if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration["ConfigStore:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "ConfigStore:ConnectionString must be set when ConfigStore:Provider is 'Postgres'. " +
                    "Add it to appsettings.Development.json or a user secret.");

            var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
            services.AddSingleton(dataSource);
            services.AddSingleton<SchemaManager>();
            services.AddSingleton<PostgresConfigStore>();
            services.AddSingleton<IConfigStore>(sp =>
                new EncryptingConfigStore(
                    sp.GetRequiredService<PostgresConfigStore>(),
                    sp.GetRequiredService<ICredentialProtector>(),
                    sp.GetRequiredService<ILogger<EncryptingConfigStore>>()));
        }
        else
        {
            services.AddSingleton<LocalConfigStore>();
            services.AddSingleton<IConfigStore>(sp =>
                new EncryptingConfigStore(
                    sp.GetRequiredService<LocalConfigStore>(),
                    sp.GetRequiredService<ICredentialProtector>(),
                    sp.GetRequiredService<ILogger<EncryptingConfigStore>>()));
        }

        return services;
    }

    /// <summary>
    /// If PostgreSQL is configured, runs pending schema migrations.
    /// Call this after <see cref="WebApplication"/> is built but before <c>app.Run()</c>.
    /// Retries on transient database connection failures to handle container startup timing.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        if (app.Services.GetService<SchemaManager>() is not { } schemaManager)
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILogger<SchemaManager>>();
        const int maxRetries = 5;
        const int delaySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await schemaManager.ApplyAsync();
                return;
            }
            catch (NpgsqlException) when (attempt < maxRetries)
            {
                logger.LogWarning(
                    "Database connection failed (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    attempt,
                    maxRetries,
                    delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    private static string ResolveKeyRingPath(IConfiguration configuration)
    {
        var configured = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        // Use the content root obtained from the hosting environment path that WebApplication
        // sets during startup. Fall back to working directory if not yet set.
        var contentRoot = configuration["ContentRoot"] ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "config", "keys"));
    }
}
