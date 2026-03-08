using Encina.Security.ABAC;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML comparison functions (greater-than, less-than,
/// greater-than-or-equal, less-than-or-equal) across all data types.
/// </summary>
public sealed class ComparisonFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region Integer comparisons

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, false)]
    public void IntegerGreaterThan_ReturnsExpected(int a, int b, bool expected)
    {
        Eval(XACMLFunctionIds.IntegerGreaterThan, a, b).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 3, false)]
    [InlineData(5, 5, false)]
    public void IntegerLessThan_ReturnsExpected(int a, int b, bool expected)
    {
        Eval(XACMLFunctionIds.IntegerLessThan, a, b).Should().Be(expected);
    }

    [Theory]
    [InlineData(5, 3, true)]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, false)]
    public void IntegerGreaterThanOrEqual_ReturnsExpected(int a, int b, bool expected)
    {
        Eval(XACMLFunctionIds.IntegerGreaterThanOrEqual, a, b).Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(5, 3, false)]
    public void IntegerLessThanOrEqual_ReturnsExpected(int a, int b, bool expected)
    {
        Eval(XACMLFunctionIds.IntegerLessThanOrEqual, a, b).Should().Be(expected);
    }

    #endregion

    #region Double comparisons

    [Theory]
    [InlineData(3.14, 2.71, true)]
    [InlineData(2.71, 3.14, false)]
    [InlineData(1.0, 1.0, false)]
    public void DoubleGreaterThan_ReturnsExpected(double a, double b, bool expected)
    {
        Eval(XACMLFunctionIds.DoubleGreaterThan, a, b).Should().Be(expected);
    }

    [Theory]
    [InlineData(2.71, 3.14, true)]
    [InlineData(3.14, 2.71, false)]
    public void DoubleLessThan_ReturnsExpected(double a, double b, bool expected)
    {
        Eval(XACMLFunctionIds.DoubleLessThan, a, b).Should().Be(expected);
    }

    #endregion

    #region String comparisons

    [Theory]
    [InlineData("b", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("a", "a", false)]
    public void StringGreaterThan_ReturnsExpected(string a, string b, bool expected)
    {
        Eval(XACMLFunctionIds.StringGreaterThan, a, b).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "b", true)]
    [InlineData("b", "a", false)]
    public void StringLessThan_ReturnsExpected(string a, string b, bool expected)
    {
        Eval(XACMLFunctionIds.StringLessThan, a, b).Should().Be(expected);
    }

    #endregion

    #region Wrong arg count

    [Fact]
    public void Comparison_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerGreaterThan)!;
        var act = () => fn.Evaluate([1]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
