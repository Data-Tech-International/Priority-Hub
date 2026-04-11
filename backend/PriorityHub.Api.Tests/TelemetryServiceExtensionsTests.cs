using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PriorityHub.Api.Extensions;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class TelemetryServiceExtensionsTests
{
    [Fact]
    public void AddPriorityHubTelemetry_NoConnectionString_RegistersNoOpTelemetryService()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.First(d => d.ServiceType == typeof(ITelemetryService));
        Assert.Equal(typeof(NoOpTelemetryService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_EmptyConnectionString_RegistersNoOpTelemetryService()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ApplicationInsights:ConnectionString"] = ""
        });

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.First(d => d.ServiceType == typeof(ITelemetryService));
        Assert.Equal(typeof(NoOpTelemetryService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_WithConnectionString_RegistersApplicationInsightsTelemetryService()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        });

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.First(d => d.ServiceType == typeof(ITelemetryService));
        Assert.Equal(typeof(ApplicationInsightsTelemetryService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_WithEnvVarConnectionString_RegistersApplicationInsightsTelemetryService()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        });

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.First(d => d.ServiceType == typeof(ITelemetryService));
        Assert.Equal(typeof(ApplicationInsightsTelemetryService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_AlwaysRegistersActiveUserTracker()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IActiveUserTracker));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(ActiveUserTracker), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_WhenEnabled_RegistersTelemetryInitializer()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
        });

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryInitializer));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(PriorityHubTelemetryInitializer), descriptor.ImplementationType);
    }

    [Fact]
    public void AddPriorityHubTelemetry_WhenDisabled_DoesNotRegisterTelemetryInitializer()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        services.AddPriorityHubTelemetry(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ITelemetryInitializer));
        Assert.Null(descriptor);
    }

    [Fact]
    public void AddPriorityHubTelemetry_WhenDisabled_CanResolveNoOpTelemetryService()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        services.AddPriorityHubTelemetry(configuration);

        using var provider = services.BuildServiceProvider();
        var telemetryService = provider.GetRequiredService<ITelemetryService>();
        Assert.IsType<NoOpTelemetryService>(telemetryService);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
