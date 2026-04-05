using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

/// <summary>
/// Resolves per-provider OAuth tokens from the current authenticated session,
/// exchanging the Microsoft refresh token for provider-specific access tokens
/// when needed (e.g. Azure DevOps requires its own audience).
/// </summary>
public sealed class OauthTokenService(IConfiguration configuration, ILogger<OauthTokenService> logger, IHttpClientFactory httpClientFactory)
{
    private static readonly string[] AzureDevOpsScopes =
    [
        "https://app.vssps.visualstudio.com/user_impersonation",
        "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation"
    ];
    private const string MicrosoftGraphScope = "User.Read Tasks.Read Mail.Read offline_access";

    /// <summary>
    /// Builds a dictionary mapping provider keys to their OAuth access tokens.
    /// </summary>
    public async Task<Dictionary<string, string>> GetTokensByProviderAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var provider = httpContext.User.FindFirstValue("provider");
        var accessToken = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return tokens;
        }

        if (string.Equals(provider, "microsoft", StringComparison.OrdinalIgnoreCase))
        {
            var refreshToken = await httpContext.GetTokenAsync("refresh_token");
            var microsoftSection = configuration.GetSection("Authentication:Microsoft");

            var refreshedMicrosoftToken = await RequestAccessTokenFromRefreshTokenAsync(
                microsoftSection,
                refreshToken,
                MicrosoftGraphScope,
                cancellationToken);

            var effectiveMicrosoftAccessToken = accessToken;
            var effectiveRefreshToken = refreshToken;
            if (!string.IsNullOrWhiteSpace(refreshedMicrosoftToken?.AccessToken))
            {
                effectiveMicrosoftAccessToken = refreshedMicrosoftToken.AccessToken;
                effectiveRefreshToken = string.IsNullOrWhiteSpace(refreshedMicrosoftToken.RefreshToken)
                    ? refreshToken
                    : refreshedMicrosoftToken.RefreshToken;

                await PersistRefreshedTokensAsync(
                    httpContext,
                    effectiveMicrosoftAccessToken,
                    effectiveRefreshToken,
                    refreshedMicrosoftToken.ExpiresAt);
            }

            tokens["microsoft-tasks"] = effectiveMicrosoftAccessToken;
            tokens["outlook-flagged-mail"] = effectiveMicrosoftAccessToken;

            var azureDevOpsAccessToken = await RequestAzureDevOpsTokenAsync(
                microsoftSection,
                effectiveRefreshToken,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(azureDevOpsAccessToken?.AccessToken))
            {
                    logger.LogDebug("Azure DevOps OAuth token obtained successfully (length={Length}).", azureDevOpsAccessToken.AccessToken.Length);
                tokens["azure-devops"] = azureDevOpsAccessToken.AccessToken;
            }
            else
            {
                logger.LogWarning("Failed to exchange refresh token for Azure DevOps access token. Azure DevOps connections will require a PAT. refreshToken present={HasRefresh}", !string.IsNullOrWhiteSpace(effectiveRefreshToken));
            }
        }

        if (string.Equals(provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            tokens["github"] = accessToken;
        }

        return tokens;
    }

    /// <summary>
    /// Exchanges the stored refresh token for each linked Microsoft account connection
    /// that has a <c>LinkedAccountId</c>, returning a dictionary keyed by connection ID.
    /// Connections whose linked account is missing or whose token exchange fails are
    /// excluded from the result (the caller falls back to the primary session token or
    /// shows "needs-reauth").
    /// </summary>
    public async Task<Dictionary<string, string>> GetLinkedAccountTokensAsync(
        ProviderConfiguration config,
        IReadOnlyDictionary<string, string> connectionIdToLinkedAccountId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (config.LinkedMicrosoftAccounts.Count == 0 || connectionIdToLinkedAccountId.Count == 0)
            return result;

        var accountsById = config.LinkedMicrosoftAccounts
            .ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);

        var microsoftSection = configuration.GetSection("Authentication:Microsoft");

        foreach (var (connectionId, linkedAccountId) in connectionIdToLinkedAccountId)
        {
            if (!accountsById.TryGetValue(linkedAccountId, out var account))
            {
                logger.LogWarning("Linked account {LinkedAccountId} not found for connection {ConnectionId}.", linkedAccountId, connectionId);
                continue;
            }

            if (string.IsNullOrWhiteSpace(account.RefreshToken))
            {
                logger.LogWarning("Linked account {Email} has no refresh token.", account.Email);
                continue;
            }

            var tokenResult = await RequestAccessTokenFromRefreshTokenAsync(
                microsoftSection,
                account.RefreshToken,
                MicrosoftGraphScope,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(tokenResult?.AccessToken))
            {
                result[connectionId] = tokenResult.AccessToken;
                logger.LogDebug("Resolved linked account token for connection {ConnectionId} ({Email}).", connectionId, account.Email);
            }
            else
            {
                logger.LogWarning("Failed to exchange refresh token for linked account {Email} (connection {ConnectionId}).", account.Email, connectionId);
            }
        }

        return result;
    }

    private async Task<TokenExchangeResult?> RequestAzureDevOpsTokenAsync(
        IConfigurationSection microsoftSection,
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Azure DevOps token exchange: refreshToken present={HasRefresh}", !string.IsNullOrWhiteSpace(refreshToken));

        foreach (var scope in AzureDevOpsScopes)
        {
            logger.LogDebug("Azure DevOps token exchange: trying scope {Scope}", scope);

            var result = await RequestAccessTokenFromRefreshTokenAsync(
                microsoftSection,
                refreshToken,
                scope,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(result?.AccessToken))
            {
                logger.LogDebug("Azure DevOps token exchange: scope {Scope} succeeded, token length={Length}", scope, result.AccessToken.Length);
                return result;
            }

            logger.LogDebug("Azure DevOps token exchange: scope {Scope} returned no token.", scope);
        }

        logger.LogWarning("Azure DevOps token exchange: all scopes exhausted, no token obtained.");
        return null;
    }

    private async Task PersistRefreshedTokensAsync(
        HttpContext httpContext,
        string accessToken,
        string? refreshToken,
        string? expiresAt)
    {
        if (httpContext.Response.HasStarted)
        {
            logger.LogDebug("Skipping OAuth token persistence because the HTTP response has already started.");
            return;
        }

        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null || authResult.Properties is null)
        {
            return;
        }

        SetOrAddTokenValue(authResult.Properties, "access_token", accessToken);
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            SetOrAddTokenValue(authResult.Properties, "refresh_token", refreshToken);
        }

        if (!string.IsNullOrWhiteSpace(expiresAt))
        {
            SetOrAddTokenValue(authResult.Properties, "expires_at", expiresAt);
        }

        try
        {
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                authResult.Principal,
                authResult.Properties);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Skipping OAuth token persistence because response headers are not writable.");
        }
    }

    private static void SetOrAddTokenValue(AuthenticationProperties properties, string tokenName, string tokenValue)
    {
        var existingTokens = properties.GetTokens().ToList();
        var existingToken = existingTokens.FirstOrDefault(token =>
            string.Equals(token.Name, tokenName, StringComparison.Ordinal));

        if (existingToken is null)
        {
            existingTokens.Add(new AuthenticationToken { Name = tokenName, Value = tokenValue });
        }
        else
        {
            existingToken.Value = tokenValue;
        }

        properties.StoreTokens(existingTokens);
    }

    private async Task<TokenExchangeResult?> RequestAccessTokenFromRefreshTokenAsync(
        IConfigurationSection microsoftSection,
        string? refreshToken,
        string scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogWarning("No refresh token available for OAuth token exchange (scope: {Scope}).", scope);
            return null;
        }

        var clientId = microsoftSection["ClientId"];
        var clientSecret = microsoftSection["ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var tenantId = microsoftSection["TenantId"] ?? "common";
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = scope
            })
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("OAuth token exchange failed for scope {Scope} with status {StatusCode}: {Body}", scope, response.StatusCode, errorBody);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("access_token", out var accessTokenElement)
            || string.IsNullOrWhiteSpace(accessTokenElement.GetString()))
        {
            return null;
        }

        var accessToken = accessTokenElement.GetString()!;
        var refreshedToken = document.RootElement.TryGetProperty("refresh_token", out var refreshTokenElement)
            ? refreshTokenElement.GetString()
            : null;

        string? expiresAt = null;
        if (document.RootElement.TryGetProperty("expires_in", out var expiresInElement)
            && expiresInElement.TryGetInt32(out var expiresInSeconds))
        {
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds).ToString("o");
        }

        return new TokenExchangeResult(accessToken, refreshedToken, expiresAt);
    }

    private sealed record TokenExchangeResult(string AccessToken, string? RefreshToken, string? ExpiresAt);
}
