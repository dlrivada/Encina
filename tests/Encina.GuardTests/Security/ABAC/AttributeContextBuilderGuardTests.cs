using Encina.Security.ABAC;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC;

/// <summary>
/// Guard clause tests for <see cref="AttributeContextBuilder"/>.
/// </summary>
public class AttributeContextBuilderGuardTests
{
    private static readonly IReadOnlyDictionary<string, object> EmptyDict = new Dictionary<string, object>();

    #region Build Guards

    [Fact]
    public void Build_NullSubjectAttributes_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.Build(null!, EmptyDict, EmptyDict, typeof(object));
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("subjectAttributes");
    }

    [Fact]
    public void Build_NullResourceAttributes_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.Build(EmptyDict, null!, EmptyDict, typeof(object));
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("resourceAttributes");
    }

    [Fact]
    public void Build_NullEnvironmentAttributes_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.Build(EmptyDict, EmptyDict, null!, typeof(object));
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("environmentAttributes");
    }

    [Fact]
    public void Build_NullRequestType_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.Build(EmptyDict, EmptyDict, EmptyDict, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void Build_ValidInputs_ReturnsContext()
    {
        var context = AttributeContextBuilder.Build(EmptyDict, EmptyDict, EmptyDict, typeof(string));
        context.RequestType.ShouldBe(typeof(string));
        context.SubjectAttributes.IsEmpty.ShouldBeTrue();
        context.ResourceAttributes.IsEmpty.ShouldBeTrue();
        context.EnvironmentAttributes.IsEmpty.ShouldBeTrue();
        context.ActionAttributes.IsEmpty.ShouldBeFalse(); // auto-generated action bag
    }

    [Fact]
    public void Build_IncludeAdviceDefault_IsTrue()
    {
        var context = AttributeContextBuilder.Build(EmptyDict, EmptyDict, EmptyDict, typeof(string));
        context.IncludeAdvice.ShouldBeTrue();
    }

    [Fact]
    public void Build_IncludeAdviceFalse_SetsCorrectly()
    {
        var context = AttributeContextBuilder.Build(EmptyDict, EmptyDict, EmptyDict, typeof(string), includeAdvice: false);
        context.IncludeAdvice.ShouldBeFalse();
    }

    #endregion

    #region ToBag Guards

    [Fact]
    public void ToBag_NullAttributes_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.ToBag(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("attributes");
    }

    [Fact]
    public void ToBag_EmptyDictionary_ReturnsEmptyBag()
    {
        var bag = AttributeContextBuilder.ToBag(EmptyDict);
        bag.IsEmpty.ShouldBeTrue();
        bag.ShouldBeSameAs(AttributeBag.Empty);
    }

    [Fact]
    public void ToBag_WithStringValue_InfersStringDataType()
    {
        var attrs = new Dictionary<string, object> { ["name"] = "Alice" };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Count.ShouldBe(1);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.String);
        bag.Values[0].Value.ShouldBe("Alice");
    }

    [Fact]
    public void ToBag_WithIntValue_InfersIntegerDataType()
    {
        var attrs = new Dictionary<string, object> { ["age"] = 25 };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.Integer);
    }

    [Fact]
    public void ToBag_WithBoolValue_InfersBooleanDataType()
    {
        var attrs = new Dictionary<string, object> { ["active"] = true };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.Boolean);
    }

    [Fact]
    public void ToBag_WithDoubleValue_InfersDoubleDataType()
    {
        var attrs = new Dictionary<string, object> { ["score"] = 99.5 };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.Double);
    }

    [Fact]
    public void ToBag_WithDateTimeValue_InfersDateTimeDataType()
    {
        var attrs = new Dictionary<string, object> { ["timestamp"] = DateTime.UtcNow };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.DateTime);
    }

    [Fact]
    public void ToBag_WithUriValue_InfersAnyURIDataType()
    {
        var attrs = new Dictionary<string, object> { ["url"] = new Uri("https://example.com") };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.AnyURI);
    }

    [Fact]
    public void ToBag_WithUnknownType_InfersStringDataType()
    {
        var attrs = new Dictionary<string, object> { ["custom"] = new object() };
        var bag = AttributeContextBuilder.ToBag(attrs);
        bag.Values[0].DataType.ShouldBe(XACMLDataTypes.String);
    }

    #endregion
}
