using Bunit;
using Microsoft.JSInterop;
using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Services;

public class PassphraseCacheInteropTests : BunitContext
{
    public PassphraseCacheInteropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
    }

    private PassphraseCacheInterop CreateSut() =>
        new PassphraseCacheInterop(JSInterop.JSRuntime);

    [Fact]
    public async Task StorePassphraseAsync_InvokesWrapAndStore()
    {
        JSInterop.SetupVoid("PriorityHub.passphraseCache.wrapAndStore",
            invocation => invocation.Arguments[0] is "secret123").SetVoidResult();

        var sut = CreateSut();
        await sut.StorePassphraseAsync("secret123");

        JSInterop.VerifyInvoke("PriorityHub.passphraseCache.wrapAndStore");
    }

    [Fact]
    public async Task LoadPassphraseAsync_ReturnsCachedPassphrase()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult("mypassphrase");

        var sut = CreateSut();
        var result = await sut.LoadPassphraseAsync();

        Assert.Equal("mypassphrase", result);
    }

    [Fact]
    public async Task LoadPassphraseAsync_ReturnsNull_WhenNoCacheEntry()
    {
        JSInterop.Setup<string?>("PriorityHub.passphraseCache.loadAndUnwrap").SetResult(null);

        var sut = CreateSut();
        var result = await sut.LoadPassphraseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearPassphraseAsync_InvokesClearCache()
    {
        JSInterop.SetupVoid("PriorityHub.passphraseCache.clearCache").SetVoidResult();

        var sut = CreateSut();
        await sut.ClearPassphraseAsync();

        JSInterop.VerifyInvoke("PriorityHub.passphraseCache.clearCache");
    }

    [Fact]
    public async Task HasValidCacheAsync_ReturnsTrue_WhenCacheActive()
    {
        JSInterop.Setup<bool>("PriorityHub.passphraseCache.hasValidCache").SetResult(true);

        var sut = CreateSut();
        var result = await sut.HasValidCacheAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task HasValidCacheAsync_ReturnsFalse_WhenNoCacheOrExpired()
    {
        JSInterop.Setup<bool>("PriorityHub.passphraseCache.hasValidCache").SetResult(false);

        var sut = CreateSut();
        var result = await sut.HasValidCacheAsync();

        Assert.False(result);
    }
}
