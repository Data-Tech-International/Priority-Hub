using System.Security.Cryptography;
using System.Text;

namespace PriorityHub.Api.Services.Telemetry;

internal static class UserIdentityHasher
{
    public static string Hash(string userIdentityKey)
    {
        if (string.IsNullOrWhiteSpace(userIdentityKey))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(userIdentityKey.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
