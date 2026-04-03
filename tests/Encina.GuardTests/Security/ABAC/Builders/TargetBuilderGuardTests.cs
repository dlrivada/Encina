using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="TargetBuilder"/>, <see cref="AnyOfBuilder"/>, and <see cref="AllOfBuilder"/>.
/// </summary>
public class TargetBuilderGuardTests
{
    #region TargetBuilder Guards

    [Fact]
    public void AnyOf_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new TargetBuilder();
        var act = () => builder.AnyOf(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    [Fact]
    public void Build_EmptyTarget_Succeeds()
    {
        var target = new TargetBuilder().Build();
        target.AnyOfElements.Count.ShouldBe(0);
    }

    #endregion

    #region AnyOfBuilder Guards

    [Fact]
    public void AnyOfBuilder_AllOf_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new AnyOfBuilder();
        var act = () => builder.AllOf(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("configure");
    }

    #endregion

    #region AllOfBuilder Guards

    [Fact]
    public void AllOfBuilder_Match_NullFunctionId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(
            null!,
            new AttributeDesignator { Category = AttributeCategory.Subject, AttributeId = "role", DataType = XACMLDataTypes.String },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AllOfBuilder_Match_EmptyFunctionId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(
            "",
            new AttributeDesignator { Category = AttributeCategory.Subject, AttributeId = "role", DataType = XACMLDataTypes.String },
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AllOfBuilder_Match_NullDesignator_ThrowsArgumentNullException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(
            "string-equal",
            null!,
            new AttributeValue { DataType = XACMLDataTypes.String, Value = "Admin" });
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("designator");
    }

    [Fact]
    public void AllOfBuilder_Match_NullValue_ThrowsArgumentNullException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(
            "string-equal",
            new AttributeDesignator { Category = AttributeCategory.Subject, AttributeId = "role", DataType = XACMLDataTypes.String },
            null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("value");
    }

    [Fact]
    public void AllOfBuilder_MatchByCategory_NullAttributeId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(AttributeCategory.Subject, null!, XACMLDataTypes.String, ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AllOfBuilder_MatchByCategory_EmptyAttributeId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(AttributeCategory.Subject, "", XACMLDataTypes.String, ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AllOfBuilder_MatchByCategory_NullDataType_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(AttributeCategory.Subject, "role", null!, ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AllOfBuilder_MatchByCategory_EmptyDataType_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.Match(AttributeCategory.Subject, "role", "", ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void AllOfBuilder_MatchAttribute_NullAttributeId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.MatchAttribute(AttributeCategory.Subject, null!, ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AllOfBuilder_MatchAttribute_EmptyAttributeId_ThrowsArgumentException()
    {
        var builder = new AllOfBuilder();
        var act = () => builder.MatchAttribute(AttributeCategory.Subject, "", ConditionOperator.Equals, "Admin");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion
}
