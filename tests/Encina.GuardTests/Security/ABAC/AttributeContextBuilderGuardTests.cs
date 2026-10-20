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
    public void Build_ValidInputs_ReturnsContextWithTheActionNameAttribute()
    {
        var context = AttributeContextBuilder.Build(EmptyDict, EmptyDict, EmptyDict, typeof(string));
        context.RequestType.ShouldBe(typeof(string));
        context.SubjectAttributes.Count.ShouldBe(0);
        context.ResourceAttributes.Count.ShouldBe(0);
        context.EnvironmentAttributes.Count.ShouldBe(0);

        // The action category holds one attribute, "name", with the request type name.
        context.ActionAttributes.Keys.ShouldBe(["name"]);
        var action = context.ActionAttributes["name"].SingleValue();
        action.DataType.ShouldBe(XACMLDataTypes.String);
        action.Value.ShouldBe(nameof(String));
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

    #region ToAttributeBags Guards

    [Fact]
    public void ToAttributeBags_NullAttributes_ThrowsArgumentNullException()
    {
        var act = () => AttributeContextBuilder.ToAttributeBags(null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("attributes");
    }

    [Fact]
    public void ToAttributeBags_EmptyDictionary_ReturnsNoAttributes()
    {
        var bags = AttributeContextBuilder.ToAttributeBags(EmptyDict);
        bags.Count.ShouldBe(0);
    }

    [Fact]
    public void ToAttributeBags_KeepsOneBagPerAttributeId()
    {
        var attrs = new Dictionary<string, object> { ["name"] = "Alice", ["age"] = 25 };

        var bags = AttributeContextBuilder.ToAttributeBags(attrs);

        bags.Count.ShouldBe(2);
        bags["name"].SingleValue().Value.ShouldBe("Alice");
        bags["age"].SingleValue().Value.ShouldBe(25);
    }

    [Fact]
    public void ToAttributeBags_ResultCannotBeMutatedThroughADowncast()
    {
        var bags = AttributeContextBuilder.ToAttributeBags(new Dictionary<string, object> { ["name"] = "Alice" });

        (bags is IDictionary<string, AttributeBag> { IsReadOnly: false }).ShouldBeFalse();
    }

    public static TheoryData<object, string> InferredDataTypes => new()
    {
        { "Alice", XACMLDataTypes.String },
        { (sbyte)1, XACMLDataTypes.Integer },
        { (byte)1, XACMLDataTypes.Integer },
        { (short)1, XACMLDataTypes.Integer },
        { (ushort)1, XACMLDataTypes.Integer },
        { 25, XACMLDataTypes.Integer },
        { 25u, XACMLDataTypes.Integer },
        { 25L, XACMLDataTypes.Integer },
        { 25UL, XACMLDataTypes.Integer },
        { true, XACMLDataTypes.Boolean },
        { 99.5, XACMLDataTypes.Double },
        { 99.5f, XACMLDataTypes.Double },
        { 99.5m, XACMLDataTypes.Double },
        { new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc), XACMLDataTypes.DateTime },
        { new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.Zero), XACMLDataTypes.DateTime },
        { new DateOnly(2026, 10, 7), XACMLDataTypes.Date },
        { new TimeSpan(9, 30, 0), XACMLDataTypes.Time },
        { new Uri("https://example.com"), XACMLDataTypes.AnyURI },
        { Guid.Empty, XACMLDataTypes.String }
    };

    [Theory]
    [MemberData(nameof(InferredDataTypes))]
    public void ToAttributeBags_InfersTheXacmlDataType(object value, string expectedDataType)
    {
        var bags = AttributeContextBuilder.ToAttributeBags(new Dictionary<string, object> { ["attribute"] = value });

        var stored = bags["attribute"].SingleValue();
        stored.DataType.ShouldBe(expectedDataType);
        stored.Value.ShouldBe(value);
    }

    #endregion
}
