using Microsoft.AspNetCore.DataProtection;

namespace PriorityHub.Api.Services;

/// <summary>
/// Encrypts and decrypts sensitive credentials using the .NET Data Protection API.
/// Purpose string: <c>"PriorityHub.Credentials.v1"</c>.
/// </summary>
public sealed class DataProtectionCredentialProtector : ICredentialProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("PriorityHub.Credentials.v1");
    }

    /// <inheritdoc/>
    public string Protect(string plaintext) => _protector.Protect(plaintext);

    /// <inheritdoc/>
    public string? Unprotect(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch
        {
            // Value is plaintext (pre-encryption migration) or ciphertext from a rotated key.
            return null;
        }
    }
}
