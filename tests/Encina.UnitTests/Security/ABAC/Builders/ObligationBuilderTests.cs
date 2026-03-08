using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Builders;

/// <summary>
/// Unit tests for <see cref="ObligationBuilder"/>.
/// Verifies XACML 3.0 §7.18: obligation construction with FulfillOn and attribute assignments.
/// </summary>
public sealed class ObligationBuilderTests
{
    #region Constructor

    [Fact]
    public void Constructor_ValidId_DoesNotThrow()
    {
        var act = () => new ObligationBuilder("log-access");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullId_ThrowsArgumentException()
    {
        var act = () => new ObligationBuilder(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new ObligationBuilder("");

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Build — Defaults

    [Fact]
    public void Build_DefaultFulfillOn_IsPermit()
    {
        var obligation = new ObligationBuilder("ob1").Build();

        obligation.FulfillOn.Should().Be(FulfillOn.Permit);
    }

    [Fact]
    public void Build_SetsId()
    {
        var obligation = new ObligationBuilder("my-obligation").Build();

        obligation.Id.Should().Be("my-obligation");
    }

    [Fact]
    public void Build_Default_EmptyAttributeAssignments()
    {
        var obligation = new ObligationBuilder("ob1").Build();

        obligation.AttributeAssignments.Should().BeEmpty();
    }

    #endregion

    #region FulfillOn

    [Fact]
    public void OnPermit_SetsFulfillOnPermit()
    {
        var obligation = new ObligationBuilder("ob1")
            .OnPermit()
            .Build();

        obligation.FulfillOn.Should().Be(FulfillOn.Permit);
    }

    [Fact]
    public void OnDeny_SetsFulfillOnDeny()
    {
        var obligation = new ObligationBuilder("ob1")
            .OnDeny()
            .Build();

        obligation.FulfillOn.Should().Be(FulfillOn.Deny);
    }

    [Fact]
    public void FulfillOn_LastCallWins()
    {
        var obligation = new ObligationBuilder("ob1")
            .OnPermit()
            .OnDeny()
            .Build();

        obligation.FulfillOn.Should().Be(FulfillOn.Deny);
    }

    #endregion

    #region WithAttribute

    [Fact]
    public void WithAttribute_LiteralValue_AddsAssignment()
    {
        var obligation = new ObligationBuilder("ob1")
            .WithAttribute("reason", "Audit trail")
            .Build();

        obligation.AttributeAssignments.Should().ContainSingle()
            .Which.AttributeId.Should().Be("reason");
    }

    [Fact]
    public void WithAttribute_ExpressionValue_AddsAssignment()
    {
        var value = new AttributeValue
        {
            DataType = XACMLDataTypes.String,
            Value = "test"
        };

        var obligation = new ObligationBuilder("ob1")
            .WithAttribute("attr1", value)
            .Build();

        obligation.AttributeAssignments.Should().ContainSingle();
    }

    [Fact]
    public void WithAttribute_MultipleAttributes_AllAdded()
    {
        var obligation = new ObligationBuilder("ob1")
            .WithAttribute("attr1", "value1")
            .WithAttribute("attr2", "value2")
            .WithAttribute("attr3", "value3")
            .Build();

        obligation.AttributeAssignments.Should().HaveCount(3);
    }

    [Fact]
    public void WithAttribute_NullAttributeId_ThrowsArgumentException()
    {
        var act = () => new ObligationBuilder("ob1")
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

        var obligation = new ObligationBuilder("ob1")
            .WithAttribute("attr1", AttributeCategory.Subject, value)
            .Build();

        obligation.AttributeAssignments.Should().ContainSingle()
            .Which.Category.Should().Be(AttributeCategory.Subject);
    }

    #endregion
}
