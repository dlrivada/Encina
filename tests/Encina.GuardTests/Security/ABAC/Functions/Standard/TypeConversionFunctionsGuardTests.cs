using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for type conversion functions (string-from-integer, integer-from-string,
/// double-from-string, boolean-from-string, string-from-boolean, string-from-double, string-from-dateTime).
/// Covers wrong arg count, null args, unparseable strings, and correct conversions.
/// </summary>
public class TypeConversionFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region StringFromInteger

    [Fact]
    public void StringFromInteger_ValidInt_ReturnsString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromInteger)!;

        fn.Evaluate([42]).Should().Be("42");
    }

    [Fact]
    public void StringFromInteger_NegativeInt_ReturnsString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromInteger)!;

        fn.Evaluate([-100]).Should().Be("-100");
    }

    [Fact]
    public void StringFromInteger_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromInteger)!;

        var act = () => fn.Evaluate([1, 2]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 1*received 2*");
    }

    #endregion

    #region IntegerFromString

    [Fact]
    public void IntegerFromString_ValidString_ReturnsInt()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;

        fn.Evaluate(["42"]).Should().Be(42);
    }

    [Fact]
    public void IntegerFromString_InvalidString_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;

        var act = () => fn.Evaluate(["not-a-number"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot parse*integer*");
    }

    [Fact]
    public void IntegerFromString_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;

        var act = () => fn.Evaluate([null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    #endregion

    #region DoubleFromString

    [Fact]
    public void DoubleFromString_ValidString_ReturnsDouble()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleFromString)!;

        fn.Evaluate(["3.14"]).Should().Be(3.14);
    }

    [Fact]
    public void DoubleFromString_InvalidString_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleFromString)!;

        var act = () => fn.Evaluate(["abc"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot parse*double*");
    }

    #endregion

    #region BooleanFromString

    [Fact]
    public void BooleanFromString_TrueString_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanFromString)!;

        fn.Evaluate(["true"]).Should().Be(true);
    }

    [Fact]
    public void BooleanFromString_FalseString_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanFromString)!;

        fn.Evaluate(["false"]).Should().Be(false);
    }

    [Fact]
    public void BooleanFromString_InvalidString_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanFromString)!;

        var act = () => fn.Evaluate(["yes"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*cannot parse*boolean*");
    }

    #endregion

    #region StringFromBoolean

    [Fact]
    public void StringFromBoolean_True_ReturnsLowercaseTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromBoolean)!;

        fn.Evaluate([true]).Should().Be("true");
    }

    [Fact]
    public void StringFromBoolean_False_ReturnsLowercaseFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromBoolean)!;

        fn.Evaluate([false]).Should().Be("false");
    }

    #endregion

    #region StringFromDouble

    [Fact]
    public void StringFromDouble_ValidDouble_ReturnsString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromDouble)!;

        fn.Evaluate([3.14]).Should().Be("3.14");
    }

    #endregion

    #region StringFromDateTime

    [Fact]
    public void StringFromDateTime_ValidDateTime_ReturnsIso8601String()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromDateTime)!;
        var dt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);

        var result = fn.Evaluate([dt]) as string;

        result.Should().NotBeNull();
        result.Should().Contain("2026-04-03");
    }

    [Fact]
    public void StringFromDateTime_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringFromDateTime)!;

        var act = () => fn.Evaluate([null]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    #endregion
}
