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
const string GoogleScheme = "Google";
const string FacebookScheme = "Facebook";
const string JiraScheme = "Jira";
const string TrelloScheme = "Trello";
const string YandexScheme = "Yandex";
const string FacebookApiVersion = "v18.0";
const string TrelloApiVersion = "1";

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
builder.Services.AddSingleton<IImapClientFactory, ImapClientFactory>();
builder.Services.AddSingleton<ImapFlaggedMailConnector>();
builder.Services.AddSingleton<ConnectorRegistry>(sp => new ConnectorRegistry([
    sp.GetRequiredService<AzureDevOpsConnector>(),
    sp.GetRequiredService<GitHubIssuesConnector>(),
    sp.GetRequiredService<JiraConnector>(),
    sp.GetRequiredService<MicrosoftTasksConnector>(),
    sp.GetRequiredService<OutlookFlaggedMailConnector>(),
    sp.GetRequiredService<TrelloConnector>(),
    sp.GetRequiredService<ImapFlaggedMailConnector>(),
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
    })
    .AddOAuth(GoogleScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-google-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-google-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/google";
        options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        options.TokenEndpoint = "https://oauth2.googleapis.com/token";
        options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
        options.SaveTokens = true;
        options.UsePkce = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonKey("picture", "picture");
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
                context.RunClaimActions(document.RootElement);
                context.Identity?.AddClaim(new Claim("provider", "google"));
            }
        };
    })
    .AddOAuth(FacebookScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Facebook");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-facebook-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-facebook-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/facebook";
        options.AuthorizationEndpoint = $"https://www.facebook.com/{FacebookApiVersion}/dialog/oauth";
        options.TokenEndpoint = $"https://graph.facebook.com/{FacebookApiVersion}/oauth/access_token";
        options.UserInformationEndpoint = $"https://graph.facebook.com/me?fields=id,name,email,picture";
        options.SaveTokens = true;
        options.Scope.Add("email");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
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
                context.RunClaimActions(document.RootElement);
                context.Identity?.AddClaim(new Claim("provider", "facebook"));
            }
        };
    })
    .AddOAuth(JiraScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Jira");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-jira-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-jira-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/jira";
        options.AuthorizationEndpoint = "https://auth.atlassian.com/authorize";
        options.TokenEndpoint = "https://auth.atlassian.com/oauth/token";
        options.UserInformationEndpoint = "https://api.atlassian.com/me";
        options.SaveTokens = true;
        options.UsePkce = true;
        options.Scope.Add("read:me");
        options.Scope.Add("read:account");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "account_id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonKey("picture", "picture");
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
                context.RunClaimActions(document.RootElement);
                context.Identity?.AddClaim(new Claim("provider", "jira"));
            }
        };
    })
    .AddOAuth(TrelloScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Trello");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-trello-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-trello-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/trello";
        options.AuthorizationEndpoint = $"https://trello.com/{TrelloApiVersion}/authorize";
        options.TokenEndpoint = $"https://trello.com/{TrelloApiVersion}/OAuthGetAccessToken";
        options.UserInformationEndpoint = $"https://api.trello.com/{TrelloApiVersion}/members/me";
        options.SaveTokens = true;
        options.Scope.Add("read");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "fullName");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{context.Options.UserInformationEndpoint}?key={context.Options.ClientId}&token={context.AccessToken}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                context.RunClaimActions(document.RootElement);
                context.Identity?.AddClaim(new Claim("provider", "trello"));
            }
        };
    })
    .AddOAuth(YandexScheme, options =>
    {
        var section = builder.Configuration.GetSection("Authentication:Yandex");
        options.ClientId = string.IsNullOrWhiteSpace(section["ClientId"]) ? "disabled-yandex-client" : section["ClientId"]!;
        options.ClientSecret = string.IsNullOrWhiteSpace(section["ClientSecret"]) ? "disabled-yandex-secret" : section["ClientSecret"]!;
        options.CallbackPath = "/api/auth/callback/yandex";
        options.AuthorizationEndpoint = "https://oauth.yandex.com/authorize";
        options.TokenEndpoint = "https://oauth.yandex.com/token";
        options.UserInformationEndpoint = "https://login.yandex.ru/info?format=json";
        options.SaveTokens = true;
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "real_name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "default_email");
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", context.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                context.RunClaimActions(document.RootElement);
                context.Identity?.AddClaim(new Claim("provider", "yandex"));
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
    var section = configuration.GetSection("Authentication:Microsoft");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Microsoft authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Microsoft authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [MicrosoftScheme]);
});

app.MapGet("/api/auth/login/github", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:GitHub");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("GitHub authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("GitHub authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GitHubScheme]);
});

app.MapGet("/api/auth/login/google", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Google");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Google authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Google authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GoogleScheme]);
});

app.MapGet("/api/auth/login/facebook", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Facebook");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Facebook authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Facebook authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [FacebookScheme]);
});

app.MapGet("/api/auth/login/jira", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Jira");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Jira authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Jira authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [JiraScheme]);
});

app.MapGet("/api/auth/login/trello", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Trello");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Trello authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Trello authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [TrelloScheme]);
});

app.MapGet("/api/auth/login/yandex", (IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Yandex");
    if (!IsProviderEnabled(section))
    {
        return Results.Problem("Yandex authentication is not enabled.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Yandex authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [YandexScheme]);
});

app.MapPost("/api/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

// ── Linked Microsoft Account endpoints ──

app.MapGet("/api/auth/link/microsoft", (HttpContext httpContext, IConfiguration configuration) =>
{
    var section = configuration.GetSection("Authentication:Microsoft");
    if (!IsProviderConfigured(section))
    {
        return Results.Problem("Microsoft authentication is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    var userId = GetUserIdentityKey(httpContext.User);
    var tenantId = section["TenantId"] ?? "common";
    var clientId = section["ClientId"]!;
    var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/auth/link/microsoft/callback";

    // Encode user identity in the state parameter.
    var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userId));

    var authorizationUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize" +
        $"?client_id={Uri.EscapeDataString(clientId)}" +
        $"&response_type=code" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&scope={Uri.EscapeDataString("openid profile email User.Read Tasks.Read Mail.Read offline_access")}" +
        $"&prompt=select_account" +
        $"&state={Uri.EscapeDataString(state)}";

    return Results.Redirect(authorizationUrl);
}).RequireAuthorization();

app.MapGet("/api/auth/link/microsoft/callback", async (HttpContext httpContext, IConfigStore configStore, IConfiguration configuration, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    var code = httpContext.Request.Query["code"].ToString();
    var state = httpContext.Request.Query["state"].ToString();
    var error = httpContext.Request.Query["error"].ToString();

    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.Redirect("/settings?linkError=" + Uri.EscapeDataString(error));
    }

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
    {
        return Results.Redirect("/settings?linkError=missing_code");
    }

    string userId;
    try
    {
        userId = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(state));
    }
    catch
    {
        return Results.Redirect("/settings?linkError=invalid_state");
    }

    var section = configuration.GetSection("Authentication:Microsoft");
    var clientId = section["ClientId"];
    var clientSecret = section["ClientSecret"];
    var tenantId = section["TenantId"] ?? "common";
    var redirectUri = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/auth/link/microsoft/callback";

    if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
    {
        return Results.Redirect("/settings?linkError=not_configured");
    }

    var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
    var httpClient = httpClientFactory.CreateClient();

    using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        })
    };

    using var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
    if (!tokenResponse.IsSuccessStatusCode)
    {
        return Results.Redirect("/settings?linkError=token_exchange_failed");
    }

    var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
    using var tokenDoc = JsonDocument.Parse(tokenBody);

    var refreshToken = tokenDoc.RootElement.TryGetProperty("refresh_token", out var rtProp)
        ? rtProp.GetString() ?? string.Empty
        : string.Empty;

    if (string.IsNullOrWhiteSpace(refreshToken))
    {
        return Results.Redirect("/settings?linkError=no_refresh_token");
    }

    // Fetch the linked account's profile from Microsoft Graph.
    var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var atProp)
        ? atProp.GetString() ?? string.Empty
        : string.Empty;

    string displayName = string.Empty, email = string.Empty;
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        meRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        using var meResponse = await httpClient.SendAsync(meRequest, cancellationToken);
        if (meResponse.IsSuccessStatusCode)
        {
            var meBody = await meResponse.Content.ReadAsStringAsync(cancellationToken);
            using var meDoc = JsonDocument.Parse(meBody);
            displayName = meDoc.RootElement.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
            email = meDoc.RootElement.TryGetProperty("mail", out var mail) ? mail.GetString() ?? string.Empty
                : meDoc.RootElement.TryGetProperty("userPrincipalName", out var upn) ? upn.GetString() ?? string.Empty
                : string.Empty;
        }
    }

    var config = await configStore.LoadAsync(userId, cancellationToken);
    config.LinkedMicrosoftAccounts ??= [];

    // Avoid duplicate linked accounts for the same email.
    var existing = config.LinkedMicrosoftAccounts
        .FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase));
    if (existing is not null)
    {
        existing.RefreshToken = refreshToken;
        existing.DisplayName = displayName;
        existing.LinkedAt = DateTimeOffset.UtcNow;
    }
    else
    {
        config.LinkedMicrosoftAccounts.Add(new LinkedMicrosoftAccount
        {
            DisplayName = displayName,
            Email = email,
            RefreshToken = refreshToken,
        });
    }

    await configStore.SaveAsync(userId, config, cancellationToken);
    return Results.Redirect("/settings?linkSuccess=1");
});

app.MapDelete("/api/auth/link/microsoft/{accountId}", async (string accountId, HttpContext httpContext, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    var config = await configStore.LoadAsync(userId, cancellationToken);

    var account = config.LinkedMicrosoftAccounts.FirstOrDefault(a =>
        string.Equals(a.Id, accountId, StringComparison.OrdinalIgnoreCase));

    if (account is null)
    {
        return Results.NotFound(new { error = "Account not found." });
    }

    config.LinkedMicrosoftAccounts.Remove(account);
    await configStore.SaveAsync(userId, config, cancellationToken);
    return Results.Ok(new { ok = true });
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

app.MapGet("/api/dashboard", async (HttpContext httpContext, DashboardAggregator aggregator, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    var userId = GetUserIdentityKey(httpContext.User);
    var tokenService = httpContext.RequestServices.GetRequiredService<OauthTokenService>();
    var oauthTokensByProvider = await tokenService.GetTokensByProviderAsync(httpContext, cancellationToken);
    var config = await configStore.LoadAsync(userId, cancellationToken);
    var oauthTokensByConnectionId = await ResolveLinkedAccountTokensAsync(tokenService, config, cancellationToken);
    var dashboard = await aggregator.BuildAsync(userId, oauthTokensByProvider, oauthTokensByConnectionId, cancellationToken);
    return Results.Ok(dashboard);
}).RequireAuthorization();

app.MapGet("/api/dashboard/stream", async (HttpContext httpContext, DashboardAggregator aggregator, IConfigStore configStore, CancellationToken cancellationToken) =>
{
    httpContext.Response.StatusCode = StatusCodes.Status200OK;
    httpContext.Response.ContentType = "application/x-ndjson";
    httpContext.Response.Headers.CacheControl = "no-cache";
    var userId = GetUserIdentityKey(httpContext.User);
    var tokenService = httpContext.RequestServices.GetRequiredService<OauthTokenService>();
    var oauthTokensByProvider = await tokenService.GetTokensByProviderAsync(httpContext, cancellationToken);
    var config = await configStore.LoadAsync(userId, cancellationToken);
    var oauthTokensByConnectionId = await ResolveLinkedAccountTokensAsync(tokenService, config, cancellationToken);

    await foreach (var update in aggregator.StreamAsync(userId, oauthTokensByProvider, oauthTokensByConnectionId, cancellationToken))
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

static bool IsProviderEnabled(IConfigurationSection section)
{
    return section.GetValue("Enabled", false);
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

static async Task<Dictionary<string, string>> ResolveLinkedAccountTokensAsync(OauthTokenService tokenService, ProviderConfiguration config, CancellationToken cancellationToken)
{
    // Build a map of connectionId → linkedAccountId for connections that opt in.
    var connectionIdToLinkedAccountId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var conn in config.OutlookFlaggedMail)
    {
        if (!string.IsNullOrWhiteSpace(conn.LinkedAccountId))
            connectionIdToLinkedAccountId[conn.Id] = conn.LinkedAccountId;
    }

    foreach (var conn in config.MicrosoftTasks)
    {
        if (!string.IsNullOrWhiteSpace(conn.LinkedAccountId))
            connectionIdToLinkedAccountId[conn.Id] = conn.LinkedAccountId;
    }

    if (connectionIdToLinkedAccountId.Count == 0)
        return [];

    return await tokenService.GetLinkedAccountTokensAsync(config, connectionIdToLinkedAccountId, cancellationToken);
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
