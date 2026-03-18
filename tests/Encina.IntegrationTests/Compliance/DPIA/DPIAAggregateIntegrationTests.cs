using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DPIA;

/// <summary>
/// Integration tests for DPIA aggregates persisted via Marten against real PostgreSQL.
/// Verifies event store persistence, aggregate loading, state reconstruction, and lifecycle transitions.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DPIAAggregateIntegrationTests
{
    private readonly MartenFixture _fixture;

    public DPIAAggregateIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    private MartenAggregateRepository<T> CreateRepository<T>() where T : class, IAggregate
    {
        var session = _fixture.Store!.LightweightSession();
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        var logger = NullLoggerFactory.Instance.CreateLogger<MartenAggregateRepository<T>>();
        var options = Options.Create(new EncinaMartenOptions());
        return new MartenAggregateRepository<T>(
            session,
            requestContext,
            (Microsoft.Extensions.Logging.ILogger<MartenAggregateRepository<T>>)logger,
            options);
    }

    #region Create and Load

    [Fact]
    public async Task DPIA_CreateAndLoad_PersistsAndReconstructsState()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(
            id, "MyApp.Commands.ProcessHealthData", now,
            processingType: "AutomatedDecisionMaking",
            reason: "Processing health records for insurance scoring",
            tenantId: "tenant-1",
            moduleId: "compliance");

        // Act
        var createResult = await repo.CreateAsync(aggregate);

        // Assert — creation succeeds
        createResult.IsRight.ShouldBeTrue();

        // Act — load from event store
        var loadRepo = CreateRepository<DPIAAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);

        // Assert — state reconstructed from events
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.RequestTypeName.ShouldBe("MyApp.Commands.ProcessHealthData");
            loaded.ProcessingType.ShouldBe("AutomatedDecisionMaking");
            loaded.Reason.ShouldBe("Processing health records for insurance scoring");
            loaded.Status.ShouldBe(DPIAAssessmentStatus.Draft);
            loaded.TenantId.ShouldBe("tenant-1");
            loaded.ModuleId.ShouldBe("compliance");
            loaded.Result.ShouldBeNull();
            loaded.DPOConsultation.ShouldBeNull();
            loaded.ApprovedAtUtc.ShouldBeNull();
            loaded.NextReviewAtUtc.ShouldBeNull();
        });
    }

    [Fact]
    public async Task DPIA_CreateWithMinimalParameters_PersistsCorrectly()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.SimpleQuery", now);

        // Act
        await repo.CreateAsync(aggregate);

        // Assert
        var loadRepo = CreateRepository<DPIAAggregate>();
        var loadResult = await loadRepo.LoadAsync(id);
        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(loaded =>
        {
            loaded.Id.ShouldBe(id);
            loaded.RequestTypeName.ShouldBe("MyApp.Commands.SimpleQuery");
            loaded.ProcessingType.ShouldBeNull();
            loaded.Reason.ShouldBeNull();
            loaded.TenantId.ShouldBeNull();
            loaded.ModuleId.ShouldBeNull();
        });
    }

    #endregion

    #region Full Lifecycle — Happy Path

    [Fact]
    public async Task DPIA_FullLifecycle_Draft_Evaluate_Approve_Expire()
    {
        // Arrange — create assessment
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(
            id, "MyApp.Commands.ProcessBiometricData", now,
            processingType: "BiometricProcessing", reason: "Facial recognition access control");
        await repo.CreateAsync(aggregate);

        // Act — evaluate risk
        var repo1 = CreateRepository<DPIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Biometric data processing", RiskLevel.High, "Article 9 special category data", "Implement pseudonymization")],
            ProposedMitigations = [new Mitigation("Apply pseudonymization to biometric templates", "Technical", false, null)],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now.AddMinutes(30),
        }, now.AddMinutes(30));
        await repo1.SaveAsync(loaded1);

        // Act — approve
        var reviewDate = now.AddDays(365);
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Approve("dpo-1", now.AddHours(1), reviewDate);
        await repo2.SaveAsync(loaded2);

        // Act — expire
        var repo3 = CreateRepository<DPIAAggregate>();
        var loaded3 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.Expire(now.AddDays(366));
        await repo3.SaveAsync(loaded3);

        // Assert — final state from fresh load
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var finalResult = await verifyRepo.LoadAsync(id);
        finalResult.IsRight.ShouldBeTrue();
        finalResult.IfRight(final =>
        {
            final.Status.ShouldBe(DPIAAssessmentStatus.Expired);
            final.Result.ShouldNotBeNull();
            final.Result!.OverallRisk.ShouldBe(RiskLevel.High);
            final.Result.IdentifiedRisks.Count.ShouldBe(1);
            final.Result.ProposedMitigations.Count.ShouldBe(1);
            final.ApprovedAtUtc.ShouldNotBeNull();
            final.NextReviewAtUtc.ShouldBe(reviewDate);
        });
    }

    #endregion

    #region Rejection Path

    [Fact]
    public async Task DPIA_Rejection_TransitionsToRejected()
    {
        // Arrange — create and evaluate
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.MassProfile", now);
        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.VeryHigh,
            IdentifiedRisks = [new RiskItem("Mass profiling", RiskLevel.VeryHigh, "Systematic profiling at large scale", null)],
            ProposedMitigations = [],
            RequiresPriorConsultation = true,
            AssessedAtUtc = now,
        }, now);
        await repo.CreateAsync(aggregate);

        // Act — reject
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded.Reject("dpo-1", "Risk is unacceptable without mitigations", now.AddHours(2));
        await repo2.SaveAsync(loaded);

        // Assert
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(DPIAAssessmentStatus.Rejected);
    }

    #endregion

    #region Revision Path

    [Fact]
    public async Task DPIA_RevisionCycle_RequiresRevision_ThenReEvaluate_ThenApprove()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.TrackEmployees", now);
        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Systematic monitoring", RiskLevel.High, "Employee tracking", null)],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now,
        }, now);
        await repo.CreateAsync(aggregate);

        // Act — request revision
        var repo1 = CreateRepository<DPIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.RequestRevision("reviewer-1", "Missing mitigation measures for employee tracking", now.AddHours(1));
        await repo1.SaveAsync(loaded1);

        // Assert — status is RequiresRevision
        var verifyRepo1 = CreateRepository<DPIAAggregate>();
        var afterRevision = (await verifyRepo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        afterRevision.Status.ShouldBe(DPIAAssessmentStatus.RequiresRevision);

        // Act — re-evaluate with mitigations
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [new RiskItem("Systematic monitoring", RiskLevel.Medium, "Employee tracking with mitigations", null)],
            ProposedMitigations = [new Mitigation("Restrict tracking data access to HR", "Organizational", true, DateTimeOffset.UtcNow)],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now.AddHours(2),
        }, now.AddHours(2));
        await repo2.SaveAsync(loaded2);

        // Act — approve
        var repo3 = CreateRepository<DPIAAggregate>();
        var loaded3 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded3.Approve("dpo-1", now.AddHours(3));
        await repo3.SaveAsync(loaded3);

        // Assert — approved with updated risk
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.Status.ShouldBe(DPIAAssessmentStatus.Approved);
        final.Result!.OverallRisk.ShouldBe(RiskLevel.Medium);
        final.Result.ProposedMitigations.Count.ShouldBe(1);
    }

    #endregion

    #region DPO Consultation

    [Fact]
    public async Task DPIA_DPOConsultation_FullCycle_PersistsCorrectly()
    {
        // Arrange — create and evaluate
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.ProcessChildData", now);
        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Vulnerable subjects", RiskLevel.High, "Processing children's data", null)],
            ProposedMitigations = [new Mitigation("Require verified parental consent", "Organizational", false, null)],
            RequiresPriorConsultation = true,
            AssessedAtUtc = now,
        }, now);
        await repo.CreateAsync(aggregate);

        // Act — request DPO consultation
        var consultationId = Guid.NewGuid();
        var repo1 = CreateRepository<DPIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded1.RequestDPOConsultation(consultationId, "Jane Doe", "dpo@company.com", now.AddHours(1));
        await repo1.SaveAsync(loaded1);

        // Assert — consultation is pending
        var repo1v = CreateRepository<DPIAAggregate>();
        var afterRequest = (await repo1v.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        afterRequest.DPOConsultation.ShouldNotBeNull();
        afterRequest.DPOConsultation!.Id.ShouldBe(consultationId);
        afterRequest.DPOConsultation.DPOName.ShouldBe("Jane Doe");
        afterRequest.DPOConsultation.DPOEmail.ShouldBe("dpo@company.com");
        afterRequest.DPOConsultation.Decision.ShouldBe(DPOConsultationDecision.Pending);

        // Act — DPO responds with conditional approval
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        loaded2.RecordDPOResponse(
            consultationId,
            DPOConsultationDecision.ConditionallyApproved,
            now.AddHours(2),
            comments: "Acceptable with parental consent mechanism",
            conditions: "Must implement verified parental consent before production");
        await repo2.SaveAsync(loaded2);

        // Assert — DPO response recorded
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));
        final.DPOConsultation.ShouldNotBeNull();
        final.DPOConsultation!.Decision.ShouldBe(DPOConsultationDecision.ConditionallyApproved);
        final.DPOConsultation.Comments.ShouldBe("Acceptable with parental consent mechanism");
        final.DPOConsultation.Conditions.ShouldBe("Must implement verified parental consent before production");
        final.DPOConsultation.RespondedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region IsCurrent Validation

    [Fact]
    public async Task DPIA_IsCurrent_ReturnsTrueForApprovedWithFutureReview()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var reviewDate = now.AddDays(365);
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.ValidCommand", now);
        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.Low,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now,
        }, now);
        aggregate.Approve("approver-1", now, reviewDate);
        await repo.CreateAsync(aggregate);

        // Act
        var loadRepo = CreateRepository<DPIAAggregate>();
        var loaded = (await loadRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException("Load failed"));

        // Assert
        loaded.IsCurrent(DateTimeOffset.UtcNow).ShouldBeTrue();
        loaded.IsCurrent(reviewDate.AddDays(1)).ShouldBeFalse();
    }

    #endregion

    #region Cross-Aggregate Isolation and Error Cases

    [Fact]
    public async Task LoadNonExistentAggregate_ReturnsLeft()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();

        // Act
        var result = await repo.LoadAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task MultipleAssessments_AreIsolated()
    {
        // Arrange — create two independent assessments
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var repo1 = CreateRepository<DPIAAggregate>();
        var agg1 = DPIAAggregate.Create(id1, "MyApp.Commands.Type1", now, tenantId: "tenant-a");
        await repo1.CreateAsync(agg1);

        var repo2 = CreateRepository<DPIAAggregate>();
        var agg2 = DPIAAggregate.Create(id2, "MyApp.Commands.Type2", now, tenantId: "tenant-b");
        await repo2.CreateAsync(agg2);

        // Act — load each from fresh repositories
        var verifyRepo1 = CreateRepository<DPIAAggregate>();
        var verifyRepo2 = CreateRepository<DPIAAggregate>();

        var result1 = await verifyRepo1.LoadAsync(id1);
        var result2 = await verifyRepo2.LoadAsync(id2);

        // Assert — each loaded independently
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        result1.IfRight(a => a.RequestTypeName.ShouldBe("MyApp.Commands.Type1"));
        result2.IfRight(a => a.RequestTypeName.ShouldBe("MyApp.Commands.Type2"));
    }

    #endregion
}
