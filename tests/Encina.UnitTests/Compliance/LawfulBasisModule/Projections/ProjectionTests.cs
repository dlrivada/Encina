using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Events;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Marten.Projections;
using Shouldly;

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
        _lbProjection.ProjectionName.ShouldBe("LawfulBasisProjection");
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
        model.Id.ShouldBe(DefaultId);
        model.RequestTypeName.ShouldBe("MyApp.Commands.CreateOrder");
        model.Basis.ShouldBe(LawfulBasis.Contract);
        model.Purpose.ShouldBe("Order processing");
        model.LIAReference.ShouldBeNull();
        model.LegalReference.ShouldBeNull();
        model.ContractReference.ShouldBe("contract-001");
        model.RegisteredAtUtc.ShouldBe(Now);
        model.TenantId.ShouldBe("tenant-1");
        model.ModuleId.ShouldBe("module-1");
        model.IsRevoked.ShouldBeFalse();
        model.RevocationReason.ShouldBeNull();
        model.LastModifiedAtUtc.ShouldBe(Now);
        model.Version.ShouldBe(1);
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
        updated.Basis.ShouldBe(LawfulBasis.LegitimateInterests);
        updated.Purpose.ShouldBe("Fraud prevention");
        updated.LIAReference.ShouldBe("LIA-2026-FRAUD-001");
        updated.LegalReference.ShouldBeNull();
        updated.ContractReference.ShouldBeNull();
        updated.LastModifiedAtUtc.ShouldBe(changedAt);
        updated.Version.ShouldBe(2);
        // Unchanged fields
        updated.Id.ShouldBe(DefaultId);
        updated.RequestTypeName.ShouldBe("MyApp.Commands.CreateOrder");
        updated.IsRevoked.ShouldBeFalse();
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
        updated.IsRevoked.ShouldBeTrue();
        updated.RevocationReason.ShouldBe("No longer needed");
        updated.LastModifiedAtUtc.ShouldBe(revokedAt);
        updated.Version.ShouldBe(2);
    }

    #endregion

    #region LIAProjection

    private readonly LIAProjection _liaProjection = new();

    [Fact]
    public void LIAProjection_ProjectionName_ShouldReturnExpectedName()
    {
        _liaProjection.ProjectionName.ShouldBe("LIAProjection");
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
        model.Id.ShouldBe(DefaultId);
        model.Reference.ShouldBe("LIA-2026-FRAUD-001");
        model.Name.ShouldBe("Fraud Prevention LIA");
        model.Purpose.ShouldBe("Detect fraudulent transactions");

        // Assert — Purpose Test
        model.LegitimateInterest.ShouldBe("Protecting business from fraud");
        model.Benefits.ShouldBe("Reduced financial losses");
        model.ConsequencesIfNotProcessed.ShouldBe("Increased fraud exposure");

        // Assert — Necessity Test
        model.NecessityJustification.ShouldBe("No less intrusive alternative");
        model.AlternativesConsidered.ShouldBe(["Manual review", "Rule-based filtering"]);
        model.DataMinimisationNotes.ShouldBe("Only transaction metadata");

        // Assert — Balancing Test
        model.NatureOfData.ShouldBe("Transaction data, IP addresses");
        model.ReasonableExpectations.ShouldBe("Users expect fraud protection");
        model.ImpactAssessment.ShouldBe("Minimal impact on individual rights");
        model.Safeguards.ShouldBe(["Encryption", "Access controls"]);

        // Assert — Governance
        model.AssessedBy.ShouldBe("DPO");
        model.DPOInvolvement.ShouldBeTrue();
        model.AssessedAtUtc.ShouldBe(Now);
        model.Conditions.ShouldBe("Annual review required");

        // Assert — Outcome & metadata
        model.Outcome.ShouldBe(LIAOutcome.RequiresReview);
        model.Conclusion.ShouldBeNull();
        model.NextReviewAtUtc.ShouldBeNull();
        model.TenantId.ShouldBe("tenant-1");
        model.ModuleId.ShouldBe("module-1");
        model.LastModifiedAtUtc.ShouldBe(Now);
        model.Version.ShouldBe(1);
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
        updated.Outcome.ShouldBe(LIAOutcome.Approved);
        updated.Conclusion.ShouldBe("Balancing test favors controller");
        updated.LastModifiedAtUtc.ShouldBe(approvedAt);
        updated.Version.ShouldBe(2);
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
        updated.Outcome.ShouldBe(LIAOutcome.Rejected);
        updated.Conclusion.ShouldBe("Data subject rights override");
        updated.LastModifiedAtUtc.ShouldBe(rejectedAt);
        updated.Version.ShouldBe(2);
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
        updated.NextReviewAtUtc.ShouldBe(nextReview);
        updated.LastModifiedAtUtc.ShouldBe(scheduledAt);
        updated.Version.ShouldBe(2);
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
