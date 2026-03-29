using Bunit;
using Microsoft.JSInterop;
using PriorityHub.Ui.Components;

namespace PriorityHub.Ui.Tests.Components;

public class ConnectorFilterTests : BunitContext
{
    public ConnectorFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ConnectorFilter_RendersToggleButton()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        var btn = cut.Find(".tag-filter-toggle");
        Assert.NotNull(btn);
    }

    [Fact]
    public void ConnectorFilter_ShowsAllConnectorsLabel_WhenNoneSelected()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        Assert.Contains("All connectors", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void ConnectorFilter_ShowsFormattedName_WhenOneSelected()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira" })
            .Add(x => x.SelectedConnectors, new[] { "github" }));

        Assert.Contains("GitHub", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void ConnectorFilter_ShowsCount_WhenMultipleSelected()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira", "trello" })
            .Add(x => x.SelectedConnectors, new[] { "github", "jira" }));

        Assert.Contains("Connectors (2)", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void ConnectorFilter_DropdownHidden_ByDefault()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        Assert.Empty(cut.FindAll(".tag-filter-dropdown"));
    }

    [Fact]
    public void ConnectorFilter_DropdownShown_AfterToggleClick()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.NotEmpty(cut.FindAll(".tag-filter-dropdown"));
    }

    [Fact]
    public void ConnectorFilter_ShowsAllAvailableConnectors_InDropdown()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira", "trello" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var options = cut.FindAll(".tag-filter-option");
        Assert.Equal(3, options.Count);
    }

    [Fact]
    public void ConnectorFilter_ShowsClearButton_WhenConnectorsSelected()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, new[] { "github" }));

        cut.Find(".tag-filter-toggle").Click();

        Assert.NotEmpty(cut.FindAll(".tag-filter-clear"));
    }

    [Fact]
    public void ConnectorFilter_NoClearButton_WhenNoConnectorsSelected()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.Empty(cut.FindAll(".tag-filter-clear"));
    }

    [Fact]
    public void ConnectorFilter_InvokesOnConnectorsChange_OnToggle()
    {
        List<string>? received = null;

        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>())
            .Add(x => x.OnConnectorsChange, (List<string> connectors) => received = connectors));

        cut.Find(".tag-filter-toggle").Click();
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes[0].Change(true);

        Assert.NotNull(received);
        Assert.Contains("github", received);
    }

    [Fact]
    public void ConnectorFilter_ClearSelection_InvokesOnConnectorsChangeWithEmpty()
    {
        List<string>? received = null;

        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, new[] { "github" })
            .Add(x => x.OnConnectorsChange, (List<string> connectors) => received = connectors));

        cut.Find(".tag-filter-toggle").Click();
        cut.Find(".tag-filter-clear").Click();

        Assert.NotNull(received);
        Assert.Empty(received);
    }

    [Fact]
    public void ConnectorFilter_ShowsEmptyMessage_WhenNoAvailableConnectors()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, Array.Empty<string>())
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.Contains("No connectors", cut.Find(".tag-filter-empty").TextContent);
    }

    [Fact]
    public void ConnectorFilter_ToggleButtonHasAriaExpandedFalse_WhenClosed()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        var btn = cut.Find(".tag-filter-toggle");
        Assert.Equal("false", btn.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ConnectorFilter_ToggleButtonHasAriaExpandedTrue_WhenOpen()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var btn = cut.Find(".tag-filter-toggle");
        Assert.Equal("true", btn.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ConnectorFilter_DropdownHasListboxRole()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var dropdown = cut.Find(".tag-filter-dropdown");
        Assert.Equal("listbox", dropdown.GetAttribute("role"));
    }

    [Fact]
    public void ConnectorFilter_OptionsHaveOptionRole()
    {
        var cut = Render<ConnectorFilter>(p => p
            .Add(x => x.AvailableConnectors, new[] { "github", "jira" })
            .Add(x => x.SelectedConnectors, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var options = cut.FindAll("[role='option']");
        Assert.Equal(2, options.Count);
    }
}
