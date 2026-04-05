using PriorityHub.Api.Models;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

public sealed class ConfigEncryptionHelperTests
{
    // ── No-op protector for round-trip checks ───────────────────────────────

    private sealed class NoOpProtector : ICredentialProtector
    {
        public string Protect(string plaintext) => $"ENC[{plaintext}]";
        public string? Unprotect(string ciphertext) =>
            ciphertext.StartsWith("ENC[") && ciphertext.EndsWith("]")
                ? ciphertext[4..^1]
                : null; // plaintext — migration scenario
    }

    // ── Encrypt covers all [SensitiveField] properties ───────────────────────

    [Fact]
    public void Encrypt_AzureDevOps_EncryptsPersonalAccessToken()
    {
        var config = ConfigWith(ado: new AzureDevOpsConnection { PersonalAccessToken = "pat123" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[pat123]", config.AzureDevOps[0].PersonalAccessToken);
    }

    [Fact]
    public void Encrypt_Jira_EncryptsApiToken()
    {
        var config = ConfigWith(jira: new JiraConnection { ApiToken = "jira-tok" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[jira-tok]", config.Jira[0].ApiToken);
    }

    [Fact]
    public void Encrypt_GitHub_EncryptsPat()
    {
        var config = ConfigWith(github: new GitHubConnection { PersonalAccessToken = "gh-pat" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[gh-pat]", config.GitHub[0].PersonalAccessToken);
    }

    [Fact]
    public void Encrypt_Trello_EncryptsBothKeys()
    {
        var trello = new TrelloConnection { ApiKey = "key1", Token = "tok1" };
        var config = ConfigWith(trello: trello);
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[key1]", config.Trello[0].ApiKey);
        Assert.Equal("ENC[tok1]", config.Trello[0].Token);
    }

    [Fact]
    public void Encrypt_ImapFlaggedMail_EncryptsPassword()
    {
        var imap = new ImapFlaggedMailConnection { Password = "imap-pass" };
        var config = ConfigWith(imap: imap);
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[imap-pass]", config.ImapFlaggedMail[0].Password);
    }

    [Fact]
    public void Encrypt_LinkedAccount_EncryptsRefreshToken()
    {
        var linked = new LinkedMicrosoftAccount { RefreshToken = "rt-abc" };
        var config = new ProviderConfiguration { LinkedMicrosoftAccounts = [linked] };
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("ENC[rt-abc]", config.LinkedMicrosoftAccounts[0].RefreshToken);
    }

    [Fact]
    public void Encrypt_EmptyField_IsNotTransformed()
    {
        var config = ConfigWith(ado: new AzureDevOpsConnection { PersonalAccessToken = "" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        Assert.Equal("", config.AzureDevOps[0].PersonalAccessToken);
    }

    [Fact]
    public void Encrypt_NonSensitiveFields_AreNotEncrypted()
    {
        var config = ConfigWith(jira: new JiraConnection { Email = "user@test.com", ApiToken = "tok" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);

        // Email is not marked [SensitiveField]; must remain unchanged.
        Assert.Equal("user@test.com", config.Jira[0].Email);
    }

    // ── Decrypt: encrypted values are decrypted ──────────────────────────────

    [Fact]
    public void Decrypt_EncryptedValue_DecryptsCorrectly()
    {
        var config = ConfigWith(ado: new AzureDevOpsConnection { PersonalAccessToken = "ENC[my-pat]" });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Decrypt(config, protector);

        Assert.Equal("my-pat", config.AzureDevOps[0].PersonalAccessToken);
    }

    // ── Decrypt: plaintext values (migration) are left unchanged ─────────────

    [Fact]
    public void Decrypt_PlaintextValue_IsReturnedAsIs()
    {
        // Simulates a pre-encryption config file where value is still plaintext.
        var config = ConfigWith(ado: new AzureDevOpsConnection { PersonalAccessToken = "raw-pat" });
        var protector = new NoOpProtector(); // Unprotect returns null for non-ENC values

        ConfigEncryptionHelper.Decrypt(config, protector);

        // Value should be unchanged — migration behaviour.
        Assert.Equal("raw-pat", config.AzureDevOps[0].PersonalAccessToken);
    }

    // ── Round-trip: Encrypt then Decrypt returns original value ──────────────

    [Fact]
    public void EncryptThenDecrypt_RoundTrip_PreservesValue()
    {
        const string secret = "super-secret-token";
        var config = ConfigWith(jira: new JiraConnection { ApiToken = secret });
        var protector = new NoOpProtector();

        ConfigEncryptionHelper.Encrypt(config, protector);
        ConfigEncryptionHelper.Decrypt(config, protector);

        Assert.Equal(secret, config.Jira[0].ApiToken);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ProviderConfiguration ConfigWith(
        AzureDevOpsConnection? ado = null,
        JiraConnection? jira = null,
        GitHubConnection? github = null,
        TrelloConnection? trello = null,
        ImapFlaggedMailConnection? imap = null)
    {
        return new ProviderConfiguration
        {
            AzureDevOps = ado is null ? [] : [ado],
            Jira = jira is null ? [] : [jira],
            GitHub = github is null ? [] : [github],
            Trello = trello is null ? [] : [trello],
            ImapFlaggedMail = imap is null ? [] : [imap],
        };
    }
}
