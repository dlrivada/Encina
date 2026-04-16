using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML string functions (concatenate, starts-with, ends-with,
/// contains, length, normalize-space).
/// </summary>
public sealed class StringFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region string-concatenate

    [Fact]
    public void StringConcatenate_TwoStrings_ReturnsJoined()
    {
        Eval(XACMLFunctionIds.StringConcatenate, "hello", " world")
            .ShouldBe("hello world");
    }

    [Fact]
    public void StringConcatenate_EmptyStrings_ReturnsEmpty()
    {
        Eval(XACMLFunctionIds.StringConcatenate, "", "")
            .ShouldBe("");
    }

    #endregion

    #region string-starts-with

    [Theory]
    [InlineData("hello world", "hello", true)]
    [InlineData("hello world", "world", false)]
    [InlineData("", "", true)]
    public void StringStartsWith_ReturnsExpected(string str, string prefix, bool expected)
    {
        Eval(XACMLFunctionIds.StringStartsWith, prefix, str).ShouldBe(expected);
    }

    #endregion

    #region string-ends-with

    [Theory]
    [InlineData("hello world", "world", true)]
    [InlineData("hello world", "hello", false)]
    public void StringEndsWith_ReturnsExpected(string str, string suffix, bool expected)
    {
        Eval(XACMLFunctionIds.StringEndsWith, suffix, str).ShouldBe(expected);
    }

    #endregion

    #region string-contains

    [Theory]
    [InlineData("hello world", "lo wo", true)]
    [InlineData("hello world", "xyz", false)]
    [InlineData("hello", "", true)]
    public void StringContains_ReturnsExpected(string str, string substring, bool expected)
    {
        Eval(XACMLFunctionIds.StringContains, substring, str).ShouldBe(expected);
    }

    #endregion

    #region string-length

    [Theory]
    [InlineData("hello", 5)]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    public void StringLength_ReturnsExpected(string str, int expected)
    {
        Eval(XACMLFunctionIds.StringLength, str).ShouldBe(expected);
    }

    #endregion
}
