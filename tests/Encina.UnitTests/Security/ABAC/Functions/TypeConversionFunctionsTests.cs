using System.Globalization;
using Encina.Security.ABAC;
using FluentAssertions;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML type conversion functions:
/// string-from-integer, integer-from-string, double-from-string,
/// boolean-from-string, string-from-boolean, string-from-double, string-from-dateTime.
/// </summary>
public sealed class TypeConversionFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region string-from-integer

    [Fact]
    public void StringFromInteger_PositiveNumber_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromInteger, 42).Should().Be("42");
    }

    [Fact]
    public void StringFromInteger_NegativeNumber_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromInteger, -100).Should().Be("-100");
    }

    [Fact]
    public void StringFromInteger_Zero_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromInteger, 0).Should().Be("0");
    }

    [Fact]
    public void StringFromInteger_MaxValue_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromInteger, int.MaxValue)
            .Should().Be(int.MaxValue.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void StringFromInteger_MinValue_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromInteger, int.MinValue)
            .Should().Be(int.MinValue.ToString(CultureInfo.InvariantCulture));
    }

    #endregion

    #region integer-from-string

    [Fact]
    public void IntegerFromString_ValidPositive_ReturnsInt()
    {
        Eval(XACMLFunctionIds.IntegerFromString, "42").Should().Be(42);
    }

    [Fact]
    public void IntegerFromString_ValidNegative_ReturnsInt()
    {
        Eval(XACMLFunctionIds.IntegerFromString, "-100").Should().Be(-100);
    }

    [Fact]
    public void IntegerFromString_Zero_ReturnsInt()
    {
        Eval(XACMLFunctionIds.IntegerFromString, "0").Should().Be(0);
    }

    [Fact]
    public void IntegerFromString_NonNumeric_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;
        var act = () => fn.Evaluate(["abc"]);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot parse*");
    }

    [Fact]
    public void IntegerFromString_DecimalNumber_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;
        var act = () => fn.Evaluate(["3.14"]);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IntegerFromString_EmptyString_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerFromString)!;
        var act = () => fn.Evaluate([""]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region double-from-string

    [Fact]
    public void DoubleFromString_ValidDecimal_ReturnsDouble()
    {
        Eval(XACMLFunctionIds.DoubleFromString, "3.14").Should().Be(3.14);
    }

    [Fact]
    public void DoubleFromString_ValidInteger_ReturnsDouble()
    {
        Eval(XACMLFunctionIds.DoubleFromString, "42").Should().Be(42.0);
    }

    [Fact]
    public void DoubleFromString_ScientificNotation_ReturnsDouble()
    {
        Eval(XACMLFunctionIds.DoubleFromString, "1.5E2").Should().Be(150.0);
    }

    [Fact]
    public void DoubleFromString_Negative_ReturnsDouble()
    {
        Eval(XACMLFunctionIds.DoubleFromString, "-99.5").Should().Be(-99.5);
    }

    [Fact]
    public void DoubleFromString_NonNumeric_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleFromString)!;
        var act = () => fn.Evaluate(["not-a-number"]);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot parse*");
    }

    #endregion

    #region boolean-from-string

    [Fact]
    public void BooleanFromString_True_ReturnsTrue()
    {
        Eval(XACMLFunctionIds.BooleanFromString, "True").Should().Be(true);
    }

    [Fact]
    public void BooleanFromString_False_ReturnsFalse()
    {
        Eval(XACMLFunctionIds.BooleanFromString, "False").Should().Be(false);
    }

    [Fact]
    public void BooleanFromString_CaseInsensitive_Works()
    {
        Eval(XACMLFunctionIds.BooleanFromString, "true").Should().Be(true);
        Eval(XACMLFunctionIds.BooleanFromString, "TRUE").Should().Be(true);
        Eval(XACMLFunctionIds.BooleanFromString, "false").Should().Be(false);
    }

    [Fact]
    public void BooleanFromString_InvalidValue_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanFromString)!;
        var act = () => fn.Evaluate(["yes"]);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot parse*");
    }

    [Fact]
    public void BooleanFromString_NumericValue_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanFromString)!;
        var act = () => fn.Evaluate(["1"]);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region string-from-boolean

    [Fact]
    public void StringFromBoolean_True_ReturnsLowercase()
    {
        Eval(XACMLFunctionIds.StringFromBoolean, true).Should().Be("true");
    }

    [Fact]
    public void StringFromBoolean_False_ReturnsLowercase()
    {
        Eval(XACMLFunctionIds.StringFromBoolean, false).Should().Be("false");
    }

    #endregion

    #region string-from-double

    [Fact]
    public void StringFromDouble_SimpleDecimal_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromDouble, 3.14).Should().Be("3.14");
    }

    [Fact]
    public void StringFromDouble_WholeNumber_ReturnsString()
    {
        var result = (string)Eval(XACMLFunctionIds.StringFromDouble, 42.0)!;
        // InvariantCulture may return "42" or "42.0" depending on double.ToString behavior
        double.Parse(result, CultureInfo.InvariantCulture).Should().Be(42.0);
    }

    [Fact]
    public void StringFromDouble_Negative_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromDouble, -1.5).Should().Be("-1.5");
    }

    [Fact]
    public void StringFromDouble_Zero_ReturnsString()
    {
        Eval(XACMLFunctionIds.StringFromDouble, 0.0).Should().Be("0");
    }

    #endregion

    #region string-from-dateTime

    [Fact]
    public void StringFromDateTime_ReturnsISO8601()
    {
        var dt = new DateTime(2026, 3, 8, 14, 30, 0, DateTimeKind.Utc);

        var result = (string)Eval(XACMLFunctionIds.StringFromDateTime, dt)!;

        result.Should().Contain("2026-03-08");
        result.Should().Contain("14:30:00");
    }

    [Fact]
    public void StringFromDateTime_UsesOFormat()
    {
        var dt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = (string)Eval(XACMLFunctionIds.StringFromDateTime, dt)!;

        // "o" format produces roundtrippable ISO 8601
        var parsed = DateTime.ParseExact(result, "o", CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
        parsed.Should().Be(dt);
    }

    #endregion

    #region Wrong Arg Count

    [Fact]
    public void AllConversionFunctions_WrongArgCount_Throws()
    {
        var functions = new[]
        {
            XACMLFunctionIds.StringFromInteger,
            XACMLFunctionIds.IntegerFromString,
            XACMLFunctionIds.DoubleFromString,
            XACMLFunctionIds.BooleanFromString,
            XACMLFunctionIds.StringFromBoolean,
            XACMLFunctionIds.StringFromDouble,
            XACMLFunctionIds.StringFromDateTime
        };

        foreach (var fnId in functions)
        {
            var fn = _registry.GetFunction(fnId)!;
            var act = () => fn.Evaluate([]);
            act.Should().Throw<InvalidOperationException>(
                $"Function '{fnId}' should throw when given no arguments");
        }
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void IntegerRoundtrip_StringFromInteger_Then_IntegerFromString()
    {
        var original = 12345;
        var asString = (string)Eval(XACMLFunctionIds.StringFromInteger, original)!;
        var backToInt = (int)Eval(XACMLFunctionIds.IntegerFromString, asString)!;

        backToInt.Should().Be(original);
    }

    [Fact]
    public void BooleanRoundtrip_StringFromBoolean_Then_BooleanFromString()
    {
        var original = true;
        var asString = (string)Eval(XACMLFunctionIds.StringFromBoolean, original)!;
        var backToBool = (bool)Eval(XACMLFunctionIds.BooleanFromString, asString)!;

        backToBool.Should().Be(original);
    }

    [Fact]
    public void DoubleRoundtrip_StringFromDouble_Then_DoubleFromString()
    {
        var original = 3.14;
        var asString = (string)Eval(XACMLFunctionIds.StringFromDouble, original)!;
        var backToDouble = (double)Eval(XACMLFunctionIds.DoubleFromString, asString)!;

        backToDouble.Should().Be(original);
    }

    #endregion
}
