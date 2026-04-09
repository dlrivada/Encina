using Encina.Compliance.NIS2;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.PropertyTests.Compliance.NIS2;

/// <summary>
/// Property-based tests for <see cref="DefaultEncryptionValidator"/> verifying encryption
/// validation invariants using FsCheck random data generation.
/// </summary>
public sealed class DefaultEncryptionValidatorPropertyTests
{
    private static DefaultEncryptionValidator CreateValidator(NIS2Options? options = null)
    {
        var opts = options ?? new NIS2Options();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        return new(
            Options.Create(opts),
            serviceProvider,
            NullLogger<DefaultEncryptionValidator>.Instance);
    }

    /// <summary>
    /// Invariant: A data category that IS in EncryptedDataCategories always returns true.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RegisteredCategory_AlwaysReturnsTrue(NonEmptyString category)
    {
        var options = new NIS2Options();
        options.EncryptedDataCategories.Add(category.Get);
        var validator = CreateValidator(options);

        var result = validator.IsDataEncryptedAtRestAsync(category.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isEncrypted => isEncrypted,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: A data category NOT in EncryptedDataCategories always returns false.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool UnregisteredCategory_AlwaysReturnsFalse(NonEmptyString category)
    {
        // Create options with NO categories
        var options = new NIS2Options();
        var validator = CreateValidator(options);

        var result = validator.IsDataEncryptedAtRestAsync(category.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isEncrypted => !isEncrypted,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: A registered endpoint always returns true for transit encryption.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool RegisteredEndpoint_AlwaysReturnsTrue(NonEmptyString endpoint)
    {
        var options = new NIS2Options();
        options.EncryptedEndpoints.Add(endpoint.Get);
        var validator = CreateValidator(options);

        var result = validator.IsDataEncryptedInTransitAsync(endpoint.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isEncrypted => isEncrypted,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: An unregistered endpoint always returns false for transit encryption.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool UnregisteredEndpoint_AlwaysReturnsFalse(NonEmptyString endpoint)
    {
        var options = new NIS2Options();
        var validator = CreateValidator(options);

        var result = validator.IsDataEncryptedInTransitAsync(endpoint.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isEncrypted => !isEncrypted,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: ValidateEncryptionPolicyAsync returns false when no categories or endpoints are configured.
    /// </summary>
    [Fact]
    public async Task ValidateEncryptionPolicy_NoConfig_ReturnsFalse()
    {
        var validator = CreateValidator();

        var result = await validator.ValidateEncryptionPolicyAsync();

        Assert.True(result.IsRight);
        result.IfRight(v => Assert.False(v));
    }

    /// <summary>
    /// Invariant: ValidateEncryptionPolicyAsync returns true when at least one category is configured
    /// (with no IKeyProvider registered, config-only validation suffices).
    /// </summary>
    [Property(MaxTest = 20)]
    public bool ValidateEncryptionPolicy_WithCategory_ReturnsTrue(NonEmptyString category)
    {
        var options = new NIS2Options();
        options.EncryptedDataCategories.Add(category.Get);
        var validator = CreateValidator(options);

        var result = validator.ValidateEncryptionPolicyAsync().AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isValid => isValid,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: ValidateEncryptionPolicyAsync returns true when at least one endpoint is configured
    /// (with no IKeyProvider registered, config-only validation suffices).
    /// </summary>
    [Property(MaxTest = 20)]
    public bool ValidateEncryptionPolicy_WithEndpoint_ReturnsTrue(NonEmptyString endpoint)
    {
        var options = new NIS2Options();
        options.EncryptedEndpoints.Add(endpoint.Get);
        var validator = CreateValidator(options);

        var result = validator.ValidateEncryptionPolicyAsync().AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isValid => isValid,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: Case-insensitive matching for data categories (EncryptedDataCategories
    /// uses StringComparer.OrdinalIgnoreCase).
    /// </summary>
    [Property(MaxTest = 20)]
    public bool CategoryMatching_IsCaseInsensitive(NonEmptyString category)
    {
        var options = new NIS2Options();
        options.EncryptedDataCategories.Add(category.Get.ToUpperInvariant());
        var validator = CreateValidator(options);

        var result = validator.IsDataEncryptedAtRestAsync(category.Get.ToLowerInvariant())
            .AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isEncrypted => isEncrypted,
            Left: _ => false);
    }
}
