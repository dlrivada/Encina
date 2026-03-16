using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Marten.Projections;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.DPIA.ReadModels;

/// <summary>
/// Unit tests for <see cref="DPIAProjection"/>.
/// </summary>
public class DPIAProjectionTests
{
    private static readonly ProjectionContext DefaultContext = new();
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly DPIAProjection _sut = new();

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnDPIAProjection()
    {
        _sut.ProjectionName.Should().Be("DPIAProjection");
    }

    #endregion

    #region Create

    [Fact]
    public void Create_ShouldMapAllFieldsFromEvent()
    {
        // Arrange
        var @event = new DPIACreated(
            AssessmentId: DefaultId,
            RequestTypeName: "MyApp.Commands.ProcessData",
            ProcessingType: "AutomatedDecisionMaking",
            Reason: "High-risk profiling operation",
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        // Act
        var model = _sut.Create(@event, DefaultContext);

        // Assert
        model.Id.Should().Be(DefaultId);
        model.RequestTypeName.Should().Be("MyApp.Commands.ProcessData");
        model.ProcessingType.Should().Be("AutomatedDecisionMaking");
        model.Reason.Should().Be("High-risk profiling operation");
        model.Status.Should().Be(DPIAAssessmentStatus.Draft);
        model.TenantId.Should().Be("tenant-1");
        model.ModuleId.Should().Be("module-1");
        model.LastModifiedAtUtc.Should().Be(Now);
        model.Version.Should().Be(1);
        model.OverallRisk.Should().BeNull();
        model.IdentifiedRisks.Should().BeEmpty();
        model.ProposedMitigations.Should().BeEmpty();
        model.RequiresPriorConsultation.Should().BeFalse();
        model.AssessedAtUtc.Should().BeNull();
        model.DPOConsultation.Should().BeNull();
        model.ApprovedAtUtc.Should().BeNull();
        model.NextReviewAtUtc.Should().BeNull();
    }

    #endregion

    #region Apply DPIAEvaluated

    [Fact]
    public void Apply_DPIAEvaluated_ShouldSetRiskFieldsAndTransitionToInReview()
    {
        // Arrange
        var current = CreateDraftReadModel();
        var assessedAt = Now.AddHours(2);
        var occurredAt = Now.AddHours(2).AddMinutes(1);

        var risks = new List<RiskItem>
        {
            new("Data Minimization", RiskLevel.High, "Excessive data collection", "Reduce scope"),
            new("Security", RiskLevel.Medium, "Weak encryption", null),
        };
        var mitigations = new List<Mitigation>
        {
            new("Implement field-level encryption", "Technical", false, null),
        };

        var @event = new DPIAEvaluated(
            AssessmentId: DefaultId,
            OverallRisk: RiskLevel.High,
            IdentifiedRisks: risks,
            ProposedMitigations: mitigations,
            RequiresPriorConsultation: true,
            AssessedAtUtc: assessedAt,
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.OverallRisk.Should().Be(RiskLevel.High);
        result.IdentifiedRisks.Should().BeEquivalentTo(risks);
        result.ProposedMitigations.Should().BeEquivalentTo(mitigations);
        result.RequiresPriorConsultation.Should().BeTrue();
        result.AssessedAtUtc.Should().Be(assessedAt);
        result.Status.Should().Be(DPIAAssessmentStatus.InReview);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIADPOConsultationRequested

    [Fact]
    public void Apply_DPIADPOConsultationRequested_ShouldSetDPOConsultationWithPendingDecision()
    {
        // Arrange
        var current = CreateDraftReadModel();
        var consultationId = Guid.NewGuid();
        var occurredAt = Now.AddHours(3);

        var @event = new DPIADPOConsultationRequested(
            AssessmentId: DefaultId,
            ConsultationId: consultationId,
            DPOName: "Jane Smith",
            DPOEmail: "jane.smith@example.com",
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.DPOConsultation.Should().NotBeNull();
        result.DPOConsultation!.Id.Should().Be(consultationId);
        result.DPOConsultation.DPOName.Should().Be("Jane Smith");
        result.DPOConsultation.DPOEmail.Should().Be("jane.smith@example.com");
        result.DPOConsultation.RequestedAtUtc.Should().Be(occurredAt);
        result.DPOConsultation.Decision.Should().Be(DPOConsultationDecision.Pending);
        result.DPOConsultation.RespondedAtUtc.Should().BeNull();
        result.DPOConsultation.Comments.Should().BeNull();
        result.DPOConsultation.Conditions.Should().BeNull();
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIADPOResponded

    [Fact]
    public void Apply_DPIADPOResponded_ShouldUpdateDPOConsultationDecisionAndComments()
    {
        // Arrange
        var consultationId = Guid.NewGuid();
        var requestedAt = Now.AddHours(3);
        var respondedAt = Now.AddDays(1);

        var current = CreateDraftReadModel();
        current.DPOConsultation = new DPOConsultation
        {
            Id = consultationId,
            DPOName = "Jane Smith",
            DPOEmail = "jane.smith@example.com",
            RequestedAtUtc = requestedAt,
            Decision = DPOConsultationDecision.Pending,
        };

        var @event = new DPIADPOResponded(
            AssessmentId: DefaultId,
            ConsultationId: consultationId,
            Decision: DPOConsultationDecision.ConditionallyApproved,
            Comments: "Acceptable with conditions",
            Conditions: "Must implement encryption before go-live",
            OccurredAtUtc: respondedAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.DPOConsultation.Should().NotBeNull();
        result.DPOConsultation!.Decision.Should().Be(DPOConsultationDecision.ConditionallyApproved);
        result.DPOConsultation.Comments.Should().Be("Acceptable with conditions");
        result.DPOConsultation.Conditions.Should().Be("Must implement encryption before go-live");
        result.DPOConsultation.RespondedAtUtc.Should().Be(respondedAt);
        result.DPOConsultation.DPOName.Should().Be("Jane Smith");
        result.DPOConsultation.DPOEmail.Should().Be("jane.smith@example.com");
        result.LastModifiedAtUtc.Should().Be(respondedAt);
        result.Version.Should().Be(2);
    }

    [Fact]
    public void Apply_DPIADPOResponded_WhenDPOConsultationIsNull_ShouldStillIncrementVersion()
    {
        // Arrange
        var current = CreateDraftReadModel();
        current.DPOConsultation = null;

        var respondedAt = Now.AddDays(1);

        var @event = new DPIADPOResponded(
            AssessmentId: DefaultId,
            ConsultationId: Guid.NewGuid(),
            Decision: DPOConsultationDecision.Approved,
            Comments: "Looks good",
            Conditions: null,
            OccurredAtUtc: respondedAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.DPOConsultation.Should().BeNull();
        result.LastModifiedAtUtc.Should().Be(respondedAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIAApproved

    [Fact]
    public void Apply_DPIAApproved_ShouldSetApprovedStatusAndTimestamps()
    {
        // Arrange
        var current = CreateDraftReadModel();
        var occurredAt = Now.AddDays(2);
        var nextReview = Now.AddYears(1);

        var @event = new DPIAApproved(
            AssessmentId: DefaultId,
            ApprovedBy: "admin@example.com",
            NextReviewAtUtc: nextReview,
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.Status.Should().Be(DPIAAssessmentStatus.Approved);
        result.ApprovedAtUtc.Should().Be(occurredAt);
        result.NextReviewAtUtc.Should().Be(nextReview);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIARejected

    [Fact]
    public void Apply_DPIARejected_ShouldSetRejectedStatus()
    {
        // Arrange
        var current = CreateDraftReadModel();
        var occurredAt = Now.AddDays(2);

        var @event = new DPIARejected(
            AssessmentId: DefaultId,
            RejectedBy: "reviewer@example.com",
            Reason: "Insufficient mitigations for high-risk processing",
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.Status.Should().Be(DPIAAssessmentStatus.Rejected);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIARevisionRequested

    [Fact]
    public void Apply_DPIARevisionRequested_ShouldSetRequiresRevisionStatus()
    {
        // Arrange
        var current = CreateDraftReadModel();
        var occurredAt = Now.AddDays(1);

        var @event = new DPIARevisionRequested(
            AssessmentId: DefaultId,
            RequestedBy: "dpo@example.com",
            Reason: "Missing risk analysis for cross-border transfers",
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.Status.Should().Be(DPIAAssessmentStatus.RequiresRevision);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Apply DPIAExpired

    [Fact]
    public void Apply_DPIAExpired_ShouldSetExpiredStatus()
    {
        // Arrange
        var current = CreateDraftReadModel();
        current.Status = DPIAAssessmentStatus.Approved;
        var occurredAt = Now.AddYears(1);

        var @event = new DPIAExpired(
            AssessmentId: DefaultId,
            OccurredAtUtc: occurredAt);

        // Act
        var result = _sut.Apply(@event, current, DefaultContext);

        // Assert
        result.Status.Should().Be(DPIAAssessmentStatus.Expired);
        result.LastModifiedAtUtc.Should().Be(occurredAt);
        result.Version.Should().Be(2);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Create_Evaluate_Approve_ShouldProduceCorrectFinalState()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        var createdAt = Now;
        var evaluatedAt = Now.AddHours(2);
        var approvedAt = Now.AddDays(3);
        var nextReview = Now.AddYears(1);

        var risks = new List<RiskItem>
        {
            new("Purpose Limitation", RiskLevel.Medium, "Broad purpose definition", "Narrow scope"),
        };
        var mitigations = new List<Mitigation>
        {
            new("Add purpose limitation controls", "Organizational", true, evaluatedAt),
        };

        var createdEvent = new DPIACreated(
            AssessmentId: assessmentId,
            RequestTypeName: "MyApp.Commands.ProfileUser",
            ProcessingType: "Profiling",
            Reason: "Systematic profiling of user behavior",
            OccurredAtUtc: createdAt,
            TenantId: "tenant-acme",
            ModuleId: "module-analytics");

        var evaluatedEvent = new DPIAEvaluated(
            AssessmentId: assessmentId,
            OverallRisk: RiskLevel.Medium,
            IdentifiedRisks: risks,
            ProposedMitigations: mitigations,
            RequiresPriorConsultation: false,
            AssessedAtUtc: evaluatedAt,
            OccurredAtUtc: evaluatedAt.AddMinutes(1));

        var approvedEvent = new DPIAApproved(
            AssessmentId: assessmentId,
            ApprovedBy: "ciso@acme.com",
            NextReviewAtUtc: nextReview,
            OccurredAtUtc: approvedAt);

        // Act
        var model = _sut.Create(createdEvent, DefaultContext);
        model = _sut.Apply(evaluatedEvent, model, DefaultContext);
        model = _sut.Apply(approvedEvent, model, DefaultContext);

        // Assert
        model.Id.Should().Be(assessmentId);
        model.RequestTypeName.Should().Be("MyApp.Commands.ProfileUser");
        model.ProcessingType.Should().Be("Profiling");
        model.Reason.Should().Be("Systematic profiling of user behavior");
        model.Status.Should().Be(DPIAAssessmentStatus.Approved);
        model.OverallRisk.Should().Be(RiskLevel.Medium);
        model.IdentifiedRisks.Should().HaveCount(1);
        model.ProposedMitigations.Should().HaveCount(1);
        model.RequiresPriorConsultation.Should().BeFalse();
        model.AssessedAtUtc.Should().Be(evaluatedAt);
        model.ApprovedAtUtc.Should().Be(approvedAt);
        model.NextReviewAtUtc.Should().Be(nextReview);
        model.TenantId.Should().Be("tenant-acme");
        model.ModuleId.Should().Be("module-analytics");
        model.LastModifiedAtUtc.Should().Be(approvedAt);
        model.Version.Should().Be(3);
    }

    #endregion

    #region Helpers

    private DPIAReadModel CreateDraftReadModel()
    {
        var @event = new DPIACreated(
            AssessmentId: DefaultId,
            RequestTypeName: "MyApp.Commands.ProcessData",
            ProcessingType: "AutomatedDecisionMaking",
            Reason: "Risk assessment required",
            OccurredAtUtc: Now,
            TenantId: "tenant-1",
            ModuleId: "module-1");

        return _sut.Create(@event, DefaultContext);
    }

    #endregion
}
