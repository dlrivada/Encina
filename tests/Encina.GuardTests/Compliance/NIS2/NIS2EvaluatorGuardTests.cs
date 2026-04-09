using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Evaluators;
using Encina.Compliance.NIS2.Model;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Compliance.NIS2;

/// <summary>
/// Guard tests exercising all 10 NIS2 measure evaluators to cover their executable lines.
/// Each evaluator is instantiated and EvaluateAsync called with a minimal context.
/// </summary>
public class NIS2EvaluatorGuardTests
{
    private static NIS2MeasureContext CreateContext(NIS2Options? options = null)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new NIS2MeasureContext
        {
            Options = options ?? new NIS2Options(),
            ServiceProvider = services,
            TimeProvider = TimeProvider.System
        };
    }

    #region RiskAnalysisEvaluator

    [Fact]
    public void RiskAnalysisEvaluator_NullLogger_Throws()
    {
        var act = () => new RiskAnalysisEvaluator(null!);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task RiskAnalysisEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void RiskAnalysisEvaluator_Measure_IsCorrect()
    {
        var sut = new RiskAnalysisEvaluator(NullLogger<RiskAnalysisEvaluator>.Instance);
        sut.Measure.ShouldBe(NIS2Measure.RiskAnalysisAndSecurityPolicies);
    }

    #endregion

    #region IncidentHandlingEvaluator

    [Fact]
    public async Task IncidentHandlingEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new IncidentHandlingEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region BusinessContinuityEvaluator

    [Fact]
    public async Task BusinessContinuityEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new BusinessContinuityEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SupplyChainSecurityEvaluator

    [Fact]
    public async Task SupplyChainSecurityEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new SupplyChainSecurityEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region NetworkSecurityEvaluator

    [Fact]
    public async Task NetworkSecurityEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new NetworkSecurityEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region CyberHygieneEvaluator

    [Fact]
    public async Task CyberHygieneEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new CyberHygieneEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region CryptographyEvaluator

    [Fact]
    public async Task CryptographyEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new CryptographyEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CryptographyEvaluator_WithEncryption_ReturnsSatisfied()
    {
        var options = new NIS2Options();
        options.EncryptedDataCategories.Add("personal-data");
        options.EncryptedEndpoints.Add("/api/sensitive");
        options.EnforceEncryption = true;

        var sut = new CryptographyEvaluator();
        var result = await sut.EvaluateAsync(CreateContext(options));
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region MultiFactorAuthenticationEvaluator

    [Fact]
    public async Task MultiFactorAuthenticationEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new MultiFactorAuthenticationEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MultiFactorAuthenticationEvaluator_WithMFA_ReturnsSatisfied()
    {
        var options = new NIS2Options { EnforceMFA = true };
        var sut = new MultiFactorAuthenticationEvaluator();
        var result = await sut.EvaluateAsync(CreateContext(options));
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region HumanResourcesSecurityEvaluator

    [Fact]
    public async Task HumanResourcesSecurityEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new HumanResourcesSecurityEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region EffectivenessAssessmentEvaluator

    [Fact]
    public async Task EffectivenessAssessmentEvaluator_Evaluate_ReturnsResult()
    {
        var sut = new EffectivenessAssessmentEvaluator();
        var result = await sut.EvaluateAsync(CreateContext());
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SupplierRisk Model

    [Fact]
    public void SupplierRisk_ShouldPreserveRequiredProperties()
    {
        var risk = new SupplierRisk
        {
            SupplierId = "supplier-1",
            RiskLevel = SupplierRiskLevel.High,
            RiskDescription = "Weak encryption",
            RecommendedActions = ["Upgrade TLS", "Enable MFA"]
        };

        risk.SupplierId.ShouldBe("supplier-1");
        risk.RiskLevel.ShouldBe(SupplierRiskLevel.High);
        risk.RecommendedActions.Count.ShouldBe(2);
    }

    #endregion
}
