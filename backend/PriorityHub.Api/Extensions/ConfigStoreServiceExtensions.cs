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
        // Register Data Protection with a configurable key ring directory (default: config/keys/).
        var keyRingPath = configuration["DataProtection:KeyRingPath"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "config", "keys");
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
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
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        if (app.Services.GetService<SchemaManager>() is { } schemaManager)
        {
            await schemaManager.ApplyAsync();
        }
    }
}
