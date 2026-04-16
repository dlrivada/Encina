using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="AdviceBuilder"/>.
/// Verifies XACML 3.0 §7.18: advice expression construction with AppliesTo and attribute assignments.
/// </summary>
public sealed class AdviceBuilderTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidId_DoesNotThrow()
    {
        var act = () => new AdviceBuilder("notify-user");

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder(null!);

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("");

        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_DefaultAppliesTo_IsPermit()
    {
        var advice = new AdviceBuilder("adv1").Build();

        advice.AppliesTo.ShouldBe(FulfillOn.Permit);
    }

    [Fact]
    public void Build_SetsId()
    {
        var advice = new AdviceBuilder("my-advice").Build();

        advice.Id.ShouldBe("my-advice");
    }

    [Fact]
    public void Build_Default_EmptyAttributeAssignments()
    {
        var advice = new AdviceBuilder("adv1").Build();

        advice.AttributeAssignments.ShouldBeEmpty();
    }

    #endregion

    #region AppliesTo

    [Fact]
    public void OnPermit_SetsAppliesToPermit()
    {
        var advice = new AdviceBuilder("adv1")
            .OnPermit()
            .Build();

        advice.AppliesTo.ShouldBe(FulfillOn.Permit);
    }

    [Fact]
    public void OnDeny_SetsAppliesToDeny()
    {
        var advice = new AdviceBuilder("adv1")
            .OnDeny()
            .Build();

        advice.AppliesTo.ShouldBe(FulfillOn.Deny);
    }

    [Fact]
    public void AppliesTo_LastCallWins()
    {
        var advice = new AdviceBuilder("adv1")
            .OnDeny()
            .OnPermit()
            .Build();

        advice.AppliesTo.ShouldBe(FulfillOn.Permit);
    }

    #endregion

    #region WithAttribute

    [Fact]
    public void WithAttribute_LiteralValue_AddsAssignment()
    {
        var advice = new AdviceBuilder("adv1")
            .WithAttribute("message", "Contact your manager")
            .Build();

        advice.AttributeAssignments.ShouldHaveSingleItem()
            .Which.AttributeId.ShouldBe("message");
    }

    [Fact]
    public void WithAttribute_ExpressionValue_AddsAssignment()
    {
        var value = new AttributeValue
        {
            DataType = XACMLDataTypes.String,
            Value = "test"
        };

        var advice = new AdviceBuilder("adv1")
            .WithAttribute("attr1", value)
            .Build();

        advice.AttributeAssignments.ShouldHaveSingleItem();
    }

    [Fact]
    public void WithAttribute_MultipleAttributes_AllAdded()
    {
        var advice = new AdviceBuilder("adv1")
            .WithAttribute("msg", "message text")
            .WithAttribute("severity", "high")
            .Build();

        advice.AttributeAssignments.Count.ShouldBe(2);
    }

    [Fact]
    public void WithAttribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("adv1")
            .WithAttribute(null!, "value");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void WithAttribute_WithCategory_SetsCategory()
    {
        var value = new AttributeValue
        {
            DataType = XACMLDataTypes.String,
            Value = "test"
        };

        var advice = new AdviceBuilder("adv1")
            .WithAttribute("attr1", AttributeCategory.Environment, value)
            .Build();

        advice.AttributeAssignments.ShouldHaveSingleItem()
            .Which.Category.ShouldBe(AttributeCategory.Environment);
    }

    #endregion
}
