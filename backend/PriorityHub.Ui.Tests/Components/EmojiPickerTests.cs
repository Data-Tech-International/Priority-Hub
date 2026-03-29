using Bunit;
using Microsoft.JSInterop;
using PriorityHub.Ui.Components;

namespace PriorityHub.Ui.Tests.Components;

public class EmojiPickerTests : BunitContext
{
    public EmojiPickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void EmojiPicker_RendersCurrentEmoji()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🚀"));

        Assert.Contains("🚀", cut.Find(".emoji-trigger-icon").TextContent);
    }

    [Fact]
    public void EmojiPicker_PopupHidden_ByDefault()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        Assert.Empty(cut.FindAll(".emoji-popup"));
    }

    [Fact]
    public void EmojiPicker_PopupShown_AfterTriggerClick()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        Assert.NotEmpty(cut.FindAll(".emoji-popup"));
    }

    [Fact]
    public void EmojiPicker_PopupContainsSearchInput()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        Assert.NotEmpty(cut.FindAll(".emoji-search"));
    }

    [Fact]
    public void EmojiPicker_PopupContainsEmojiButtons()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        Assert.NotEmpty(cut.FindAll(".emoji-btn"));
    }

    [Fact]
    public void EmojiPicker_CurrentEmojiIsMarkedSelected()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        Assert.NotEmpty(cut.FindAll(".emoji-btn.is-selected"));
    }

    [Fact]
    public void EmojiPicker_ClosesPopup_AfterEmojiSelected()
    {
        string? received = null;
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷")
            .Add(x => x.OnEmojiSelected, (string e) => received = e));

        cut.Find(".emoji-trigger").Click();
        cut.Find(".emoji-btn").Click();

        Assert.Empty(cut.FindAll(".emoji-popup"));
    }

    [Fact]
    public void EmojiPicker_InvokesOnEmojiSelected_WhenEmojiClicked()
    {
        string? received = null;
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷")
            .Add(x => x.OnEmojiSelected, (string e) => received = e));

        cut.Find(".emoji-trigger").Click();
        cut.Find(".emoji-btn").Click();

        Assert.NotNull(received);
        Assert.False(string.IsNullOrEmpty(received));
    }

    [Fact]
    public void EmojiPicker_SearchFiltersEmoji()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        var searchInput = cut.Find(".emoji-search");
        searchInput.Input("rocket");

        // After searching for "rocket", should find the rocket emoji
        var btns = cut.FindAll(".emoji-btn");
        Assert.Contains(btns, b => b.TextContent.Contains("🚀"));
    }

    [Fact]
    public void EmojiPicker_SearchNoResults_ShowsEmptyMessage()
    {
        var cut = Render<EmojiPicker>(p => p
            .Add(x => x.Value, "🔷"));

        cut.Find(".emoji-trigger").Click();

        var searchInput = cut.Find(".emoji-search");
        searchInput.Input("zzzyyynotfound");

        Assert.NotEmpty(cut.FindAll(".emoji-empty"));
    }
}
