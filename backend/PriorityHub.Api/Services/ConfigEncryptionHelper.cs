using System.Reflection;
using PriorityHub.Api.Models;

namespace PriorityHub.Api.Services;

/// <summary>
/// Uses reflection to encrypt and decrypt all string properties marked with
/// <see cref="SensitiveFieldAttribute"/> across every connection collection
/// in a <see cref="ProviderConfiguration"/>.
/// </summary>
public static class ConfigEncryptionHelper
{
    /// <summary>
    /// Encrypts every <see cref="SensitiveFieldAttribute"/> string property on all
    /// connection objects in <paramref name="config"/>, in place.
    /// </summary>
    public static void Encrypt(ProviderConfiguration config, ICredentialProtector protector)
    {
        foreach (var item in EnumerateItems(config))
        {
            ApplySensitiveFields(item, protector.Protect);
        }
    }

    /// <summary>
    /// Decrypts every <see cref="SensitiveFieldAttribute"/> string property on all
    /// connection objects in <paramref name="config"/>, in place.
    /// Values that cannot be decrypted (e.g. still plaintext) are left unchanged
    /// so the connector receives whatever was stored.
    /// </summary>
    public static void Decrypt(ProviderConfiguration config, ICredentialProtector protector, ILogger? logger = null)
    {
        foreach (var item in EnumerateItems(config))
        {
            ApplySensitiveFields(item, plaintext =>
            {
                if (string.IsNullOrEmpty(plaintext))
                    return plaintext;

                var decrypted = protector.Unprotect(plaintext);
                if (decrypted is null)
                {
                    // Value is still plaintext — tolerate and return as-is (migration scenario).
                    logger?.LogInformation(
                        "Credential field appears unencrypted (migration). Will be encrypted on next save.");
                    return plaintext;
                }

                return decrypted;
            });
        }
    }

    private static void ApplySensitiveFields(object item, Func<string, string> transform)
    {
        var type = item.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.PropertyType != typeof(string))
                continue;
            if (prop.GetCustomAttribute<SensitiveFieldAttribute>() is null)
                continue;
            if (!prop.CanRead || !prop.CanWrite)
                continue;

            var current = (string?)prop.GetValue(item);
            if (string.IsNullOrEmpty(current))
                continue;

            prop.SetValue(item, transform(current));
        }
    }

    private static IEnumerable<object> EnumerateItems(ProviderConfiguration config)
    {
        foreach (var item in config.AzureDevOps) yield return item;
        foreach (var item in config.GitHub) yield return item;
        foreach (var item in config.Jira) yield return item;
        foreach (var item in config.MicrosoftTasks) yield return item;
        foreach (var item in config.OutlookFlaggedMail) yield return item;
        foreach (var item in config.Trello) yield return item;
        foreach (var item in config.ImapFlaggedMail) yield return item;
        foreach (var item in config.LinkedMicrosoftAccounts) yield return item;
    }
}
