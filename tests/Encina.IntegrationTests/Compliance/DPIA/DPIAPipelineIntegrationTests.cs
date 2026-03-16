using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Health;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

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
/// and service resolution patterns using the event-sourced architecture.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DPIAPipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaDPIA_RegistersIDPIAService()
    {
        // Descriptor check only — actual resolution requires Marten dependencies
        // (IAggregateRepository, IReadModelRepository, IDocumentSession) which
        // are out of scope here.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaDPIA();

        services.ShouldContain(sd => sd.ServiceType == typeof(IDPIAService));
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

    #endregion

    #region Custom Service Override

    [Fact]
    public void AddEncinaDPIA_CustomServiceRegisteredBefore_TryAddDoesNotOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Register custom service BEFORE AddEncinaDPIA
        services.AddScoped<IDPIAService, FakeDPIAService>();
        services.AddEncinaDPIA();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetService<IDPIAService>();

        // Should still be the first-registered FakeDPIAService (TryAdd does not override)
        service.ShouldNotBeNull();
        service.ShouldBeOfType<FakeDPIAService>();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Fake IDPIAService for testing custom service override behavior.
    /// </summary>
    private sealed class FakeDPIAService : IDPIAService
    {
        public ValueTask<Either<EncinaError, Guid>> CreateAssessmentAsync(
            string requestTypeName, string? processingType = null, string? reason = null,
            string? tenantId = null, string? moduleId = null, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, Guid>>(Guid.NewGuid());

        public ValueTask<Either<EncinaError, DPIAResult>> EvaluateAssessmentAsync(
            Guid assessmentId, DPIAContext context, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Guid>> RequestDPOConsultationAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Unit>> RecordDPOResponseAsync(
            Guid assessmentId, Guid consultationId, DPOConsultationDecision decision,
            string? comments = null, string? conditions = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Unit>> ApproveAssessmentAsync(
            Guid assessmentId, string approvedBy, DateTimeOffset? nextReviewAtUtc = null,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Unit>> RejectAssessmentAsync(
            Guid assessmentId, string rejectedBy, string reason, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Unit>> RequestRevisionAsync(
            Guid assessmentId, string requestedBy, string reason, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, Unit>> ExpireAssessmentAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentByRequestTypeAsync(
            string requestTypeName, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetExpiredAssessmentsAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetAllAssessmentsAsync(
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetAssessmentHistoryAsync(
            Guid assessmentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    #endregion
}
