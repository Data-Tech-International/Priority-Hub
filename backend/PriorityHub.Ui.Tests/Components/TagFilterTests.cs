using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PriorityHub.Ui.Components;

namespace PriorityHub.Ui.Tests.Components;

public class TagFilterTests : BunitContext
{
    public TagFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void TagFilter_RendersToggleButton()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        var btn = cut.Find(".tag-filter-toggle");
        Assert.NotNull(btn);
    }

    [Fact]
    public void TagFilter_ShowsAllTagsLabel_WhenNoneSelected()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        Assert.Contains("All tags", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void TagFilter_ShowsTagName_WhenOneSelected()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature" })
            .Add(x => x.SelectedTags, new[] { "bug" }));

        Assert.Contains("bug", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void TagFilter_ShowsCount_WhenMultipleSelected()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature", "docs" })
            .Add(x => x.SelectedTags, new[] { "bug", "feature" }));

        Assert.Contains("Tags (2)", cut.Find(".tag-filter-toggle").TextContent);
    }

    [Fact]
    public void TagFilter_DropdownHidden_ByDefault()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        Assert.Empty(cut.FindAll(".tag-filter-dropdown"));
    }

    [Fact]
    public void TagFilter_DropdownShown_AfterToggleClick()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.NotEmpty(cut.FindAll(".tag-filter-dropdown"));
    }

    [Fact]
    public void TagFilter_ShowsAllAvailableTags_InDropdown()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature", "docs" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var options = cut.FindAll(".tag-filter-option");
        Assert.Equal(3, options.Count);
    }

    [Fact]
    public void TagFilter_ShowsClearButton_WhenTagsSelected()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, new[] { "bug" }));

        cut.Find(".tag-filter-toggle").Click();

        Assert.NotEmpty(cut.FindAll(".tag-filter-clear"));
    }

    [Fact]
    public void TagFilter_NoClearButton_WhenNoTagsSelected()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.Empty(cut.FindAll(".tag-filter-clear"));
    }

    [Fact]
    public void TagFilter_InvokesOnTagsChange_OnToggle()
    {
        List<string>? received = null;

        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature" })
            .Add(x => x.SelectedTags, Array.Empty<string>())
            .Add(x => x.OnTagsChange, (List<string> tags) => received = tags));

        cut.Find(".tag-filter-toggle").Click();
        var checkboxes = cut.FindAll("input[type='checkbox']");
        checkboxes[0].Change(true);

        Assert.NotNull(received);
        Assert.Contains("bug", received);
    }

    [Fact]
    public void TagFilter_ClearSelection_InvokesOnTagsChangeWithEmpty()
    {
        List<string>? received = null;

        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, new[] { "bug" })
            .Add(x => x.OnTagsChange, (List<string> tags) => received = tags));

        cut.Find(".tag-filter-toggle").Click();
        cut.Find(".tag-filter-clear").Click();

        Assert.NotNull(received);
        Assert.Empty(received);
    }

    [Fact]
    public void TagFilter_ShowsEmptyMessage_WhenNoAvailableTags()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, Array.Empty<string>())
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        Assert.Contains("No tags", cut.Find(".tag-filter-empty").TextContent);
    }

    [Fact]
    public void TagFilter_ToggleButtonHasAriaExpandedFalse_WhenClosed()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        var btn = cut.Find(".tag-filter-toggle");
        Assert.Equal("false", btn.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TagFilter_ToggleButtonHasAriaExpandedTrue_WhenOpen()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var btn = cut.Find(".tag-filter-toggle");
        Assert.Equal("true", btn.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TagFilter_DropdownHasListboxRole()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var dropdown = cut.Find(".tag-filter-dropdown");
        Assert.Equal("listbox", dropdown.GetAttribute("role"));
    }

    [Fact]
    public void TagFilter_OptionsHaveOptionRole()
    {
        var cut = Render<TagFilter>(p => p
            .Add(x => x.AvailableTags, new[] { "bug", "feature" })
            .Add(x => x.SelectedTags, Array.Empty<string>()));

        cut.Find(".tag-filter-toggle").Click();

        var options = cut.FindAll("[role='option']");
        Assert.Equal(2, options.Count);
    }
}
