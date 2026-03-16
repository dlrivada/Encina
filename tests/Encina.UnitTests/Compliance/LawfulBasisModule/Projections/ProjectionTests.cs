using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Events;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Marten.Projections;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Projections;

/// <summary>
/// Unit tests for <see cref="LawfulBasisProjection"/> and <see cref="LIAProjection"/>.
/// </summary>
public class ProjectionTests
{
    private static readonly ProjectionContext DefaultContext = new();
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region LawfulBasisProjection

    private readonly LawfulBasisProjection _lbProjection = new();

    [Fact]
    public void LawfulBasisProjection_ProjectionName_ShouldReturnExpectedName()
    {
        _lbProjection.ProjectionName.Should().Be("LawfulBasisProjection");
    }

    [Fact]
    public void LawfulBasisProjection_Create_MapsAllFields()
    {
        // Arrange
        var @event = new LawfulBasisRegistered(
            DefaultId,
            "MyApp.Commands.CreateOrder",
            LawfulBasis.Contract,
            Purpose: "Order processing",
            LIAReference: null,
            LegalReference: null,
            ContractReference: "contract-001",
            RegisteredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var model = _lbProjection.Create(@event, DefaultContext);

        // Assert
        model.Id.Should().Be(DefaultId);
        model.RequestTypeName.Should().Be("MyApp.Commands.CreateOrder");
        model.Basis.Should().Be(LawfulBasis.Contract);
        model.Purpose.Should().Be("Order processing");
        model.LIAReference.Should().BeNull();
        model.LegalReference.Should().BeNull();
        model.ContractReference.Should().Be("contract-001");
        model.RegisteredAtUtc.Should().Be(Now);
        model.TenantId.Should().Be("tenant-1");
        model.ModuleId.Should().Be("module-1");
        model.IsRevoked.Should().BeFalse();
        model.RevocationReason.Should().BeNull();
        model.LastModifiedAtUtc.Should().Be(Now);
        model.Version.Should().Be(1);
    }

    [Fact]
    public void LawfulBasisProjection_Apply_ChangedEvent_UpdatesBasisAndReferences()
    {
        // Arrange
        var current = CreateActiveReadModel();
        var changedAt = Now.AddDays(5);

        var @event = new LawfulBasisChanged(
            DefaultId,
            OldBasis: LawfulBasis.Contract,
            NewBasis: LawfulBasis.LegitimateInterests,
            Purpose: "Fraud prevention",
            LIAReference: "LIA-2026-FRAUD-001",
            LegalReference: null,
            ContractReference: null,
            ChangedAtUtc: changedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var updated = _lbProjection.Apply(@event, current, DefaultContext);

        // Assert
        updated.Basis.Should().Be(LawfulBasis.LegitimateInterests);
        updated.Purpose.Should().Be("Fraud prevention");
        updated.LIAReference.Should().Be("LIA-2026-FRAUD-001");
        updated.LegalReference.Should().BeNull();
        updated.ContractReference.Should().BeNull();
        updated.LastModifiedAtUtc.Should().Be(changedAt);
        updated.Version.Should().Be(2);
        // Unchanged fields
        updated.Id.Should().Be(DefaultId);
        updated.RequestTypeName.Should().Be("MyApp.Commands.CreateOrder");
        updated.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void LawfulBasisProjection_Apply_RevokedEvent_SetsIsRevoked()
    {
        // Arrange
        var current = CreateActiveReadModel();
        var revokedAt = Now.AddDays(10);

        var @event = new LawfulBasisRevoked(
            DefaultId,
            Reason: "No longer needed",
            RevokedAtUtc: revokedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var updated = _lbProjection.Apply(@event, current, DefaultContext);

        // Assert
        updated.IsRevoked.Should().BeTrue();
        updated.RevocationReason.Should().Be("No longer needed");
        updated.LastModifiedAtUtc.Should().Be(revokedAt);
        updated.Version.Should().Be(2);
    }

    #endregion

    #region LIAProjection

    private readonly LIAProjection _liaProjection = new();

    [Fact]
    public void LIAProjection_ProjectionName_ShouldReturnExpectedName()
    {
        _liaProjection.ProjectionName.Should().Be("LIAProjection");
    }

    [Fact]
    public void LIAProjection_Create_MapsAllEDPBFields()
    {
        // Arrange
        var @event = new LIACreated(
            DefaultId,
            Reference: "LIA-2026-FRAUD-001",
            Name: "Fraud Prevention LIA",
            Purpose: "Detect fraudulent transactions",
            LegitimateInterest: "Protecting business from fraud",
            Benefits: "Reduced financial losses",
            ConsequencesIfNotProcessed: "Increased fraud exposure",
            NecessityJustification: "No less intrusive alternative",
            AlternativesConsidered: ["Manual review", "Rule-based filtering"],
            DataMinimisationNotes: "Only transaction metadata",
            NatureOfData: "Transaction data, IP addresses",
            ReasonableExpectations: "Users expect fraud protection",
            ImpactAssessment: "Minimal impact on individual rights",
            Safeguards: ["Encryption", "Access controls"],
            AssessedBy: "DPO",
            DPOInvolvement: true,
            AssessedAtUtc: Now,
            Conditions: "Annual review required",
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var model = _liaProjection.Create(@event, DefaultContext);

        // Assert — Identity
        model.Id.Should().Be(DefaultId);
        model.Reference.Should().Be("LIA-2026-FRAUD-001");
        model.Name.Should().Be("Fraud Prevention LIA");
        model.Purpose.Should().Be("Detect fraudulent transactions");

        // Assert — Purpose Test
        model.LegitimateInterest.Should().Be("Protecting business from fraud");
        model.Benefits.Should().Be("Reduced financial losses");
        model.ConsequencesIfNotProcessed.Should().Be("Increased fraud exposure");

        // Assert — Necessity Test
        model.NecessityJustification.Should().Be("No less intrusive alternative");
        model.AlternativesConsidered.Should().BeEquivalentTo(["Manual review", "Rule-based filtering"]);
        model.DataMinimisationNotes.Should().Be("Only transaction metadata");

        // Assert — Balancing Test
        model.NatureOfData.Should().Be("Transaction data, IP addresses");
        model.ReasonableExpectations.Should().Be("Users expect fraud protection");
        model.ImpactAssessment.Should().Be("Minimal impact on individual rights");
        model.Safeguards.Should().BeEquivalentTo(["Encryption", "Access controls"]);

        // Assert — Governance
        model.AssessedBy.Should().Be("DPO");
        model.DPOInvolvement.Should().BeTrue();
        model.AssessedAtUtc.Should().Be(Now);
        model.Conditions.Should().Be("Annual review required");

        // Assert — Outcome & metadata
        model.Outcome.Should().Be(LIAOutcome.RequiresReview);
        model.Conclusion.Should().BeNull();
        model.NextReviewAtUtc.Should().BeNull();
        model.TenantId.Should().Be("tenant-1");
        model.ModuleId.Should().Be("module-1");
        model.LastModifiedAtUtc.Should().Be(Now);
        model.Version.Should().Be(1);
    }

    [Fact]
    public void LIAProjection_Apply_ApprovedEvent_SetsOutcome()
    {
        // Arrange
        var current = CreatePendingLIAReadModel();
        var approvedAt = Now.AddDays(3);

        var @event = new LIAApproved(
            DefaultId,
            Conclusion: "Balancing test favors controller",
            ApprovedBy: "dpo@example.com",
            ApprovedAtUtc: approvedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var updated = _liaProjection.Apply(@event, current, DefaultContext);

        // Assert
        updated.Outcome.Should().Be(LIAOutcome.Approved);
        updated.Conclusion.Should().Be("Balancing test favors controller");
        updated.LastModifiedAtUtc.Should().Be(approvedAt);
        updated.Version.Should().Be(2);
    }

    [Fact]
    public void LIAProjection_Apply_RejectedEvent_SetsOutcome()
    {
        // Arrange
        var current = CreatePendingLIAReadModel();
        var rejectedAt = Now.AddDays(3);

        var @event = new LIARejected(
            DefaultId,
            Conclusion: "Data subject rights override",
            RejectedBy: "dpo@example.com",
            RejectedAtUtc: rejectedAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var updated = _liaProjection.Apply(@event, current, DefaultContext);

        // Assert
        updated.Outcome.Should().Be(LIAOutcome.Rejected);
        updated.Conclusion.Should().Be("Data subject rights override");
        updated.LastModifiedAtUtc.Should().Be(rejectedAt);
        updated.Version.Should().Be(2);
    }

    [Fact]
    public void LIAProjection_Apply_ReviewScheduledEvent_SetsNextReviewDate()
    {
        // Arrange
        var current = CreatePendingLIAReadModel();
        current.Outcome = LIAOutcome.Approved; // Reviews are only scheduled for approved LIAs
        var scheduledAt = Now.AddDays(5);
        var nextReview = Now.AddMonths(6);

        var @event = new LIAReviewScheduled(
            DefaultId,
            NextReviewAtUtc: nextReview,
            ScheduledBy: "governance@example.com",
            ScheduledAtUtc: scheduledAt,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var updated = _liaProjection.Apply(@event, current, DefaultContext);

        // Assert
        updated.NextReviewAtUtc.Should().Be(nextReview);
        updated.LastModifiedAtUtc.Should().Be(scheduledAt);
        updated.Version.Should().Be(2);
    }

    #endregion

    #region Helpers

    private static LawfulBasisReadModel CreateActiveReadModel()
    {
        return new LawfulBasisReadModel
        {
            Id = DefaultId,
            RequestTypeName = "MyApp.Commands.CreateOrder",
            Basis = LawfulBasis.Contract,
            Purpose = "Order processing",
            ContractReference = "contract-001",
            RegisteredAtUtc = Now,
            LastModifiedAtUtc = Now,
            Version = 1
        };
    }

    private static LIAReadModel CreatePendingLIAReadModel()
    {
        return new LIAReadModel
        {
            Id = DefaultId,
            Reference = "LIA-2026-FRAUD-001",
            Name = "Fraud Prevention LIA",
            Purpose = "Detect fraudulent transactions",
            LegitimateInterest = "Protecting business from fraud",
            Benefits = "Reduced financial losses",
            ConsequencesIfNotProcessed = "Increased fraud exposure",
            NecessityJustification = "No less intrusive alternative",
            AlternativesConsidered = ["Manual review"],
            DataMinimisationNotes = "Only transaction metadata",
            NatureOfData = "Transaction data",
            ReasonableExpectations = "Users expect fraud protection",
            ImpactAssessment = "Minimal impact",
            Safeguards = ["Encryption"],
            AssessedBy = "DPO",
            DPOInvolvement = true,
            AssessedAtUtc = Now,
            Outcome = LIAOutcome.RequiresReview,
            LastModifiedAtUtc = Now,
            Version = 1
        };
    }

    #endregion
}
