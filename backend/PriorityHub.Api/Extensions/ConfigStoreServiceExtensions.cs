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
    /// </summary>
    public static IServiceCollection AddConfigStore(this IServiceCollection services, IConfiguration configuration)
    {
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
            services.AddSingleton<IConfigStore, PostgresConfigStore>();
        }
        else
        {
            services.AddSingleton<IConfigStore, LocalConfigStore>();
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
