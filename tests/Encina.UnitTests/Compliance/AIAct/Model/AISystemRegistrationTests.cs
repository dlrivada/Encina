using Encina.Compliance.AIAct.Model;
using Shouldly;

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

        reg.ProhibitedPractices.ShouldBeEmpty();
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

        modified.RiskLevel.ShouldBe(AIRiskLevel.MinimalRisk);
        original.RiskLevel.ShouldBe(AIRiskLevel.HighRisk);
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

        reg.Provider.ShouldBeNull();
        reg.Version.ShouldBeNull();
        reg.Description.ShouldBeNull();
        reg.DeploymentContext.ShouldBeNull();
    }
}
