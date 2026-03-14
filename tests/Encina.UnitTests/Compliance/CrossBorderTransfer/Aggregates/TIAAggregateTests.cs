#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FluentAssertions;

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
        tia.Id.Should().Be(id);
        tia.SourceCountryCode.Should().Be("DE");
        tia.DestinationCountryCode.Should().Be("US");
        tia.DataCategory.Should().Be("personal-data");
        tia.Status.Should().Be(TIAStatus.Draft);
        tia.TenantId.Should().Be("tenant-1");
        tia.ModuleId.Should().Be("module-1");
        tia.RiskScore.Should().BeNull();
        tia.Findings.Should().BeNull();
        tia.AssessorId.Should().BeNull();
        tia.DPOReviewedAtUtc.Should().BeNull();
        tia.CompletedAtUtc.Should().BeNull();
        tia.RequiredSupplementaryMeasures.Should().BeEmpty();
    }

    [Fact]
    public void Create_NullSourceCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), null!, "US", "personal-data", "user-1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("sourceCountryCode");
    }

    [Fact]
    public void Create_NullDestinationCountryCode_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", null!, "personal-data", "user-1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("destinationCountryCode");
    }

    [Fact]
    public void Create_NullDataCategory_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", null!, "user-1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("dataCategory");
    }

    [Fact]
    public void Create_NullCreatedBy_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "personal-data", null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("createdBy");
    }

    [Fact]
    public void AssessRisk_DraftStatus_SetsRiskScoreAndStatus()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        tia.AssessRisk(0.75, "High surveillance risk", "assessor-1");

        // Assert
        tia.RiskScore.Should().Be(0.75);
        tia.Findings.Should().Be("High surveillance risk");
        tia.AssessorId.Should().Be("assessor-1");
        tia.Status.Should().Be(TIAStatus.InProgress);
    }

    [Fact]
    public void AssessRisk_CompletedStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateCompletedTIA();

        // Act
        var act = () => tia.AssessRisk(0.5, "Re-assessment", "assessor-2");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AssessRisk_ScoreAboveOne_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.AssessRisk(1.1, "findings", "assessor-1");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("riskScore");
    }

    [Fact]
    public void AssessRisk_ScoreBelowZero_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.AssessRisk(-0.1, "findings", "assessor-1");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("riskScore");
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
        tia.RequiredSupplementaryMeasures.Should().HaveCount(1);
        tia.RequiredSupplementaryMeasures[0].Id.Should().Be(measureId);
        tia.RequiredSupplementaryMeasures[0].Type.Should().Be(SupplementaryMeasureType.Technical);
        tia.RequiredSupplementaryMeasures[0].Description.Should().Be("End-to-end encryption required");
        tia.RequiredSupplementaryMeasures[0].IsImplemented.Should().BeFalse();
    }

    [Fact]
    public void RequireSupplementaryMeasure_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Audit rights");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitForDPOReview_InProgressStatus_ChangesPendingDPOReview()
    {
        // Arrange
        var tia = CreateInProgressTIA();

        // Act
        tia.SubmitForDPOReview("submitter-1");

        // Assert
        tia.Status.Should().Be(TIAStatus.PendingDPOReview);
    }

    [Fact]
    public void SubmitForDPOReview_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.SubmitForDPOReview("submitter-1");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApproveDPOReview_PendingDPOReview_SetsDPOReviewedAtUtc()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.ApproveDPOReview("dpo-1");

        // Assert
        tia.DPOReviewedAtUtc.Should().NotBeNull();
        tia.DPOReviewedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ApproveDPOReview_InProgressStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateInProgressTIA();

        // Act
        var act = () => tia.ApproveDPOReview("dpo-1");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RejectDPOReview_PendingDPOReview_ReturnsToInProgress()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.RejectDPOReview("dpo-1", "Insufficient supplementary measures");

        // Assert
        tia.Status.Should().Be(TIAStatus.InProgress);
        tia.DPOReviewedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Complete_PendingDPOReview_SetsCompleted()
    {
        // Arrange
        var tia = CreatePendingDPOReviewTIA();

        // Act
        tia.Complete();

        // Assert
        tia.Status.Should().Be(TIAStatus.Completed);
        tia.CompletedAtUtc.Should().NotBeNull();
        tia.CompletedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Expire_CompletedStatus_SetsExpired()
    {
        // Arrange
        var tia = CreateCompletedTIA();

        // Act
        tia.Expire();

        // Assert
        tia.Status.Should().Be(TIAStatus.Expired);
    }

    [Fact]
    public void Expire_DraftStatus_ThrowsInvalidOperation()
    {
        // Arrange
        var tia = CreateDraftTIA();

        // Act
        var act = () => tia.Expire();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FullLifecycle_DraftToCompleted_SuccessfulTransition()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act - Create (Draft)
        var tia = TIAAggregate.Create(id, "DE", "US", "personal-data", "user-1", "tenant-1", "module-1");
        tia.Status.Should().Be(TIAStatus.Draft);

        // Act - Assess risk (InProgress)
        tia.AssessRisk(0.8, "High risk due to FISA 702", "assessor-1");
        tia.Status.Should().Be(TIAStatus.InProgress);

        // Act - Add supplementary measures
        tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "E2E encryption");
        tia.RequireSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Audit rights clause");
        tia.RequiredSupplementaryMeasures.Should().HaveCount(2);

        // Act - Submit for DPO review (PendingDPOReview)
        tia.SubmitForDPOReview("submitter-1");
        tia.Status.Should().Be(TIAStatus.PendingDPOReview);

        // Act - Approve DPO review
        tia.ApproveDPOReview("dpo-1");
        tia.DPOReviewedAtUtc.Should().NotBeNull();

        // Act - Complete
        tia.Complete();
        tia.Status.Should().Be(TIAStatus.Completed);
        tia.CompletedAtUtc.Should().NotBeNull();

        // Assert - Final state
        tia.Id.Should().Be(id);
        tia.SourceCountryCode.Should().Be("DE");
        tia.DestinationCountryCode.Should().Be("US");
        tia.DataCategory.Should().Be("personal-data");
        tia.RiskScore.Should().Be(0.8);
        tia.Findings.Should().Be("High risk due to FISA 702");
        tia.AssessorId.Should().Be("assessor-1");
        tia.TenantId.Should().Be("tenant-1");
        tia.ModuleId.Should().Be("module-1");
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
