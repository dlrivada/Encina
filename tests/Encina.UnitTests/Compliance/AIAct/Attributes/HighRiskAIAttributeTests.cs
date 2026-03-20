using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Attributes;

/// <summary>
/// Unit tests for <see cref="HighRiskAIAttribute"/>.
/// </summary>
public class HighRiskAIAttributeTests
{
    [Fact]
    public void Category_ShouldBeSettable()
    {
        var attr = new HighRiskAIAttribute { Category = AISystemCategory.LawEnforcement };
        attr.Category.Should().Be(AISystemCategory.LawEnforcement);
    }

    [Fact]
    public void SystemId_ShouldDefaultToNull()
    {
        var attr = new HighRiskAIAttribute { Category = AISystemCategory.EmploymentWorkersManagement };
        attr.SystemId.Should().BeNull();
    }

    [Fact]
    public void SystemId_ShouldBeSettable()
    {
        var attr = new HighRiskAIAttribute
        {
            Category = AISystemCategory.EmploymentWorkersManagement,
            SystemId = "cv-screener"
        };
        attr.SystemId.Should().Be("cv-screener");
    }

    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        var attr = new HighRiskAIAttribute { Category = AISystemCategory.EmploymentWorkersManagement };
        attr.Provider.Should().BeNull();
        attr.Version.Should().BeNull();
        attr.Description.Should().BeNull();
    }
}
