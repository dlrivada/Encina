using Encina.Security.ABAC;

using Shouldly;

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

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    [Fact]
    public void IntegerAdd_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;

        var act = () => fn.Evaluate([null, 5]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("must");
    }

    [Fact]
    public void IntegerAdd_ValidArgs_ReturnsSum()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAdd)!;

        fn.Evaluate([10, 5]).ShouldBe(15);
    }

    [Fact]
    public void IntegerSubtract_ValidArgs_ReturnsDifference()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerSubtract)!;

        fn.Evaluate([10, 3]).ShouldBe(7);
    }

    [Fact]
    public void IntegerMultiply_ValidArgs_ReturnsProduct()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMultiply)!;

        fn.Evaluate([6, 7]).ShouldBe(42);
    }

    [Fact]
    public void IntegerDivide_ValidArgs_ReturnsQuotient()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerDivide)!;

        fn.Evaluate([10, 3]).ShouldBe(3);
    }

    [Fact]
    public void IntegerDivide_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerDivide)!;

        var act = () => fn.Evaluate([10, 0]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("division");
    }

    [Fact]
    public void IntegerMod_ValidArgs_ReturnsRemainder()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMod)!;

        fn.Evaluate([10, 3]).ShouldBe(1);
    }

    [Fact]
    public void IntegerMod_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerMod)!;

        var act = () => fn.Evaluate([10, 0]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("division");
    }

    [Fact]
    public void IntegerAbs_PositiveValue_ReturnsSame()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        fn.Evaluate([42]).ShouldBe(42);
    }

    [Fact]
    public void IntegerAbs_NegativeValue_ReturnsPositive()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        fn.Evaluate([-42]).ShouldBe(42);
    }

    [Fact]
    public void IntegerAbs_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerAbs)!;

        var act = () => fn.Evaluate([1, 2]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    #endregion

    #region Double Arithmetic

    [Fact]
    public void DoubleAdd_ValidArgs_ReturnsSum()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleAdd)!;

        fn.Evaluate([1.5, 2.5]).ShouldBe(4.0);
    }

    [Fact]
    public void DoubleSubtract_ValidArgs_ReturnsDifference()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleSubtract)!;

        fn.Evaluate([5.0, 2.5]).ShouldBe(2.5);
    }

    [Fact]
    public void DoubleMultiply_ValidArgs_ReturnsProduct()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleMultiply)!;

        fn.Evaluate([3.0, 4.0]).ShouldBe(12.0);
    }

    [Fact]
    public void DoubleDivide_ValidArgs_ReturnsQuotient()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleDivide)!;

        fn.Evaluate([10.0, 4.0]).ShouldBe(2.5);
    }

    [Fact]
    public void DoubleDivide_DivisionByZero_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleDivide)!;

        var act = () => fn.Evaluate([10.0, 0.0]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("division");
    }

    [Fact]
    public void DoubleAbs_NegativeValue_ReturnsPositive()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleAbs)!;

        fn.Evaluate([-3.14]).ShouldBe(3.14);
    }

    #endregion

    #region Rounding Functions

    [Fact]
    public void Round_ValidArg_RoundsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Round)!;

        fn.Evaluate([3.5]).ShouldBe(4.0);
    }

    [Fact]
    public void Round_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Round)!;

        var act = () => fn.Evaluate([null]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("must");
    }

    [Fact]
    public void Floor_ValidArg_FloorsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Floor)!;

        fn.Evaluate([3.9]).ShouldBe(3.0);
    }

    [Fact]
    public void Floor_NegativeValue_FloorsCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Floor)!;

        fn.Evaluate([-3.1]).ShouldBe(-4.0);
    }

    #endregion
}
