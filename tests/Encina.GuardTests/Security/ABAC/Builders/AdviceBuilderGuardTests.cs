using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Builders;

/// <summary>
/// Guard clause tests for <see cref="AdviceBuilder"/>.
/// </summary>
public class AdviceBuilderGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullId_ThrowsArgumentNullException()
    {
        var act = () => new AdviceBuilder(null!);
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("");
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceId_ThrowsArgumentException()
    {
        var act = () => new AdviceBuilder("   ");
        act.ShouldThrow<ArgumentException>();
    }

    #endregion

    #region WithAttribute Guards

    [Fact]
    public void WithAttribute_Expression_NullAttributeId_ThrowsArgumentException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute(null!, new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithAttribute_Expression_EmptyAttributeId_ThrowsArgumentException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute("", new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" });
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void WithAttribute_Expression_NullValue_ThrowsArgumentNullException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute("attr-1", (IExpression)null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("value");
    }

    [Fact]
    public void WithAttribute_Literal_NullAttributeId_ThrowsArgumentException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute(null!, (object?)"test");
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithAttribute_CategoryScoped_NullAttributeId_ThrowsArgumentException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute(null!, AttributeCategory.Subject, new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" });
        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void WithAttribute_CategoryScoped_NullValue_ThrowsArgumentNullException()
    {
        var builder = new AdviceBuilder("adv-1");
        var act = () => builder.WithAttribute("attr-1", AttributeCategory.Subject, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("value");
    }

    #endregion

    #region Build

    [Fact]
    public void Build_ReturnsAdviceWithCorrectId()
    {
        var advice = new AdviceBuilder("adv-1").Build();
        advice.Id.ShouldBe("adv-1");
        advice.AppliesTo.ShouldBe(FulfillOn.Permit); // default
    }

    [Fact]
    public void OnDeny_SetsAppliesToDeny()
    {
        var advice = new AdviceBuilder("adv-1").OnDeny().Build();
        advice.AppliesTo.ShouldBe(FulfillOn.Deny);
    }

    #endregion
}
