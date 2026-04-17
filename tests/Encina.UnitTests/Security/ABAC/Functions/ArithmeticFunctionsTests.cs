using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML arithmetic functions (integer and double add, subtract,
/// multiply, divide, mod, abs, floor, round).
/// </summary>
public sealed class ArithmeticFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region Integer arithmetic

    [Theory]
    [InlineData(3, 4, 7)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    public void IntegerAdd_ReturnsExpected(int a, int b, int expected)
    {
        Eval(XACMLFunctionIds.IntegerAdd, a, b).ShouldBe(expected);
    }

    [Theory]
    [InlineData(10, 3, 7)]
    [InlineData(5, 5, 0)]
    [InlineData(0, 1, -1)]
    public void IntegerSubtract_ReturnsExpected(int a, int b, int expected)
    {
        Eval(XACMLFunctionIds.IntegerSubtract, a, b).ShouldBe(expected);
    }

    [Theory]
    [InlineData(3, 4, 12)]
    [InlineData(0, 100, 0)]
    [InlineData(-2, 3, -6)]
    public void IntegerMultiply_ReturnsExpected(int a, int b, int expected)
    {
        Eval(XACMLFunctionIds.IntegerMultiply, a, b).ShouldBe(expected);
    }

    [Theory]
    [InlineData(10, 3, 3)]
    [InlineData(9, 3, 3)]
    [InlineData(-10, 3, -3)]
    public void IntegerDivide_ReturnsExpected(int a, int b, int expected)
    {
        Eval(XACMLFunctionIds.IntegerDivide, a, b).ShouldBe(expected);
    }

    [Fact]
    public void IntegerDivide_ByZero_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerDivide)!;
        var act = () => fn.Evaluate([10, 0]);
        Should.Throw<Exception>(act);
    }

    [Theory]
    [InlineData(10, 3, 1)]
    [InlineData(9, 3, 0)]
    [InlineData(7, 2, 1)]
    public void IntegerMod_ReturnsExpected(int a, int b, int expected)
    {
        Eval(XACMLFunctionIds.IntegerMod, a, b).ShouldBe(expected);
    }

    #endregion

    #region Double arithmetic

    [Theory]
    [InlineData(1.5, 2.5, 4.0)]
    [InlineData(0.0, 0.0, 0.0)]
    public void DoubleAdd_ReturnsExpected(double a, double b, double expected)
    {
        ((double)Eval(XACMLFunctionIds.DoubleAdd, a, b)!).ShouldBeInRange(expected - 0.001, expected + 0.001);
    }

    [Theory]
    [InlineData(5.0, 2.0, 3.0)]
    [InlineData(1.0, 1.0, 0.0)]
    public void DoubleSubtract_ReturnsExpected(double a, double b, double expected)
    {
        ((double)Eval(XACMLFunctionIds.DoubleSubtract, a, b)!).ShouldBeInRange(expected - 0.001, expected + 0.001);
    }

    [Theory]
    [InlineData(2.5, 4.0, 10.0)]
    [InlineData(0.0, 100.0, 0.0)]
    public void DoubleMultiply_ReturnsExpected(double a, double b, double expected)
    {
        ((double)Eval(XACMLFunctionIds.DoubleMultiply, a, b)!).ShouldBeInRange(expected - 0.001, expected + 0.001);
    }

    [Theory]
    [InlineData(10.0, 4.0, 2.5)]
    [InlineData(1.0, 3.0, 0.333)]
    public void DoubleDivide_ReturnsExpected(double a, double b, double expected)
    {
        ((double)Eval(XACMLFunctionIds.DoubleDivide, a, b)!).ShouldBeInRange(expected - 0.01, expected + 0.01);
    }

    #endregion

    #region Wrong arg count

    [Fact]
    public void Arithmetic_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;
        var act = () => fn.Evaluate([1, 2, 3]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion
}
