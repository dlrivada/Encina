using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Model;
using Encina.Testing.Time;

using Shouldly;

using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="DefaultSupplyChainSecurityValidator"/>.
/// </summary>
public class DefaultSupplyChainSecurityValidatorTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly DateTimeOffset _baseTime = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    public DefaultSupplyChainSecurityValidatorTests()
    {
        _timeProvider = new FakeTimeProvider(_baseTime);
    }

    private DefaultSupplyChainSecurityValidator CreateSut(NIS2Options options) =>
        new(Options.Create(options), _timeProvider);

    private static NIS2Options CreateOptionsWithSupplier(
        string supplierId,
        string name,
        SupplierRiskLevel riskLevel,
        DateTimeOffset? lastAssessment = null,
        string? certification = null)
    {
        var options = new NIS2Options();
        options.AddSupplier(supplierId, s =>
        {
            s.Name = name;
            s.RiskLevel = riskLevel;
            s.LastAssessmentAtUtc = lastAssessment;
            s.CertificationStatus = certification;
        });
        return options;
    }

    #region AssessSupplierAsync

    [Fact]
    public async Task AssessSupplierAsync_KnownSupplier_ShouldReturnAssessment()
    {
        // Arrange
        var options = CreateOptionsWithSupplier(
            "acme-cloud",
            "ACME Cloud",
            SupplierRiskLevel.Low,
            lastAssessment: _baseTime.AddMonths(-3),
            certification: "ISO 27001");
        var sut = CreateSut(options);

        // Act
        var result = await sut.AssessSupplierAsync("acme-cloud");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(r => r, _ => null!);
        assessment.SupplierId.ShouldBe("acme-cloud");
        assessment.OverallRisk.ShouldBe(SupplierRiskLevel.Low);
        assessment.AssessedAtUtc.ShouldBe(_baseTime);
    }

    [Fact]
    public async Task AssessSupplierAsync_UnknownSupplier_ShouldReturnError()
    {
        // Arrange
        var options = new NIS2Options();
        var sut = CreateSut(options);

        // Act
        var result = await sut.AssessSupplierAsync("unknown-supplier");

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(_ => default, e => e);
        error.Message.ShouldContain("unknown-supplier");
        error.Message.ShouldContain("not registered");
    }

    [Fact]
    public async Task AssessSupplierAsync_ExpiredAssessment_ShouldAddMediumRisk()
    {
        // Arrange — last assessment was 13 months ago (beyond the 12-month threshold)
        var options = CreateOptionsWithSupplier(
            "stale-vendor",
            "Stale Vendor",
            SupplierRiskLevel.Low,
            lastAssessment: _baseTime.AddMonths(-13),
            certification: "SOC 2");
        var sut = CreateSut(options);

        // Act
        var result = await sut.AssessSupplierAsync("stale-vendor");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(r => r, _ => null!);
        assessment.Risks.ShouldContain(r =>
            r.RiskLevel == SupplierRiskLevel.Medium &&
            r.RiskDescription.Contains("older than 12 months"));
    }

    [Fact]
    public async Task AssessSupplierAsync_CriticalRiskLevel_ShouldAddHighRisk()
    {
        // Arrange
        var options = CreateOptionsWithSupplier(
            "risky-supplier",
            "Risky Supplier",
            SupplierRiskLevel.Critical,
            lastAssessment: _baseTime.AddMonths(-1),
            certification: "ISO 27001");
        var sut = CreateSut(options);

        // Act
        var result = await sut.AssessSupplierAsync("risky-supplier");

        // Assert
        result.IsRight.ShouldBeTrue();
        var assessment = result.Match(r => r, _ => null!);
        assessment.OverallRisk.ShouldBe(SupplierRiskLevel.Critical);
        assessment.Risks.ShouldContain(r =>
            r.RiskLevel == SupplierRiskLevel.Critical &&
            r.RiskDescription.Contains("Critical"));
    }

    #endregion

    #region ValidateSupplierForOperationAsync

    [Fact]
    public async Task ValidateSupplierForOperationAsync_LowRisk_ShouldReturnTrue()
    {
        // Arrange
        var options = CreateOptionsWithSupplier(
            "safe-vendor",
            "Safe Vendor",
            SupplierRiskLevel.Low);
        var sut = CreateSut(options);

        // Act
        var result = await sut.ValidateSupplierForOperationAsync("safe-vendor");

        // Assert
        result.IsRight.ShouldBeTrue();
        var isAcceptable = result.Match(r => r, _ => false);
        isAcceptable.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateSupplierForOperationAsync_CriticalRisk_ShouldReturnFalse()
    {
        // Arrange
        var options = CreateOptionsWithSupplier(
            "critical-vendor",
            "Critical Vendor",
            SupplierRiskLevel.Critical);
        var sut = CreateSut(options);

        // Act
        var result = await sut.ValidateSupplierForOperationAsync("critical-vendor");

        // Assert
        result.IsRight.ShouldBeTrue();
        var isAcceptable = result.Match(r => r, _ => true);
        isAcceptable.ShouldBeFalse();
    }

    #endregion

    #region GetSupplierRisksAsync

    [Fact]
    public async Task GetSupplierRisksAsync_ShouldReturnHighRiskSuppliersOnly()
    {
        // Arrange
        var options = new NIS2Options();
        options.AddSupplier("low-risk", s =>
        {
            s.Name = "Low Risk Corp";
            s.RiskLevel = SupplierRiskLevel.Low;
        });
        options.AddSupplier("high-risk", s =>
        {
            s.Name = "High Risk Corp";
            s.RiskLevel = SupplierRiskLevel.High;
        });
        options.AddSupplier("critical-risk", s =>
        {
            s.Name = "Critical Risk Corp";
            s.RiskLevel = SupplierRiskLevel.Critical;
        });
        var sut = CreateSut(options);

        // Act
        var result = await sut.GetSupplierRisksAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var risks = result.Match(r => r, _ => null!);
        risks.Count.ShouldBe(2);
        risks.ShouldContain(r => r.SupplierId == "high-risk");
        risks.ShouldContain(r => r.SupplierId == "critical-risk");
        risks.ShouldNotContain(r => r.SupplierId == "low-risk");
    }

    #endregion
}
