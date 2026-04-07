using PriorityHub.Api.Models;

namespace PriorityHub.Api.Tests;

public sealed class ProviderConfigurationTests
{
    [Fact]
    public void HasAnyConnections_EmptyConfig_ReturnsFalse()
    {
        var config = new ProviderConfiguration();
        Assert.False(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithAzureDevOps_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            AzureDevOps = [new AzureDevOpsConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithGitHub_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            GitHub = [new GitHubConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithJira_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            Jira = [new JiraConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithTrello_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            Trello = [new TrelloConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithImapFlaggedMail_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            ImapFlaggedMail = [new ImapFlaggedMailConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithMicrosoftTasks_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            MicrosoftTasks = [new MicrosoftTasksConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }

    [Fact]
    public void HasAnyConnections_WithOutlookFlaggedMail_ReturnsTrue()
    {
        var config = new ProviderConfiguration
        {
            OutlookFlaggedMail = [new OutlookFlaggedMailConnection { Id = "test" }]
        };
        Assert.True(config.HasAnyConnections());
    }
}
