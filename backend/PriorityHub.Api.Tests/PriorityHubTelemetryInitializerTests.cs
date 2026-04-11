using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using PriorityHub.Api.Services.Telemetry;
using System.Security.Claims;

namespace PriorityHub.Api.Tests;

public sealed class PriorityHubTelemetryInitializerTests
{
    [Fact]
    public void Initialize_AuthenticatedUserWithEmail_SetsHashedAuthenticatedUserId()
    {
        var email = "user@example.com";
        var expectedHash = UserIdentityHasher.Hash(email);
        var initializer = CreateInitializer(CreateAuthenticatedUser(email: email));
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Equal(expectedHash, telemetry.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Initialize_AuthenticatedUserWithEmail_DoesNotExposeRawEmail()
    {
        var email = "user@example.com";
        var initializer = CreateInitializer(CreateAuthenticatedUser(email: email));
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.NotEqual(email, telemetry.Context.User.AuthenticatedUserId);
        Assert.DoesNotContain(email, telemetry.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Initialize_WithProviderClaim_SetsAuthProviderProperty()
    {
        var initializer = CreateInitializer(CreateAuthenticatedUser(email: "user@example.com", provider: "google"));
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Equal("google", telemetry.Properties["authProvider"]);
    }

    [Fact]
    public void Initialize_WithoutProviderClaim_DoesNotSetAuthProviderProperty()
    {
        var initializer = CreateInitializer(CreateAuthenticatedUser(email: "user@example.com", provider: null));
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.False(telemetry.Properties.ContainsKey("authProvider"));
    }

    [Fact]
    public void Initialize_UnauthenticatedUser_DoesNotSetAuthenticatedUserId()
    {
        var identity = new ClaimsIdentity(); // not authenticated
        var user = new ClaimsPrincipal(identity);
        var initializer = CreateInitializer(user);
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Null(telemetry.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Initialize_NullHttpContext_DoesNotSetAuthenticatedUserId()
    {
        var accessor = new StubHttpContextAccessor(null);
        var initializer = new PriorityHubTelemetryInitializer(accessor);
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Null(telemetry.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Initialize_NoEmailClaim_UsesFallbackSubProvider()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "sub-123"),
            new("provider", "github")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var expectedHash = UserIdentityHasher.Hash("sub-123_github");

        var initializer = CreateInitializer(user);
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Equal(expectedHash, telemetry.Context.User.AuthenticatedUserId);
    }

    [Fact]
    public void Initialize_NoEmailNoSubNoClaims_UsesUnknownFallback()
    {
        var identity = new ClaimsIdentity([], "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var expectedHash = UserIdentityHasher.Hash("unknown_unknown");

        var initializer = CreateInitializer(user);
        var telemetry = new RequestTelemetry();

        initializer.Initialize(telemetry);

        Assert.Equal(expectedHash, telemetry.Context.User.AuthenticatedUserId);
    }

    private static PriorityHubTelemetryInitializer CreateInitializer(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var accessor = new StubHttpContextAccessor(httpContext);
        return new PriorityHubTelemetryInitializer(accessor);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string email, string? provider = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };

        if (provider is not null)
        {
            claims.Add(new Claim("provider", provider));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private sealed class StubHttpContextAccessor(HttpContext? httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
