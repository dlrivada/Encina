#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer;

using Shouldly;

using Microsoft.Extensions.Options;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Services;

public class CrossBorderTransferOptionsValidatorTests
{
    private readonly CrossBorderTransferOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = new CrossBorderTransferOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TIARiskThreshold_BelowZero_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            TIARiskThreshold = -0.1
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("TIARiskThreshold");
    }

    [Fact]
    public void Validate_TIARiskThreshold_AboveOne_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            TIARiskThreshold = 1.5
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("TIARiskThreshold");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Validate_TIARiskThreshold_AtBoundaries_ReturnsSuccess(double threshold)
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            TIARiskThreshold = threshold
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_CacheTTLMinutes_Zero_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            CacheTTLMinutes = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("CacheTTLMinutes");
    }

    [Fact]
    public void Validate_CacheTTLMinutes_Negative_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            CacheTTLMinutes = -5
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("CacheTTLMinutes");
    }

    [Fact]
    public void Validate_DefaultTIAExpirationDays_ZeroWhenSet_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultTIAExpirationDays = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultTIAExpirationDays");
    }

    [Fact]
    public void Validate_DefaultTIAExpirationDays_NullAllowed_ReturnsSuccess()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultTIAExpirationDays = null
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DefaultSCCExpirationDays_NegativeWhenSet_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultSCCExpirationDays = -10
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultSCCExpirationDays");
    }

    [Fact]
    public void Validate_DefaultTransferExpirationDays_ZeroWhenSet_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultTransferExpirationDays = 0
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultTransferExpirationDays");
    }

    [Fact]
    public void Validate_DefaultSourceCountryCode_Empty_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultSourceCountryCode = ""
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultSourceCountryCode");
    }

    [Fact]
    public void Validate_DefaultSourceCountryCode_Whitespace_ReturnsFail()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            DefaultSourceCountryCode = "   "
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("DefaultSourceCountryCode");
    }

    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNull()
    {
        // Act
        var act = () => _validator.Validate(null, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Validate_MultipleFailures_ReturnsAllErrors()
    {
        // Arrange
        var options = new CrossBorderTransferOptions
        {
            TIARiskThreshold = 2.0,
            CacheTTLMinutes = -1,
            DefaultSourceCountryCode = "",
            DefaultTIAExpirationDays = -10
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("TIARiskThreshold");
        result.FailureMessage!.ShouldContain("CacheTTLMinutes");
        result.FailureMessage!.ShouldContain("DefaultSourceCountryCode");
        result.FailureMessage!.ShouldContain("DefaultTIAExpirationDays");
    }
}
