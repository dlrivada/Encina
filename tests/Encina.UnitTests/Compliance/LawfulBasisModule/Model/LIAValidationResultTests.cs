using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Model;

/// <summary>
/// Unit tests for <see cref="LIAValidationResult"/>.
/// </summary>
public class LIAValidationResultTests
{
    [Fact]
    public void Approved_CreatesValidResult()
    {
        var result = LIAValidationResult.Approved();

        result.IsValid.ShouldBeTrue();
        result.Outcome.ShouldBe(LIAOutcome.Approved);
        result.RejectionReason.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }

    [Fact]
    public void Rejected_WithoutReason_CreatesInvalidResult()
    {
        var result = LIAValidationResult.Rejected();

        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.Rejected);
        result.RejectionReason.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }

    [Fact]
    public void Rejected_WithReason_StoresReason()
    {
        var result = LIAValidationResult.Rejected("Insufficient legitimate interest");

        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.Rejected);
        result.RejectionReason.ShouldBe("Insufficient legitimate interest");
        result.RequiresReview.ShouldBeFalse();
    }

    [Fact]
    public void PendingReview_CreatesInvalidResult_WithRequiresReviewTrue()
    {
        var result = LIAValidationResult.PendingReview();

        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBe(LIAOutcome.RequiresReview);
        result.RequiresReview.ShouldBeTrue();
        result.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void NotFound_CreatesInvalidResult_WithNoOutcome()
    {
        var result = LIAValidationResult.NotFound();

        result.IsValid.ShouldBeFalse();
        result.Outcome.ShouldBeNull();
        result.RejectionReason.ShouldBeNull();
        result.RequiresReview.ShouldBeFalse();
    }
}
