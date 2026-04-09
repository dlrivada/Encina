#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="TIAAggregate"/> verifying argument validation
/// and state transition guards on all factory and instance methods.
/// </summary>
public class TIAAggregateGuardTests
{
    #region Create Guards

    [Fact]
    public void Create_NullSourceCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), null!, "US", "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Create_EmptySourceCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "", "US", "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Create_WhitespaceSourceCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "   ", "US", "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Create_NullDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", null!, "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Create_EmptyDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "", "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Create_WhitespaceDestinationCountryCode_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "   ", "personal-data", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Create_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", null!, "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "   ", "user1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_NullCreatedBy_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("createdBy");
    }

    [Fact]
    public void Create_EmptyCreatedBy_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("createdBy");
    }

    [Fact]
    public void Create_WhitespaceCreatedBy_ThrowsArgumentException()
    {
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("createdBy");
    }

    [Fact]
    public void Create_ValidParameters_ReturnsAggregate()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", "user1");

        aggregate.ShouldNotBeNull();
        aggregate.Status.ShouldBe(TIAStatus.Draft);
        aggregate.SourceCountryCode.ShouldBe("DE");
        aggregate.DestinationCountryCode.ShouldBe("US");
        aggregate.DataCategory.ShouldBe("personal-data");
    }

    #endregion

    #region AssessRisk Guards

    [Fact]
    public void AssessRisk_NullAssessorId_ThrowsArgumentException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessorId");
    }

    [Fact]
    public void AssessRisk_EmptyAssessorId_ThrowsArgumentException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessorId");
    }

    [Fact]
    public void AssessRisk_WhitespaceAssessorId_ThrowsArgumentException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("assessorId");
    }

    [Fact]
    public void AssessRisk_RiskScoreBelowZero_ThrowsArgumentOutOfRangeException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.AssessRisk(-0.1, "findings", "assessor1");

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("riskScore");
    }

    [Fact]
    public void AssessRisk_RiskScoreAboveOne_ThrowsArgumentOutOfRangeException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.AssessRisk(1.1, "findings", "assessor1");

        Should.Throw<ArgumentOutOfRangeException>(act).ParamName.ShouldBe("riskScore");
    }

    [Fact]
    public void AssessRisk_StatusCompleted_ThrowsInvalidOperationException()
    {
        var sut = CreateCompletedAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", "assessor1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AssessRisk_StatusPendingDPOReview_ThrowsInvalidOperationException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", "assessor1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AssessRisk_StatusExpired_ThrowsInvalidOperationException()
    {
        var sut = CreateExpiredAggregate();

        var act = () => sut.AssessRisk(0.5, "findings", "assessor1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AssessRisk_ValidFromDraft_Succeeds()
    {
        var sut = CreateDraftAggregate();

        sut.AssessRisk(0.5, "findings", "assessor1");

        sut.Status.ShouldBe(TIAStatus.InProgress);
        sut.RiskScore.ShouldBe(0.5);
    }

    [Fact]
    public void AssessRisk_ValidFromInProgress_Succeeds()
    {
        var sut = CreateInProgressAggregate();

        sut.AssessRisk(0.7, "updated findings", "assessor2");

        sut.Status.ShouldBe(TIAStatus.InProgress);
        sut.RiskScore.ShouldBe(0.7);
    }

    #endregion

    #region RequireSupplementaryMeasure Guards

    [Fact]
    public void RequireSupplementaryMeasure_NullDescription_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void RequireSupplementaryMeasure_EmptyDescription_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void RequireSupplementaryMeasure_WhitespaceDescription_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void RequireSupplementaryMeasure_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RequireSupplementaryMeasure_StatusCompleted_ThrowsInvalidOperationException()
    {
        var sut = CreateCompletedAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RequireSupplementaryMeasure_StatusPendingDPOReview_ThrowsInvalidOperationException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RequireSupplementaryMeasure_ValidFromInProgress_AddsMeasure()
    {
        var sut = CreateInProgressAggregate();

        sut.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption at rest");

        sut.RequiredSupplementaryMeasures.Count.ShouldBe(1);
    }

    #endregion

    #region SubmitForDPOReview Guards

    [Fact]
    public void SubmitForDPOReview_NullSubmittedBy_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.SubmitForDPOReview(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedBy");
    }

    [Fact]
    public void SubmitForDPOReview_EmptySubmittedBy_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.SubmitForDPOReview("");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedBy");
    }

    [Fact]
    public void SubmitForDPOReview_WhitespaceSubmittedBy_ThrowsArgumentException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.SubmitForDPOReview("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("submittedBy");
    }

    [Fact]
    public void SubmitForDPOReview_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.SubmitForDPOReview("user1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void SubmitForDPOReview_StatusCompleted_ThrowsInvalidOperationException()
    {
        var sut = CreateCompletedAggregate();

        var act = () => sut.SubmitForDPOReview("user1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void SubmitForDPOReview_ValidFromInProgress_TransitionsToPendingDPOReview()
    {
        var sut = CreateInProgressAggregate();

        sut.SubmitForDPOReview("user1");

        sut.Status.ShouldBe(TIAStatus.PendingDPOReview);
    }

    #endregion

    #region ApproveDPOReview Guards

    [Fact]
    public void ApproveDPOReview_NullReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.ApproveDPOReview(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void ApproveDPOReview_EmptyReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.ApproveDPOReview("");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void ApproveDPOReview_WhitespaceReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.ApproveDPOReview("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void ApproveDPOReview_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.ApproveDPOReview("dpo1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void ApproveDPOReview_StatusInProgress_ThrowsInvalidOperationException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.ApproveDPOReview("dpo1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void ApproveDPOReview_StatusCompleted_ThrowsInvalidOperationException()
    {
        var sut = CreateCompletedAggregate();

        var act = () => sut.ApproveDPOReview("dpo1");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void ApproveDPOReview_ValidFromPendingDPOReview_SetsReviewTimestamp()
    {
        var sut = CreatePendingDPOReviewAggregate();

        sut.ApproveDPOReview("dpo1");

        sut.DPOReviewedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region RejectDPOReview Guards

    [Fact]
    public void RejectDPOReview_NullReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview(null!, "reason");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void RejectDPOReview_EmptyReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview("", "reason");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void RejectDPOReview_WhitespaceReviewedBy_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview("   ", "reason");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reviewedBy");
    }

    [Fact]
    public void RejectDPOReview_NullReason_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview("dpo1", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void RejectDPOReview_EmptyReason_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview("dpo1", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void RejectDPOReview_WhitespaceReason_ThrowsArgumentException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.RejectDPOReview("dpo1", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void RejectDPOReview_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.RejectDPOReview("dpo1", "Insufficient analysis");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RejectDPOReview_StatusInProgress_ThrowsInvalidOperationException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.RejectDPOReview("dpo1", "Insufficient analysis");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RejectDPOReview_ValidFromPendingDPOReview_ReturnsToInProgress()
    {
        var sut = CreatePendingDPOReviewAggregate();

        sut.RejectDPOReview("dpo1", "Needs more analysis");

        sut.Status.ShouldBe(TIAStatus.InProgress);
    }

    #endregion

    #region Complete Guards

    [Fact]
    public void Complete_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.Complete();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Complete_StatusInProgress_ThrowsInvalidOperationException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.Complete();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Complete_StatusCompleted_ThrowsInvalidOperationException()
    {
        var sut = CreateCompletedAggregate();

        var act = () => sut.Complete();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Complete_ValidFromPendingDPOReview_TransitionsToCompleted()
    {
        var sut = CreatePendingDPOReviewAggregate();

        sut.Complete();

        sut.Status.ShouldBe(TIAStatus.Completed);
        sut.CompletedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Expire Guards

    [Fact]
    public void Expire_StatusDraft_ThrowsInvalidOperationException()
    {
        var sut = CreateDraftAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_StatusInProgress_ThrowsInvalidOperationException()
    {
        var sut = CreateInProgressAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_StatusPendingDPOReview_ThrowsInvalidOperationException()
    {
        var sut = CreatePendingDPOReviewAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_ValidFromCompleted_TransitionsToExpired()
    {
        var sut = CreateCompletedAggregate();

        sut.Expire();

        sut.Status.ShouldBe(TIAStatus.Expired);
    }

    #endregion

    #region Helpers

    private static TIAAggregate CreateDraftAggregate() =>
        TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", "user1");

    private static TIAAggregate CreateInProgressAggregate()
    {
        var agg = CreateDraftAggregate();
        agg.AssessRisk(0.5, "Some findings", "assessor1");
        return agg;
    }

    private static TIAAggregate CreatePendingDPOReviewAggregate()
    {
        var agg = CreateInProgressAggregate();
        agg.SubmitForDPOReview("user1");
        return agg;
    }

    private static TIAAggregate CreateCompletedAggregate()
    {
        var agg = CreatePendingDPOReviewAggregate();
        agg.Complete();
        return agg;
    }

    private static TIAAggregate CreateExpiredAggregate()
    {
        var agg = CreateCompletedAggregate();
        agg.Expire();
        return agg;
    }

    #endregion
}
