using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for logical functions (and, or, not, n-of).
/// Covers short-circuit behavior, wrong arg count, null args, non-boolean args, and edge cases.
/// </summary>
public class LogicalFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region And

    [Fact]
    public void And_TooFewArgs_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        var act = () => fn.Evaluate(Array.Empty<object?>());

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 1*received 0*");
    }

    [Fact]
    public void And_AllTrue_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        fn.Evaluate([true, true, true]).Should().Be(true);
    }

    [Fact]
    public void And_OneFalse_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        fn.Evaluate([true, false, true]).Should().Be(false);
    }

    [Fact]
    public void And_SingleTrue_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        fn.Evaluate([true]).Should().Be(true);
    }

    [Fact]
    public void And_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        var act = () => fn.Evaluate([true, null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void And_NonBooleanArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        var act = () => fn.Evaluate([true, 42]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot convert*Boolean*");
    }

    [Fact]
    public void And_StringBooleanValues_CoercesCorrectly()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.And)!;

        fn.Evaluate(["true", "true"]).Should().Be(true);
    }

    #endregion

    #region Or

    [Fact]
    public void Or_TooFewArgs_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Or)!;

        var act = () => fn.Evaluate(Array.Empty<object?>());

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 1*received 0*");
    }

    [Fact]
    public void Or_AllFalse_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Or)!;

        fn.Evaluate([false, false, false]).Should().Be(false);
    }

    [Fact]
    public void Or_OneTrue_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Or)!;

        fn.Evaluate([false, true, false]).Should().Be(true);
    }

    [Fact]
    public void Or_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Or)!;

        var act = () => fn.Evaluate([false, null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    #endregion

    #region Not

    [Fact]
    public void Not_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Not)!;

        var act = () => fn.Evaluate([true, false]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 1*received 2*");
    }

    [Fact]
    public void Not_True_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Not)!;

        fn.Evaluate([true]).Should().Be(false);
    }

    [Fact]
    public void Not_False_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Not)!;

        fn.Evaluate([false]).Should().Be(true);
    }

    [Fact]
    public void Not_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.Not)!;

        var act = () => fn.Evaluate([null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    #endregion

    #region NOf

    [Fact]
    public void NOf_TooFewArgs_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;

        var act = () => fn.Evaluate([2]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 2*received 1*");
    }

    [Fact]
    public void NOf_ThresholdMet_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;

        // Need 2 true values: true, true, false => 2 >= 2 => true
        fn.Evaluate([2, true, true, false]).Should().Be(true);
    }

    [Fact]
    public void NOf_ThresholdNotMet_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;

        // Need 3 true values: true, true, false => 2 < 3 => false
        fn.Evaluate([3, true, true, false]).Should().Be(false);
    }

    [Fact]
    public void NOf_ZeroThreshold_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;

        // N=0 means always true
        fn.Evaluate([0, false, false]).Should().Be(true);
    }

    [Fact]
    public void NOf_NullBooleanArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.NOf)!;

        var act = () => fn.Evaluate([1, null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    #endregion
}
