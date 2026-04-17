using Encina.Marten.GDPR;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShreddingOptionsValidatorTests
{
    private readonly CryptoShreddingOptionsValidator _sut = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        // Arrange
        var options = new CryptoShreddingOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_InvalidKeyRotationDays_Fails(int days)
    {
        // Arrange
        var options = new CryptoShreddingOptions { KeyRotationDays = days };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("KeyRotationDays");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_InvalidAnonymizedPlaceholder_Fails(string? placeholder)
    {
        // Arrange
        var options = new CryptoShreddingOptions { AnonymizedPlaceholder = placeholder! };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("AnonymizedPlaceholder");
    }

    [Fact]
    public void Validate_MultipleErrors_ReportsAll()
    {
        // Arrange
        var options = new CryptoShreddingOptions
        {
            KeyRotationDays = -1,
            AnonymizedPlaceholder = ""
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldNotBeNull();
        result.FailureMessage.ShouldContain("KeyRotationDays");
        result.FailureMessage.ShouldContain("AnonymizedPlaceholder");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void Validate_ValidKeyRotationDays_Succeeds(int days)
    {
        // Arrange
        var options = new CryptoShreddingOptions { KeyRotationDays = days };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CustomPlaceholder_Succeeds()
    {
        // Arrange
        var options = new CryptoShreddingOptions { AnonymizedPlaceholder = "***" };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }
}
