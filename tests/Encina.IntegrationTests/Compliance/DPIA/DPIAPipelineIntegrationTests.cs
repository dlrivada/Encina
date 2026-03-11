using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Health;
using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.DPIA;

/// <summary>
/// Integration tests for the DPIA pipeline verifying DI registration,
/// options configuration, risk criteria, assessment engine lifecycle,
/// and concurrent access patterns using in-memory stores.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DPIAPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaDPIA_RegistersIDPIAStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetService<IDPIAStore>();
        store.ShouldNotBeNull();
        store.ShouldBeOfType<InMemoryDPIAStore>();
    }

    [Fact]
    public void AddEncinaDPIA_RegistersIDPIAAuditStore()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var auditStore = provider.GetService<IDPIAAuditStore>();
        auditStore.ShouldNotBeNull();
        auditStore.ShouldBeOfType<InMemoryDPIAAuditStore>();
    }

    [Fact]
    public void AddEncinaDPIA_RegistersIDPIATemplateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var templateProvider = provider.GetService<IDPIATemplateProvider>();
        templateProvider.ShouldNotBeNull();
        templateProvider.ShouldBeOfType<DefaultDPIATemplateProvider>();
    }

    [Fact]
    public void AddEncinaDPIA_RegistersIDPIAAssessmentEngine_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var engine = scope.ServiceProvider.GetService<IDPIAAssessmentEngine>();
        engine.ShouldNotBeNull();
        engine.ShouldBeOfType<DefaultDPIAAssessmentEngine>();
    }

    [Fact]
    public void AddEncinaDPIA_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();

        services.ShouldContain(
            d => d.ServiceType == typeof(IPipelineBehavior<,>)
              && d.ImplementationType == typeof(DPIARequiredPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaDPIA_StoresAreSingletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store1 = provider.GetService<IDPIAStore>();
        var store2 = provider.GetService<IDPIAStore>();

        store1.ShouldBeSameAs(store2);

        var audit1 = provider.GetService<IDPIAAuditStore>();
        var audit2 = provider.GetService<IDPIAAuditStore>();

        audit1.ShouldBeSameAs(audit2);
    }

    [Fact]
    public void AddEncinaDPIA_AssessmentEngineIsScoped_DifferentPerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        IDPIAAssessmentEngine? engine1, engine2;

        using (var scope1 = provider.CreateScope())
        {
            engine1 = scope1.ServiceProvider.GetService<IDPIAAssessmentEngine>();
        }

        using (var scope2 = provider.CreateScope())
        {
            engine2 = scope2.ServiceProvider.GetService<IDPIAAssessmentEngine>();
        }

        engine1.ShouldNotBeSameAs(engine2);
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddEncinaDPIA_DefaultOptions_HaveCorrectValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<DPIAOptions>>().Value;

        options.EnforcementMode.ShouldBe(DPIAEnforcementMode.Warn);
        options.BlockWithoutDPIA.ShouldBeFalse();
        options.DefaultReviewPeriod.ShouldBe(TimeSpan.FromDays(365));
        options.DPOEmail.ShouldBeNull();
        options.DPOName.ShouldBeNull();
        options.PublishNotifications.ShouldBeTrue();
        options.TrackAuditTrail.ShouldBeTrue();
        options.EnableExpirationMonitoring.ShouldBeFalse();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromHours(1));
        options.AutoRegisterFromAttributes.ShouldBeFalse();
        options.AutoDetectHighRisk.ShouldBeFalse();
        options.AssembliesToScan.ShouldBeEmpty();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaDPIA_CustomOptions_AreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.EnforcementMode = DPIAEnforcementMode.Block;
            options.DefaultReviewPeriod = TimeSpan.FromDays(180);
            options.DPOEmail = "dpo@test.com";
            options.DPOName = "Test DPO";
            options.PublishNotifications = false;
            options.TrackAuditTrail = false;
            options.ExpirationCheckInterval = TimeSpan.FromMinutes(30);
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<DPIAOptions>>().Value;

        options.EnforcementMode.ShouldBe(DPIAEnforcementMode.Block);
        options.BlockWithoutDPIA.ShouldBeTrue();
        options.DefaultReviewPeriod.ShouldBe(TimeSpan.FromDays(180));
        options.DPOEmail.ShouldBe("dpo@test.com");
        options.DPOName.ShouldBe("Test DPO");
        options.PublishNotifications.ShouldBeFalse();
        options.TrackAuditTrail.ShouldBeFalse();
        options.ExpirationCheckInterval.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void AddEncinaDPIA_BlockWithoutDPIA_SetsEnforcementModeToBlock()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.BlockWithoutDPIA = true;
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<DPIAOptions>>().Value;

        options.EnforcementMode.ShouldBe(DPIAEnforcementMode.Block);
    }

    #endregion

    #region Built-in Risk Criteria

    [Fact]
    public void AddEncinaDPIA_Registers6BuiltInRiskCriteria()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var criteria = provider.GetServices<IRiskCriterion>().ToList();

        criteria.Count.ShouldBe(6);
        criteria.ShouldContain(c => c is SystematicProfilingCriterion);
        criteria.ShouldContain(c => c is SpecialCategoryDataCriterion);
        criteria.ShouldContain(c => c is SystematicMonitoringCriterion);
        criteria.ShouldContain(c => c is AutomatedDecisionMakingCriterion);
        criteria.ShouldContain(c => c is LargeScaleProcessingCriterion);
        criteria.ShouldContain(c => c is VulnerableSubjectsCriterion);
    }

    [Fact]
    public void AddEncinaDPIA_RiskCriteria_HaveUniqueNames()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var criteria = provider.GetServices<IRiskCriterion>().ToList();
        var names = criteria.Select(c => c.Name).ToList();

        names.Distinct().Count().ShouldBe(names.Count);
    }

    #endregion

    #region Health Check

    [Fact]
    public void AddEncinaDPIA_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDPIA_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.AddHealthCheck = false;
        });

        services.Any(d => d.ServiceType == typeof(HealthCheckService))
            .ShouldBeFalse();
    }

    #endregion

    #region Conditional Service Registration

    [Fact]
    public void AddEncinaDPIA_WithExpirationMonitoring_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.EnableExpirationMonitoring = true;
        });

        services.ShouldContain(
            d => d.ServiceType == typeof(IHostedService)
              && d.ImplementationType == typeof(DPIAReviewReminderService));
    }

    [Fact]
    public void AddEncinaDPIA_WithoutExpirationMonitoring_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.EnableExpirationMonitoring = false;
        });

        services.ShouldNotContain(
            d => d.ServiceType == typeof(IHostedService)
              && d.ImplementationType == typeof(DPIAReviewReminderService));
    }

    [Fact]
    public void AddEncinaDPIA_WithAutoRegistration_RegistersAutoRegistrationHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.AutoRegisterFromAttributes = true;
        });

        services.ShouldContain(
            d => d.ServiceType == typeof(IHostedService)
              && d.ImplementationType == typeof(DPIAAutoRegistrationHostedService));
    }

    [Fact]
    public void AddEncinaDPIA_WithoutAutoRegistration_DoesNotRegisterAutoRegistrationHostedService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();

        services.ShouldNotContain(
            d => d.ServiceType == typeof(IHostedService)
              && d.ImplementationType == typeof(DPIAAutoRegistrationHostedService));
    }

    #endregion

    #region Assessment Engine Lifecycle

    [Fact]
    public async Task AssessmentEngine_FullLifecycle_AssessStoreRetrieveAudit()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.TrackAuditTrail = true;
            options.PublishNotifications = false;
            options.DPOEmail = "dpo@integration-test.com";
            options.DPOName = "Integration Test DPO";
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDPIAAssessmentEngine>();
        var store = provider.GetRequiredService<IDPIAStore>();
        var auditStore = provider.GetRequiredService<IDPIAAuditStore>();

        // Step 1: Perform an assessment with high-risk triggers
        var context = new DPIAContext
        {
            RequestType = typeof(DPIAPipelineIntegrationTests),
            ProcessingType = "AutomatedDecisionMaking",
            DataCategories = ["BiometricData", "HealthData"],
            HighRiskTriggers =
            [
                HighRiskTriggers.BiometricData,
                HighRiskTriggers.AutomatedDecisionMaking,
                HighRiskTriggers.LargeScaleProcessing
            ]
        };

        var assessResult = await engine.AssessAsync(context);
        assessResult.IsRight.ShouldBeTrue("Assessment should succeed");

        var dpiaResult = assessResult.Match(Right: r => r, Left: _ => null!);
        dpiaResult.ShouldNotBeNull();
        dpiaResult.IdentifiedRisks.ShouldNotBeEmpty();
        new[] { RiskLevel.High, RiskLevel.VeryHigh }.ShouldContain(dpiaResult.OverallRisk);

        // Step 2: Save assessment to store
        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = typeof(DPIAPipelineIntegrationTests).FullName!,
            Status = DPIAAssessmentStatus.Approved,
            Result = dpiaResult,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            NextReviewAtUtc = DateTimeOffset.UtcNow.AddDays(365)
        };

        var saveResult = await store.SaveAssessmentAsync(assessment);
        saveResult.IsRight.ShouldBeTrue("Save should succeed");

        // Step 3: Retrieve assessment by request type
        var getResult = await store.GetAssessmentAsync(typeof(DPIAPipelineIntegrationTests).FullName!);
        getResult.IsRight.ShouldBeTrue("Get should succeed");

        var retrievedOption = getResult.Match(
            Right: opt => opt,
            Left: _ => Option<DPIAAssessment>.None);
        retrievedOption.IsSome.ShouldBeTrue("Assessment should be found");
        retrievedOption.IfSome(a =>
        {
            a.Status.ShouldBe(DPIAAssessmentStatus.Approved);
            a.IsCurrent(DateTimeOffset.UtcNow).ShouldBeTrue("Should be current and valid");
        });

        // Step 4: Record audit entry
        var auditEntry = new DPIAAuditEntry
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment.Id,
            Action = "Approved",
            PerformedBy = "integration-test",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = "Assessment approved during integration test lifecycle"
        };

        var auditResult = await auditStore.RecordAuditEntryAsync(auditEntry);
        auditResult.IsRight.ShouldBeTrue("Audit record should succeed");

        // Step 5: Retrieve audit trail
        var trailResult = await auditStore.GetAuditTrailAsync(assessment.Id);
        trailResult.IsRight.ShouldBeTrue("Get audit trail should succeed");

        var trail = trailResult.Match(
            Right: entries => entries,
            Left: _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        trail.Count.ShouldBeGreaterThanOrEqualTo(1);
        trail.ShouldContain(e => e.Action == "Approved");
    }

    [Fact]
    public async Task AssessmentEngine_RequiresDPIA_ReturnsTrueForDecoratedType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDPIAAssessmentEngine>();

        // RequiresDPIA checks for [RequiresDPIA] attribute on the type.
        // Without a decorated type, it should return false.
        var result = await engine.RequiresDPIAAsync(typeof(string));
        result.IsRight.ShouldBeTrue();

        var requiresDpia = result.Match(Right: r => r, Left: _ => true);
        requiresDpia.ShouldBeFalse("string type is not decorated with [RequiresDPIA]");
    }

    [Fact]
    public async Task AssessmentEngine_DPOConsultation_WithConfiguredDPO()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA(options =>
        {
            options.DPOEmail = "dpo@integration-test.com";
            options.DPOName = "Integration DPO";
        });
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();

        // Create and save an assessment first
        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Test.DPOConsultation",
            Status = DPIAAssessmentStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        await store.SaveAssessmentAsync(assessment);

        using var scope = provider.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDPIAAssessmentEngine>();

        var consultResult = await engine.RequestDPOConsultationAsync(assessment.Id);
        consultResult.IsRight.ShouldBeTrue("DPO consultation should succeed");

        var consultation = consultResult.Match(Right: c => c, Left: _ => null!);
        consultation.ShouldNotBeNull();
    }

    #endregion

    #region Store Roundtrip

    [Fact]
    public async Task Store_SaveAndRetrieveById_Roundtrip()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();

        var assessment = new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Test.Roundtrip",
            Status = DPIAAssessmentStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await store.SaveAssessmentAsync(assessment);

        var byId = await store.GetAssessmentByIdAsync(assessment.Id);
        byId.IsRight.ShouldBeTrue();

        var option = byId.Match(Right: o => o, Left: _ => Option<DPIAAssessment>.None);
        option.IsSome.ShouldBeTrue();
        option.IfSome(a => a.RequestTypeName.ShouldBe("Test.Roundtrip"));
    }

    [Fact]
    public async Task Store_GetAllAssessments_ReturnsAllSaved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();

        await store.SaveAssessmentAsync(CreateAssessment("Test.All.A"));
        await store.SaveAssessmentAsync(CreateAssessment("Test.All.B"));
        await store.SaveAssessmentAsync(CreateAssessment("Test.All.C"));

        var result = await store.GetAllAssessmentsAsync();
        result.IsRight.ShouldBeTrue();

        var all = result.Match(Right: list => list, Left: _ => []);
        all.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Store_Delete_RemovesAssessment()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();

        var assessment = CreateAssessment("Test.Delete");
        await store.SaveAssessmentAsync(assessment);

        var deleteResult = await store.DeleteAssessmentAsync(assessment.Id);
        deleteResult.IsRight.ShouldBeTrue();

        var getResult = await store.GetAssessmentByIdAsync(assessment.Id);
        var option = getResult.Match(Right: o => o, Left: _ => Option<DPIAAssessment>.None);
        option.IsNone.ShouldBeTrue("Deleted assessment should not be found");
    }

    [Fact]
    public async Task Store_GetExpired_ReturnsOnlyExpiredApproved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();
        var now = DateTimeOffset.UtcNow;

        // Expired (approved, past review)
        await store.SaveAssessmentAsync(new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Test.Expired.Approved",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = now.AddDays(-400),
            NextReviewAtUtc = now.AddDays(-30)
        });

        // Not expired (approved, future review)
        await store.SaveAssessmentAsync(new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Test.NotExpired.Approved",
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = now.AddDays(-100),
            NextReviewAtUtc = now.AddDays(265)
        });

        // Draft with past review (should NOT be returned as expired)
        await store.SaveAssessmentAsync(new DPIAAssessment
        {
            Id = Guid.NewGuid(),
            RequestTypeName = "Test.Draft.PastReview",
            Status = DPIAAssessmentStatus.Draft,
            CreatedAtUtc = now.AddDays(-400),
            NextReviewAtUtc = now.AddDays(-30)
        });

        var result = await store.GetExpiredAssessmentsAsync(now);
        result.IsRight.ShouldBeTrue();

        var expired = result.Match(Right: list => list, Left: _ => []);
        expired.Count.ShouldBe(1);
        expired[0].RequestTypeName.ShouldBe("Test.Expired.Approved");
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task InMemoryStores_ConcurrentAccess_ThreadSafe()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();
        var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<IDPIAStore>();
        var auditStore = provider.GetRequiredService<IDPIAAuditStore>();

        const int concurrentOps = 50;

        // Concurrent saves
        var saveTasks = Enumerable.Range(0, concurrentOps)
            .Select(i => store.SaveAssessmentAsync(CreateAssessment($"Test.Concurrent.{i}")).AsTask());

        await Task.WhenAll(saveTasks);

        var allResult = await store.GetAllAssessmentsAsync();
        var all = allResult.Match(Right: list => list, Left: _ => []);
        all.Count.ShouldBe(concurrentOps);

        // Concurrent audit writes
        var assessmentId = Guid.NewGuid();
        var auditTasks = Enumerable.Range(0, concurrentOps)
            .Select(i => auditStore.RecordAuditEntryAsync(new DPIAAuditEntry
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                Action = $"Action-{i}",
                PerformedBy = "concurrent-test",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Details = $"Concurrent audit entry {i}"
            }).AsTask());

        await Task.WhenAll(auditTasks);

        var trailResult = await auditStore.GetAuditTrailAsync(assessmentId);
        var trail = trailResult.Match(Right: entries => entries, Left: _ => (IReadOnlyList<DPIAAuditEntry>)[]);
        trail.Count.ShouldBe(concurrentOps);
    }

    #endregion

    #region Custom Store Override

    [Fact]
    public void AddEncinaDPIA_CustomStoreRegisteredBefore_TryAddDoesNotOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom store BEFORE AddEncinaDPIA
        services.AddSingleton<IDPIAStore, InMemoryDPIAStore>();
        services.AddEncinaDPIA();

        var provider = services.BuildServiceProvider();
        var store = provider.GetService<IDPIAStore>();

        // Should still be the first-registered InMemoryDPIAStore (TryAdd does not override)
        store.ShouldNotBeNull();
        store.ShouldBeOfType<InMemoryDPIAStore>();
    }

    #endregion

    #region Helpers

    private static DPIAAssessment CreateAssessment(
        string requestTypeName,
        DPIAAssessmentStatus status = DPIAAssessmentStatus.Draft) => new()
        {
            Id = Guid.NewGuid(),
            RequestTypeName = requestTypeName,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    #endregion
}
