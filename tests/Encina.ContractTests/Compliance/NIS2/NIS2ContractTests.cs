#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Encina.ContractTests.Compliance.NIS2;

/// <summary>
/// Contract tests verifying that all 10 NIS2 measure evaluators correctly implement
/// the <see cref="INIS2MeasureEvaluator"/> interface and that the complete set covers
/// every <see cref="NIS2Measure"/> enum value without duplicates or gaps.
/// </summary>
public sealed class NIS2MeasureEvaluatorContractTests
{
    private static readonly IReadOnlyList<INIS2MeasureEvaluator> Evaluators = BuildEvaluators();

    /// <summary>
    /// Builds the 10 measure evaluators by resolving them from the DI container
    /// (evaluators are internal sealed, so direct instantiation is not possible).
    /// </summary>
    private static IReadOnlyList<INIS2MeasureEvaluator> BuildEvaluators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
            options.EntityType = NIS2EntityType.Essential;
            options.Sector = NIS2Sector.DigitalInfrastructure;
        });

        var provider = services.BuildServiceProvider();
        return provider.GetServices<INIS2MeasureEvaluator>().ToList();
    }

    /// <summary>
    /// Provides evaluator instances for parameterized tests.
    /// </summary>
    public static TheoryData<INIS2MeasureEvaluator> AllEvaluators
    {
        get
        {
            var data = new TheoryData<INIS2MeasureEvaluator>();
            foreach (var evaluator in Evaluators)
            {
                data.Add(evaluator);
            }

            return data;
        }
    }

    // -- Coverage contract: All 10 measures are covered --

    [Fact]
    public void AllEvaluators_ShouldCoverAllTenMeasures()
    {
        var measures = Evaluators
            .Select(e => e.Measure)
            .ToHashSet();

        var expectedMeasures = Enum.GetValues<NIS2Measure>();

        measures.SetEquals(expectedMeasures).ShouldBeTrue(
            "every NIS2Measure enum value must have a corresponding evaluator");
    }

    [Fact]
    public void AllEvaluators_ExactlyTenRegistered()
    {
        Evaluators.Count.ShouldBe(10,
            "NIS2 Art. 21(2) defines exactly 10 mandatory measures (a)-(j)");
    }

    // -- Uniqueness contract: No duplicate measures --

    [Fact]
    public void AllEvaluators_ShouldHaveUniqueMeasures()
    {
        var measures = Evaluators.Select(e => e.Measure).ToList();

        measures.ShouldBeUnique(
            "each evaluator must assess a distinct NIS2 measure");
    }

    // -- Interface contract: Each evaluator's Measure property is a valid enum value --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public void Measure_ShouldBeDefinedEnumValue(INIS2MeasureEvaluator evaluator)
    {
        Enum.IsDefined(evaluator.Measure).ShouldBeTrue(
            $"evaluator {evaluator.GetType().Name} should return a defined NIS2Measure value");
    }

    // -- Interface contract: EvaluateAsync returns Right (never throws) --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_ShouldReturnRight_WithDefaultOptions(INIS2MeasureEvaluator evaluator)
    {
        // Arrange — default options with a minimal service provider
        var context = CreateDefaultContext();

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert — evaluators should always return a result, never an error
        result.IsRight.ShouldBeTrue(
            $"evaluator {evaluator.GetType().Name} should return Right, not Left (error)");
    }

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_ShouldReturnRight_WithFullyConfiguredOptions(INIS2MeasureEvaluator evaluator)
    {
        // Arrange — fully configured options where all measures should be satisfied
        var context = CreateFullyConfiguredContext();

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue(
            $"evaluator {evaluator.GetType().Name} should return Right with fully configured options");
    }

    // -- Interface contract: EvaluateAsync result contains matching measure --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_ResultMeasure_ShouldMatchEvaluatorMeasure(INIS2MeasureEvaluator evaluator)
    {
        // Arrange
        var context = CreateDefaultContext();

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IfRight(r => r.Measure.ShouldBe(evaluator.Measure,
            $"the result Measure should match the evaluator's Measure property"));
    }

    // -- Interface contract: EvaluateAsync result has non-empty Details --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_ResultDetails_ShouldNotBeEmpty(INIS2MeasureEvaluator evaluator)
    {
        // Arrange
        var context = CreateDefaultContext();

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IfRight(r => r.Details.ShouldNotBeNullOrWhiteSpace(
            "evaluator results must include a meaningful Details description"));
    }

    // -- Interface contract: EvaluateAsync supports CancellationToken --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_WithCancellationToken_ShouldStillReturnRight(INIS2MeasureEvaluator evaluator)
    {
        // Arrange
        var context = CreateDefaultContext();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await evaluator.EvaluateAsync(context, cts.Token);

        // Assert
        result.IsRight.ShouldBeTrue(
            $"evaluator {evaluator.GetType().Name} should accept a CancellationToken gracefully");
    }

    // -- Interface contract: NotSatisfied results include Recommendations --

    [Theory]
    [MemberData(nameof(AllEvaluators))]
    public async Task EvaluateAsync_WhenNotSatisfied_ShouldIncludeRecommendations(INIS2MeasureEvaluator evaluator)
    {
        // Arrange — default options likely leave most measures unsatisfied
        var context = CreateDefaultContext();

        // Act
        var result = await evaluator.EvaluateAsync(context);

        // Assert
        result.IfRight(r =>
        {
            if (!r.IsSatisfied)
            {
                r.Recommendations.ShouldNotBeEmpty(
                    "not-satisfied results must include actionable recommendations");
            }
        });
    }

    // -- Helpers --

    private static NIS2MeasureContext CreateDefaultContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();
        var provider = services.BuildServiceProvider();

        return new NIS2MeasureContext
        {
            Options = new NIS2Options(),
            TimeProvider = TimeProvider.System,
            ServiceProvider = provider
        };
    }

    private static NIS2MeasureContext CreateFullyConfiguredContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(options =>
        {
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
            options.EncryptedEndpoints.Add("https://api.example.com");
            options.AddSupplier("test-supplier", s =>
            {
                s.Name = "Test Supplier";
                s.RiskLevel = SupplierRiskLevel.Low;
            });
        });
        var provider = services.BuildServiceProvider();

        var options = new NIS2Options
        {
            HasRiskAnalysisPolicy = true,
            HasIncidentHandlingProcedures = true,
            HasBusinessContinuityPlan = true,
            HasNetworkSecurityPolicy = true,
            HasEffectivenessAssessment = true,
            HasCyberHygieneProgram = true,
            HasHumanResourcesSecurity = true,
            EnforceMFA = true,
            EnforceEncryption = true
        };
        options.EncryptedDataCategories.Add("PII");
        options.EncryptedEndpoints.Add("https://api.example.com");
        options.AddSupplier("test-supplier", s =>
        {
            s.Name = "Test Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        return new NIS2MeasureContext
        {
            Options = options,
            TimeProvider = TimeProvider.System,
            ServiceProvider = provider
        };
    }
}
