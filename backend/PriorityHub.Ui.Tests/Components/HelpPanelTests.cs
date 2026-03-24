using Bunit;
using PriorityHub.Ui.Components;

namespace PriorityHub.Ui.Tests.Components;

public class HelpPanelTests : BunitContext
{
    [Fact]
    public void HelpPanel_RendersToggleButton_WithTitle()
    {
        var cut = Render<HelpPanel>(parameters => parameters
            .Add(p => p.ContextKey, "test.key")
            .Add(p => p.Title, "Test Help Title")
            .AddChildContent("<p>Help content here</p>"));

        var toggle = cut.Find(".help-toggle");
        Assert.Contains("Test Help Title", toggle.TextContent);
    }

    [Fact]
    public void HelpPanel_StartsCollapsed_ContentNotVisible()
    {
        var cut = Render<HelpPanel>(parameters => parameters
            .Add(p => p.ContextKey, "test.key")
            .Add(p => p.Title, "Test Title")
            .AddChildContent("<p>Hidden content</p>"));

        Assert.Empty(cut.FindAll(".help-content"));
    }

    [Fact]
    public void HelpPanel_TogglesOpen_OnClick()
    {
        var cut = Render<HelpPanel>(parameters => parameters
            .Add(p => p.ContextKey, "test.key")
            .Add(p => p.Title, "Test Title")
            .AddChildContent("<p>Now visible</p>"));

        cut.Find(".help-toggle").Click();

        var content = cut.Find(".help-content");
        Assert.Contains("Now visible", content.InnerHtml);
    }

    [Fact]
    public void HelpPanel_ShowsDownChevron_WhenClosed()
    {
        var cut = Render<HelpPanel>(parameters => parameters
            .Add(p => p.ContextKey, "test.key")
            .Add(p => p.Title, "Test Title")
            .AddChildContent("<p>Content</p>"));

        var chevron = cut.Find(".help-chevron");
        Assert.Contains("▼", chevron.TextContent);
    }

    [Fact]
    public void HelpPanel_ShowsUpChevron_WhenOpen()
    {
        var cut = Render<HelpPanel>(parameters => parameters
            .Add(p => p.ContextKey, "test.key")
            .Add(p => p.Title, "Test Title")
            .AddChildContent("<p>Content</p>"));

        cut.Find(".help-toggle").Click();

        var chevron = cut.Find(".help-chevron");
        Assert.Contains("▲", chevron.TextContent);
    }
}
