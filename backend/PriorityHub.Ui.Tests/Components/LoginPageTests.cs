using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PriorityHub.Ui.Components.Pages;

namespace PriorityHub.Ui.Tests.Components;

public class LoginPageTests : BunitContext
{
    [Fact]
    public void LoginPage_RendersSignInButtons()
    {
        var cut = Render<LoginPage>();

        var microsoftButton = cut.Find("a.microsoft");
        Assert.Contains("Sign in with Microsoft", microsoftButton.TextContent);

        var githubButton = cut.Find("a.github");
        Assert.Contains("Sign in with GitHub", githubButton.TextContent);
    }

    [Fact]
    public void LoginPage_RendersLoginHeading()
    {
        var cut = Render<LoginPage>();

        var h1 = cut.Find("h1");
        Assert.Contains("Sign in to access your unified priority dashboard", h1.TextContent);
    }

    [Fact]
    public void LoginPage_ShowsErrorBanner_WhenErrorParam()
    {
        var navManager = Services.GetRequiredService<NavigationManager>();
        navManager.NavigateTo("/login?error=Auth+failed");

        var cut = Render<LoginPage>();

        var banner = cut.Find(".status-banner.status-error");
        Assert.Contains("Auth failed", banner.TextContent);
    }

    [Fact]
    public void LoginPage_NoErrorBanner_WhenNoError()
    {
        var cut = Render<LoginPage>();

        Assert.Empty(cut.FindAll(".status-banner.status-error"));
    }

    [Fact]
    public void LoginPage_LinksPointToAuthEndpoints()
    {
        var cut = Render<LoginPage>();

        var msLink = cut.Find("a.microsoft");
        Assert.Equal("/api/auth/login/microsoft", msLink.GetAttribute("href"));

        var ghLink = cut.Find("a.github");
        Assert.Equal("/api/auth/login/github", ghLink.GetAttribute("href"));
    }
}
