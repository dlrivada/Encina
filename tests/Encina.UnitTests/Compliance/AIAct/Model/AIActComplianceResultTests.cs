using Encina.Compliance.AIAct.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Model;

/// <summary>
/// Unit tests for <see cref="AIActComplianceResult"/>.
/// </summary>
public class AIActComplianceResultTests
{
    [Fact]
    public void Violations_ShouldDefaultToEmptyList()
    {
        var result = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.MinimalRisk,
            IsProhibited = false,
            RequiresHumanOversight = false,
            RequiresTransparency = false,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };

        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        var original = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.MinimalRisk,
            IsProhibited = false,
            RequiresHumanOversight = false,
            RequiresTransparency = false,
            EvaluatedAtUtc = DateTimeOffset.UtcNow
        };

        var modified = original with { RiskLevel = AIRiskLevel.HighRisk };

        modified.RiskLevel.Should().Be(AIRiskLevel.HighRisk);
        original.RiskLevel.Should().Be(AIRiskLevel.MinimalRisk);
    }

    [Fact]
    public void RecordEquality_SameValues_ShouldBeEqual()
    {
        var time = DateTimeOffset.UtcNow;
        var a = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = true,
            EvaluatedAtUtc = time
        };
        var b = new AIActComplianceResult
        {
            SystemId = "sys-1",
            RiskLevel = AIRiskLevel.HighRisk,
            IsProhibited = false,
            RequiresHumanOversight = true,
            RequiresTransparency = true,
            EvaluatedAtUtc = time
        };

        a.Should().Be(b);
    }
}
