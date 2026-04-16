using Encina.Compliance.AIAct.Attributes;
using Encina.Compliance.AIAct.Model;
using Shouldly;

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
        attr.Category.ShouldBe(AISystemCategory.LawEnforcement);
    }

    [Fact]
    public void SystemId_ShouldDefaultToNull()
    {
        var attr = new HighRiskAIAttribute { Category = AISystemCategory.EmploymentWorkersManagement };
        attr.SystemId.ShouldBeNull();
    }

    [Fact]
    public void SystemId_ShouldBeSettable()
    {
        var attr = new HighRiskAIAttribute
        {
            Category = AISystemCategory.EmploymentWorkersManagement,
            SystemId = "cv-screener"
        };
        attr.SystemId.ShouldBe("cv-screener");
    }

    [Fact]
    public void OptionalProperties_ShouldDefaultToNull()
    {
        var attr = new HighRiskAIAttribute { Category = AISystemCategory.EmploymentWorkersManagement };
        attr.Provider.ShouldBeNull();
        attr.Version.ShouldBeNull();
        attr.Description.ShouldBeNull();
    }
}
