using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using PriorityHub.Ui.Components;
using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Components;

public class NavBarTests : BunitContext
{
    public NavBarTests()
    {
        Services.AddSingleton<WorkItemRanker>();
    }

    [Fact]
    public void NavBar_RendersDashboardAndSettingsLinks()
    {
        this.AddAuthorization().SetNotAuthorized();

        var cut = Render<NavBar>();

        var links = cut.FindAll("a.nav-link");
        var texts = links.Select(l => l.TextContent.Trim()).ToList();
        Assert.Contains("Dashboard", texts);
        Assert.Contains("Settings", texts);
    }

    [Fact]
    public void NavBar_DashboardLinkPointsToRoot()
    {
        this.AddAuthorization().SetNotAuthorized();

        var cut = Render<NavBar>();

        var dashLink = cut.FindAll("a.nav-link").First(l => l.TextContent.Contains("Dashboard"));
        Assert.Equal("/", dashLink.GetAttribute("href"));
    }

    [Fact]
    public void NavBar_SettingsLinkPointsToSettings()
    {
        this.AddAuthorization().SetNotAuthorized();

        var cut = Render<NavBar>();

        var settingsLink = cut.FindAll("a.nav-link").First(l => l.TextContent.Contains("Settings"));
        Assert.Equal("/settings", settingsLink.GetAttribute("href"));
    }

    [Fact]
    public void NavBar_ShowsSignOutButton_WhenAuthorized()
    {
        this.AddAuthorization().SetAuthorized("testuser");

        var cut = Render<NavBar>();

        var signoutBtn = cut.Find(".signout-button");
        Assert.NotNull(signoutBtn);
    }

    [Fact]
    public void NavBar_SignOutButtonHasAriaLabel()
    {
        this.AddAuthorization().SetAuthorized("testuser");

        var cut = Render<NavBar>();

        var signoutBtn = cut.Find(".signout-button");
        Assert.Equal("Sign out", signoutBtn.GetAttribute("aria-label"));
    }

    [Fact]
    public void NavBar_DoesNotShowSignOutButton_WhenNotAuthorized()
    {
        this.AddAuthorization().SetNotAuthorized();

        var cut = Render<NavBar>();

        Assert.Empty(cut.FindAll(".signout-button"));
    }
}
