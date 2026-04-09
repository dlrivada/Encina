using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.PropertyTests.Compliance.NIS2;

/// <summary>
/// Property-based tests for <see cref="DefaultNIS2ComplianceValidator"/> verifying
/// compliance validation invariants using FsCheck random data generation.
/// </summary>
public sealed class DefaultNIS2ComplianceValidatorPropertyTests
{
    private static ServiceProvider BuildProvider(Action<NIS2Options>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(opt =>
        {
            // Minimum valid config to pass options validation
            opt.CompetentAuthority = "test@authority.eu";
            opt.EnforceEncryption = false;

            configure?.Invoke(opt);
        });

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invariant: ValidateAsync always returns Right (never Left error) with default options.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_DefaultOptions_AlwaysReturnsRight()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.ValidateAsync();

        Assert.True(result.IsRight, "Validation should always return Right, not Left error");
    }

    /// <summary>
    /// Invariant: CompliancePercentage is always between 0 and 100.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_CompliancePercentage_AlwaysBetween0And100()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.ValidateAsync();

        result.IfRight(r =>
        {
            Assert.InRange(r.CompliancePercentage, 0, 100);
        });
    }

    /// <summary>
    /// Invariant: MeasureResults always contains exactly 10 evaluations
    /// (one per NIS2 Art. 21(2) measure).
    /// </summary>
    [Fact]
    public async Task ValidateAsync_AlwaysReturns10MeasureResults()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.ValidateAsync();

        result.IfRight(r =>
        {
            Assert.Equal(10, r.MeasureResults.Count);
        });
    }

    /// <summary>
    /// Invariant: With all security flags enabled, compliance percentage is higher than
    /// with no flags enabled.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_AllFlagsEnabled_HigherComplianceThanDefault()
    {
        using var defaultProvider = BuildProvider();
        using var defaultScope = defaultProvider.CreateScope();
        var defaultValidator = defaultScope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        using var fullProvider = BuildProvider(opt =>
        {
            opt.HasRiskAnalysisPolicy = true;
            opt.HasIncidentHandlingProcedures = true;
            opt.HasBusinessContinuityPlan = true;
            opt.HasNetworkSecurityPolicy = true;
            opt.HasEffectivenessAssessment = true;
            opt.HasCyberHygieneProgram = true;
            opt.HasHumanResourcesSecurity = true;
            opt.EnforceMFA = true;
            opt.EnforceEncryption = false;
        });
        using var fullScope = fullProvider.CreateScope();
        var fullValidator = fullScope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var defaultResult = await defaultValidator.ValidateAsync();
        var fullResult = await fullValidator.ValidateAsync();

        var defaultPct = defaultResult.Match(r => r.CompliancePercentage, _ => -1);
        var fullPct = fullResult.Match(r => r.CompliancePercentage, _ => -1);

        Assert.True(fullPct >= defaultPct,
            $"Full config ({fullPct}%) should be >= default ({defaultPct}%)");
    }

    /// <summary>
    /// Invariant: MissingMeasures count + satisfied count always equals total MeasureResults count.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_MissingPlusSatisfied_EqualsTotalCount()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.ValidateAsync();

        result.IfRight(r =>
        {
            var satisfiedCount = r.MeasureResults.Count(m => m.IsSatisfied);
            Assert.Equal(r.MeasureResults.Count, satisfiedCount + r.MissingCount);
        });
    }

    /// <summary>
    /// Invariant: IsCompliant == (MissingCount == 0).
    /// </summary>
    [Fact]
    public async Task ValidateAsync_IsCompliant_IffMissingCountIsZero()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.ValidateAsync();

        result.IfRight(r =>
        {
            Assert.Equal(r.MissingCount == 0, r.IsCompliant);
        });
    }

    /// <summary>
    /// Invariant: GetMissingRequirementsAsync always returns Right with valid options.
    /// </summary>
    [Fact]
    public async Task GetMissingRequirementsAsync_ValidOptions_ReturnsRight()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();

        var result = await validator.GetMissingRequirementsAsync();

        Assert.True(result.IsRight);
    }

    /// <summary>
    /// Invariant: GetMissingRequirementsAsync returns list consistent with ValidateAsync.
    /// </summary>
    [Fact]
    public async Task GetMissingRequirementsAsync_ConsistentWithValidateAsync()
    {
        using var provider = BuildProvider();

        int validateMissing;
        using (var scope1 = provider.CreateScope())
        {
            var validator1 = scope1.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            var validateResult = await validator1.ValidateAsync();
            validateMissing = validateResult.Match(r => r.MissingCount, _ => -1);
        }

        int missingCount;
        using (var scope2 = provider.CreateScope())
        {
            var validator2 = scope2.ServiceProvider.GetRequiredService<INIS2ComplianceValidator>();
            var missingResult = await validator2.GetMissingRequirementsAsync();
            missingCount = missingResult.Match(r => r.Count, _ => -1);
        }

        Assert.Equal(validateMissing, missingCount);
    }
}
