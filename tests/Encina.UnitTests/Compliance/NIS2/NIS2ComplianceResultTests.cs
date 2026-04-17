using Encina.Compliance.NIS2.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.NIS2;

public sealed class NIS2ComplianceResultTests
{
    #region Create Factory

    [Fact]
    public void Create_AllMeasuresSatisfied_ShouldBeCompliant()
    {
        // Arrange
        var results = new List<NIS2MeasureResult>
        {
            NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK"),
            NIS2MeasureResult.Satisfied(NIS2Measure.IncidentHandling, "OK"),
            NIS2MeasureResult.Satisfied(NIS2Measure.BusinessContinuity, "OK"),
        };

        // Act
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential, NIS2Sector.Energy, results, DateTimeOffset.UtcNow);

        // Assert
        result.IsCompliant.ShouldBeTrue();
        result.MissingCount.ShouldBe(0);
        result.MissingMeasures.ShouldBeEmpty();
        result.CompliancePercentage.ShouldBe(100);
    }

    [Fact]
    public void Create_SomeMeasuresNotSatisfied_ShouldNotBeCompliant()
    {
        // Arrange
        var results = new List<NIS2MeasureResult>
        {
            NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK"),
            NIS2MeasureResult.NotSatisfied(NIS2Measure.IncidentHandling, "Missing", ["Fix it"]),
            NIS2MeasureResult.Satisfied(NIS2Measure.BusinessContinuity, "OK"),
        };

        // Act
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important, NIS2Sector.Health, results, DateTimeOffset.UtcNow);

        // Assert
        result.IsCompliant.ShouldBeFalse();
        result.MissingCount.ShouldBe(1);
        result.MissingMeasures.ShouldContain(NIS2Measure.IncidentHandling);
        result.CompliancePercentage.ShouldBe(66); // 2/3 * 100 = 66 (integer division)
    }

    [Fact]
    public void Create_AllMeasuresNotSatisfied_ShouldHaveZeroPercentage()
    {
        // Arrange
        var results = new List<NIS2MeasureResult>
        {
            NIS2MeasureResult.NotSatisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "No", ["Fix"]),
            NIS2MeasureResult.NotSatisfied(NIS2Measure.IncidentHandling, "No", ["Fix"]),
        };

        // Act
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential, NIS2Sector.Transport, results, DateTimeOffset.UtcNow);

        // Assert
        result.IsCompliant.ShouldBeFalse();
        result.MissingCount.ShouldBe(2);
        result.CompliancePercentage.ShouldBe(0);
    }

    [Fact]
    public void Create_EmptyResults_ShouldBeCompliant()
    {
        // Arrange & Act
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential, NIS2Sector.Energy, [], DateTimeOffset.UtcNow);

        // Assert — no measures evaluated: IsCompliant is true (no missing), but percentage is 0 (no results)
        result.IsCompliant.ShouldBeTrue();
        result.MissingCount.ShouldBe(0);
        result.CompliancePercentage.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldPreserveEntityTypeAndSector()
    {
        // Arrange & Act
        var now = DateTimeOffset.UtcNow;
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Important, NIS2Sector.Banking, [], now);

        // Assert
        result.EntityType.ShouldBe(NIS2EntityType.Important);
        result.Sector.ShouldBe(NIS2Sector.Banking);
        result.EvaluatedAtUtc.ShouldBe(now);
    }

    #endregion

    #region CompliancePercentage Edge Cases

    [Fact]
    public void CompliancePercentage_OneOfTen_ShouldBeTen()
    {
        // Arrange
        var results = Enum.GetValues<NIS2Measure>()
            .Select((m, i) => i == 0
                ? NIS2MeasureResult.Satisfied(m, "OK")
                : NIS2MeasureResult.NotSatisfied(m, "No", ["Fix"]))
            .ToList();

        // Act
        var result = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential, NIS2Sector.Energy, results, DateTimeOffset.UtcNow);

        // Assert
        result.CompliancePercentage.ShouldBe(10); // 1/10 = 10%
    }

    #endregion
}
