using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Shouldly;
using DPIAExpiredEvent = Encina.Compliance.DPIA.Events.DPIAExpired;

namespace Encina.UnitTests.Compliance.DPIA.Aggregates;

/// <summary>
/// Unit tests for <see cref="DPIAAggregate"/>.
/// </summary>
public class DPIAAggregateTests
{
    private static readonly Guid DefaultId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    #region Helpers

    private static DPIAResult CreateDefaultResult(DateTimeOffset? assessedAtUtc = null) => new()
    {
        OverallRisk = RiskLevel.High,
        IdentifiedRisks = [new RiskItem("Data Minimization", RiskLevel.High, "Excessive data collection", "Limit fields collected")],
        ProposedMitigations = [new Mitigation("Implement field-level encryption", "Technical", false, null)],
        RequiresPriorConsultation = false,
        AssessedAtUtc = assessedAtUtc ?? Now.AddHours(1),
    };

    private static DPIAAggregate CreateDraftAggregate(
        Guid? id = null,
        string? processingType = null,
        string? reason = null,
        string? tenantId = null,
        string? moduleId = null)
    {
        return DPIAAggregate.Create(
            id ?? DefaultId,
            "MyApp.Commands.ProcessUserData",
            Now,
            processingType ?? "AutomatedDecisionMaking",
            reason ?? "High-risk profiling operation",
            tenantId,
            moduleId);
    }

    private static DPIAAggregate CreateInReviewAggregate()
    {
        var aggregate = CreateDraftAggregate();
        aggregate.Evaluate(CreateDefaultResult(), Now.AddHours(1));
        return aggregate;
    }

    private static DPIAAggregate CreateApprovedAggregate(DateTimeOffset? nextReviewAtUtc = null)
    {
        var aggregate = CreateInReviewAggregate();
        aggregate.Approve("admin@company.com", Now.AddHours(2), nextReviewAtUtc);
        return aggregate;
    }

    private static DPIAAggregate CreateRejectedAggregate()
    {
        var aggregate = CreateInReviewAggregate();
        aggregate.Reject("admin@company.com", "Unacceptable residual risk", Now.AddHours(2));
        return aggregate;
    }

    private static DPIAAggregate CreateRequiresRevisionAggregate()
    {
        var aggregate = CreateInReviewAggregate();
        aggregate.RequestRevision("dpo@company.com", "Insufficient mitigation detail", Now.AddHours(2));
        return aggregate;
    }

    private static DPIAAggregate CreateExpiredAggregate()
    {
        var aggregate = CreateApprovedAggregate(Now.AddDays(90));
        aggregate.Expire(Now.AddDays(91));
        return aggregate;
    }

    #endregion

    #region Create

    [Fact]
    public void Create_ValidParameters_SetsStatusToDraft()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Draft);
    }

    [Fact]
    public void Create_ValidParameters_SetsId()
    {
        // Act
        var id = Guid.NewGuid();
        var aggregate = CreateDraftAggregate(id: id);

        // Assert
        aggregate.Id.ShouldBe(id);
    }

    [Fact]
    public void Create_ValidParameters_SetsRequestTypeName()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.RequestTypeName.ShouldBe("MyApp.Commands.ProcessUserData");
    }

    [Fact]
    public void Create_ValidParameters_SetsProcessingType()
    {
        // Act
        var aggregate = CreateDraftAggregate(processingType: "LargeScaleProcessing");

        // Assert
        aggregate.ProcessingType.ShouldBe("LargeScaleProcessing");
    }

    [Fact]
    public void Create_ValidParameters_SetsReason()
    {
        // Act
        var aggregate = CreateDraftAggregate(reason: "Custom reason");

        // Assert
        aggregate.Reason.ShouldBe("Custom reason");
    }

    [Fact]
    public void Create_ValidParameters_SetsTenantId()
    {
        // Act
        var aggregate = CreateDraftAggregate(tenantId: "tenant-1");

        // Assert
        aggregate.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void Create_ValidParameters_SetsModuleId()
    {
        // Act
        var aggregate = CreateDraftAggregate(moduleId: "module-1");

        // Assert
        aggregate.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Create_ValidParameters_ResultIsNull()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.Result.ShouldBeNull();
    }

    [Fact]
    public void Create_ValidParameters_DPOConsultationIsNull()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.DPOConsultation.ShouldBeNull();
    }

    [Fact]
    public void Create_ValidParameters_ApprovedAtUtcIsNull()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.ApprovedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_ValidParameters_NextReviewAtUtcIsNull()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.NextReviewAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_ValidParameters_RaisesDPIACreatedEvent()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.UncommittedEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<DPIACreated>();
    }

    [Fact]
    public void Create_ValidParameters_DPIACreatedEventContainsCorrectData()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = DPIAAggregate.Create(
            id,
            "MyApp.Commands.ProcessUserData",
            Now,
            "AutomatedDecisionMaking",
            "High-risk profiling",
            "tenant-1",
            "module-1");

        // Assert
        var evt = aggregate.UncommittedEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<DPIACreated>();
        evt.AssessmentId.ShouldBe(id);
        evt.RequestTypeName.ShouldBe("MyApp.Commands.ProcessUserData");
        evt.ProcessingType.ShouldBe("AutomatedDecisionMaking");
        evt.Reason.ShouldBe("High-risk profiling");
        evt.OccurredAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void Create_NullOptionalParameters_SetsNullValues()
    {
        // Act
        var aggregate = DPIAAggregate.Create(
            DefaultId,
            "MyApp.Commands.ProcessUserData",
            Now);

        // Assert
        aggregate.ProcessingType.ShouldBeNull();
        aggregate.Reason.ShouldBeNull();
        aggregate.TenantId.ShouldBeNull();
        aggregate.ModuleId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidRequestTypeName_ThrowsArgumentException(string? requestTypeName)
    {
        // Act
        var act = () => DPIAAggregate.Create(DefaultId, requestTypeName!, Now);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_VersionIsOne()
    {
        // Act
        var aggregate = CreateDraftAggregate();

        // Assert
        aggregate.Version.ShouldBe(1);
    }

    #endregion

    #region Evaluate

    [Fact]
    public void Evaluate_FromDraft_SetsStatusToInReview()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();
        var result = CreateDefaultResult();

        // Act
        aggregate.Evaluate(result, Now.AddHours(1));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.InReview);
    }

    [Fact]
    public void Evaluate_FromDraft_SetsResult()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();
        var result = CreateDefaultResult();

        // Act
        aggregate.Evaluate(result, Now.AddHours(1));

        // Assert
        aggregate.Result.ShouldNotBeNull();
        aggregate.Result!.OverallRisk.ShouldBe(RiskLevel.High);
        aggregate.Result.IdentifiedRisks.Count.ShouldBe(1);
        aggregate.Result.ProposedMitigations.Count.ShouldBe(1);
        aggregate.Result.RequiresPriorConsultation.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_FromDraft_RaisesDPIAEvaluatedEvent()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();
        var result = CreateDefaultResult();

        // Act
        aggregate.Evaluate(result, Now.AddHours(1));

        // Assert
        aggregate.UncommittedEvents.Count.ShouldBe(2);
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIAEvaluated>();
    }

    [Fact]
    public void Evaluate_FromDraft_DPIAEvaluatedEventContainsCorrectData()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();
        var assessedAt = Now.AddMinutes(30);
        var result = CreateDefaultResult(assessedAtUtc: assessedAt);
        var occurredAt = Now.AddHours(1);

        // Act
        aggregate.Evaluate(result, occurredAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIAEvaluated>();
        evt.AssessmentId.ShouldBe(aggregate.Id);
        evt.OverallRisk.ShouldBe(RiskLevel.High);
        evt.IdentifiedRisks.Count.ShouldBe(1);
        evt.ProposedMitigations.Count.ShouldBe(1);
        evt.RequiresPriorConsultation.ShouldBeFalse();
        evt.AssessedAtUtc.ShouldBe(assessedAt);
        evt.OccurredAtUtc.ShouldBe(occurredAt);
    }

    [Fact]
    public void Evaluate_FromRequiresRevision_SetsStatusToInReview()
    {
        // Arrange
        var aggregate = CreateRequiresRevisionAggregate();
        var result = CreateDefaultResult();

        // Act
        aggregate.Evaluate(result, Now.AddHours(3));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.InReview);
    }

    [Fact]
    public void Evaluate_FromRequiresRevision_UpdatesResult()
    {
        // Arrange
        var aggregate = CreateRequiresRevisionAggregate();
        var newResult = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = Now.AddHours(3),
        };

        // Act
        aggregate.Evaluate(newResult, Now.AddHours(3));

        // Assert
        aggregate.Result!.OverallRisk.ShouldBe(RiskLevel.Medium);
    }

    [Fact]
    public void Evaluate_FromInReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var result = CreateDefaultResult();

        // Act
        var act = () => aggregate.Evaluate(result, Now.AddHours(2));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("InReview");
    }

    [Fact]
    public void Evaluate_FromApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate();
        var result = CreateDefaultResult();

        // Act
        var act = () => aggregate.Evaluate(result, Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Approved");
    }

    [Fact]
    public void Evaluate_FromRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRejectedAggregate();
        var result = CreateDefaultResult();

        // Act
        var act = () => aggregate.Evaluate(result, Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Rejected");
    }

    [Fact]
    public void Evaluate_FromExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateExpiredAggregate();
        var result = CreateDefaultResult();

        // Act
        var act = () => aggregate.Evaluate(result, Now.AddDays(92));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Expired");
    }

    [Fact]
    public void Evaluate_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.Evaluate(null!, Now.AddHours(1));

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region RequestDPOConsultation

    [Fact]
    public void RequestDPOConsultation_FromInReview_SetsDPOConsultation()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();

        // Act
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));

        // Assert
        aggregate.DPOConsultation.ShouldNotBeNull();
        aggregate.DPOConsultation!.Id.ShouldBe(consultationId);
        aggregate.DPOConsultation.DPOName.ShouldBe("Jane Doe");
        aggregate.DPOConsultation.DPOEmail.ShouldBe("jane@company.com");
        aggregate.DPOConsultation.Decision.ShouldBe(DPOConsultationDecision.Pending);
        aggregate.DPOConsultation.RequestedAtUtc.ShouldBe(Now.AddHours(2));
        aggregate.DPOConsultation.RespondedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void RequestDPOConsultation_FromInReview_RaisesEvent()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();

        // Act
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIADPOConsultationRequested>()
            .ConsultationId.ShouldBe(consultationId);
    }

    [Fact]
    public void RequestDPOConsultation_FromDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane Doe", "jane@company.com", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Draft");
    }

    [Fact]
    public void RequestDPOConsultation_FromApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate();

        // Act
        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane Doe", "jane@company.com", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Approved");
    }

    [Fact]
    public void RequestDPOConsultation_FromRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRejectedAggregate();

        // Act
        var act = () => aggregate.RequestDPOConsultation(Guid.NewGuid(), "Jane Doe", "jane@company.com", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Rejected");
    }

    #endregion

    #region RecordDPOResponse

    [Fact]
    public void RecordDPOResponse_MatchingPendingConsultation_UpdatesDecision()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));

        // Act
        aggregate.RecordDPOResponse(consultationId, DPOConsultationDecision.Approved, Now.AddHours(3));

        // Assert
        aggregate.DPOConsultation!.Decision.ShouldBe(DPOConsultationDecision.Approved);
        aggregate.DPOConsultation.RespondedAtUtc.ShouldBe(Now.AddHours(3));
    }

    [Fact]
    public void RecordDPOResponse_WithCommentsAndConditions_SetsValues()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));

        // Act
        aggregate.RecordDPOResponse(
            consultationId,
            DPOConsultationDecision.ConditionallyApproved,
            Now.AddHours(3),
            comments: "Needs encryption for sensitive fields",
            conditions: "Implement AES-256 encryption before go-live");

        // Assert
        aggregate.DPOConsultation!.Decision.ShouldBe(DPOConsultationDecision.ConditionallyApproved);
        aggregate.DPOConsultation.Comments.ShouldBe("Needs encryption for sensitive fields");
        aggregate.DPOConsultation.Conditions.ShouldBe("Implement AES-256 encryption before go-live");
    }

    [Fact]
    public void RecordDPOResponse_MatchingPendingConsultation_RaisesEvent()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));

        // Act
        aggregate.RecordDPOResponse(consultationId, DPOConsultationDecision.Approved, Now.AddHours(3));

        // Assert
        aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIADPOResponded>()
            .ConsultationId.ShouldBe(consultationId);
    }

    [Fact]
    public void RecordDPOResponse_WrongConsultationId_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));
        var wrongId = Guid.NewGuid();

        // Act
        var act = () => aggregate.RecordDPOResponse(wrongId, DPOConsultationDecision.Approved, Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain(wrongId.ToString());
    }

    [Fact]
    public void RecordDPOResponse_NoConsultationExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        var act = () => aggregate.RecordDPOResponse(Guid.NewGuid(), DPOConsultationDecision.Approved, Now.AddHours(2));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("No pending DPO consultation");
    }

    [Fact]
    public void RecordDPOResponse_AlreadyResponded_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));
        aggregate.RecordDPOResponse(consultationId, DPOConsultationDecision.Approved, Now.AddHours(3));

        // Act
        var act = () => aggregate.RecordDPOResponse(consultationId, DPOConsultationDecision.Rejected, Now.AddHours(4));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("already been responded");
    }

    #endregion

    #region Approve

    [Fact]
    public void Approve_FromInReview_SetsStatusToApproved()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.Approve("admin@company.com", Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Approved);
    }

    [Fact]
    public void Approve_FromInReview_SetsApprovedAtUtc()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var approvedAt = Now.AddHours(2);

        // Act
        aggregate.Approve("admin@company.com", approvedAt);

        // Assert
        aggregate.ApprovedAtUtc.ShouldBe(approvedAt);
    }

    [Fact]
    public void Approve_WithNextReviewDate_SetsNextReviewAtUtc()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var nextReview = Now.AddDays(90);

        // Act
        aggregate.Approve("admin@company.com", Now.AddHours(2), nextReview);

        // Assert
        aggregate.NextReviewAtUtc.ShouldBe(nextReview);
    }

    [Fact]
    public void Approve_WithoutNextReviewDate_NextReviewAtUtcIsNull()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.Approve("admin@company.com", Now.AddHours(2));

        // Assert
        aggregate.NextReviewAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Approve_FromInReview_RaisesDPIAApprovedEvent()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();
        var nextReview = Now.AddDays(90);

        // Act
        aggregate.Approve("admin@company.com", Now.AddHours(2), nextReview);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIAApproved>();
        evt.AssessmentId.ShouldBe(aggregate.Id);
        evt.ApprovedBy.ShouldBe("admin@company.com");
        evt.NextReviewAtUtc.ShouldBe(nextReview);
        evt.OccurredAtUtc.ShouldBe(Now.AddHours(2));
    }

    [Fact]
    public void Approve_FromDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.Approve("admin@company.com", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Draft");
    }

    [Fact]
    public void Approve_FromApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate();

        // Act
        var act = () => aggregate.Approve("admin@company.com", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Approved");
    }

    [Fact]
    public void Approve_FromRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRejectedAggregate();

        // Act
        var act = () => aggregate.Approve("admin@company.com", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Rejected");
    }

    [Fact]
    public void Approve_FromRequiresRevision_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRequiresRevisionAggregate();

        // Act
        var act = () => aggregate.Approve("admin@company.com", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("RequiresRevision");
    }

    #endregion

    #region Reject

    [Fact]
    public void Reject_FromInReview_SetsStatusToRejected()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.Reject("admin@company.com", "Unacceptable risk level", Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Rejected);
    }

    [Fact]
    public void Reject_FromInReview_RaisesDPIARejectedEvent()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.Reject("admin@company.com", "Unacceptable risk level", Now.AddHours(2));

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIARejected>();
        evt.AssessmentId.ShouldBe(aggregate.Id);
        evt.RejectedBy.ShouldBe("admin@company.com");
        evt.Reason.ShouldBe("Unacceptable risk level");
        evt.OccurredAtUtc.ShouldBe(Now.AddHours(2));
    }

    [Fact]
    public void Reject_FromDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.Reject("admin@company.com", "Reason", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Draft");
    }

    [Fact]
    public void Reject_FromApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate();

        // Act
        var act = () => aggregate.Reject("admin@company.com", "Reason", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Approved");
    }

    #endregion

    #region RequestRevision

    [Fact]
    public void RequestRevision_FromInReview_SetsStatusToRequiresRevision()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.RequestRevision("dpo@company.com", "Insufficient mitigation detail", Now.AddHours(2));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.RequiresRevision);
    }

    [Fact]
    public void RequestRevision_FromInReview_RaisesDPIARevisionRequestedEvent()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        aggregate.RequestRevision("dpo@company.com", "Needs more detail", Now.AddHours(2));

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIARevisionRequested>();
        evt.AssessmentId.ShouldBe(aggregate.Id);
        evt.RequestedBy.ShouldBe("dpo@company.com");
        evt.Reason.ShouldBe("Needs more detail");
        evt.OccurredAtUtc.ShouldBe(Now.AddHours(2));
    }

    [Fact]
    public void RequestRevision_FromDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.RequestRevision("dpo@company.com", "Reason", Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Draft");
    }

    [Fact]
    public void RequestRevision_FromApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate();

        // Act
        var act = () => aggregate.RequestRevision("dpo@company.com", "Reason", Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Approved");
    }

    #endregion

    #region Expire

    [Fact]
    public void Expire_FromApproved_SetsStatusToExpired()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: Now.AddDays(90));

        // Act
        aggregate.Expire(Now.AddDays(91));

        // Assert
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Expired);
    }

    [Fact]
    public void Expire_FromApproved_RaisesDPIAExpiredEvent()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: Now.AddDays(90));
        var expiredAt = Now.AddDays(91);

        // Act
        aggregate.Expire(expiredAt);

        // Assert
        var evt = aggregate.UncommittedEvents[^1].ShouldBeOfType<DPIAExpiredEvent>();
        evt.AssessmentId.ShouldBe(aggregate.Id);
        evt.OccurredAtUtc.ShouldBe(expiredAt);
    }

    [Fact]
    public void Expire_FromDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var act = () => aggregate.Expire(Now.AddHours(1));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Draft");
    }

    [Fact]
    public void Expire_FromInReview_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        var act = () => aggregate.Expire(Now.AddHours(2));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("InReview");
    }

    [Fact]
    public void Expire_FromRejected_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRejectedAggregate();

        // Act
        var act = () => aggregate.Expire(Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Rejected");
    }

    [Fact]
    public void Expire_FromRequiresRevision_ThrowsInvalidOperationException()
    {
        // Arrange
        var aggregate = CreateRequiresRevisionAggregate();

        // Act
        var act = () => aggregate.Expire(Now.AddHours(3));

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("RequiresRevision");
    }

    #endregion

    #region IsCurrent

    [Fact]
    public void IsCurrent_ApprovedWithNoReviewDate_ReturnsTrue()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: null);

        // Act
        var result = aggregate.IsCurrent(Now.AddDays(365));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCurrent_ApprovedWithFutureReviewDate_ReturnsTrue()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: Now.AddDays(90));

        // Act
        var result = aggregate.IsCurrent(Now.AddDays(30));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsCurrent_ApprovedWithPastReviewDate_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: Now.AddDays(90));

        // Act
        var result = aggregate.IsCurrent(Now.AddDays(91));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_ApprovedWithExactReviewDate_ReturnsFalse()
    {
        // Arrange
        var nextReview = Now.AddDays(90);
        var aggregate = CreateApprovedAggregate(nextReviewAtUtc: nextReview);

        // Act
        var result = aggregate.IsCurrent(nextReview);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_Draft_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateDraftAggregate();

        // Act
        var result = aggregate.IsCurrent(Now);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_InReview_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateInReviewAggregate();

        // Act
        var result = aggregate.IsCurrent(Now.AddHours(2));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_Rejected_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateRejectedAggregate();

        // Act
        var result = aggregate.IsCurrent(Now.AddHours(3));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsCurrent_Expired_ReturnsFalse()
    {
        // Arrange
        var aggregate = CreateExpiredAggregate();

        // Act
        var result = aggregate.IsCurrent(Now.AddDays(92));

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Create_Evaluate_Approve_Expire()
    {
        // Create
        var id = Guid.NewGuid();
        var aggregate = DPIAAggregate.Create(
            id,
            "MyApp.Commands.ProcessUserData",
            Now,
            "AutomatedDecisionMaking",
            "High-risk profiling",
            "tenant-1",
            "module-1");
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Draft);
        aggregate.Version.ShouldBe(1);

        // Evaluate
        var result = CreateDefaultResult();
        aggregate.Evaluate(result, Now.AddHours(1));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.InReview);
        aggregate.Result.ShouldNotBeNull();
        aggregate.Version.ShouldBe(2);

        // Approve
        var nextReview = Now.AddDays(90);
        aggregate.Approve("admin@company.com", Now.AddHours(2), nextReview);
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Approved);
        aggregate.ApprovedAtUtc.ShouldBe(Now.AddHours(2));
        aggregate.NextReviewAtUtc.ShouldBe(nextReview);
        aggregate.IsCurrent(Now.AddDays(30)).ShouldBeTrue();
        aggregate.Version.ShouldBe(3);

        // Expire
        aggregate.Expire(Now.AddDays(91));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Expired);
        aggregate.IsCurrent(Now.AddDays(91)).ShouldBeFalse();
        aggregate.Version.ShouldBe(4);

        // Verify full event stream
        aggregate.UncommittedEvents.Count.ShouldBe(4);
        aggregate.UncommittedEvents[0].ShouldBeOfType<DPIACreated>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<DPIAEvaluated>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<DPIAApproved>();
        aggregate.UncommittedEvents[3].ShouldBeOfType<DPIAExpiredEvent>();
    }

    [Fact]
    public void FullLifecycle_Create_Evaluate_RequestRevision_Reevaluate_Approve()
    {
        // Create
        var aggregate = CreateDraftAggregate();
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Draft);

        // Evaluate
        aggregate.Evaluate(CreateDefaultResult(), Now.AddHours(1));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.InReview);

        // Request revision
        aggregate.RequestRevision("dpo@company.com", "Needs more mitigation detail", Now.AddHours(2));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.RequiresRevision);

        // Re-evaluate after revision
        var updatedResult = new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [new RiskItem("Data Minimization", RiskLevel.Medium, "Reduced data collection", null)],
            ProposedMitigations = [new Mitigation("Field-level encryption", "Technical", true, Now.AddHours(3))],
            RequiresPriorConsultation = false,
            AssessedAtUtc = Now.AddHours(4),
        };
        aggregate.Evaluate(updatedResult, Now.AddHours(4));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.InReview);
        aggregate.Result!.OverallRisk.ShouldBe(RiskLevel.Medium);

        // Approve
        aggregate.Approve("admin@company.com", Now.AddHours(5));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Approved);

        // Verify event stream
        aggregate.UncommittedEvents.Count.ShouldBe(5);
        aggregate.UncommittedEvents[0].ShouldBeOfType<DPIACreated>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<DPIAEvaluated>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<DPIARevisionRequested>();
        aggregate.UncommittedEvents[3].ShouldBeOfType<DPIAEvaluated>();
        aggregate.UncommittedEvents[4].ShouldBeOfType<DPIAApproved>();
    }

    [Fact]
    public void FullLifecycle_Create_Evaluate_DPOConsultation_Approve()
    {
        // Create and evaluate
        var aggregate = CreateInReviewAggregate();

        // Request DPO consultation
        var consultationId = Guid.NewGuid();
        aggregate.RequestDPOConsultation(consultationId, "Jane Doe", "jane@company.com", Now.AddHours(2));
        aggregate.DPOConsultation.ShouldNotBeNull();
        aggregate.DPOConsultation!.Decision.ShouldBe(DPOConsultationDecision.Pending);

        // Record DPO response
        aggregate.RecordDPOResponse(
            consultationId,
            DPOConsultationDecision.Approved,
            Now.AddHours(3),
            comments: "Assessment is thorough");
        aggregate.DPOConsultation.Decision.ShouldBe(DPOConsultationDecision.Approved);

        // Approve
        aggregate.Approve("admin@company.com", Now.AddHours(4));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Approved);

        // Verify event stream
        aggregate.UncommittedEvents.Count.ShouldBe(5);
        aggregate.UncommittedEvents[0].ShouldBeOfType<DPIACreated>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<DPIAEvaluated>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<DPIADPOConsultationRequested>();
        aggregate.UncommittedEvents[3].ShouldBeOfType<DPIADPOResponded>();
        aggregate.UncommittedEvents[4].ShouldBeOfType<DPIAApproved>();
    }

    [Fact]
    public void FullLifecycle_Create_Evaluate_Reject()
    {
        // Create and evaluate
        var aggregate = CreateInReviewAggregate();

        // Reject
        aggregate.Reject("admin@company.com", "Unacceptable residual risk", Now.AddHours(2));
        aggregate.Status.ShouldBe(DPIAAssessmentStatus.Rejected);

        // Verify event stream
        aggregate.UncommittedEvents.Count.ShouldBe(3);
        aggregate.UncommittedEvents[0].ShouldBeOfType<DPIACreated>();
        aggregate.UncommittedEvents[1].ShouldBeOfType<DPIAEvaluated>();
        aggregate.UncommittedEvents[2].ShouldBeOfType<DPIARejected>();
    }

    #endregion
}
