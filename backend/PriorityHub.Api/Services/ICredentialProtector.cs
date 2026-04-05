namespace PriorityHub.Api.Services;

/// <summary>
/// Encrypts and decrypts sensitive credential strings for at-rest storage.
/// </summary>
public interface ICredentialProtector
{
    /// <summary>Encrypts a plaintext string. Returns an opaque ciphertext string.</summary>
    string Protect(string plaintext);

    /// <summary>
    /// Decrypts a previously encrypted ciphertext string.
    /// Returns <see langword="null"/> when decryption fails (e.g., value is still plaintext
    /// from before encryption was introduced, or the key ring has rotated).
    /// </summary>
    string? Unprotect(string ciphertext);
}
