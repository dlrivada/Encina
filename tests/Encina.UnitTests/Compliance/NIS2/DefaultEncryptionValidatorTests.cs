#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Compliance.NIS2;
using Encina.Security.Encryption.Abstractions;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="DefaultEncryptionValidator"/>.
/// </summary>
public class DefaultEncryptionValidatorTests
{
    private static DefaultEncryptionValidator CreateSut(
        NIS2Options? options = null,
        IServiceProvider? serviceProvider = null) =>
        new(Options.Create(options ?? new NIS2Options()),
            serviceProvider ?? new ServiceCollection().BuildServiceProvider(),
            NullLogger<DefaultEncryptionValidator>.Instance);

    private static NIS2Options CreateOptionsWithEncryption(
        IEnumerable<string>? dataCategories = null,
        IEnumerable<string>? endpoints = null)
    {
        var options = new NIS2Options();

        if (dataCategories is not null)
        {
            foreach (var category in dataCategories)
            {
                options.EncryptedDataCategories.Add(category);
            }
        }

        if (endpoints is not null)
        {
            foreach (var endpoint in endpoints)
            {
                options.EncryptedEndpoints.Add(endpoint);
            }
        }

        return options;
    }

    #region IsDataEncryptedAtRestAsync

    [Fact]
    public async Task IsDataEncryptedAtRestAsync_RegisteredCategory_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptionsWithEncryption(dataCategories: ["PII", "Financial"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.IsDataEncryptedAtRestAsync("PII");

        // Assert
        result.IsRight.Should().BeTrue();
        var isEncrypted = result.Match(r => r, _ => false);
        isEncrypted.Should().BeTrue();
    }

    [Fact]
    public async Task IsDataEncryptedAtRestAsync_UnregisteredCategory_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptionsWithEncryption(dataCategories: ["PII"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.IsDataEncryptedAtRestAsync("Marketing");

        // Assert
        result.IsRight.Should().BeTrue();
        var isEncrypted = result.Match(r => r, _ => true);
        isEncrypted.Should().BeFalse();
    }

    #endregion

    #region IsDataEncryptedInTransitAsync

    [Fact]
    public async Task IsDataEncryptedInTransitAsync_RegisteredEndpoint_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptionsWithEncryption(endpoints: ["https://api.example.com", "https://payments.example.com"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.IsDataEncryptedInTransitAsync("https://api.example.com");

        // Assert
        result.IsRight.Should().BeTrue();
        var isEncrypted = result.Match(r => r, _ => false);
        isEncrypted.Should().BeTrue();
    }

    [Fact]
    public async Task IsDataEncryptedInTransitAsync_UnregisteredEndpoint_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptionsWithEncryption(endpoints: ["https://api.example.com"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.IsDataEncryptedInTransitAsync("https://unregistered.example.com");

        // Assert
        result.IsRight.Should().BeTrue();
        var isEncrypted = result.Match(r => r, _ => true);
        isEncrypted.Should().BeFalse();
    }

    #endregion

    #region ValidateEncryptionPolicyAsync

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_HasDataCategories_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptionsWithEncryption(dataCategories: ["PII", "Financial"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => false);
        hasPolicy.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_NoConfiguration_ShouldReturnFalse()
    {
        // Arrange — empty options, no categories or endpoints
        var sut = CreateSut();

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.Should().BeFalse();
    }

    #endregion

    #region ValidateEncryptionPolicyAsync — IKeyProvider Integration

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_KeyProviderWithActiveKey_ShouldReturnTrue()
    {
        // Arrange — IKeyProvider registered and returns an active key
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Right<EncinaError, string>("active-key-id")));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IKeyProvider)).Returns(keyProvider);

        var options = CreateOptionsWithEncryption(dataCategories: ["PII"]);
        var sut = CreateSut(options, sp);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert — has policy AND active key → true
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => false);
        hasPolicy.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_KeyProviderWithNoActiveKey_ShouldReturnFalse()
    {
        // Arrange — IKeyProvider registered but returns empty key id (no active key)
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Right<EncinaError, string>(string.Empty)));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IKeyProvider)).Returns(keyProvider);

        var options = CreateOptionsWithEncryption(dataCategories: ["PII"]);
        var sut = CreateSut(options, sp);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert — config says encrypted BUT infrastructure has no active key → false
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.Should().BeFalse("IKeyProvider exists but has no active key — infrastructure mismatch");
    }

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_KeyProviderReturnsError_ShouldReturnFalse()
    {
        // Arrange — IKeyProvider registered but GetCurrentKeyIdAsync returns Left (error)
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, string>>(
                Left<EncinaError, string>(EncinaError.New("Key vault unavailable"))));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IKeyProvider)).Returns(keyProvider);

        var options = CreateOptionsWithEncryption(dataCategories: ["PII"]);
        var sut = CreateSut(options, sp);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert — IKeyProvider error treated as no active key
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.Should().BeFalse("IKeyProvider returned error — treated as no active key");
    }

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_KeyProviderThrows_ShouldReturnFalseViaResilience()
    {
        // Arrange — IKeyProvider throws (resilience should catch)
        var keyProvider = Substitute.For<IKeyProvider>();
        keyProvider.GetCurrentKeyIdAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Key vault connection failed"));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IKeyProvider)).Returns(keyProvider);

        var options = CreateOptionsWithEncryption(dataCategories: ["PII"]);
        var sut = CreateSut(options, sp);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert — resilience catches exception, returns fallback (false)
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => true);
        hasPolicy.Should().BeFalse("exception in IKeyProvider should be caught by resilience helper");
    }

    [Fact]
    public async Task ValidateEncryptionPolicyAsync_NoKeyProvider_WithCategories_ShouldReturnTrue()
    {
        // Arrange — no IKeyProvider registered but config has categories
        var options = CreateOptionsWithEncryption(dataCategories: ["PII", "Financial"]);
        var sut = CreateSut(options);

        // Act
        var result = await sut.ValidateEncryptionPolicyAsync();

        // Assert — config-only validation is sufficient when no IKeyProvider
        result.IsRight.Should().BeTrue();
        var hasPolicy = result.Match(r => r, _ => false);
        hasPolicy.Should().BeTrue("no IKeyProvider means config-only validation");
    }

    #endregion
}
