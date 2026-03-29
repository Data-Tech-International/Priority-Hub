using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Services;

public class EmojiDataTests
{
    [Theory]
    [InlineData("🔷", true)]
    [InlineData("🐙", true)]
    [InlineData("✅", true)]
    [InlineData("📧", true)]
    [InlineData("🚀", true)]
    [InlineData("❤️", true)]       // multi code point emoji (base + variation selector)
    [InlineData("abc", false)]
    [InlineData("ab", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmoji_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, EmojiData.IsValidEmoji(input));
    }

    [Fact]
    public void IsValidEmoji_MultipleChars_ReturnsFalse()
    {
        Assert.False(EmojiData.IsValidEmoji("hello world"));
    }

    [Fact]
    public void Categories_ContainsConnectorsCategory()
    {
        Assert.Contains(EmojiData.Categories, c => c.Name == "Connectors");
    }

    [Fact]
    public void Categories_ConnectorsContainsAllDefaults()
    {
        var connectors = EmojiData.Categories.First(c => c.Name == "Connectors");
        var emojis = connectors.Emojis.Select(e => e.Emoji).ToList();

        Assert.Contains("🔷", emojis);
        Assert.Contains("🐙", emojis);
        Assert.Contains("📋", emojis);
        Assert.Contains("📌", emojis);
        Assert.Contains("✅", emojis);
        Assert.Contains("📧", emojis);
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllCategories()
    {
        var result = EmojiData.Search(string.Empty);
        Assert.Equal(EmojiData.Categories.Count, result.Count);
    }

    [Fact]
    public void Search_WhitespaceQuery_ReturnsAllCategories()
    {
        var result = EmojiData.Search("   ");
        Assert.Equal(EmojiData.Categories.Count, result.Count);
    }

    [Fact]
    public void Search_MatchesName()
    {
        var result = EmojiData.Search("rocket");
        Assert.Contains(result, c => c.Emojis.Any(e => e.Emoji == "🚀"));
    }

    [Fact]
    public void Search_MatchesKeyword()
    {
        var result = EmojiData.Search("github");
        Assert.Contains(result, c => c.Emojis.Any(e => e.Emoji == "🐙"));
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var result = EmojiData.Search("zzzyyyxxxnotfound");
        Assert.Empty(result);
    }

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        var lower = EmojiData.Search("rocket");
        var upper = EmojiData.Search("ROCKET");

        Assert.Equal(lower.Count, upper.Count);
    }

    [Fact]
    public void Search_ReturnsOnlyMatchingEmojisWithinCategory()
    {
        var result = EmojiData.Search("rocket");
        foreach (var category in result)
        {
            foreach (var entry in category.Emojis)
            {
                var matchesName = entry.Name.Contains("rocket", StringComparison.OrdinalIgnoreCase);
                var matchesKeyword = entry.Keywords.Any(k => k.Contains("rocket", StringComparison.OrdinalIgnoreCase));
                var matchesEmoji = entry.Emoji.Contains("rocket", StringComparison.OrdinalIgnoreCase);
                Assert.True(matchesName || matchesKeyword || matchesEmoji);
            }
        }
    }
}
