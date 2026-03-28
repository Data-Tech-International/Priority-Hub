using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

public sealed class OauthTokenServiceTests
{
    [Fact]
    public async Task MicrosoftProvider_RefreshesGraphToken_AndPersistsUpdatedTokens()
    {
        var configuration = BuildConfiguration();
                var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
                        JsonResponse("""
                        {
                            "access_token": "graph-new-access",
                            "refresh_token": "graph-new-refresh",
                            "expires_in": 3600
                        }
                        """),
                        JsonResponse("""
                        {
                            "access_token": "ado-access"
                        }
                        """)
                ])));

        var tokenService = new OauthTokenService(configuration, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));
        var httpContext = CreateHttpContext(
            provider: "microsoft",
            accessToken: "stale-access",
            refreshToken: "stale-refresh",
            out var authService);

        var tokens = await tokenService.GetTokensByProviderAsync(httpContext, CancellationToken.None);

        Assert.Equal("graph-new-access", tokens["microsoft-tasks"]);
        Assert.Equal("graph-new-access", tokens["outlook-flagged-mail"]);
        Assert.Equal("ado-access", tokens["azure-devops"]);

        Assert.NotNull(authService.LastSignInProperties);
        Assert.Equal("graph-new-access", authService.LastSignInProperties!.GetTokenValue("access_token"));
        Assert.Equal("graph-new-refresh", authService.LastSignInProperties.GetTokenValue("refresh_token"));
        Assert.False(string.IsNullOrWhiteSpace(authService.LastSignInProperties.GetTokenValue("expires_at")));
    }

    [Fact]
    public async Task MicrosoftProvider_WhenGraphRefreshFails_FallsBackToCurrentAccessToken()
    {
        var configuration = BuildConfiguration();
        var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
            ErrorResponse(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}"""),
            JsonResponse("""{"access_token":"ado-access"}""")
        ])));

        var tokenService = new OauthTokenService(configuration, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));
        var httpContext = CreateHttpContext(
            provider: "microsoft",
            accessToken: "current-access",
            refreshToken: "refresh-token",
            out var authService);

        var tokens = await tokenService.GetTokensByProviderAsync(httpContext, CancellationToken.None);

        Assert.Equal("current-access", tokens["microsoft-tasks"]);
        Assert.Equal("current-access", tokens["outlook-flagged-mail"]);
        Assert.Equal("ado-access", tokens["azure-devops"]);
        Assert.Null(authService.LastSignInProperties);
    }

    [Fact]
    public async Task GitHubProvider_ReturnsGitHubToken_WithoutRefreshExchange()
    {
        var configuration = BuildConfiguration();
        var handler = new CountingHandler();
        var tokenService = new OauthTokenService(
            configuration,
            NullLogger<OauthTokenService>.Instance,
            new StaticHttpClientFactory(new HttpClient(handler)));

        var httpContext = CreateHttpContext(
            provider: "github",
            accessToken: "github-access",
            refreshToken: null,
            out _);

        var tokens = await tokenService.GetTokensByProviderAsync(httpContext, CancellationToken.None);

        Assert.Equal("github-access", tokens["github"]);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task MicrosoftProvider_WhenResponseHasStarted_DoesNotPersistTokensAndStillReturnsRefreshedAccessTokens()
    {
        var configuration = BuildConfiguration();
        var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
            JsonResponse("""
            {
                "access_token": "graph-new-access",
                "refresh_token": "graph-new-refresh",
                "expires_in": 3600
            }
            """),
            JsonResponse("""
            {
                "access_token": "ado-access"
            }
            """)
        ])));

        var tokenService = new OauthTokenService(configuration, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));
        var httpContext = CreateHttpContext(
            provider: "microsoft",
            accessToken: "stale-access",
            refreshToken: "stale-refresh",
            out var authService);
        httpContext.Features.Set<IHttpResponseFeature>(new StartedHttpResponseFeature());

        var tokens = await tokenService.GetTokensByProviderAsync(httpContext, CancellationToken.None);

        Assert.Equal("graph-new-access", tokens["microsoft-tasks"]);
        Assert.Equal("graph-new-access", tokens["outlook-flagged-mail"]);
        Assert.Equal("ado-access", tokens["azure-devops"]);
        Assert.Null(authService.LastSignInProperties);
    }

    [Fact]
    public async Task MicrosoftProvider_WhenPrimaryAzureDevOpsScopeFails_UsesFallbackScope()
    {
        var configuration = BuildConfiguration();
        var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
            JsonResponse("""
            {
                "access_token": "graph-new-access",
                "refresh_token": "graph-new-refresh",
                "expires_in": 3600
            }
            """),
            ErrorResponse(HttpStatusCode.BadRequest, """{"error":"invalid_scope"}"""),
            JsonResponse("""{"access_token":"ado-access"}""")
        ])));

        var tokenService = new OauthTokenService(configuration, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));
        var httpContext = CreateHttpContext(
            provider: "microsoft",
            accessToken: "stale-access",
            refreshToken: "stale-refresh",
            out _);

        var tokens = await tokenService.GetTokensByProviderAsync(httpContext, CancellationToken.None);

        Assert.Equal("graph-new-access", tokens["microsoft-tasks"]);
        Assert.Equal("ado-access", tokens["azure-devops"]);
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Microsoft:ClientId"] = "client-id",
            ["Authentication:Microsoft:ClientSecret"] = "client-secret",
            ["Authentication:Microsoft:TenantId"] = "common"
        }).Build();

    private static DefaultHttpContext CreateHttpContext(
        string provider,
        string accessToken,
        string? refreshToken,
        out StubAuthenticationService authService)
    {
        var claims = new List<Claim> { new("provider", provider) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var properties = new AuthenticationProperties();
        var authTokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = accessToken }
        };

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            authTokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }

        properties.StoreTokens(authTokens);

        var ticket = new AuthenticationTicket(
            principal,
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme);

        authService = new StubAuthenticationService(ticket);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService>(authService);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = principal
        };

        return httpContext;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}

internal sealed class StaticHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => httpClient;
}

internal sealed class QueueMessageHandler(Queue<HttpResponseMessage> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (responses.Count == 0)
        {
            throw new InvalidOperationException("No queued response available.");
        }

        return Task.FromResult(responses.Dequeue());
    }
}

internal sealed class CountingHandler : HttpMessageHandler
{
    public int CallCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount += 1;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }
}

internal sealed class StubAuthenticationService(AuthenticationTicket ticket) : IAuthenticationService
{
    public AuthenticationProperties? LastSignInProperties { get; private set; }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        throw new NotImplementedException();
    }

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        throw new NotImplementedException();
    }

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        LastSignInProperties = properties;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        throw new NotImplementedException();
    }
}

internal sealed class StartedHttpResponseFeature : IHttpResponseFeature
{
    public int StatusCode { get; set; }

    public string? ReasonPhrase { get; set; }

    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

    public Stream Body { get; set; } = Stream.Null;

    public bool HasStarted => true;

    public void OnCompleted(Func<object, Task> callback, object state)
    {
    }

    public void OnStarting(Func<object, Task> callback, object state)
    {
    }
}
