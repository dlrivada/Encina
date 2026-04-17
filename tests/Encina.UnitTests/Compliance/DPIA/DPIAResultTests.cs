#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIAResult"/>.
/// </summary>
public class DPIAResultTests
{
    private static DPIAResult CreateResult(RiskLevel overallRisk) => new()
    {
        OverallRisk = overallRisk,
        IdentifiedRisks = [],
        ProposedMitigations = [],
        RequiresPriorConsultation = false,
        AssessedAtUtc = DateTimeOffset.UtcNow,
    };

    #region IsAcceptable Tests

    [Theory]
    [InlineData(RiskLevel.Low, true)]
    [InlineData(RiskLevel.Medium, true)]
    [InlineData(RiskLevel.High, false)]
    [InlineData(RiskLevel.VeryHigh, false)]
    public void IsAcceptable_ShouldReturnExpectedValue(RiskLevel level, bool expected)
    {
        var result = CreateResult(level);

        result.IsAcceptable.ShouldBe(expected);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_ShouldBeSetCorrectly()
    {
        var risks = new List<RiskItem>
        {
            new("Cat1", RiskLevel.High, "Description", "Mitigation"),
        };
        var mitigations = new List<Mitigation>
        {
            new("Encrypt data", "Technical", false, null),
        };
        var now = DateTimeOffset.UtcNow;

        var result = new DPIAResult
        {
            OverallRisk = RiskLevel.High,
            IdentifiedRisks = risks,
            ProposedMitigations = mitigations,
            RequiresPriorConsultation = true,
            AssessedAtUtc = now,
            AssessedBy = "tester",
        };

        result.OverallRisk.ShouldBe(RiskLevel.High);
        result.IdentifiedRisks.Count.ShouldBe(1);
        result.ProposedMitigations.Count.ShouldBe(1);
        result.RequiresPriorConsultation.ShouldBeTrue();
        result.AssessedAtUtc.ShouldBe(now);
        result.AssessedBy.ShouldBe("tester");
    }

    #endregion
}
