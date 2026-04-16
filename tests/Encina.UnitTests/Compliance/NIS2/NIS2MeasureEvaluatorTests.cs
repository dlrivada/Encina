#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Compliance.GDPR;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Evaluators;
using Encina.Compliance.NIS2.Model;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for all 10 NIS2 measure evaluators (Art. 21(2)(a)-(j)).
/// </summary>
public class NIS2MeasureEvaluatorTests
{
    #region Helpers

    private static NIS2MeasureContext CreateContext(Action<NIS2Options>? configure = null, IServiceProvider? serviceProvider = null)
    {
        var options = new NIS2Options();
        configure?.Invoke(options);
        return new NIS2MeasureContext
        {
            Options = options,
            TimeProvider = TimeProvider.System,
            ServiceProvider = serviceProvider ?? Substitute.For<IServiceProvider>()
        };
    }

    private static async Task AssertSatisfied(INIS2MeasureEvaluator evaluator, NIS2MeasureContext context)
    {
        var result = await evaluator.EvaluateAsync(context);
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.IsSatisfied.ShouldBeTrue());
    }

    private static List<ProcessingActivity> CreateDummyActivities(int count) =>
        Enumerable.Range(0, count).Select(i => new ProcessingActivity
        {
            Id = Guid.NewGuid(),
            Name = $"Activity-{i}",
            Purpose = $"Purpose-{i}",
            LawfulBasis = LawfulBasis.LegitimateInterests,
            RequestType = typeof(object),
            CategoriesOfDataSubjects = [$"Subject-{i}"],
            CategoriesOfPersonalData = [$"Data-{i}"],
            Recipients = [$"Recipient-{i}"],
            RetentionPeriod = TimeSpan.FromDays(365),
            SecurityMeasures = $"Measure-{i}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastUpdatedAtUtc = DateTimeOffset.UtcNow
        }).ToList();

    private static async Task AssertNotSatisfied(INIS2MeasureEvaluator evaluator, NIS2MeasureContext context)
    {
        var result = await evaluator.EvaluateAsync(context);
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.IsSatisfied.ShouldBeFalse());
    }

    #endregion

    #region RiskAnalysisEvaluator

    [Fact]
    public async Task RiskAnalysisEvaluator_WhenPolicyInPlace_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = true);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_WhenNoPolicyInPlace_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_WithGDPRValidatorAndActivities_ShouldMentionArt35Alignment()
    {
        // Arrange — IGDPRComplianceValidator + IProcessingActivityRegistry with 3 activities
        var gdprValidator = Substitute.For<IGDPRComplianceValidator>();
        var processingRegistry = Substitute.For<IProcessingActivityRegistry>();
        processingRegistry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(
                Right<EncinaError, IReadOnlyList<ProcessingActivity>>(
                    CreateDummyActivities(3))));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IGDPRComplianceValidator)).Returns(gdprValidator);
        sp.GetService(typeof(IProcessingActivityRegistry)).Returns(processingRegistry);

        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = true, sp);

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.IsSatisfied.ShouldBeTrue();
            r.Details.ShouldContain("3 activities");
            r.Details.ShouldContain("Art. 35");
        });
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_WithGDPRValidatorAndEmptyRegistry_ShouldMentionEmptyRegistry()
    {
        // Arrange — IGDPRComplianceValidator present, registry present but empty
        var gdprValidator = Substitute.For<IGDPRComplianceValidator>();
        var processingRegistry = Substitute.For<IProcessingActivityRegistry>();
        processingRegistry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(
                Right<EncinaError, IReadOnlyList<ProcessingActivity>>(
                    CreateDummyActivities(0))));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IGDPRComplianceValidator)).Returns(gdprValidator);
        sp.GetService(typeof(IProcessingActivityRegistry)).Returns(processingRegistry);

        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = true, sp);

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.IsSatisfied.ShouldBeTrue();
            r.Details.ShouldContain("empty");
        });
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_WithGDPRValidatorButNoRegistry_ShouldMentionValidatorOnly()
    {
        // Arrange — IGDPRComplianceValidator present, no IProcessingActivityRegistry
        var gdprValidator = Substitute.For<IGDPRComplianceValidator>();

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IGDPRComplianceValidator)).Returns(gdprValidator);

        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = true, sp);

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.IsSatisfied.ShouldBeTrue();
            r.Details.ShouldContain("GDPR compliance validator");
        });
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_RegistryCallFails_ShouldGracefullyDegrade()
    {
        // Arrange — IProcessingActivityRegistry throws
        var processingRegistry = Substitute.For<IProcessingActivityRegistry>();
        processingRegistry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database unavailable"));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IProcessingActivityRegistry)).Returns(processingRegistry);

        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = true, sp);

        // Act — resilience should catch the exception
        var result = await evaluator.EvaluateAsync(context);

        // Assert — still returns a result (satisfied based on policy flag)
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.IsSatisfied.ShouldBeTrue());
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_WhenNotSatisfied_ShouldRecommendGDPRIntegration()
    {
        // Arrange — no policy, no GDPR validator, no registry
        var evaluator = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var context = CreateContext(o => o.HasRiskAnalysisPolicy = false);

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.IsSatisfied.ShouldBeFalse();
            r.Recommendations.ShouldContain(rec => rec.Contains("GDPR"));
            r.Recommendations.ShouldContain(rec => rec.Contains("IProcessingActivityRegistry"));
        });
    }

    #endregion

    #region IncidentHandlingEvaluator

    [Fact]
    public async Task IncidentHandlingEvaluator_WhenFullyConfigured_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new IncidentHandlingEvaluator();
        var context = CreateContext(o =>
        {
            o.HasIncidentHandlingProcedures = true;
            o.CompetentAuthority = "csirt@test.eu";
        });

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task IncidentHandlingEvaluator_WhenNotConfigured_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new IncidentHandlingEvaluator();
        var context = CreateContext(o => o.HasIncidentHandlingProcedures = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region BusinessContinuityEvaluator

    [Fact]
    public async Task BusinessContinuityEvaluator_WhenPlanInPlace_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new BusinessContinuityEvaluator();
        var context = CreateContext(o => o.HasBusinessContinuityPlan = true);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task BusinessContinuityEvaluator_WhenNoPlan_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new BusinessContinuityEvaluator();
        var context = CreateContext(o => o.HasBusinessContinuityPlan = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region SupplyChainSecurityEvaluator

    [Fact]
    public async Task SupplyChainSecurityEvaluator_WhenSuppliersRegisteredNoCritical_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new SupplyChainSecurityEvaluator();
        var context = CreateContext(o => o.AddSupplier("sup-1", s =>
        {
            s.Name = "Test Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        }));

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task SupplyChainSecurityEvaluator_WhenNoSuppliers_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new SupplyChainSecurityEvaluator();
        var context = CreateContext();

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    [Fact]
    public async Task SupplyChainSecurityEvaluator_WhenCriticalSupplier_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new SupplyChainSecurityEvaluator();
        var context = CreateContext(o => o.AddSupplier("sup-1", s =>
        {
            s.Name = "Critical Supplier";
            s.RiskLevel = SupplierRiskLevel.Critical;
        }));

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region NetworkSecurityEvaluator

    [Fact]
    public async Task NetworkSecurityEvaluator_WhenPolicyInPlace_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new NetworkSecurityEvaluator();
        var context = CreateContext(o => o.HasNetworkSecurityPolicy = true);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task NetworkSecurityEvaluator_WhenNoPolicy_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new NetworkSecurityEvaluator();
        var context = CreateContext(o => o.HasNetworkSecurityPolicy = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region EffectivenessAssessmentEvaluator

    [Fact]
    public async Task EffectivenessAssessmentEvaluator_WhenAssessmentInPlace_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new EffectivenessAssessmentEvaluator();
        var context = CreateContext(o => o.HasEffectivenessAssessment = true);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task EffectivenessAssessmentEvaluator_WhenNoAssessment_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new EffectivenessAssessmentEvaluator();
        var context = CreateContext(o => o.HasEffectivenessAssessment = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region CyberHygieneEvaluator

    [Fact]
    public async Task CyberHygieneEvaluator_WhenFullyConfigured_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new CyberHygieneEvaluator();
        var context = CreateContext(o =>
        {
            o.HasCyberHygieneProgram = true;
            o.ManagementAccountability = ManagementAccountabilityRecord.Create(
                "John Doe",
                "CISO",
                DateTimeOffset.UtcNow,
                ["Risk Analysis"]);
            o.ManagementAccountability = new ManagementAccountabilityRecord
            {
                ResponsiblePerson = "John Doe",
                Role = "CISO",
                AcknowledgedAtUtc = DateTimeOffset.UtcNow,
                ComplianceAreas = ["Risk Analysis"],
                TrainingCompletedAtUtc = DateTimeOffset.UtcNow
            };
        });

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task CyberHygieneEvaluator_WhenNoProgram_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new CyberHygieneEvaluator();
        var context = CreateContext(o => o.HasCyberHygieneProgram = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region CryptographyEvaluator

    [Fact]
    public async Task CryptographyEvaluator_WhenFullyConfigured_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new CryptographyEvaluator();
        var context = CreateContext(o =>
        {
            o.EncryptedDataCategories.Add("PII");
            o.EncryptedEndpoints.Add("https://api.example.com");
            o.EnforceEncryption = true;
        });

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task CryptographyEvaluator_WhenMissingCategories_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new CryptographyEvaluator();
        var context = CreateContext(o => o.EnforceEncryption = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region HumanResourcesSecurityEvaluator

    [Fact]
    public async Task HumanResourcesSecurityEvaluator_WhenPolicyInPlace_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new HumanResourcesSecurityEvaluator();
        var context = CreateContext(o => o.HasHumanResourcesSecurity = true);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task HumanResourcesSecurityEvaluator_WhenNoPolicy_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new HumanResourcesSecurityEvaluator();
        var context = CreateContext(o => o.HasHumanResourcesSecurity = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion

    #region MultiFactorAuthenticationEvaluator

    [Fact]
    public async Task MultiFactorAuthenticationEvaluator_WhenEnforcedWithCustomEnforcer_ShouldBeSatisfied()
    {
        // Arrange
        var evaluator = new MultiFactorAuthenticationEvaluator();
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IMFAEnforcer)).Returns(Substitute.For<IMFAEnforcer>());
        var context = CreateContext(o => o.EnforceMFA = true, sp);

        // Act & Assert
        await AssertSatisfied(evaluator, context);
    }

    [Fact]
    public async Task MultiFactorAuthenticationEvaluator_WhenDisabled_ShouldBeNotSatisfied()
    {
        // Arrange
        var evaluator = new MultiFactorAuthenticationEvaluator();
        var context = CreateContext(o => o.EnforceMFA = false);

        // Act & Assert
        await AssertNotSatisfied(evaluator, context);
    }

    #endregion
}
