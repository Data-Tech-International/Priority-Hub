using Bunit;
using Bunit.TestDoubles;
using PriorityHub.Ui.Components.Layout;

namespace PriorityHub.Ui.Tests.Components;

public class FooterTests : BunitContext
{
    [Fact]
    public void Footer_RendersVersionString()
    {
        var cut = Render<Footer>();

        var version = cut.Find(".app-footer__version");
        Assert.StartsWith("v", version.TextContent.Trim());
    }

    [Fact]
    public void Footer_FeedbackLinkPointsToGitHubIssues()
    {
        var cut = Render<Footer>();

        var feedbackLink = cut.FindAll("a.app-footer__link")
            .First(a => a.TextContent.Contains("Feedback"));
        Assert.Equal(
            "https://github.com/Data-Tech-International/Priority-Hub/issues",
            feedbackLink.GetAttribute("href"));
    }

    [Fact]
    public void Footer_FeedbackLinkOpensInNewTab()
    {
        var cut = Render<Footer>();

        var feedbackLink = cut.FindAll("a.app-footer__link")
            .First(a => a.TextContent.Contains("Feedback"));
        Assert.Equal("_blank", feedbackLink.GetAttribute("target"));
    }

    [Fact]
    public void Footer_RendersCopyrightText()
    {
        var cut = Render<Footer>();

        var footer = cut.Find(".app-footer");
        Assert.Contains("2026 Data Tech International", footer.TextContent);
    }

    [Fact]
    public void Footer_LicenseLinkPointsToLicenseFile()
    {
        var cut = Render<Footer>();

        var licenseLink = cut.FindAll("a.app-footer__link")
            .First(a => a.TextContent.Contains("MIT License"));
        Assert.Equal(
            "https://github.com/Data-Tech-International/Priority-Hub/blob/main/LICENSE",
            licenseLink.GetAttribute("href"));
    }

    [Fact]
    public void Footer_LicenseLinkOpensInNewTab()
    {
        var cut = Render<Footer>();

        var licenseLink = cut.FindAll("a.app-footer__link")
            .First(a => a.TextContent.Contains("MIT License"));
        Assert.Equal("_blank", licenseLink.GetAttribute("target"));
    }
}
