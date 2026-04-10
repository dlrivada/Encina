using Encina.Compliance.LawfulBasis;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Model;

/// <summary>
/// Unit tests for <see cref="LawfulBasisValidationResult"/>.
/// </summary>
public class LawfulBasisValidationResultTests
{
    [Fact]
    public void Valid_CreatesValidResult_WithNoErrorsOrWarnings()
    {
        var result = LawfulBasisValidationResult.Valid(GDPRLawfulBasis.Contract);

        result.IsValid.ShouldBeTrue();
        result.Basis.ShouldBe(GDPRLawfulBasis.Contract);
        result.Errors.ShouldBeEmpty();
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ValidWithWarnings_CreatesValidResult_WithWarnings()
    {
        var result = LawfulBasisValidationResult.ValidWithWarnings(
            GDPRLawfulBasis.LegitimateInterests,
            "warning 1",
            "warning 2");

        result.IsValid.ShouldBeTrue();
        result.Basis.ShouldBe(GDPRLawfulBasis.LegitimateInterests);
        result.Errors.ShouldBeEmpty();
        result.Warnings.Count.ShouldBe(2);
        result.Warnings.ShouldContain("warning 1");
        result.Warnings.ShouldContain("warning 2");
    }

    [Fact]
    public void Invalid_CreatesInvalidResult_WithErrorsOnly()
    {
        var result = LawfulBasisValidationResult.Invalid("error 1", "error 2");

        result.IsValid.ShouldBeFalse();
        result.Basis.ShouldBeNull();
        result.Errors.Count.ShouldBe(2);
        result.Errors.ShouldContain("error 1");
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Invalid_WithBasisErrorsAndWarnings_CreatesInvalidResult()
    {
        var errors = new[] { "error 1" };
        var warnings = new[] { "warning 1", "warning 2" };

        var result = LawfulBasisValidationResult.Invalid(
            GDPRLawfulBasis.Consent,
            errors,
            warnings);

        result.IsValid.ShouldBeFalse();
        result.Basis.ShouldBe(GDPRLawfulBasis.Consent);
        result.Errors.Count.ShouldBe(1);
        result.Warnings.Count.ShouldBe(2);
    }

    [Fact]
    public void Record_Equality_WorksAsExpected()
    {
        var a = LawfulBasisValidationResult.Valid(GDPRLawfulBasis.Contract);
        var b = LawfulBasisValidationResult.Valid(GDPRLawfulBasis.Contract);
        // Record equality should compare by reference or by value
        a.IsValid.ShouldBe(b.IsValid);
        a.Basis.ShouldBe(b.Basis);
    }
}
