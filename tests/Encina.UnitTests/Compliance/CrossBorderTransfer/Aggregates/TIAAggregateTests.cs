#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Aggregates;

public class TIAAggregateTests
{
    [Fact]
    public void Create_ValidParams_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var tia = TIAAggregate.Create(id, "DE", "US", "personal-data", "user-1", "tenant-1", "module-1");

        // Assert
        tia.Id.ShouldBe(id);
        tia.SourceCountryCode.ShouldBe("DE");
        tia.DestinationCountryCode.ShouldBe("US");
        tia.DataCategory.ShouldBe("personal-data");
        tia.Status.ShouldBe(TIAStatus.Draft);
        tia.TenantId.ShouldBe("tenant-1");
        tia.ModuleId.ShouldBe("module-1");
        tia.RiskScore.ShouldBeNull();
        tia.Findings.ShouldBeNull();
        tia.AssessorId.ShouldBeNull();
        tia.DPOReviewedAtUtc.ShouldBeNull();
        tia.CompletedAtUtc.ShouldBeNull();
        tia.RequiredSupplementaryMeasures.ShouldBeEmpty();
    }

    [Fact]
    public void Create_NullSourceCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), null!, "US", "personal-data", "user-1");

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("sourceCountryCode");
    }

    [Fact]
    public void Create_NullDestinationCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", null!, "personal-data", "user-1");

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("destinationCountryCode");
    }

    [Fact]
    public void Create_NullDataCategory_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", null!, "user-1");

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Create_NullCreatedBy_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", null!);

        // Assert
        Should.Throw<ArgumentException>(act)
            .ParamName.ShouldBe("createdBy");
    }

    [Fact]
    public void AssessRisk_DraftStatus_SetsRiskScoreAndStatus()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        tia.AssessRisk(0.75, "High surveillance risk", "assessor-1");

        // Assert
        tia.RiskScore.ShouldBe(0.75);
        tia.Findings.ShouldBe("High surveillance risk");
        tia.AssessorId.ShouldBe("assessor-1");
        tia.Status.ShouldBe(TIAStatus.InProgress);
    }

    [Fact]
    public void AssessRisk_CompletedStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateCompletedTIA();

        // Act
        var act = () => tia.AssessRisk(0.5, "Re-assessment", "assessor-2");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AssessRisk_ScoreAboveOne_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.AssessRisk(1.1, "findings", "assessor-1");

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("riskScore");
    }

    [Fact]
    public void AssessRisk_ScoreBelowZero_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.AssessRisk(-0.1, "findings", "assessor-1");

        // Assert
        Should.Throw<ArgumentOutOfRangeException>(act)
            .ParamName.ShouldBe("riskScore");
    }

    [Fact]
    public void RequireSupplementaryMeasure_InProgressStatus_AddsMeasure()
    {
        // Arrange
        var tia = CreateInProgressTIA();
        var measureId = Guid.NewGuid();

        // Act
        tia.RequireSupplementaryMeasure(measureId, SupplementaryMeasureType.Technical, "End-to-end encryption required");

        // Assert
        tia.RequiredSupplementaryMeasures.Count.ShouldBe(1);
        tia.RequiredSupplementaryMeasures[0].Id.ShouldBe(measureId);
        tia.RequiredSupplementaryMeasures[0].Type.ShouldBe(SupplementaryMeasureType.Technical);
        tia.RequiredSupplementaryMeasures[0].Description.ShouldBe("End-to-end encryption required");
        tia.RequiredSupplementaryMeasures[0].IsImplemented.ShouldBeFalse();
    }

    [Fact]
    public void RequireSupplementaryMeasure_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Audit rights");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void SubmitForDPOReview_InProgressStatus_ChangesPendingDPOReview()
    {
        // Arrange
        var tia = CreateInProgressTIA();

        // Act
        tia.SubmitForDPOReview("submitter-1");

        // Assert
        tia.Status.ShouldBe(TIAStatus.PendingDPOReview);
    }

    [Fact]
    public void SubmitForDPOReview_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.SubmitForDPOReview("submitter-1");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void ApproveDPOReview_PendingDPOReview_SetsDPOReviewedAtUtc()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.ApproveDPOReview("dpo-1");

        // Assert
        tia.DPOReviewedAtUtc.ShouldNotBeNull();
        tia.DPOReviewedAtUtc!.Value.ShouldBeInRange(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(5), DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ApproveDPOReview_InProgressStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateInProgressTIA();

        // Act
        var act = () => tia.ApproveDPOReview("dpo-1");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void RejectDPOReview_PendingDPOReview_ReturnsToInProgress()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.RejectDPOReview("dpo-1", "Insufficient supplementary measures");

        // Assert
        tia.Status.ShouldBe(TIAStatus.InProgress);
        tia.DPOReviewedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Complete_PendingDPOReview_SetsCompleted()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.Complete();

        // Assert
        tia.Status.ShouldBe(TIAStatus.Completed);
        tia.CompletedAtUtc.ShouldNotBeNull();
        tia.CompletedAtUtc!.Value.ShouldBeInRange(DateTimeOffset.UtcNow - TimeSpan.FromSeconds(5), DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Expire_CompletedStatus_SetsExpired()
    {
        // Arrange
        var tia = CreateCompletedTIA();

        // Act
        tia.Expire();

        // Assert
        tia.Status.ShouldBe(TIAStatus.Expired);
    }

    [Fact]
    public void Expire_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.Expire();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void FullLifecycle_DraftToCompleted_SuccessfulTransition()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act - Create (Draft)
        var tia = TIAAggregate.Create(id, "DE", "US", "personal-data", "user-1", "tenant-1", "module-1");
        tia.Status.ShouldBe(TIAStatus.Draft);

        // Act - Assess risk (InProgress)
        tia.AssessRisk(0.8, "High risk due to FISA 702", "assessor-1");
        tia.Status.ShouldBe(TIAStatus.InProgress);

        // Act - Add supplementary measures
        tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "E2E encryption");
        tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Audit rights clause");
        tia.RequiredSupplementaryMeasures.Count.ShouldBe(2);

        // Act - Submit for DPO review (PendingDPOReview)
        tia.SubmitForDPOReview("submitter-1");
        tia.Status.ShouldBe(TIAStatus.PendingDPOReview);

        // Act - Approve DPO review
        tia.ApproveDPOReview("dpo-1");
        tia.DPOReviewedAtUtc.ShouldNotBeNull();

        // Act - Complete
        tia.Complete();
        tia.Status.ShouldBe(TIAStatus.Completed);
        tia.CompletedAtUtc.ShouldNotBeNull();

        // Assert - Final state
        tia.Id.ShouldBe(id);
        tia.SourceCountryCode.ShouldBe("DE");
        tia.DestinationCountryCode.ShouldBe("US");
        tia.DataCategory.ShouldBe("personal-data");
        tia.RiskScore.ShouldBe(0.8);
        tia.Findings.ShouldBe("High risk due to FISA 702");
        tia.AssessorId.ShouldBe("assessor-1");
        tia.TenantId.ShouldBe("tenant-1");
        tia.ModuleId.ShouldBe("module-1");
    }

    // --- Helper methods to advance aggregate to specific states ---

    private static TIAAggregate CreateDraftTIA()
    {
        return TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", "user-1");
    }

    private static TIAAggregate CreateInProgressTIA()
    {
        var tia = CreateDraftTIA();
        tia.AssessRisk(0.5, "Medium risk", "assessor-1");
        return tia;
    }

    private static TIAAggregate CreatePendingDPOReviewTIA()
    {
        var tia = CreateInProgressTIA();
        tia.SubmitForDPOReview("submitter-1");
        return tia;
    }

    private static TIAAggregate CreateCompletedTIA()
    {
        var tia = CreatePendingDPOReviewTIA();
        tia.Complete();
        return tia;
    }
}
