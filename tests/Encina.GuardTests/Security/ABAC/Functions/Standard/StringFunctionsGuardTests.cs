using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for string functions (concatenate, starts-with, ends-with, contains,
/// substring, normalize-space, normalize-to-lower-case, string-length).
/// Covers wrong arg count, edge cases, and correct evaluation.
/// </summary>
public class StringFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region StringConcatenate

    [Fact]
    public void StringConcatenate_TooFewArgs_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringConcatenate)!;

        var act = () => fn.Evaluate(["only-one"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 2*received 1*");
    }

    [Fact]
    public void StringConcatenate_TwoArgs_ReturnsConcatenated()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringConcatenate)!;

        fn.Evaluate(["hello", " world"]).Should().Be("hello world");
    }

    [Fact]
    public void StringConcatenate_ThreeArgs_ReturnsConcatenated()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringConcatenate)!;

        fn.Evaluate(["a", "b", "c"]).Should().Be("abc");
    }

    [Fact]
    public void StringConcatenate_NullArg_CoercesToEmptyString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringConcatenate)!;

        fn.Evaluate([null, "world"]).Should().Be("world");
    }

    #endregion

    #region StringStartsWith

    [Fact]
    public void StringStartsWith_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringStartsWith)!;

        var act = () => fn.Evaluate(["only-one"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 2*received 1*");
    }

    [Fact]
    public void StringStartsWith_MatchingPrefix_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringStartsWith)!;

        fn.Evaluate(["hello", "hello world"]).Should().Be(true);
    }

    [Fact]
    public void StringStartsWith_NonMatchingPrefix_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringStartsWith)!;

        fn.Evaluate(["world", "hello world"]).Should().Be(false);
    }

    #endregion

    #region StringEndsWith

    [Fact]
    public void StringEndsWith_MatchingSuffix_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEndsWith)!;

        fn.Evaluate(["world", "hello world"]).Should().Be(true);
    }

    [Fact]
    public void StringEndsWith_NonMatchingSuffix_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEndsWith)!;

        fn.Evaluate(["hello", "hello world"]).Should().Be(false);
    }

    #endregion

    #region StringContains

    [Fact]
    public void StringContains_SubstringPresent_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringContains)!;

        fn.Evaluate(["lo wo", "hello world"]).Should().Be(true);
    }

    [Fact]
    public void StringContains_SubstringAbsent_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringContains)!;

        fn.Evaluate(["xyz", "hello world"]).Should().Be(false);
    }

    #endregion

    #region StringSubstring

    [Fact]
    public void StringSubstring_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        var act = () => fn.Evaluate(["hello", 0]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 3*received 2*");
    }

    [Fact]
    public void StringSubstring_ValidRange_ReturnsSubstring()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        fn.Evaluate(["hello world", 0, 5]).Should().Be("hello");
    }

    [Fact]
    public void StringSubstring_EndMinusOne_ReturnsToEnd()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        fn.Evaluate(["hello world", 6, -1]).Should().Be("world");
    }

    [Fact]
    public void StringSubstring_NegativeBegin_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        var act = () => fn.Evaluate(["hello", -1, 3]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*begin index*out of range*");
    }

    [Fact]
    public void StringSubstring_BeginBeyondLength_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        var act = () => fn.Evaluate(["hi", 10, -1]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*begin index*out of range*");
    }

    [Fact]
    public void StringSubstring_EndBeforeBegin_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        var act = () => fn.Evaluate(["hello", 3, 1]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*end index*out of range*");
    }

    [Fact]
    public void StringSubstring_EndBeyondLength_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringSubstring)!;

        var act = () => fn.Evaluate(["hi", 0, 100]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*end index*out of range*");
    }

    #endregion

    #region StringNormalizeSpace

    [Fact]
    public void StringNormalizeSpace_MultipleSpaces_NormalizesToSingle()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringNormalizeSpace)!;

        fn.Evaluate(["  hello   world  "]).Should().Be("hello world");
    }

    [Fact]
    public void StringNormalizeSpace_TabsAndNewlines_NormalizesToSingle()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringNormalizeSpace)!;

        fn.Evaluate(["hello\t\n  world"]).Should().Be("hello world");
    }

    [Fact]
    public void StringNormalizeSpace_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringNormalizeSpace)!;

        var act = () => fn.Evaluate(["a", "b"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 1*received 2*");
    }

    #endregion

    #region StringNormalizeToLowerCase

    [Fact]
    public void StringNormalizeToLowerCase_MixedCase_ReturnsLower()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringNormalizeToLowerCase)!;

        fn.Evaluate(["Hello WORLD"]).Should().Be("hello world");
    }

    #endregion

    #region StringLength

    [Fact]
    public void StringLength_ValidString_ReturnsLength()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringLength)!;

        fn.Evaluate(["hello"]).Should().Be(5);
    }

    [Fact]
    public void StringLength_EmptyString_ReturnsZero()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringLength)!;

        fn.Evaluate([""]).Should().Be(0);
    }

    [Fact]
    public void StringLength_NullArg_ReturnsZero()
    {
        // CoerceToString(null) returns empty string
        var fn = _registry.GetFunction(XACMLFunctionIds.StringLength)!;

        fn.Evaluate([null]).Should().Be(0);
    }

    #endregion
}
