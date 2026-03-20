using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Evaluators;
using Encina.Compliance.NIS2.Health;
using Encina.Compliance.NIS2.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Encina.IntegrationTests.Compliance.NIS2;

/// <summary>
/// Integration tests for NIS2 pipeline verifying DI registration,
/// options configuration, service resolution, and health check wiring.
/// </summary>
[Trait("Category", "Integration")]
public sealed class NIS2PipelineIntegrationTests
{
    #region DI Registration

    [Fact]
    public void AddEncinaNIS2_RegistersINIS2ComplianceValidator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        services.ShouldContain(sd => sd.ServiceType == typeof(INIS2ComplianceValidator));
    }

    [Fact]
    public void AddEncinaNIS2_RegistersINIS2IncidentHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        services.ShouldContain(sd => sd.ServiceType == typeof(INIS2IncidentHandler));
    }

    [Fact]
    public void AddEncinaNIS2_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        services.ShouldContain(
            d => d.ServiceType == typeof(IPipelineBehavior<,>)
              && d.ImplementationType == typeof(NIS2CompliancePipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaNIS2_Registers10MeasureEvaluators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        var evaluators = provider.GetServices<INIS2MeasureEvaluator>().ToList();

        evaluators.Count.ShouldBe(10);
        evaluators.ShouldContain(e => e is RiskAnalysisEvaluator);
        evaluators.ShouldContain(e => e is IncidentHandlingEvaluator);
        evaluators.ShouldContain(e => e is BusinessContinuityEvaluator);
        evaluators.ShouldContain(e => e is SupplyChainSecurityEvaluator);
        evaluators.ShouldContain(e => e is NetworkSecurityEvaluator);
        evaluators.ShouldContain(e => e is EffectivenessAssessmentEvaluator);
        evaluators.ShouldContain(e => e is CyberHygieneEvaluator);
        evaluators.ShouldContain(e => e is CryptographyEvaluator);
        evaluators.ShouldContain(e => e is HumanResourcesSecurityEvaluator);
        evaluators.ShouldContain(e => e is MultiFactorAuthenticationEvaluator);
    }

    [Fact]
    public void AddEncinaNIS2_MeasureEvaluators_HaveUniqueMeasures()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        var evaluators = provider.GetServices<INIS2MeasureEvaluator>().ToList();
        var measures = evaluators.Select(e => e.Measure).ToList();

        measures.Distinct().Count().ShouldBe(measures.Count);
    }

    [Fact]
    public void AddEncinaNIS2_ResolvesComplianceValidator_InScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<INIS2ComplianceValidator>();

        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<DefaultNIS2ComplianceValidator>();
    }

    [Fact]
    public void AddEncinaNIS2_ComplianceValidatorIsScoped_DifferentPerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        INIS2ComplianceValidator? v1, v2;

        using (var scope1 = provider.CreateScope())
        {
            v1 = scope1.ServiceProvider.GetService<INIS2ComplianceValidator>();
        }

        using (var scope2 = provider.CreateScope())
        {
            v2 = scope2.ServiceProvider.GetService<INIS2ComplianceValidator>();
        }

        v1.ShouldNotBeSameAs(v2);
    }

    [Fact]
    public void AddEncinaNIS2_RegistersDefaultSingletons()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        provider.GetService<ISupplyChainSecurityValidator>().ShouldNotBeNull();
        provider.GetService<IMFAEnforcer>().ShouldNotBeNull();
        provider.GetService<IEncryptionValidator>().ShouldNotBeNull();
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void NIS2Options_Defaults_HaveCorrectValues()
    {
        // Test defaults of the options class directly (DI resolution triggers validation
        // which rejects defaults — Essential requires CompetentAuthority)
        var options = new NIS2Options();

        options.EntityType.ShouldBe(NIS2EntityType.Essential);
        options.EnforcementMode.ShouldBe(NIS2EnforcementMode.Warn);
        options.EnforceMFA.ShouldBeTrue();
        options.EnforceEncryption.ShouldBeTrue();
        options.IncidentNotificationHours.ShouldBe(24);
        options.CompetentAuthority.ShouldBeNull();
        options.AddHealthCheck.ShouldBeFalse();
        options.HasRiskAnalysisPolicy.ShouldBeFalse();
        options.HasIncidentHandlingProcedures.ShouldBeFalse();
        options.HasBusinessContinuityPlan.ShouldBeFalse();
        options.PublishNotifications.ShouldBeTrue();
        options.ExternalCallTimeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.ComplianceCacheTTL.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaNIS2_CustomOptions_AreApplied()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.EntityType = NIS2EntityType.Important;
            options.Sector = NIS2Sector.Manufacturing;
            options.EnforcementMode = NIS2EnforcementMode.Block;
            options.EnforceMFA = false;
            options.EnforceEncryption = false;
            options.CompetentAuthority = "csirt@test.eu";
            options.ExternalCallTimeout = TimeSpan.FromSeconds(10);
            options.ComplianceCacheTTL = TimeSpan.FromMinutes(15);
        });
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<NIS2Options>>().Value;

        options.EntityType.ShouldBe(NIS2EntityType.Important);
        options.Sector.ShouldBe(NIS2Sector.Manufacturing);
        options.EnforcementMode.ShouldBe(NIS2EnforcementMode.Block);
        options.EnforceMFA.ShouldBeFalse();
        options.CompetentAuthority.ShouldBe("csirt@test.eu");
        options.ExternalCallTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        options.ComplianceCacheTTL.ShouldBe(TimeSpan.FromMinutes(15));
    }

    #endregion

    #region Health Check

    [Fact]
    public void AddEncinaNIS2_WithHealthCheck_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.AddHealthCheck = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetService<HealthCheckService>();
        healthCheckService.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaNIS2_WithoutHealthCheck_DoesNotRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.AddHealthCheck = false;
        });

        services.Any(d => d.ServiceType == typeof(HealthCheckService))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task HealthCheck_FullyCompliant_ReturnsHealthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.AddHealthCheck = true;
            options.EntityType = NIS2EntityType.Essential;
            options.Sector = NIS2Sector.DigitalInfrastructure;
            options.CompetentAuthority = "csirt@test.eu";
            options.HasRiskAnalysisPolicy = true;
            options.HasIncidentHandlingProcedures = true;
            options.HasBusinessContinuityPlan = true;
            options.HasNetworkSecurityPolicy = true;
            options.HasEffectivenessAssessment = true;
            options.HasCyberHygieneProgram = true;
            options.HasHumanResourcesSecurity = true;
            options.EnforceMFA = true;
            options.EnforceEncryption = true;
            options.EncryptedDataCategories.Add("PII");
            options.EncryptedEndpoints.Add("https://api.test.com");
            options.AddSupplier("test-supplier", s =>
            {
                s.Name = "Test";
                s.RiskLevel = SupplierRiskLevel.Low;
                s.LastAssessmentAtUtc = DateTimeOffset.UtcNow;
                s.CertificationStatus = "ISO 27001";
            });
            options.ManagementAccountability = new ManagementAccountabilityRecord
            {
                ResponsiblePerson = "Jane Doe",
                Role = "CISO",
                AcknowledgedAtUtc = DateTimeOffset.UtcNow.AddDays(-30),
                ComplianceAreas = ["Risk Analysis", "Incident Handling", "Supply Chain"],
                TrainingCompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-15)
            };
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        report.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task HealthCheck_MissingMeasures_ReturnsDegraded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.AddHealthCheck = true;
            options.CompetentAuthority = "csirt@test.eu";
            options.EnforceEncryption = false;
            // Only set a few measures — most are missing
            options.HasRiskAnalysisPolicy = true;
            options.HasIncidentHandlingProcedures = true;
        });
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        report.Status.ShouldBe(HealthStatus.Degraded);
    }

    #endregion

    #region Custom Service Override

    [Fact]
    public void AddEncinaNIS2_CustomMFAEnforcerRegisteredBefore_TryAddDoesNotOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IMFAEnforcer, FakeMFAEnforcer>();
        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var enforcer = provider.GetService<IMFAEnforcer>();

        enforcer.ShouldNotBeNull();
        enforcer.ShouldBeOfType<FakeMFAEnforcer>();
    }

    [Fact]
    public void AddEncinaNIS2_CustomEncryptionValidatorRegisteredBefore_TryAddDoesNotOverride()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IEncryptionValidator, FakeEncryptionValidator>();
        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var validator = provider.GetService<IEncryptionValidator>();

        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<FakeEncryptionValidator>();
    }

    #endregion

    #region Helpers

    private sealed class FakeMFAEnforcer : IMFAEnforcer
    {
        public ValueTask<Either<EncinaError, bool>> IsMFAEnabledAsync(
            string userId, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, bool>>(true);

        public ValueTask<Either<EncinaError, Unit>> RequireMFAAsync<TRequest>(
            TRequest request, IRequestContext context, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
    }

    private sealed class FakeEncryptionValidator : IEncryptionValidator
    {
        public ValueTask<Either<EncinaError, bool>> IsDataEncryptedAtRestAsync(
            string dataCategory, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, bool>>(true);

        public ValueTask<Either<EncinaError, bool>> IsDataEncryptedInTransitAsync(
            string endpoint, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, bool>>(true);

        public ValueTask<Either<EncinaError, bool>> ValidateEncryptionPolicyAsync(
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<Either<EncinaError, bool>>(true);
    }

    #endregion
}
