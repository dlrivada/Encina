using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.PropertyTests.Compliance.NIS2;

/// <summary>
/// Property-based tests for <see cref="DefaultSupplyChainSecurityValidator"/> verifying
/// supply chain validation invariants using FsCheck random data generation.
/// </summary>
public sealed class DefaultSupplyChainSecurityValidatorPropertyTests
{
    private static ISupplyChainSecurityValidator CreateValidator(NIS2Options? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2(opt =>
        {
            // Minimum valid config to pass options validation
            opt.CompetentAuthority = "test@authority.eu";
            opt.EnforceEncryption = false;

            if (options is not null)
            {
                foreach (var (key, val) in options.Suppliers)
                {
                    opt.AddSupplier(key, s =>
                    {
                        s.Name = val.Name;
                        s.RiskLevel = val.RiskLevel;
                        s.LastAssessmentAtUtc = val.LastAssessmentAtUtc;
                        s.CertificationStatus = val.CertificationStatus;
                    });
                }
            }
        });

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ISupplyChainSecurityValidator>();
    }

    /// <summary>
    /// Invariant: Assessing an unregistered supplier always returns Left (error).
    /// </summary>
    [Property(MaxTest = 30)]
    public bool AssessSupplier_UnregisteredSupplier_AlwaysReturnsLeft(NonEmptyString supplierId)
    {
        var validator = CreateValidator();

        var result = validator.AssessSupplierAsync(supplierId.Get).AsTask().GetAwaiter().GetResult();

        return result.IsLeft;
    }

    /// <summary>
    /// Invariant: Assessing a registered Low-risk supplier always returns Right.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool AssessSupplier_RegisteredSupplier_AlwaysReturnsRight(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Test Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        var validator = CreateValidator(options);

        var result = validator.AssessSupplierAsync(supplierId.Get).AsTask().GetAwaiter().GetResult();

        return result.IsRight;
    }

    /// <summary>
    /// Invariant: High-risk suppliers always produce at least one risk finding.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool AssessSupplier_HighRisk_AlwaysHasRiskFindings(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Risky Supplier";
            s.RiskLevel = SupplierRiskLevel.High;
        });

        var validator = CreateValidator(options);

        var result = validator.AssessSupplierAsync(supplierId.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: assessment => assessment.Risks.Count > 0,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: Critical-risk suppliers always produce risk findings with Critical risk.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool AssessSupplier_CriticalRisk_HasCriticalRiskFinding(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Critical Supplier";
            s.RiskLevel = SupplierRiskLevel.Critical;
        });

        var validator = CreateValidator(options);

        var result = validator.AssessSupplierAsync(supplierId.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: assessment => assessment.OverallRisk == SupplierRiskLevel.Critical,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: ValidateSupplierForOperationAsync returns Left for unregistered suppliers.
    /// </summary>
    [Property(MaxTest = 30)]
    public bool ValidateSupplier_UnregisteredSupplier_AlwaysReturnsLeft(NonEmptyString supplierId)
    {
        var validator = CreateValidator();

        var result = validator.ValidateSupplierForOperationAsync(supplierId.Get)
            .AsTask().GetAwaiter().GetResult();

        return result.IsLeft;
    }

    /// <summary>
    /// Invariant: Critical-risk suppliers are never acceptable for operations.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool ValidateSupplier_CriticalRisk_NeverAcceptable(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Critical Supplier";
            s.RiskLevel = SupplierRiskLevel.Critical;
        });

        var validator = CreateValidator(options);

        var result = validator.ValidateSupplierForOperationAsync(supplierId.Get)
            .AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isAcceptable => !isAcceptable,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: Low-risk suppliers are always acceptable for operations.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool ValidateSupplier_LowRisk_AlwaysAcceptable(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Safe Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        var validator = CreateValidator(options);

        var result = validator.ValidateSupplierForOperationAsync(supplierId.Get)
            .AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: isAcceptable => isAcceptable,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: GetSupplierRisksAsync always returns Right.
    /// </summary>
    [Fact]
    public async Task GetSupplierRisks_AlwaysReturnsRight()
    {
        var validator = CreateValidator();

        var result = await validator.GetSupplierRisksAsync();

        Assert.True(result.IsRight);
    }

    /// <summary>
    /// Invariant: GetSupplierRisksAsync returns risks only for High/Critical suppliers.
    /// </summary>
    [Fact]
    public async Task GetSupplierRisks_OnlyReturnsHighAndCriticalRisks()
    {
        var options = new NIS2Options();
        options.AddSupplier("low", s =>
        {
            s.Name = "Low Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });
        options.AddSupplier("high", s =>
        {
            s.Name = "High Supplier";
            s.RiskLevel = SupplierRiskLevel.High;
        });

        var validator = CreateValidator(options);

        var result = await validator.GetSupplierRisksAsync();

        result.IfRight(risks =>
        {
            Assert.All(risks, r =>
                Assert.True(r.RiskLevel >= SupplierRiskLevel.High));
        });
    }

    /// <summary>
    /// Invariant: Assessment NextAssessmentDueAtUtc is always after AssessedAtUtc.
    /// </summary>
    [Property(MaxTest = 20)]
    public bool AssessSupplier_NextAssessment_IsAlwaysAfterCurrentAssessment(NonEmptyString supplierId)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId.Get, s =>
        {
            s.Name = "Test Supplier";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        var validator = CreateValidator(options);

        var result = validator.AssessSupplierAsync(supplierId.Get).AsTask().GetAwaiter().GetResult();

        return result.Match(
            Right: assessment => assessment.NextAssessmentDueAtUtc > assessment.AssessedAtUtc,
            Left: _ => false);
    }

    /// <summary>
    /// Invariant: Higher risk suppliers have shorter reassessment intervals.
    /// Critical = 1 month, High = 3 months, Medium = 6 months, Low = 12 months.
    /// </summary>
    [Fact]
    public async Task AssessSupplier_HigherRisk_ShorterReassessmentInterval()
    {
        var options = new NIS2Options();
        options.AddSupplier("low", s =>
        {
            s.Name = "Low";
            s.RiskLevel = SupplierRiskLevel.Low;
            s.LastAssessmentAtUtc = DateTimeOffset.UtcNow;
            s.CertificationStatus = "ISO 27001";
        });
        options.AddSupplier("critical", s =>
        {
            s.Name = "Critical";
            s.RiskLevel = SupplierRiskLevel.Critical;
            s.LastAssessmentAtUtc = DateTimeOffset.UtcNow;
            s.CertificationStatus = "ISO 27001";
        });

        var validator = CreateValidator(options);

        var lowResult = await validator.AssessSupplierAsync("low");
        var criticalResult = await validator.AssessSupplierAsync("critical");

        lowResult.IfRight(low =>
        {
            criticalResult.IfRight(critical =>
            {
                var lowInterval = low.NextAssessmentDueAtUtc - low.AssessedAtUtc;
                var criticalInterval = critical.NextAssessmentDueAtUtc - critical.AssessedAtUtc;

                Assert.True(criticalInterval < lowInterval,
                    $"Critical interval ({criticalInterval.TotalDays}d) should be < Low interval ({lowInterval.TotalDays}d)");
            });
        });
    }
}
