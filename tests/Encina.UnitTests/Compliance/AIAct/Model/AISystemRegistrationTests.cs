using Encina.Compliance.AIAct.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Model;

/// <summary>
/// Unit tests for <see cref="AISystemRegistration"/>.
/// </summary>
public class AISystemRegistrationTests
{
    [Fact]
    public void ProhibitedPractices_ShouldDefaultToEmptyList()
    {
        var reg = new AISystemRegistration
        {
            SystemId = "sys-1",
            Name = "Test System",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        reg.ProhibitedPractices.Should().BeEmpty();
    }

    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        var original = new AISystemRegistration
        {
            SystemId = "sys-1",
            Name = "Original",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        var modified = original with { RiskLevel = AIRiskLevel.MinimalRisk };

        modified.RiskLevel.Should().Be(AIRiskLevel.MinimalRisk);
        original.RiskLevel.Should().Be(AIRiskLevel.HighRisk);
    }

    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        var reg = new AISystemRegistration
        {
            SystemId = "sys-1",
            Name = "Test",
            Category = AISystemCategory.EmploymentWorkersManagement,
            RiskLevel = AIRiskLevel.HighRisk,
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

        reg.Provider.Should().BeNull();
        reg.Version.Should().BeNull();
        reg.Description.Should().BeNull();
        reg.DeploymentContext.Should().BeNull();
    }
}
