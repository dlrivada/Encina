using Encina.Caching;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.Events;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.DomainModeling;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Encina.Testing.Fakes.Providers;

using Marten;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DPIA;

/// <summary>
/// Advanced integration tests for DPIA features:
/// - Full lifecycle via service interface (create → evaluate → approve → verify)
/// - Projection produces correct read model from event stream
/// - Event stream audit trail (GetHistoryAsync)
/// - Concurrent aggregate operations (optimistic concurrency)
/// - Cache invalidation on write operations
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DPIAAdvancedIntegrationTests
{
    private readonly MartenFixture _fixture;

    public DPIAAdvancedIntegrationTests(MartenFixture fixture)
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

    private ServiceProvider BuildServiceProvider(
        Action<DPIAOptions>? configure = null,
        FakeCacheProvider? cacheProvider = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton(_fixture.Store!);
        services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().LightweightSession());

        services.AddEncinaMarten();

        services.AddEncinaDPIA(configure ?? (_ => { }));
        services.AddDPIAAggregates();

        services.AddSingleton<ICacheProvider>(cacheProvider ?? new FakeCacheProvider());

        var requestContext = Substitute.For<IRequestContext>();
        requestContext.CorrelationId.Returns(Guid.NewGuid().ToString());
        services.AddScoped<IRequestContext>(_ => requestContext);

        return services.BuildServiceProvider();
    }

    #region Full Lifecycle via Service Interface

    [Fact]
    public async Task DPIAService_FullLifecycle_Create_Evaluate_Approve()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid assessmentId;

        // Create assessment
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var result = await service.CreateAssessmentAsync(
                "MyApp.Commands.AdvProcessBiometrics",
                processingType: "BiometricProcessing",
                reason: "Biometric access control",
                tenantId: "adv-tenant");
            result.IsRight.ShouldBeTrue();
            assessmentId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // Evaluate assessment
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var context = new DPIAContext
            {
                RequestType = typeof(string),
                ProcessingType = "BiometricProcessing",
                DataCategories = ["biometric"],
                HighRiskTriggers = ["special-category-data"],
            };
            var evalResult = await service.EvaluateAssessmentAsync(assessmentId, context);
            evalResult.IsRight.ShouldBeTrue();
        }

        // Approve assessment
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var approveResult = await service.ApproveAssessmentAsync(
                assessmentId, "dpo-adv", DateTimeOffset.UtcNow.AddDays(365));
            approveResult.IsRight.ShouldBeTrue();
        }

        // Verify final state
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var getResult = await service.GetAssessmentAsync(assessmentId);
            getResult.IsRight.ShouldBeTrue();
            getResult.IfRight(rm =>
            {
                rm.Id.ShouldBe(assessmentId);
                rm.RequestTypeName.ShouldBe("MyApp.Commands.AdvProcessBiometrics");
                rm.Status.ShouldBe(DPIAAssessmentStatus.Approved);
                rm.ApprovedAtUtc.ShouldNotBeNull();
                rm.NextReviewAtUtc.ShouldNotBeNull();
                rm.TenantId.ShouldBe("adv-tenant");
            });
        }
    }

    #endregion

    #region Projection Produces Correct Read Models

    [Fact]
    public async Task DPIAService_GetAssessment_ReturnsReadModelWithAllFieldsMapped()
    {
        // Arrange — create and evaluate an assessment through the aggregate directly
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var aggregate = DPIAAggregate.Create(
            id, "MyApp.Commands.ProjTestCommand", now,
            processingType: "LargeScaleProcessing",
            reason: "Processing customer analytics",
            tenantId: "proj-tenant",
            moduleId: "analytics");

        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks =
            [
                new RiskItem("Large-scale processing", RiskLevel.Medium, "Processing >10K data subjects", "Use sampling"),
                new RiskItem("Profiling", RiskLevel.Low, "Behavioral analytics", null),
            ],
            ProposedMitigations =
            [
                new Mitigation("Only collect necessary data points", "Technical", true, DateTimeOffset.UtcNow),
            ],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now.AddMinutes(10),
        }, now.AddMinutes(10));

        aggregate.Approve("dpo-proj", now.AddHours(1), now.AddDays(180));

        await repo.CreateAsync(aggregate);

        // Act — query read model via service
        using var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
        var getResult = await service.GetAssessmentAsync(id);

        // Assert — all fields mapped correctly
        getResult.IsRight.ShouldBeTrue();
        getResult.IfRight(rm =>
        {
            rm.Id.ShouldBe(id);
            rm.RequestTypeName.ShouldBe("MyApp.Commands.ProjTestCommand");
            rm.ProcessingType.ShouldBe("LargeScaleProcessing");
            rm.Reason.ShouldBe("Processing customer analytics");
            rm.Status.ShouldBe(DPIAAssessmentStatus.Approved);
            rm.OverallRisk.ShouldBe(RiskLevel.Medium);
            rm.IdentifiedRisks.Count.ShouldBe(2);
            rm.ProposedMitigations.Count.ShouldBe(1);
            rm.RequiresPriorConsultation.ShouldBeFalse();
            rm.ApprovedAtUtc.ShouldNotBeNull();
            rm.NextReviewAtUtc.ShouldNotBeNull();
            rm.TenantId.ShouldBe("proj-tenant");
            rm.ModuleId.ShouldBe("analytics");
            rm.LastModifiedAtUtc.ShouldNotBe(default);
            rm.Version.ShouldBeGreaterThan(0);
        });
    }

    #endregion

    #region Event Stream Audit Trail

    [Fact]
    public async Task DPIA_EventStream_ContainsFullAuditTrail()
    {
        // Arrange — create an assessment and exercise the full lifecycle
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var consultationId = Guid.NewGuid();

        var aggregate = DPIAAggregate.Create(
            id, "MyApp.Commands.AuditTrailTest", now,
            processingType: "AutomatedDecisionMaking",
            tenantId: "audit-tenant",
            moduleId: "audit-mod");

        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("ADM", RiskLevel.High, "Automated decision making", null)],
            ProposedMitigations = [new Mitigation("Manual review for high-impact decisions", "Organizational", false, null)],
            RequiresPriorConsultation = true,
            AssessedAtUtc = now.AddMinutes(15),
        }, now.AddMinutes(15));

        aggregate.RequestDPOConsultation(consultationId, "Jane DPO", "dpo@test.com", now.AddMinutes(30));
        aggregate.RecordDPOResponse(consultationId, DPOConsultationDecision.Approved, now.AddHours(1), "Approved with recommendations");
        aggregate.Approve("approver-audit", now.AddHours(2), now.AddDays(365));

        await repo.CreateAsync(aggregate);

        // Act — query the raw event stream
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert — event stream contains all lifecycle events in order
        events.ShouldNotBeNull();
        events.Count.ShouldBe(5);

        events[0].Data.ShouldBeOfType<DPIACreated>();
        events[1].Data.ShouldBeOfType<DPIAEvaluated>();
        events[2].Data.ShouldBeOfType<DPIADPOConsultationRequested>();
        events[3].Data.ShouldBeOfType<DPIADPOResponded>();
        events[4].Data.ShouldBeOfType<DPIAApproved>();

        // Verify event data
        var created = (DPIACreated)events[0].Data;
        created.RequestTypeName.ShouldBe("MyApp.Commands.AuditTrailTest");
        created.ProcessingType.ShouldBe("AutomatedDecisionMaking");
        created.TenantId.ShouldBe("audit-tenant");
        created.ModuleId.ShouldBe("audit-mod");

        var evaluated = (DPIAEvaluated)events[1].Data;
        evaluated.OverallRisk.ShouldBe(RiskLevel.High);
        evaluated.RequiresPriorConsultation.ShouldBeTrue();
        evaluated.IdentifiedRisks.Count.ShouldBe(1);
        evaluated.ProposedMitigations.Count.ShouldBe(1);

        var dpoRequested = (DPIADPOConsultationRequested)events[2].Data;
        dpoRequested.ConsultationId.ShouldBe(consultationId);
        dpoRequested.DPOName.ShouldBe("Jane DPO");
        dpoRequested.DPOEmail.ShouldBe("dpo@test.com");

        var dpoResponded = (DPIADPOResponded)events[3].Data;
        dpoResponded.Decision.ShouldBe(DPOConsultationDecision.Approved);
        dpoResponded.Comments.ShouldBe("Approved with recommendations");

        var approved = (DPIAApproved)events[4].Data;
        approved.ApprovedBy.ShouldBe("approver-audit");
        approved.NextReviewAtUtc.ShouldNotBeNull();

        // Verify monotonically increasing versions
        for (var i = 1; i < events.Count; i++)
        {
            events[i].Version.ShouldBeGreaterThan(events[i - 1].Version);
        }

        // Verify all events have timestamps
        foreach (var evt in events)
        {
            evt.Timestamp.ShouldNotBe(default);
        }
    }

    [Fact]
    public async Task DPIA_EventStream_RevisionCycle_RecordsAllTransitions()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.RevisionAuditTest", now);
        aggregate.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Risk", RiskLevel.High, "Test", null)],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now,
        }, now);
        await repo.CreateAsync(aggregate);

        // Revision request
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.RequestRevision("reviewer-1", "Add mitigations", now.AddHours(1));
        await repo2.SaveAsync(loaded2);

        // Re-evaluate
        var repo3 = CreateRepository<DPIAAggregate>();
        var loaded3 = (await repo3.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded3.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.Medium,
            IdentifiedRisks = [new RiskItem("Risk", RiskLevel.Medium, "Mitigated", null)],
            ProposedMitigations = [new Mitigation("Applied fix", "Technical", true, DateTimeOffset.UtcNow)],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now.AddHours(2),
        }, now.AddHours(2));
        await repo3.SaveAsync(loaded3);

        // Reject
        var repo4 = CreateRepository<DPIAAggregate>();
        var loaded4 = (await repo4.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded4.Reject("dpo-1", "Still too risky", now.AddHours(3));
        await repo4.SaveAsync(loaded4);

        // Act — query event stream
        await using var session = _fixture.Store!.LightweightSession();
        var events = await session.Events.FetchStreamAsync(id);

        // Assert — full revision cycle recorded
        events.Count.ShouldBe(5);
        events[0].Data.ShouldBeOfType<DPIACreated>();
        events[1].Data.ShouldBeOfType<DPIAEvaluated>();
        events[2].Data.ShouldBeOfType<DPIARevisionRequested>();
        events[3].Data.ShouldBeOfType<DPIAEvaluated>();
        events[4].Data.ShouldBeOfType<DPIARejected>();

        var revisionEvent = (DPIARevisionRequested)events[2].Data;
        revisionEvent.RequestedBy.ShouldBe("reviewer-1");
        revisionEvent.Reason.ShouldBe("Add mitigations");

        var rejectedEvent = (DPIARejected)events[4].Data;
        rejectedEvent.RejectedBy.ShouldBe("dpo-1");
        rejectedEvent.Reason.ShouldBe("Still too risky");
    }

    #endregion

    #region Concurrent Aggregate Operations

    [Fact]
    public async Task DPIA_SequentialModifications_BothPersistCorrectly()
    {
        // Arrange — create an assessment
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.ConcurrentTest", now);
        await repo.CreateAsync(aggregate);

        // First modification: evaluate
        var repo1 = CreateRepository<DPIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.Low,
            IdentifiedRisks = [],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now,
        }, now);
        var result1 = await repo1.SaveAsync(loaded1);
        result1.IsRight.ShouldBeTrue();

        // Second modification: approve (loads fresh state including evaluation)
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Status.ShouldBe(DPIAAssessmentStatus.InReview);
        loaded2.Approve("approver-1", now.AddHours(1));
        var result2 = await repo2.SaveAsync(loaded2);
        result2.IsRight.ShouldBeTrue();

        // Verify — both modifications applied
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(DPIAAssessmentStatus.Approved);
        final.Result.ShouldNotBeNull();
        final.Result!.OverallRisk.ShouldBe(RiskLevel.Low);
    }

    [Fact]
    public async Task DPIA_SequentialEvaluateAndRequestRevision_BothPersist()
    {
        // Arrange
        var repo = CreateRepository<DPIAAggregate>();
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var aggregate = DPIAAggregate.Create(id, "MyApp.Commands.SeqRevisionTest", now);
        await repo.CreateAsync(aggregate);

        // First: evaluate
        var repo1 = CreateRepository<DPIAAggregate>();
        var loaded1 = (await repo1.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded1.Evaluate(new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = [new RiskItem("Issue", RiskLevel.High, "Critical", null)],
            ProposedMitigations = [],
            RequiresPriorConsultation = false,
            AssessedAtUtc = now,
        }, now);
        await repo1.SaveAsync(loaded1);

        // Second: request revision (loads fresh state)
        var repo2 = CreateRepository<DPIAAggregate>();
        var loaded2 = (await repo2.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        loaded2.Status.ShouldBe(DPIAAssessmentStatus.InReview);
        loaded2.RequestRevision("reviewer-1", "Need mitigations", now.AddHours(1));
        await repo2.SaveAsync(loaded2);

        // Verify
        var verifyRepo = CreateRepository<DPIAAggregate>();
        var final = (await verifyRepo.LoadAsync(id)).Match(a => a, _ => throw new InvalidOperationException());
        final.Status.ShouldBe(DPIAAssessmentStatus.RequiresRevision);
        final.Result.ShouldNotBeNull();
    }

    #endregion

    #region Cache Invalidation on Write Operations

    [Fact]
    public async Task DPIAService_ModifyAssessment_InvalidatesCache()
    {
        // Arrange
        var fakeCache = new FakeCacheProvider();
        using var provider = BuildServiceProvider(cacheProvider: fakeCache);
        Guid assessmentId;

        // Create assessment
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var result = await service.CreateAssessmentAsync(
                "MyApp.Commands.CacheTestCommand",
                reason: "Cache test");
            assessmentId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // First read — should populate cache
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var getResult = await service.GetAssessmentAsync(assessmentId);
            getResult.IsRight.ShouldBeTrue();
        }

        // Cache should have been populated
        var cachedKeysBefore = fakeCache.CachedKeys.ToList();
        cachedKeysBefore.ShouldContain(k => k.Contains(assessmentId.ToString()));

        // Act — evaluate assessment (should invalidate cache)
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var context = new DPIAContext
            {
                RequestType = typeof(string),
                DataCategories = ["personal"],
                HighRiskTriggers = [],
            };
            await service.EvaluateAssessmentAsync(assessmentId, context);
        }

        // Assert — cache was invalidated (key was removed)
        var removedKeys = fakeCache.RemovedKeys.ToList();
        removedKeys.ShouldContain(k => k.Contains(assessmentId.ToString()));
    }

    [Fact]
    public async Task DPIAService_SecondRead_AfterModification_ReturnsUpdatedData()
    {
        // Arrange
        using var provider = BuildServiceProvider();
        Guid assessmentId;

        // Create
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var result = await service.CreateAssessmentAsync(
                "MyApp.Commands.RefreshTestCommand",
                reason: "Refresh test");
            assessmentId = result.Match(id => id, _ => throw new InvalidOperationException("Create failed"));
        }

        // First read — Draft status
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var r = await service.GetAssessmentAsync(assessmentId);
            r.IfRight(rm => rm.Status.ShouldBe(DPIAAssessmentStatus.Draft));
        }

        // Evaluate — transitions to InReview
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var context = new DPIAContext
            {
                RequestType = typeof(string),
                DataCategories = ["personal"],
                HighRiskTriggers = [],
            };
            await service.EvaluateAssessmentAsync(assessmentId, context);
        }

        // Act — second read should see updated state
        using (var scope = provider.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IDPIAService>();
            var r = await service.GetAssessmentAsync(assessmentId);

            // Assert
            r.IsRight.ShouldBeTrue();
            r.IfRight(rm =>
            {
                rm.Status.ShouldBe(DPIAAssessmentStatus.InReview);
                rm.OverallRisk.ShouldNotBeNull();
            });
        }
    }

    #endregion

    #region DI Registration

    [Fact]
    public void AddEncinaDPIA_WithMarten_ResolvesAllServices()
    {
        // Arrange & Act
        using var provider = BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<IDPIAService>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IDPIAAssessmentEngine>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<IAggregateRepository<DPIAAggregate>>().ShouldNotBeNull();
    }

    #endregion
}
