using PriorityHub.Api.Models;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

public sealed class LocalConfigStoreTests : IDisposable
{
    // ContentRootPath = _tempRoot/api/bin → UserConfigDirectory = _tempRoot/config/users
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private LocalConfigStore CreateStore() =>
        new(new TestHostEnvironment(Path.Combine(_tempRoot, "api", "bin")));

    // ── SanitizeForFileName ──────────────────────────────────────────────────

    [Theory]
    [InlineData("alice@example.com", "alice_at_example_com")]
    [InlineData("ALICE@EXAMPLE.COM", "alice_at_example_com")]
    [InlineData("user.name@domain.org", "user_name_at_domain_org")]
    [InlineData("ivan_pavlovic_at_dti_rs", "ivan_pavlovic_at_dti_rs")]
    public void SanitizeForFileName_NormalizesEmailsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, LocalConfigStore.SanitizeForFileName(input));
    }

    [Fact]
    public void SanitizeForFileName_EmptyString_ReturnsUserFallback()
    {
        Assert.Equal("user", LocalConfigStore.SanitizeForFileName(string.Empty));
    }

    [Fact]
    public void SanitizeForFileName_WhitespaceOnly_ReturnsUserFallback()
    {
        Assert.Equal("user", LocalConfigStore.SanitizeForFileName("   "));
    }

    // ── Normalize ────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_NullLists_InitializesAll()
    {
        var config = new ProviderConfiguration
        {
            AzureDevOps = null!,
            GitHub = null!,
            Jira = null!,
            MicrosoftTasks = null!,
            OutlookFlaggedMail = null!,
            Trello = null!,
            ImapFlaggedMail = null!,
            LinkedMicrosoftAccounts = null!,
            Preferences = null!
        };

        var result = LocalConfigStore.Normalize(config);

        Assert.NotNull(result.AzureDevOps);
        Assert.NotNull(result.GitHub);
        Assert.NotNull(result.Jira);
        Assert.NotNull(result.MicrosoftTasks);
        Assert.NotNull(result.OutlookFlaggedMail);
        Assert.NotNull(result.Trello);
        Assert.NotNull(result.ImapFlaggedMail);
        Assert.NotNull(result.LinkedMicrosoftAccounts);
        Assert.NotNull(result.Preferences);
        Assert.NotNull(result.Preferences.OrderedItemIds);
    }

    [Fact]
    public void Normalize_ExistingLists_PreservesItems()
    {
        var existing = new List<AzureDevOpsConnection> { new() { Name = "Work" } };
        var config = new ProviderConfiguration { AzureDevOps = existing };

        LocalConfigStore.Normalize(config);

        Assert.Single(config.AzureDevOps);
        Assert.Equal("Work", config.AzureDevOps[0].Name);
    }

    // ── LoadAsync / SaveAsync round-trip ────────────────────────────────────

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsEmptyConfiguration()
    {
        var store = CreateStore();
        var config = await store.LoadAsync("nonexistent@example.com", CancellationToken.None);

        Assert.NotNull(config);
        Assert.Empty(config.AzureDevOps);
        Assert.Empty(config.Trello);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsIdenticalData()
    {
        var store = CreateStore();
        const string userId = "roundtrip@test.com";

        var saved = new ProviderConfiguration
        {
            AzureDevOps = [new AzureDevOpsConnection { Name = "Persist Me", Organization = "myorg", Project = "myproj" }],
            Preferences = new UserPreferences { OrderedItemIds = ["ADO-1", "ADO-2"] }
        };

        await store.SaveAsync(userId, saved, CancellationToken.None);
        var loaded = await store.LoadAsync(userId, CancellationToken.None);

        Assert.Single(loaded.AzureDevOps);
        Assert.Equal("Persist Me", loaded.AzureDevOps[0].Name);
        Assert.Equal(2, loaded.Preferences.OrderedItemIds.Count);
        Assert.Contains("ADO-1", loaded.Preferences.OrderedItemIds);
    }

    // ── GetConfigPath behaviour ──────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_CreatesFileAtSanitizedPath()
    {
        var store = CreateStore();
        var expectedDir = Path.Combine(_tempRoot, "config", "users");

        await store.SaveAsync("test@email.com", new ProviderConfiguration(), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(expectedDir, "test_at_email_com.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
