using MailKit.Net.Imap;

namespace PriorityHub.Api.Services.Connectors;

/// <summary>
/// Creates <see cref="IImapClient"/> instances for use by <see cref="ImapFlaggedMailConnector"/>.
/// Abstracted to enable test doubles without requiring a live IMAP server.
/// </summary>
public interface IImapClientFactory
{
    /// <summary>Returns a new (not yet connected) <see cref="IImapClient"/> instance.</summary>
    IImapClient CreateClient();
}

/// <summary>Production implementation that creates real <see cref="ImapClient"/> instances.</summary>
public sealed class ImapClientFactory : IImapClientFactory
{
    public IImapClient CreateClient() => new ImapClient();
}
