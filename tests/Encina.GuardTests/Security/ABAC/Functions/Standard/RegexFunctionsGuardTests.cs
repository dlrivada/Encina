using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for regex functions (string-regexp-match).
/// Covers wrong arg count, null pattern, invalid regex, matching, and non-matching patterns.
/// </summary>
public class RegexFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    [Fact]
    public void StringRegexpMatch_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        var act = () => fn.Evaluate(["pattern"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 2*received 1*");
    }

    [Fact]
    public void StringRegexpMatch_NullPattern_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        var act = () => fn.Evaluate([null, "hello"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void StringRegexpMatch_InvalidRegex_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        var act = () => fn.Evaluate(["[invalid", "hello"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*invalid regex pattern*");
    }

    [Fact]
    public void StringRegexpMatch_MatchingPattern_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        fn.Evaluate([@"^\d{3}-\d{4}$", "123-4567"]).Should().Be(true);
    }

    [Fact]
    public void StringRegexpMatch_NonMatchingPattern_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        fn.Evaluate([@"^\d+$", "not-a-number"]).Should().Be(false);
    }

    [Fact]
    public void StringRegexpMatch_EmptyInput_PatternMatchesEmpty()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        fn.Evaluate(["^$", ""]).Should().Be(true);
    }

    [Fact]
    public void StringRegexpMatch_NullInput_CoercesToEmptyString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;

        fn.Evaluate(["^$", null]).Should().Be(true);
    }
}
