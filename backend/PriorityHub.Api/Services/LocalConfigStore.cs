using System.Text.Json;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

public sealed class LocalConfigStore(IHostEnvironment environment) : IConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string UserConfigDirectory => Path.Combine(environment.ContentRootPath, "..", "..", "config", "users");

    public async Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken)
    {
        var configPath = GetConfigPath(userId);

        if (!File.Exists(configPath))
        {
            return new ProviderConfiguration();
        }

        await using var stream = File.OpenRead(configPath);
        var config = await JsonSerializer.DeserializeAsync<ProviderConfiguration>(stream, JsonOptions, cancellationToken);
        return config ?? new ProviderConfiguration();
    }

    public async Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken)
    {
        var configPath = GetConfigPath(userId);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        await using var stream = File.Create(configPath);
        await JsonSerializer.SerializeAsync(stream, Normalize(configuration), JsonOptions, cancellationToken);
    }

    private string GetConfigPath(string userId)
    {
        var fileName = SanitizeForFileName(string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim());
        return Path.Combine(UserConfigDirectory, $"{fileName}.json");
    }

    internal static string SanitizeForFileName(string value)
    {
        var normalized = value.ToLowerInvariant()
            .Replace("@", "_at_")
            .Replace('.', '_');

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeChars = normalized.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray();
        var result = new string(safeChars);

        return string.IsNullOrWhiteSpace(result) ? "user" : result;
    }

    internal static ProviderConfiguration Normalize(ProviderConfiguration configuration)
    {
        configuration.AzureDevOps ??= [];
        configuration.GitHub ??= [];
        configuration.Jira ??= [];
        configuration.MicrosoftTasks ??= [];
        configuration.OutlookFlaggedMail ??= [];
        configuration.Trello ??= [];
        configuration.ImapFlaggedMail ??= [];
        configuration.LinkedMicrosoftAccounts ??= [];
        configuration.Preferences ??= new UserPreferences();
        configuration.Preferences.OrderedItemIds ??= [];
        return configuration;
    }
}