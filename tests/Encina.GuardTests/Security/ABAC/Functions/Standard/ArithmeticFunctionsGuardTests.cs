using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for arithmetic functions (integer add/sub/mul/div/mod/abs, double add/sub/mul/div/abs, round, floor).
/// Covers null args, wrong arg count, division by zero, overflow, and correct evaluation.
/// </summary>
public class ArithmeticFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region Integer Arithmetic

    [Fact]
    public void IntegerAdd_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;

        var act = () => fn.Evaluate([1]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 2*received 1*");
    }

    [Fact]
    public void IntegerAdd_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;

        var act = () => fn.Evaluate([null, 5]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void IntegerAdd_ValidArgs_ReturnsSum()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;

        fn.Evaluate([10, 5]).Should().Be(15);
    }

    [Fact]
    public void IntegerSubtract_ValidArgs_ReturnsDifference()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerSubtract)!;

        fn.Evaluate([10, 3]).Should().Be(7);
    }

    [Fact]
    public void IntegerMultiply_ValidArgs_ReturnsProduct()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMultiply)!;

        fn.Evaluate([6, 7]).Should().Be(42);
    }

    [Fact]
    public void IntegerDivide_ValidArgs_ReturnsQuotient()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerDivide)!;

        fn.Evaluate([10, 3]).Should().Be(3);
    }

    [Fact]
    public void IntegerDivide_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerDivide)!;

        var act = () => fn.Evaluate([10, 0]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*division by zero*");
    }

    [Fact]
    public void IntegerMod_ValidArgs_ReturnsRemainder()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMod)!;

        fn.Evaluate([10, 3]).Should().Be(1);
    }

    [Fact]
    public void IntegerMod_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMod)!;

        var act = () => fn.Evaluate([10, 0]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*division by zero*");
    }

    [Fact]
    public void IntegerAbs_PositiveValue_ReturnsSame()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        fn.Evaluate([42]).Should().Be(42);
    }

    [Fact]
    public void IntegerAbs_NegativeValue_ReturnsPositive()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        fn.Evaluate([-42]).Should().Be(42);
    }

    [Fact]
    public void IntegerAbs_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        var act = () => fn.Evaluate([1, 2]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 1*received 2*");
    }

    #endregion

    #region Double Arithmetic

    [Fact]
    public void DoubleAdd_ValidArgs_ReturnsSum()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleAdd)!;

        fn.Evaluate([1.5, 2.5]).Should().Be(4.0);
    }

    [Fact]
    public void DoubleSubtract_ValidArgs_ReturnsDifference()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleSubtract)!;

        fn.Evaluate([5.0, 2.5]).Should().Be(2.5);
    }

    [Fact]
    public void DoubleMultiply_ValidArgs_ReturnsProduct()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleMultiply)!;

        fn.Evaluate([3.0, 4.0]).Should().Be(12.0);
    }

    [Fact]
    public void DoubleDivide_ValidArgs_ReturnsQuotient()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleDivide)!;

        fn.Evaluate([10.0, 4.0]).Should().Be(2.5);
    }

    [Fact]
    public void DoubleDivide_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleDivide)!;

        var act = () => fn.Evaluate([10.0, 0.0]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*division by zero*");
    }

    [Fact]
    public void DoubleAbs_NegativeValue_ReturnsPositive()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleAbs)!;

        fn.Evaluate([-3.14]).Should().Be(3.14);
    }

    #endregion

    #region Rounding Functions

    [Fact]
    public void Round_ValidArg_RoundsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Round)!;

        fn.Evaluate([3.5]).Should().Be(4.0);
    }

    [Fact]
    public void Round_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Round)!;

        var act = () => fn.Evaluate([null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void Floor_ValidArg_FloorsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Floor)!;

        fn.Evaluate([3.9]).Should().Be(3.0);
    }

    [Fact]
    public void Floor_NegativeValue_FloorsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Floor)!;

        fn.Evaluate([-3.1]).Should().Be(-4.0);
    }

    #endregion
}
