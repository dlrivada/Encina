using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Property-based tests for <see cref="TIAAggregate"/> verifying lifecycle
/// invariants across randomized inputs using FsCheck.
/// </summary>
public class TIAAggregatePropertyTests
{
    #region Factory Invariants

    /// <summary>
    /// Invariant: Create preserves source and destination country codes.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Create_PreservesCountryCodes(NonEmptyString source, NonEmptyString dest)
    {
        var sourceCode = source.Get.Trim();
        var destCode = dest.Get.Trim();

        if (string.IsNullOrWhiteSpace(sourceCode) || string.IsNullOrWhiteSpace(destCode))
        {
            return true; // Skip whitespace-only inputs
        }

        var aggregate = TIAAggregate.Create(
            Guid.NewGuid(), sourceCode, destCode, "personal-data", "assessor-1");

        return aggregate.SourceCountryCode == sourceCode &&
               aggregate.DestinationCountryCode == destCode &&
               aggregate.Status == TIAStatus.Draft;
    }

    #endregion

    #region Risk Assessment Invariants

    /// <summary>
    /// Invariant: AssessRisk with a valid score preserves the score in the aggregate.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AssessRisk_ValidScore_PreservesScore()
    {
        var scoreGen = Arb.From(Gen.Choose(0, 100));

        return Prop.ForAll(scoreGen, intScore =>
        {
            var riskScore = intScore / 100.0;
            var aggregate = TIAAggregate.Create(
                Guid.NewGuid(), "DE", "US", "personal-data", "assessor-1");

            aggregate.AssessRisk(riskScore, "Test findings", "assessor-1");

            return aggregate.RiskScore.HasValue &&
                   Math.Abs(aggregate.RiskScore.Value - riskScore) < 1e-10 &&
                   aggregate.Status == TIAStatus.InProgress;
        });
    }

    #endregion

    #region Lifecycle Invariants

    /// <summary>
    /// Invariant: The full TIA lifecycle progresses monotonically through
    /// Draft -> InProgress -> PendingDPOReview -> Completed.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool FullLifecycle_StatusProgression_IsMonotonic()
    {
        var aggregate = TIAAggregate.Create(
            Guid.NewGuid(), "DE", "CN", "personal-data", "assessor-1");

        // Draft
        var statusAfterCreate = aggregate.Status;
        if (statusAfterCreate != TIAStatus.Draft)
        {
            return false;
        }

        // Draft -> InProgress (via risk assessment)
        aggregate.AssessRisk(0.7, "High risk country", "assessor-1");
        var statusAfterAssess = aggregate.Status;
        if (statusAfterAssess != TIAStatus.InProgress)
        {
            return false;
        }

        // InProgress -> PendingDPOReview
        aggregate.SubmitForDPOReview("assessor-1");
        var statusAfterSubmit = aggregate.Status;
        if (statusAfterSubmit != TIAStatus.PendingDPOReview)
        {
            return false;
        }

        // PendingDPOReview -> Completed (via DPO approval + complete)
        aggregate.ApproveDPOReview("dpo-1");
        aggregate.Complete();
        var statusAfterComplete = aggregate.Status;
        if (statusAfterComplete != TIAStatus.Completed)
        {
            return false;
        }

        // Completed -> Expired
        aggregate.Expire();
        var statusAfterExpire = aggregate.Status;

        return statusAfterExpire == TIAStatus.Expired;
    }

    #endregion
}
