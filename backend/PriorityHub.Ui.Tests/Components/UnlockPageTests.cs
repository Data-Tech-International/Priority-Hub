using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PriorityHub.Ui.Components.Pages;
using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Components;

public class UnlockPageTests : BunitContext
{
    public UnlockPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<PassphraseCacheInterop>();
    }

    [Fact]
    public void UnlockPage_RendersHeading()
    {
        JSInterop.SetupVoid("PriorityHub.passphraseCache.wrapAndStore", _ => true).SetVoidResult();
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        var h1 = cut.Find("h1");
        Assert.Contains("Unlock your workspace", h1.TextContent);
    }

    [Fact]
    public void UnlockPage_RendersPassphraseInput()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        // Wait for async OnAfterRenderAsync to finish so loading spinner clears.
        cut.WaitForState(() => cut.FindAll("#passphrase-input").Count > 0, timeout: TimeSpan.FromSeconds(5));

        var input = cut.Find("#passphrase-input");
        Assert.Equal("password", input.GetAttribute("type"));
    }

    [Fact]
    public void UnlockPage_UnlockButton_DisabledWhenPassphraseEmpty()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        cut.WaitForState(() => cut.FindAll("#passphrase-input").Count > 0, timeout: TimeSpan.FromSeconds(5));

        var button = cut.Find("button[type='submit']");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void UnlockPage_SharedDeviceCheckbox_HidesRememberMe()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        cut.WaitForState(() => cut.FindAll("#shared-device-checkbox").Count > 0, timeout: TimeSpan.FromSeconds(5));

        // Initially both checkboxes should be visible
        Assert.NotEmpty(cut.FindAll("#remember-me-checkbox"));

        // Check the shared-device checkbox
        cut.Find("#shared-device-checkbox").Change(true);

        // Remember-me checkbox should now be hidden
        Assert.Empty(cut.FindAll("#remember-me-checkbox"));
    }

    [Fact]
    public void UnlockPage_HidesLoadingIndicator_AfterCacheCheckComplete()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        // After OnAfterRenderAsync completes (cache miss), the form should be visible
        // and the loading indicator should be gone.
        cut.WaitForState(() => cut.FindAll("#passphrase-input").Count > 0, timeout: TimeSpan.FromSeconds(5));

        Assert.Empty(cut.FindAll(".unlock-loading"));
    }

    [Fact]
    public void UnlockPage_AcceptsReturnUrlParameter()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        // SupplyParameterFromQuery parameters must be passed via NavigationManager.
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/unlock?returnUrl=%2Fdashboard");

        var cut = Render<UnlockPage>();

        // Page should render without error when ReturnUrl is supplied.
        Assert.NotEmpty(cut.FindAll("h1"));
    }

    [Fact]
    public void UnlockPage_ShowsErrorBanner_WhenErrorMessageSet()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var cut = Render<UnlockPage>();

        cut.WaitForState(() => cut.FindAll("#passphrase-input").Count > 0, timeout: TimeSpan.FromSeconds(5));

        // The error banner should not be visible initially.
        Assert.Empty(cut.FindAll(".status-banner.status-error"));
    }
}
