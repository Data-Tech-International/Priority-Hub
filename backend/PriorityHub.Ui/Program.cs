using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Components.Authorization;
using PriorityHub.Api.Extensions;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;
using PriorityHub.Api.Services.Connectors;
using PriorityHub.Ui.Components;
using PriorityHub.Ui.Services;

var builder = WebApplication.CreateBuilder(args);

const string MicrosoftScheme = "Microsoft";
const string GitHubScheme = "GitHub";

// ── Blazor Server ──
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Backend services (shared with API) ──
builder.Services.AddConfigStore(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AzureDevOpsConnector>();
builder.Services.AddHttpClient<GitHubIssuesConnector>();
builder.Services.AddHttpClient<JiraConnector>();
builder.Services.AddHttpClient<MicrosoftTasksConnector>();
builder.Services.AddHttpClient<OutlookFlaggedMailConnector>();
builder.Services.AddHttpClient<TrelloConnector>();
builder.Services.AddSingleton<ConnectorRegistry>(sp => new ConnectorRegistry([
    sp.GetRequiredService<AzureDevOpsConnector>(),
    sp.GetRequiredService<GitHubIssuesConnector>(),
    sp.GetRequiredService<JiraConnector>(),
    sp.GetRequiredService<MicrosoftTasksConnector>(),
    sp.GetRequiredService<OutlookFlaggedMailConnector>(),
    sp.GetRequiredService<TrelloConnector>(),
]));
builder.Services.AddSingleton<DashboardAggregator>();
builder.Services.AddSingleton<WorkItemRanker>();
builder.Services.AddScoped<OauthTokenService>();
builder.Services.AddScoped<PassphraseCacheInterop>();

// ── Authentication ──
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "priorityhub.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    })
    .AddOAuth(MicrosoftScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Microsoft");
        var tenantId = section["TenantId"] ?? "common";

        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-microsoft-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-microsoft-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/microsoft";
        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
        options.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        options.UserInformationEndpoint = "https://graph.microsoft.com/v1.0/me";
        options.SaveTokens = true;
        options.UsePkce = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("User.Read");
        options.Scope.Add("offline_access");
        options.Scope.Add("Tasks.Read");
        options.Scope.Add("Mail.Read");
        options.Scope.Add("https://app.vssps.visualstudio.com/user_impersonation");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                var user = document.RootElement;
                context.RunClaimActions(user);

                var email = user.TryGetProperty("mail", out var mailElement)
                    ? mailElement.GetString()
                    : user.TryGetProperty("userPrincipalName", out var upnElement)
                        ? upnElement.GetString()
                        : string.Empty;

                if (!string.IsNullOrWhiteSpace(email))
                {
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
                }

                context.Identity?.AddClaim(new Claim("provider", "microsoft"));
            }
        };
    })
    .AddOAuth(GitHubScheme, options =>
    // --- Additional providers (scaffold) ---
    .AddOAuth("Jira", options =>
    {
        // TODO: Configure Jira OAuth endpoints and options
        options.ClientId = builder.Configuration["Authentication:Jira:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Jira:ClientSecret"] ?? "";
        options.CallbackPath = "/api/auth/callback/jira";
        // ...
    })
    .AddOAuth("Yandex", options =>
    {
        // TODO: Configure Yandex OAuth endpoints and options
        options.ClientId = builder.Configuration["Authentication:Yandex:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Yandex:ClientSecret"] ?? "";
        options.CallbackPath = "/api/auth/callback/yandex";
        // ...
    })
    .AddOAuth("Trello", options =>
    {
        // TODO: Configure Trello OAuth endpoints and options
        options.ClientId = builder.Configuration["Authentication:Trello:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Trello:ClientSecret"] ?? "";
        options.CallbackPath = "/api/auth/callback/trello";
        // ...
    })
    .AddOAuth("Google", options =>
    {
        // TODO: Configure Google OAuth endpoints and options
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/api/auth/callback/google";
        // ...
    })
    .AddOAuth("Facebook", options =>
    {
        // TODO: Configure Facebook OAuth endpoints and options
        options.ClientId = builder.Configuration["Authentication:Facebook:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Facebook:ClientSecret"] ?? "";
        options.CallbackPath = "/api/auth/callback/facebook";
        // ...
    })
    {
        var section = builder.Configuration.GetSection("Authentication:GitHub");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-github-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-github-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/github";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = true;
        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.Scope.Add("repo");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var userRequest = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                userRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                userRequest.Headers.UserAgent.ParseAdd("PriorityHub");

                using var userResponse = await context.Backchannel.SendAsync(userRequest, context.HttpContext.RequestAborted);
                userResponse.EnsureSuccessStatusCode();

                using var userDocument = JsonDocument.Parse(await userResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                var user = userDocument.RootElement;
                context.RunClaimActions(user);

                if (string.IsNullOrWhiteSpace(context.Identity?.Name) && user.TryGetProperty("login", out var loginElement))
                {
                    var login = loginElement.GetString();
                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Name, login));
                    }
                }

                var email = user.TryGetProperty("email", out var emailElement)
                    ? emailElement.GetString()
                    : await FetchGitHubEmailAsync(context);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
                }

                if (user.TryGetProperty("avatar_url", out var avatarElement) && !string.IsNullOrWhiteSpace(avatarElement.GetString()))
                {
                    context.Identity?.AddClaim(new Claim("picture", avatarElement.GetString()!));
                }

                context.Identity?.AddClaim(new Claim("provider", "github"));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthStateProvider>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// ── API endpoints (kept from existing Program.cs) ──
app.MapGet("/api/connectors", (ConnectorRegistry registry) =>
{
    var metadata = registry.GetAll().Select(c => new
    {
        providerKey = c.ProviderKey,
        displayName = c.DisplayName,
        description = c.Description,
        configFields = c.ConfigFields
    });
    return Results.Ok(metadata);
}).RequireAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { ok = true, generatedAt = DateTimeOffset.UtcNow.ToString("O") }));

app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(CreateAuthUser(user))).RequireAuthorization();

app.MapGet("/api/auth/login/microsoft", (IConfiguration configuration) =>
{
    if (!IsProviderConfigured(configuration.GetSection("Authentication:Microsoft")))
    {
        return Results.Problem("Microsoft authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [MicrosoftScheme]);
});

app.MapGet("/api/auth/login/github", (IConfiguration configuration) =>
{
    if (!IsProviderConfigured(configuration.GetSection("Authentication:GitHub")))
    {
        return Results.Problem("GitHub authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GitHubScheme]);
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/api/config", async (HttpContext httpContext, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    var config = await configStore.LoadAsync(userId, cancellationToken);
    return Results.Ok(config);
}).RequireAuthorization();

app.MapPut("/api/config", async (HttpContext httpContext, ProviderConfiguration configuration, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    await configStore.SaveAsync(userId, configuration, cancellationToken);
    var saved = await configStore.LoadAsync(userId, cancellationToken);
    return Results.Ok(saved);
}).RequireAuthorization();

app.MapPut("/api/preferences/order", async (HttpContext httpContext, UserPreferences preferences, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    var config = await configStore.LoadAsync(userId, cancellationToken);
    config.Preferences = new UserPreferences
    {
        OrderedItemIds = preferences.OrderedItemIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
    };

    await configStore.SaveAsync(userId, config, cancellationToken);
    return Results.Ok(config.Preferences);
}).RequireAuthorization();

app.MapGet("/api/dashboard", async (HttpContext httpContext, DashboardAggregator aggregator, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    var oauthTokensByProvider = await GetOauthTokensByProviderAsync(httpContext.User, httpContext, app.Configuration, cancellationToken);
    var dashboard = await aggregator.BuildAsync(userId, oauthTokensByProvider, cancellationToken);
    return Results.Ok(dashboard);
}).RequireAuthorization();

app.MapGet("/api/dashboard/stream", async (HttpContext httpContext, DashboardAggregator aggregator, CancellationToken cancellationToken) =>
{
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    httpContext.Response.ContentType = "application/x-ndjson";
    httpContext.Response.Headers.CacheControl = "no-cache";
    var userId = GetUserIdentityKey(httpContext.User);
    var oauthTokensByProvider = await GetOauthTokensByProviderAsync(httpContext.User, httpContext, app.Configuration, cancellationToken);

    await foreach (var update in aggregator.StreamAsync(userId, oauthTokensByProvider, cancellationToken))
    {
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(update, JsonSerializerOptions.Web), cancellationToken);
        await httpContext.Response.WriteAsync("\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
}).RequireAuthorization();

// ── Blazor routes ──
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.ApplyDatabaseMigrationsAsync();

app.Run();

// ── Helper methods ──

static AuthUser CreateAuthUser(ClaimsPrincipal user)
{
    return new AuthUser
    {
        Sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        Name = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
        Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        Picture = user.FindFirstValue("picture") ?? string.Empty,
        Provider = user.FindFirstValue("provider") ?? string.Empty
    };
}

static bool IsProviderConfigured(IConfigurationSection section)
{
    return !string.IsNullOrWhiteSpace(section["ClientId"]) && !string.IsNullOrWhiteSpace(section["ClientSecret"]);
}

static string GetUserIdentityKey(ClaimsPrincipal user)
{
    var email = user.FindFirstValue(ClaimTypes.Email);
    if (!string.IsNullOrWhiteSpace(email))
    {
        return email;
    }

    var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
    var provider = user.FindFirstValue("provider") ?? "unknown";
    return $"{sub}_{provider}";
}

static async Task<Dictionary<string, string>> GetOauthTokensByProviderAsync(ClaimsPrincipal user, HttpContext httpContext, IConfiguration configuration, CancellationToken cancellationToken)
{
    var tokenService = httpContext.RequestServices.GetRequiredService<OauthTokenService>();
    return await tokenService.GetTokensByProviderAsync(httpContext, cancellationToken);
}

static async Task<string?> FetchGitHubEmailAsync(OAuthCreatingTicketContext context)
{
    using var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
    emailRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
    emailRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    emailRequest.Headers.UserAgent.ParseAdd("PriorityHub");

    using var emailResponse = await context.Backchannel.SendAsync(emailRequest, context.HttpContext.RequestAborted);
    emailResponse.EnsureSuccessStatusCode();

    using var emailDocument = JsonDocument.Parse(await emailResponse.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
    foreach (var email in emailDocument.RootElement.EnumerateArray())
    {
        if (email.TryGetProperty("primary", out var primary) && primary.GetBoolean()
            && email.TryGetProperty("email", out var emailAddress))
        {
            return emailAddress.GetString();
        }
    }

    return null;
}
