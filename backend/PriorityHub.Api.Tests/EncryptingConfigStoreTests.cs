using Microsoft.Extensions.Logging.Abstractions;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

/// <summary>
/// Tests for <see cref="EncryptingConfigStore"/> decorator.
/// Uses an in-memory store and a prefix-based no-op protector to verify
/// that sensitive fields are encrypted on save and decrypted on load.
/// </summary>
public sealed class EncryptingConfigStoreTests
{
    private sealed class InMemoryConfigStore : IConfigStore
    {
        private readonly Dictionary<string, ProviderConfiguration> _store = new();

        public Task<ProviderConfiguration> LoadAsync(string userId, CancellationToken cancellationToken)
        {
            _store.TryGetValue(userId, out var config);
            return Task.FromResult(config ?? new ProviderConfiguration());
        }

        public Task SaveAsync(string userId, ProviderConfiguration configuration, CancellationToken cancellationToken)
        {
            _store[userId] = configuration;
            return Task.CompletedTask;
        }

        public ProviderConfiguration? GetRaw(string userId) =>
            _store.TryGetValue(userId, out var config) ? config : null;
    }

    private sealed class PrefixProtector : ICredentialProtector
    {
        private const string Prefix = "ENC:";

        public string Protect(string plaintext) => Prefix + plaintext;

        public string? Unprotect(string ciphertext) =>
            ciphertext.StartsWith(Prefix) ? ciphertext[Prefix.Length..] : null;
    }

    private static EncryptingConfigStore CreateStore(InMemoryConfigStore inner) =>
        new(inner, new PrefixProtector(), NullLogger<EncryptingConfigStore>.Instance);

    [Fact]
    public async Task SaveAsync_EncryptsSensitiveFields_InStoredConfig()
    {
        var inner = new InMemoryConfigStore();
        var store = CreateStore(inner);
        const string userId = "user@test.com";

        var config = new ProviderConfiguration
        {
            AzureDevOps = [new AzureDevOpsConnection { PersonalAccessToken = "my-pat" }]
        };

        await store.SaveAsync(userId, config, CancellationToken.None);

        // The inner store must have the encrypted value.
        var raw = inner.GetRaw(userId);
        Assert.NotNull(raw);
        Assert.Equal("ENC:my-pat", raw!.AzureDevOps[0].PersonalAccessToken);
    }

    [Fact]
    public async Task LoadAsync_DecryptsSensitiveFields_WhenCiphertextPresent()
    {
        var inner = new InMemoryConfigStore();
        // Seed the inner store with already-encrypted data.
        await inner.SaveAsync("user@test.com", new ProviderConfiguration
        {
            AzureDevOps = [new AzureDevOpsConnection { PersonalAccessToken = "ENC:my-pat" }]
        }, CancellationToken.None);

        var store = CreateStore(inner);
        var loaded = await store.LoadAsync("user@test.com", CancellationToken.None);

        Assert.Equal("my-pat", loaded.AzureDevOps[0].PersonalAccessToken);
    }

    [Fact]
    public async Task LoadAsync_PlaintextValue_LeftUnchanged_MigrationScenario()
    {
        var inner = new InMemoryConfigStore();
        // Simulate pre-encryption file: value is raw plaintext.
        await inner.SaveAsync("user@test.com", new ProviderConfiguration
        {
            Jira = [new JiraConnection { ApiToken = "old-plaintext-token" }]
        }, CancellationToken.None);

        var store = CreateStore(inner);
        var loaded = await store.LoadAsync("user@test.com", CancellationToken.None);

        // Migration: plaintext is returned as-is so the connector still works.
        Assert.Equal("old-plaintext-token", loaded.Jira[0].ApiToken);
    }

    [Fact]
    public async Task SaveAsync_DoesNotMutateCallerConfig()
    {
        // The EncryptingConfigStore must clone before encrypting — caller retains plaintext.
        var inner = new InMemoryConfigStore();
        var store = CreateStore(inner);
        const string userId = "user@test.com";

        var original = new ProviderConfiguration
        {
            AzureDevOps = [new AzureDevOpsConnection { PersonalAccessToken = "pat-value" }]
        };

        await store.SaveAsync(userId, original, CancellationToken.None);

        // Caller's object must still hold plaintext.
        Assert.Equal("pat-value", original.AzureDevOps[0].PersonalAccessToken);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrip_ReturnsOriginalPlaintext()
    {
        var inner = new InMemoryConfigStore();
        var store = CreateStore(inner);
        const string userId = "user@test.com";

        var config = new ProviderConfiguration
        {
            Trello = [new TrelloConnection { ApiKey = "key-abc", Token = "tok-xyz" }]
        };

        await store.SaveAsync(userId, config, CancellationToken.None);
        var loaded = await store.LoadAsync(userId, CancellationToken.None);

        Assert.Equal("key-abc", loaded.Trello[0].ApiKey);
        Assert.Equal("tok-xyz", loaded.Trello[0].Token);
    }

    [Fact]
    public async Task SaveAsync_ImapPassword_IsEncrypted()
    {
        var inner = new InMemoryConfigStore();
        var store = CreateStore(inner);
        const string userId = "user@test.com";

        var config = new ProviderConfiguration
        {
            ImapFlaggedMail = [new ImapFlaggedMailConnection { Password = "imap-pass" }]
        };

        await store.SaveAsync(userId, config, CancellationToken.None);

        var raw = inner.GetRaw(userId);
        Assert.Equal("ENC:imap-pass", raw!.ImapFlaggedMail[0].Password);
    }

    [Fact]
    public async Task SaveAsync_LinkedAccountRefreshToken_IsEncrypted()
    {
        var inner = new InMemoryConfigStore();
        var store = CreateStore(inner);
        const string userId = "user@test.com";

        var config = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [new LinkedMicrosoftAccount { RefreshToken = "rt-secret" }]
        };

        await store.SaveAsync(userId, config, CancellationToken.None);

        var raw = inner.GetRaw(userId);
        Assert.Equal("ENC:rt-secret", raw!.LinkedMicrosoftAccounts[0].RefreshToken);
    }
}
