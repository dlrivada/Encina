using Encina.Compliance.Consent;
using Shouldly;

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
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
        result.MissingPurposes.ShouldBeEmpty();
    }

    #endregion

    #region ValidWithWarnings Factory Method

    [Fact]
    public void ValidWithWarnings_ShouldReturnValidResultWithWarnings()
    {
        // Act
        var result = ConsentValidationResult.ValidWithWarnings("Consent expiring soon", "Version outdated");

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Warnings.Count.ShouldBe(2);
        result.Warnings.ShouldContain("Consent expiring soon");
        result.Warnings.ShouldContain("Version outdated");
        result.MissingPurposes.ShouldBeEmpty();
    }

    [Fact]
    public void ValidWithWarnings_NoArgs_ShouldReturnValidWithEmptyWarnings()
    {
        // Act
        var result = ConsentValidationResult.ValidWithWarnings();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Warnings.ShouldBeEmpty();
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
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.ShouldContain("Missing consent for marketing");
        result.Warnings.ShouldBeEmpty();
        result.MissingPurposes.Count.ShouldBe(1);
        result.MissingPurposes.ShouldContain(ConsentPurposes.Marketing);
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
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
        result.MissingPurposes.Count.ShouldBe(2);
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
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Warnings.Count.ShouldBe(1);
        result.Warnings.ShouldContain("Version nearing end-of-life");
        result.MissingPurposes.Count.ShouldBe(1);
    }

    #endregion
}
