using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Services;

public class HelpContentTests
{
    [Fact]
    public void Entries_ContainsAllExpectedKeys()
    {
        var expectedKeys = new[]
        {
            "dashboard.overview",
            "dashboard.filters",
            "settings.connectors.azure-devops",
            "settings.connectors.jira",
            "settings.connectors.trello",
            "settings.connectors.github",
            "settings.connectors.microsoft-tasks",
            "settings.connectors.outlook-flagged-mail",
            "settings.export"
        };

        foreach (var key in expectedKeys)
        {
            Assert.True(HelpContent.Entries.ContainsKey(key), $"Missing key: {key}");
        }
    }

    [Fact]
    public void Entries_AllHaveNonEmptyTitleAndBody()
    {
        foreach (var (key, entry) in HelpContent.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Title), $"Empty title for {key}");
            Assert.NotEmpty(entry.Body);
            Assert.All(entry.Body, line => Assert.False(string.IsNullOrWhiteSpace(line)));
        }
    }

    [Fact]
    public void Entries_LookupIsCaseInsensitive()
    {
        Assert.True(HelpContent.Entries.ContainsKey("DASHBOARD.OVERVIEW"));
        Assert.True(HelpContent.Entries.ContainsKey("Dashboard.Overview"));
    }
}
