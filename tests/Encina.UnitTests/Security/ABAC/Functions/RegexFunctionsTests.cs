using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML regex functions (string-regexp-match).
/// </summary>
public sealed class RegexFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    [Fact]
    public void StringRegexpMatch_MatchFound_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.StringRegexpMatch, @"^\d+$", "12345").ShouldBe(true);
    }

    [Fact]
    public void StringRegexpMatch_NoMatch_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.StringRegexpMatch, @"^\d+$", "abc").ShouldBe(false);
    }

    [Fact]
    public void StringRegexpMatch_ComplexPattern_Works()
    {
        Eval(XACMLFunctionIds.StringRegexpMatch, @"^[a-z]+@[a-z]+\.[a-z]+$", "user@example.com")
            .ShouldBe(true);
    }

    [Fact]
    public void StringRegexpMatch_EmptyString_EmptyPattern_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.StringRegexpMatch, "", "").ShouldBe(true);
    }

    [Fact]
    public void StringRegexpMatch_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringRegexpMatch)!;
        var act = () => fn.Evaluate(["pattern"]);
        Should.Throw<InvalidOperationException>(act);
    }
}
