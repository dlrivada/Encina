using Encina.Security.ABAC;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML logical functions (and, or, not, n-of).
/// </summary>
public sealed class LogicalFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region and

    [Fact]
    public void And_AllTrue_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.And, true, true, true).Should().Be(true);
    }

    [Fact]
    public void And_OneFalse_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.And, true, false, true).Should().Be(false);
    }

    [Fact]
    public void And_SingleTrue_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.And, true).Should().Be(true);
    }

    [Fact]
    public void And_SingleFalse_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.And, false).Should().Be(false);
    }

    [Fact]
    public void And_NoArgs_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;
        var act = () => fn.Evaluate([]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region or

    [Fact]
    public void Or_AllFalse_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.Or, false, false, false).Should().Be(false);
    }

    [Fact]
    public void Or_OneTrue_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.Or, false, true, false).Should().Be(true);
    }

    [Fact]
    public void Or_SingleTrue_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.Or, true).Should().Be(true);
    }

    #endregion

    #region not

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Not_ReturnsExpected(bool input, bool expected)
    {
        Eval(XACMLFunctionIds.Not, input).Should().Be(expected);
    }

    [Fact]
    public void Not_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Not)!;
        var act = () => fn.Evaluate([true, false]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region n-of

    [Fact]
    public void NOf_ThresholdMet_ReturnsTrue()
    {
        // n=2, with 3 true values → true
        Eval(XACMLFunctionIds.NOf, 2, true, true, false).Should().Be(true);
    }

    [Fact]
    public void NOf_ThresholdNotMet_ReturnsFalse()
    {
        // n=3, with only 2 true values → false
        Eval(XACMLFunctionIds.NOf, 3, true, true, false).Should().Be(false);
    }

    [Fact]
    public void NOf_ZeroThreshold_AlwaysTrue()
    {
        // n=0, any values → true (0 out of N need to be true)
        Eval(XACMLFunctionIds.NOf, 0, false, false).Should().Be(true);
    }

    [Fact]
    public void NOf_ExactThreshold_ReturnsTrue()
    {
        // n=3, exactly 3 true → true
        Eval(XACMLFunctionIds.NOf, 3, true, true, true).Should().Be(true);
    }

    [Fact]
    public void NOf_NotEnoughArgs_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;
        // Only N, no boolean args
        var act = () => fn.Evaluate([2]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
