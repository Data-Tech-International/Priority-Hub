using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PriorityHub.Api.Models;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

/// <summary>
/// Phase 3 tests for <see cref="OauthTokenService.GetLinkedAccountTokensAsync"/>.
/// </summary>
public sealed class LinkedAccountTokenServiceTests
{
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Microsoft:ClientId"] = "client-id",
            ["Authentication:Microsoft:ClientSecret"] = "client-secret",
            ["Authentication:Microsoft:TenantId"] = "common"
        }).Build();

    [Fact]
    public async Task GetLinkedAccountTokens_EmptyLinkedAccounts_ReturnsEmpty()
    {
        var config = BuildConfiguration();
        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(new HttpClient()));

        var providerConfig = new ProviderConfiguration();
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedAccountTokens_NoConnectionsRequiringLinkedAccount_ReturnsEmpty()
    {
        var config = BuildConfiguration();
        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(new HttpClient()));

        var providerConfig = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [new LinkedMicrosoftAccount { Id = "acc-1", RefreshToken = "rt" }]
        };

        // Empty connectionIdToLinkedAccountId → nothing to resolve.
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedAccountTokens_LinkedAccountMissing_ReturnsEmpty()
    {
        var config = BuildConfiguration();
        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(new HttpClient()));

        var providerConfig = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [] // no accounts stored
        };

        // Connection references a linked account ID that doesn't exist.
        var connectionMap = new Dictionary<string, string> { ["conn-1"] = "missing-account-id" };
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, connectionMap, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedAccountTokens_TokenExchangeSucceeds_ReturnsConnectionToken()
    {
        var config = BuildConfiguration();
        var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"linked-access-token"}""", Encoding.UTF8, "application/json")
            }
        ])));

        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));

        var providerConfig = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [new LinkedMicrosoftAccount { Id = "acc-1", Email = "linked@example.com", RefreshToken = "linked-rt" }]
        };

        var connectionMap = new Dictionary<string, string> { ["conn-outlook"] = "acc-1" };
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, connectionMap, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("linked-access-token", result["conn-outlook"]);
    }

    [Fact]
    public async Task GetLinkedAccountTokens_TokenExchangeFails_ConnectionNotIncluded()
    {
        var config = BuildConfiguration();
        var httpClient = new HttpClient(new QueueMessageHandler(new Queue<HttpResponseMessage>([
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":"invalid_grant"}""", Encoding.UTF8, "application/json")
            }
        ])));

        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(httpClient));

        var providerConfig = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [new LinkedMicrosoftAccount { Id = "acc-1", Email = "linked@example.com", RefreshToken = "bad-rt" }]
        };

        var connectionMap = new Dictionary<string, string> { ["conn-outlook"] = "acc-1" };
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, connectionMap, CancellationToken.None);

        // Token exchange failed → connection not in result (caller handles "needs-reauth").
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinkedAccountTokens_LinkedAccountHasNoRefreshToken_ConnectionNotIncluded()
    {
        var config = BuildConfiguration();
        var tokenService = new OauthTokenService(config, NullLogger<OauthTokenService>.Instance, new StaticHttpClientFactory(new HttpClient()));

        var providerConfig = new ProviderConfiguration
        {
            LinkedMicrosoftAccounts = [new LinkedMicrosoftAccount { Id = "acc-1", Email = "linked@example.com", RefreshToken = "" }]
        };

        var connectionMap = new Dictionary<string, string> { ["conn-outlook"] = "acc-1" };
        var result = await tokenService.GetLinkedAccountTokensAsync(providerConfig, connectionMap, CancellationToken.None);

        Assert.Empty(result);
    }
}
