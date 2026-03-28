using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PriorityHub.Api.Tests;

internal static class TestHelpers
{
    /// <summary>Parses an inline JSON string into a JsonElement.</summary>
    public static JsonElement JsonOf(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json, JsonSerializerOptions.Web);

    /// <summary>Serializes an anonymous object to a JsonElement using Web defaults.</summary>
    public static JsonElement JsonObject(object value) =>
        JsonSerializer.SerializeToElement(value, JsonSerializerOptions.Web);
}

/// <summary>Minimal IHostEnvironment implementation for tests.</summary>
internal sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "PriorityHub.Api.Tests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

/// <summary>No-op ILogger implementation for tests.</summary>
internal sealed class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

