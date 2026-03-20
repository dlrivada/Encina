using Encina.Compliance.AIAct.Attributes;
using FluentAssertions;

namespace Encina.UnitTests.Compliance.AIAct.Attributes;

/// <summary>
/// Unit tests for <see cref="RequireHumanOversightAttribute"/>.
/// </summary>
public class RequireHumanOversightAttributeTests
{
    [Fact]
    public void Reason_ShouldBeSettable()
    {
        var attr = new RequireHumanOversightAttribute { Reason = "Loan approval" };
        attr.Reason.Should().Be("Loan approval");
    }

    [Fact]
    public void SystemId_ShouldDefaultToNull()
    {
        var attr = new RequireHumanOversightAttribute { Reason = "Test" };
        attr.SystemId.Should().BeNull();
    }

    [Fact]
    public void SystemId_ShouldBeSettable()
    {
        var attr = new RequireHumanOversightAttribute
        {
            Reason = "Test",
            SystemId = "loan-system"
        };
        attr.SystemId.Should().Be("loan-system");
    }
}
