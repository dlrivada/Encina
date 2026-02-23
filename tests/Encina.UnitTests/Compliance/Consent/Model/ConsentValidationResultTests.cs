using Encina.Compliance.Consent;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.Consent.Model;

/// <summary>
/// Unit tests for <see cref="ConsentValidationResult"/>.
/// </summary>
public class ConsentValidationResultTests
{
    #region Valid Factory Method

    [Fact]
    public void Valid_ShouldReturnValidResultWithNoErrorsOrWarnings()
    {
        // Act
        var result = ConsentValidationResult.Valid();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.MissingPurposes.Should().BeEmpty();
    }

    #endregion

    #region ValidWithWarnings Factory Method

    [Fact]
    public void ValidWithWarnings_ShouldReturnValidResultWithWarnings()
    {
        // Act
        var result = ConsentValidationResult.ValidWithWarnings("Consent expiring soon", "Version outdated");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(2);
        result.Warnings.Should().Contain("Consent expiring soon");
        result.Warnings.Should().Contain("Version outdated");
        result.MissingPurposes.Should().BeEmpty();
    }

    [Fact]
    public void ValidWithWarnings_NoArgs_ShouldReturnValidWithEmptyWarnings()
    {
        // Act
        var result = ConsentValidationResult.ValidWithWarnings();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }

    #endregion

    #region Invalid Factory Method (Errors + MissingPurposes)

    [Fact]
    public void Invalid_WithErrorsAndMissingPurposes_ShouldReturnInvalidResult()
    {
        // Arrange
        var errors = new List<string> { "Missing consent for marketing" };
        var missingPurposes = new List<string> { ConsentPurposes.Marketing };

        // Act
        var result = ConsentValidationResult.Invalid(errors, missingPurposes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain("Missing consent for marketing");
        result.Warnings.Should().BeEmpty();
        result.MissingPurposes.Should().HaveCount(1);
        result.MissingPurposes.Should().Contain(ConsentPurposes.Marketing);
    }

    [Fact]
    public void Invalid_WithMultipleErrors_ShouldContainAllErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var missingPurposes = new List<string> { "purpose-a", "purpose-b" };

        // Act
        var result = ConsentValidationResult.Invalid(errors, missingPurposes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.MissingPurposes.Should().HaveCount(2);
    }

    #endregion

    #region Invalid Factory Method (Errors + Warnings + MissingPurposes)

    [Fact]
    public void Invalid_WithErrorsWarningsAndMissingPurposes_ShouldReturnInvalidResultWithAll()
    {
        // Arrange
        var errors = new List<string> { "Missing consent" };
        var warnings = new List<string> { "Version nearing end-of-life" };
        var missingPurposes = new List<string> { ConsentPurposes.Analytics };

        // Act
        var result = ConsentValidationResult.Invalid(errors, warnings, missingPurposes);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Warnings.Should().HaveCount(1);
        result.Warnings.Should().Contain("Version nearing end-of-life");
        result.MissingPurposes.Should().HaveCount(1);
    }

    #endregion
}
