using System.Security.Cryptography;
using System.Text;
using PriorityHub.Api.Services.Telemetry;

namespace PriorityHub.Api.Tests;

public sealed class UserIdentityHasherTests
{
    [Fact]
    public void Hash_KnownInput_ReturnsExpectedSha256Hex()
    {
        // Pre-computed SHA-256 of "user@example.com"
        var expected = ComputeExpectedHash("user@example.com");

        var result = UserIdentityHasher.Hash("user@example.com");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Hash_SameInput_ReturnsConsistentResult()
    {
        var first = UserIdentityHasher.Hash("consistent-test");
        var second = UserIdentityHasher.Hash("consistent-test");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Hash_NullOrWhitespace_ReturnsEmptyString(string? input)
    {
        var result = UserIdentityHasher.Hash(input!);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Hash_InputWithSurroundingWhitespace_ReturnsSameAsWithout()
    {
        var trimmed = UserIdentityHasher.Hash("user@example.com");
        var withSpaces = UserIdentityHasher.Hash("  user@example.com  ");

        Assert.Equal(trimmed, withSpaces);
    }

    [Fact]
    public void Hash_ReturnsLowercaseHex()
    {
        var result = UserIdentityHasher.Hash("user@example.com");

        Assert.Equal(result, result.ToLowerInvariant());
    }

    [Fact]
    public void Hash_DoesNotReturnOriginalInput()
    {
        const string input = "user@example.com";

        var result = UserIdentityHasher.Hash(input);

        Assert.NotEqual(input, result);
        Assert.DoesNotContain(input, result);
    }

    [Fact]
    public void Hash_DifferentInputs_ReturnDifferentHashes()
    {
        var hash1 = UserIdentityHasher.Hash("alice@example.com");
        var hash2 = UserIdentityHasher.Hash("bob@example.com");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_Returns64CharHexString()
    {
        // SHA-256 produces 32 bytes = 64 hex chars
        var result = UserIdentityHasher.Hash("user@example.com");

        Assert.Equal(64, result.Length);
        Assert.Matches("^[0-9a-f]{64}$", result);
    }

    private static string ComputeExpectedHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
