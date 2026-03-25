using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PriorityHub.Ui.Services;

namespace PriorityHub.Ui.Tests.Services;

public sealed class ServerAuthStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsUserFromHttpContext()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var provider = new ServerAuthStateProvider(accessor);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("Alice", state.User.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAnonymous_WhenHttpContextIsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };

        var provider = new ServerAuthStateProvider(accessor);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated ?? false);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAuthenticatedUser_WhenAuthenticated()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "test@example.com")],
            "oauth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        var provider = new ServerAuthStateProvider(accessor);
        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("test@example.com", state.User.FindFirst(ClaimTypes.Email)?.Value);
    }
}
