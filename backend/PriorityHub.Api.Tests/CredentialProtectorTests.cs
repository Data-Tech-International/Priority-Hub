using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using PriorityHub.Api.Services;

namespace PriorityHub.Api.Tests;

public sealed class CredentialProtectorTests : IDisposable
{
    private readonly string _keyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly DataProtectionCredentialProtector _protector;

    public CredentialProtectorTests()
    {
        Directory.CreateDirectory(_keyDir);

        var services = new ServiceCollection();
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(_keyDir))
            .SetApplicationName("PriorityHub.Test");
        var provider = services.BuildServiceProvider();

        _protector = new DataProtectionCredentialProtector(
            provider.GetRequiredService<IDataProtectionProvider>());
    }

    [Fact]
    public void Protect_And_Unprotect_RoundTrip_ReturnsOriginalValue()
    {
        const string plaintext = "my-secret-api-token";

        var ciphertext = _protector.Protect(plaintext);
        var decrypted = _protector.Unprotect(ciphertext);

        Assert.NotEqual(plaintext, ciphertext);   // must be encrypted
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Protect_EmptyString_ReturnsNonEmptyAndRoundTrips()
    {
        var ciphertext = _protector.Protect(string.Empty);
        var decrypted = _protector.Unprotect(ciphertext);

        Assert.False(string.IsNullOrEmpty(ciphertext));
        Assert.Equal(string.Empty, decrypted);
    }

    [Fact]
    public void Unprotect_PlaintextValue_ReturnsNull()
    {
        // A raw plaintext string is not valid ciphertext — should return null.
        var result = _protector.Unprotect("this-is-not-ciphertext");

        Assert.Null(result);
    }

    [Fact]
    public void Unprotect_TamperedCiphertext_ReturnsNull()
    {
        var ciphertext = _protector.Protect("secret");
        var tampered = ciphertext[..^4] + "XXXX";

        Assert.Null(_protector.Unprotect(tampered));
    }

    public void Dispose()
    {
        if (Directory.Exists(_keyDir))
            Directory.Delete(_keyDir, recursive: true);
    }
}
