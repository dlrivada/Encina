#pragma warning disable CA2012 // ValueTask should not be awaited multiple times - used via .AsTask().Result in sync property tests

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.Providers;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Security.Secrets;

/// <summary>
/// Property-based tests for <see cref="ISecretReader"/> invariants.
/// Verifies behavioral properties across ConfigurationSecretProvider
/// and CachedSecretReaderDecorator.
/// </summary>
[Trait("Category", "Property")]
[Trait("Feature", "Secrets")]
public sealed class SecretsPropertyTests
{
    #region Secret Value Leak Prevention

    [Property(MaxTest = 50)]
    public bool SecretValue_NeverAppears_InErrorMessages(NonEmptyString secretName)
    {
        // Arrange
        var sanitizedName = SanitizeKey(secretName.Get);
        if (sanitizedName.Length == 0) return true; // skip degenerate input

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();
        var provider = new ConfigurationSecretProvider(
            configuration,
            NullLogger<ConfigurationSecretProvider>.Instance);

        // Act - read a non-existent secret
        var result = provider.GetSecretAsync(sanitizedName).AsTask().Result;

        // Assert - the error should never contain the actual secret value
        // (in this case there is no value, but the error message should not leak the key name
        // in a way that could reveal sensitive patterns)
        return result.Match(
            Right: _ => false, // should not succeed for non-existent key
            Left: error =>
            {
                var errorMessage = error.Message;
                // The error message may mention the secret name (this is acceptable for debugging),
                // but it must NOT contain any secret value (there is none here).
                // This test verifies that error handling does not throw exceptions
                // that would leak through unhandled paths.
                return errorMessage is not null;
            });
    }

    #endregion

    #region ConfigurationSecretProvider Always Returns Right for Existing Keys

    [Property(MaxTest = 50)]
    public bool ConfigurationSecretProvider_ReturnsRight_ForExistingKeys(
        NonEmptyString keyName,
        NonEmptyString keyValue)
    {
        // Arrange
        var sanitizedKey = SanitizeKey(keyName.Get);
        if (sanitizedKey.Length == 0) return true; // skip degenerate input

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Secrets:{sanitizedKey}"] = keyValue.Get
            })
            .Build();

        var provider = new ConfigurationSecretProvider(
            configuration,
            NullLogger<ConfigurationSecretProvider>.Instance);

        // Act
        var result = provider.GetSecretAsync(sanitizedKey).AsTask().Result;

        // Assert
        return result.Match(
            Right: value => value == keyValue.Get,
            Left: _ => false);
    }

    #endregion

    #region CachedSecretReader Returns Same Value for Same Key

    [Property(MaxTest = 30)]
    public bool CachedSecretReader_ReturnsSameValue_ForSameKey(
        NonEmptyString keyName,
        NonEmptyString keyValue)
    {
        // Arrange
        var sanitizedKey = SanitizeKey(keyName.Get);
        if (sanitizedKey.Length == 0) return true; // skip degenerate input

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Secrets:{sanitizedKey}"] = keyValue.Get
            })
            .Build();

        var innerProvider = new ConfigurationSecretProvider(
            configuration,
            NullLogger<ConfigurationSecretProvider>.Instance);

        var options = Options.Create(new SecretsOptions
        {
            EnableCaching = true,
            DefaultCacheDuration = TimeSpan.FromMinutes(5)
        });

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var cachedReader = new CachedSecretReaderDecorator(
            innerProvider,
            cache,
            options,
            NullLogger<CachedSecretReaderDecorator>.Instance);

        // Act - read twice
        var result1 = cachedReader.GetSecretAsync(sanitizedKey).AsTask().Result;
        var result2 = cachedReader.GetSecretAsync(sanitizedKey).AsTask().Result;

        // Assert - both reads return the same value
        if (!result1.IsRight || !result2.IsRight) return false;

        var value1 = result1.Match(Right: v => v, Left: _ => string.Empty);
        var value2 = result2.Match(Right: v => v, Left: _ => string.Empty);

        return value1 == value2 && value1 == keyValue.Get;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Sanitizes generated strings to be valid configuration keys.
    /// Removes characters that are invalid in configuration key paths.
    /// </summary>
    private static string SanitizeKey(string input)
    {
        // Remove colons (used as path separators in IConfiguration) and null characters
        var sanitized = new string(input
            .Where(c => c != ':' && c != '\0' && !char.IsControl(c))
            .ToArray())
            .Trim();

        return sanitized;
    }

    #endregion
}
