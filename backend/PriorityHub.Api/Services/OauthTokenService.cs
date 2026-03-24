using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace PriorityHub.Api.Services;

/// <summary>
/// Resolves per-provider OAuth tokens from the current authenticated session,
/// exchanging the Microsoft refresh token for provider-specific access tokens
/// when needed (e.g. Azure DevOps requires its own audience).
/// </summary>
public sealed class OauthTokenService(IConfiguration configuration, ILogger<OauthTokenService> logger)
{
    private const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation";

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
            tokens["microsoft-tasks"] = accessToken;
            tokens["outlook-flagged-mail"] = accessToken;

            var refreshToken = await httpContext.GetTokenAsync("refresh_token");
            var microsoftSection = configuration.GetSection("Authentication:Microsoft");
            var azureDevOpsAccessToken = await RequestAccessTokenFromRefreshTokenAsync(
                microsoftSection,
                refreshToken,
                AzureDevOpsScope,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(azureDevOpsAccessToken))
            {
                tokens["azure-devops"] = azureDevOpsAccessToken;
            }
            else
            {
                logger.LogWarning("Failed to exchange refresh token for Azure DevOps access token. Azure DevOps connections will require a PAT.");
            }
        }

        if (string.Equals(provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            tokens["github"] = accessToken;
        }

        return tokens;
    }

    private async Task<string?> RequestAccessTokenFromRefreshTokenAsync(
        IConfigurationSection microsoftSection,
        string? refreshToken,
        string scope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            logger.LogWarning("No refresh token available for Azure DevOps token exchange.");
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

        using var httpClient = new HttpClient();
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
            logger.LogWarning("Azure DevOps token exchange failed with status {StatusCode}: {Body}", response.StatusCode, errorBody);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("access_token", out var tokenElement)
            ? tokenElement.GetString()
            : null;
    }
}
