using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="PrivacyByDesignOptionsValidator"/> (internal).
/// </summary>
public class PrivacyByDesignOptionsValidatorTests
{
    private readonly PrivacyByDesignOptionsValidator _sut = new();

    [Fact]
    public void Validate_ValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn,
            PrivacyLevel = PrivacyLevel.Standard,
            MinimizationScoreThreshold = 0.7
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DefaultOptions_ShouldSucceed()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_InvalidEnforcementMode_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)99
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("EnforcementMode");
        result.FailureMessage!.ShouldContain("99");
    }

    [Fact]
    public void Validate_InvalidPrivacyLevel_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            PrivacyLevel = (PrivacyLevel)42
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("PrivacyLevel");
        result.FailureMessage!.ShouldContain("42");
    }

    [Fact]
    public void Validate_MinimizationScoreThreshold_BelowZero_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = -0.1
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("MinimizationScoreThreshold");
        result.FailureMessage!.ShouldContain("0.0 and 1.0");
    }

    [Fact]
    public void Validate_MinimizationScoreThreshold_AboveOne_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = 1.1
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("MinimizationScoreThreshold");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Validate_MinimizationScoreThreshold_WithinRange_ShouldSucceed(double threshold)
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            MinimizationScoreThreshold = threshold
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_PurposeWithoutDescription_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", purpose =>
        {
            purpose.LegalBasis = "Contract";
        });

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Order Processing");
        result.FailureMessage!.ShouldContain("Description");
    }

    [Fact]
    public void Validate_PurposeWithoutLegalBasis_ShouldFail()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", purpose =>
        {
            purpose.Description = "Processing personal data for order fulfillment.";
        });

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Order Processing");
        result.FailureMessage!.ShouldContain("LegalBasis");
    }

    [Fact]
    public void Validate_PurposeWithBothFieldsMissing_ShouldReportBothFailures()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", _ => { });

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Description");
        result.FailureMessage!.ShouldContain("LegalBasis");
    }

    [Fact]
    public void Validate_ValidPurpose_ShouldSucceed()
    {
        // Arrange
        var options = new PrivacyByDesignOptions();
        options.AddPurpose("Order Processing", purpose =>
        {
            purpose.Description = "Processing personal data for order fulfillment.";
            purpose.LegalBasis = "Contract";
            purpose.AllowedFields.AddRange(["ProductId", "Quantity"]);
        });

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_MultipleInvalidSettings_ShouldReportAllFailures()
    {
        // Arrange
        var options = new PrivacyByDesignOptions
        {
            EnforcementMode = (PrivacyByDesignEnforcementMode)99,
            PrivacyLevel = (PrivacyLevel)42,
            MinimizationScoreThreshold = 1.5
        };

        // Act
        var result = _sut.Validate(null, options);

        // Assert
        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("EnforcementMode");
        result.FailureMessage!.ShouldContain("PrivacyLevel");
        result.FailureMessage!.ShouldContain("MinimizationScoreThreshold");
    }

    [Fact]
    public void Validate_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.Validate(null, null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }
}
