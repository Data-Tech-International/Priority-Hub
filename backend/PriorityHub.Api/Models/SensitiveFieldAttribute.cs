namespace PriorityHub.Api.Models;

/// <summary>
/// Marks a string property as containing sensitive data (e.g., API keys, passwords, tokens)
/// that must be encrypted before being written to storage and decrypted after loading.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveFieldAttribute : Attribute { }
