using Microsoft.JSInterop;

namespace PriorityHub.Ui.Services;

/// <summary>
/// Provides Blazor Server-side access to the browser passphrase cache that is
/// backed by WebCrypto AES-GCM wrapping and IndexedDB persistence.
/// </summary>
/// <remarks>
/// All persistent unlock material lives exclusively in the browser's IndexedDB.
/// The raw passphrase and provider credentials are never sent to or stored on
/// the server.  The AES-GCM device key is stored as a non-extractable
/// <c>CryptoKey</c> object so that its raw bytes are never exposed to
/// JavaScript consumers or DevTools.
/// </remarks>
public sealed class PassphraseCacheInterop
{
    private readonly IJSRuntime _js;

    /// <summary>Initialises a new instance of <see cref="PassphraseCacheInterop"/>.</summary>
    public PassphraseCacheInterop(IJSRuntime js) => _js = js;

    /// <summary>
    /// Wraps <paramref name="passphrase"/> with a freshly generated
    /// non-extractable AES-GCM device key and persists the wrapped material
    /// plus a 90-day TTL to the browser's IndexedDB.
    /// </summary>
    /// <param name="passphrase">The plaintext passphrase to cache.</param>
    /// <remarks>
    /// This method must only be called when the user has explicitly opted in
    /// to the remember-me feature on a trusted device.  It must not be called
    /// when the user has indicated they are on a shared or public device.
    /// </remarks>
    public ValueTask StorePassphraseAsync(string passphrase) =>
        _js.InvokeVoidAsync("PriorityHub.passphraseCache.wrapAndStore", passphrase);

    /// <summary>
    /// Attempts to restore the passphrase from the browser's IndexedDB cache.
    /// </summary>
    /// <returns>
    /// The cached passphrase if a valid, non-expired entry is found; otherwise
    /// <see langword="null"/>.  Returns <see langword="null"/> and wipes the
    /// entry if the TTL has elapsed or AES-GCM integrity verification fails.
    /// </returns>
    public ValueTask<string?> LoadPassphraseAsync() =>
        _js.InvokeAsync<string?>("PriorityHub.passphraseCache.loadAndUnwrap");

    /// <summary>
    /// Immediately removes all cached unlock material from the browser's
    /// IndexedDB, regardless of TTL.
    /// </summary>
    public ValueTask ClearPassphraseAsync() =>
        _js.InvokeVoidAsync("PriorityHub.passphraseCache.clearCache");

    /// <summary>
    /// Returns <see langword="true"/> if a valid, non-expired cached entry
    /// exists in the browser's IndexedDB.
    /// </summary>
    public ValueTask<bool> HasValidCacheAsync() =>
        _js.InvokeAsync<bool>("PriorityHub.passphraseCache.hasValidCache");
}
