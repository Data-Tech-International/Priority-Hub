using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace PriorityHub.Api.Services.Telemetry;

public sealed class PriorityHubTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var identityKey = ResolveIdentityKey(user);
        if (string.IsNullOrWhiteSpace(identityKey))
        {
            return;
        }

        telemetry.Context.User.AuthenticatedUserId = UserIdentityHasher.Hash(identityKey);

        if (telemetry is ISupportProperties withProperties)
        {
            var provider = user.FindFirstValue("provider") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(provider))
            {
                withProperties.Properties["authProvider"] = provider;
            }
        }
    }

    private static string ResolveIdentityKey(ClaimsPrincipal user)
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
}
