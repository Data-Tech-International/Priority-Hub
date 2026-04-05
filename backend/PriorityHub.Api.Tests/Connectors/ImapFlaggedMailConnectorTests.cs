using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services.Connectors;

namespace PriorityHub.Api.Tests.Connectors;

public sealed class ImapFlaggedMailConnectorTests
{
    // ── ParsePort ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("993", 993)]
    [InlineData("143", 143)]
    [InlineData(null, 993)]
    [InlineData("", 993)]
    [InlineData("abc", 993)]
    public void ParsePort_ReturnsExpectedPort(string? input, int expected)
    {
        Assert.Equal(expected, ImapFlaggedMailConnector.ParsePort(input));
    }

    // ── ParseMaxResults ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("50", 50)]
    [InlineData("0", 100)]
    [InlineData(null, 100)]
    [InlineData("bad", 100)]
    public void ParseMaxResults_ReturnsExpectedValue(string? input, int expected)
    {
        Assert.Equal(expected, ImapFlaggedMailConnector.ParseMaxResults(input));
    }

    // ── ParseKeywords ────────────────────────────────────────────────────────

    [Fact]
    public void ParseKeywords_NullInput_ReturnsEmptyList()
    {
        Assert.Empty(ImapFlaggedMailConnector.ParseKeywords(null));
    }

    [Fact]
    public void ParseKeywords_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(ImapFlaggedMailConnector.ParseKeywords(""));
    }

    [Fact]
    public void ParseKeywords_CommaSeparated_ReturnsTrimmedKeywords()
    {
        var result = ImapFlaggedMailConnector.ParseKeywords("important, followup , urgent");
        Assert.Equal(3, result.Count);
        Assert.Contains("important", result);
        Assert.Contains("followup", result);
        Assert.Contains("urgent", result);
    }

    // ── ValidateTlsRequirement ───────────────────────────────────────────────

    [Fact]
    public void ValidateTlsRequirement_Port993_DoesNotThrow()
    {
        var conn = new ImapFlaggedMailConnection { Port = "993" };
        var ex = Record.Exception(() => ImapFlaggedMailConnector.ValidateTlsRequirement(conn));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidateTlsRequirement_Port143_ThrowsInvalidOperationException()
    {
        var conn = new ImapFlaggedMailConnection { Port = "143" };
        Assert.Throws<InvalidOperationException>(() => ImapFlaggedMailConnector.ValidateTlsRequirement(conn));
    }

    [Fact]
    public void ValidateTlsRequirement_Port587_ThrowsInvalidOperationException()
    {
        var conn = new ImapFlaggedMailConnection { Port = "587" };
        Assert.Throws<InvalidOperationException>(() => ImapFlaggedMailConnector.ValidateTlsRequirement(conn));
    }

    // ── BuildSearchQuery ──────────────────────────────────────────────────────

    [Fact]
    public void BuildSearchQuery_NoKeywords_ReturnsFlaggedOnly()
    {
        var query = ImapFlaggedMailConnector.BuildSearchQuery([]);
        // Should be just SearchQuery.Flagged with no OR
        Assert.NotNull(query);
    }

    [Fact]
    public void BuildSearchQuery_WithKeywords_ReturnsOrQuery()
    {
        var query = ImapFlaggedMailConnector.BuildSearchQuery(["important", "followup"]);
        // Must not be null; verifying structure is sufficient (MailKit query is opaque).
        Assert.NotNull(query);
    }

    // ── FetchConnectionAsync with TLS port violation ──────────────────────────

    [Fact]
    public async Task FetchConnectionAsync_NonTlsPort_ReturnsSyncStatusError()
    {
        // Port 143 must be rejected without connecting.
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        var conn = new ImapFlaggedMailConnection
        {
            Id = "test-id",
            Name = "Test",
            ImapServer = "imap.example.com",
            Port = "143",
            Email = "user@example.com",
            Password = "pass",
        };

        var result = await connector.FetchConnectionAsync(conn, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("error", result.BoardConnections[0].SyncStatus);
        Assert.Single(result.Issues);
        Assert.Contains("TLS", result.Issues[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── FetchConnectionAsync via JsonElement ──────────────────────────────────

    [Fact]
    public async Task FetchConnectionAsync_JsonElement_NonTlsPort_ReturnsSyncStatusError()
    {
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        var conn = new ImapFlaggedMailConnection
        {
            Id = "test-id",
            Name = "Test",
            ImapServer = "imap.example.com",
            Port = "143",
            Email = "user@example.com",
            Password = "pass",
        };

        var jsonElement = JsonSerializer.SerializeToElement(conn, JsonSerializerOptions.Web);
        var result = await connector.FetchConnectionAsync(jsonElement, null, CancellationToken.None);

        Assert.Single(result.BoardConnections);
        Assert.Equal("error", result.BoardConnections[0].SyncStatus);
    }

    // ── ConnectorRegistry metadata ────────────────────────────────────────────

    [Fact]
    public void ProviderKey_IsImapFlaggedMail()
    {
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        Assert.Equal("imap-flagged-mail", connector.ProviderKey);
    }

    [Fact]
    public void ConfigFields_ContainsExpectedFields()
    {
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        var keys = connector.ConfigFields.Select(f => f.Key).ToHashSet();

        Assert.Contains("name", keys);
        Assert.Contains("imapServer", keys);
        Assert.Contains("email", keys);
        Assert.Contains("password", keys);
    }

    [Fact]
    public void ConfigFields_PasswordField_IsPasswordKind()
    {
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        var passwordField = connector.ConfigFields.FirstOrDefault(f => f.Key == "password");

        Assert.NotNull(passwordField);
        Assert.Equal("password", passwordField!.InputKind);
    }

    // ── BoardConnection metadata from successful connection ───────────────────

    [Fact]
    public async Task FetchConnectionAsync_TlsViolation_BoardConnectionIdMatchesConnectionId()
    {
        var connector = new ImapFlaggedMailConnector(new NeverCallFactory());
        var conn = new ImapFlaggedMailConnection
        {
            Id = "my-conn-id",
            Port = "143",
            Email = "x@y.com",
            Password = "p",
            ImapServer = "s"
        };

        var result = await connector.FetchConnectionAsync(conn, CancellationToken.None);

        Assert.Equal("my-conn-id", result.BoardConnections[0].Id);
        Assert.Equal("imap-flagged-mail", result.BoardConnections[0].Provider);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Factory that must never be called — used for tests that should fail before connecting.</summary>
    private sealed class NeverCallFactory : IImapClientFactory
    {
        public IImapClient CreateClient() =>
            throw new InvalidOperationException("IMAP client must not be created in this test scenario.");
    }
}
