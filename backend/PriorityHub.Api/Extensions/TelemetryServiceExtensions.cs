using Microsoft.ApplicationInsights.Extensibility;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Extensions;

public static class TelemetryServiceExtensions
{
    public static IServiceCollection AddPriorityHubTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));
        services.AddSingleton<IActiveUserTracker, ActiveUserTracker>();

        var connectionString = configuration["ApplicationInsights:ConnectionString"];
        var isEnabled = !string.IsNullOrWhiteSpace(connectionString)
            || !string.IsNullOrWhiteSpace(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);

        if (!isEnabled)
        {
            services.AddSingleton<ITelemetryService, NoOpTelemetryService>();
            return services;
        }

        services.AddSingleton<ITelemetryInitializer, PriorityHubTelemetryInitializer>();
        services.AddSingleton<ITelemetryService, ApplicationInsightsTelemetryService>();
        services.AddHostedService<RegisteredUserMetricsBackgroundService>();
        return services;
    }
}
