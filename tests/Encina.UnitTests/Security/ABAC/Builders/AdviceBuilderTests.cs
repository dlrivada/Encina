using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;

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

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_DefaultAppliesTo_IsPermit()
    {
        var advice = new AdviceBuilder("adv1").Build();

        advice.AppliesTo.Should().Be(FulfillOn.Permit);
    }

    [Fact]
    public void Build_SetsId()
    {
        var advice = new AdviceBuilder("my-advice").Build();

        advice.Id.Should().Be("my-advice");
    }

    [Fact]
    public void Build_Default_EmptyAttributeAssignments()
    {
        var advice = new AdviceBuilder("adv1").Build();

        advice.AttributeAssignments.Should().BeEmpty();
    }

    #endregion

    #region AppliesTo

    [Fact]
    public void OnPermit_SetsAppliesToPermit()
    {
        var advice = new AdviceBuilder("adv1")
            .OnPermit()
            .Build();

        advice.AppliesTo.Should().Be(FulfillOn.Permit);
    }

    [Fact]
    public void OnDeny_SetsAppliesToDeny()
    {
        var advice = new AdviceBuilder("adv1")
            .OnDeny()
            .Build();

        advice.AppliesTo.Should().Be(FulfillOn.Deny);
    }

    [Fact]
    public void AppliesTo_LastCallWins()
    {
        var advice = new AdviceBuilder("adv1")
            .OnDeny()
            .OnPermit()
            .Build();

        advice.AppliesTo.Should().Be(FulfillOn.Permit);
    }

    #endregion

    #region WithAttribute

    [Fact]
    public void WithAttribute_LiteralValue_AddsAssignment()
    {
        var advice = new AdviceBuilder("adv1")
            .WithAttribute("message", "Contact your manager")
            .Build();

        advice.AttributeAssignments.Should().ContainSingle()
            .Which.AttributeId.Should().Be("message");
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

        advice.AttributeAssignments.Should().ContainSingle();
    }

    [Fact]
    public void WithAttribute_MultipleAttributes_AllAdded()
    {
        var advice = new AdviceBuilder("adv1")
            .WithAttribute("msg", "message text")
            .WithAttribute("severity", "high")
            .Build();

        advice.AttributeAssignments.Should().HaveCount(2);
    }

    [Fact]
    public void WithAttribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("adv1")
            .WithAttribute(null!, "value");

        act.Should().Throw<ArgumentException>();
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

        advice.AttributeAssignments.Should().ContainSingle()
            .Which.Category.Should().Be(AttributeCategory.Environment);
    }

    #endregion
}
