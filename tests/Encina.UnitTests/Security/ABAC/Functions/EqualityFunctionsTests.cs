using Encina.Security.ABAC;
using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Functions;

/// <summary>
/// Unit tests for XACML equality functions (string-equal, boolean-equal, integer-equal,
/// double-equal, date-equal, dateTime-equal, time-equal).
/// </summary>
public sealed class EqualityFunctionsTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    private object? Eval(string fnId, params object?[] args) =>
        _registry.GetFunction(fnId)!.Evaluate(args);

    #region string-equal

    [Theory]
    [InlineData("hello", "hello", true)]
    [InlineData("hello", "HELLO", false)]
    [InlineData("", "", true)]
    [InlineData("abc", "xyz", false)]
    public void StringEqual_ReturnsExpected(string a, string b, bool expected)
    {
        Eval(XACMLFunctionIds.StringEqual, a, b).ShouldBe(expected);
    }

    [Fact]
    public void StringEqual_WrongArgCount_Throws()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;
        var act = () => fn.Evaluate(["one"]);
        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region boolean-equal

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    public void BooleanEqual_ReturnsExpected(bool a, bool b, bool expected)
    {
        Eval(XACMLFunctionIds.BooleanEqual, a, b).ShouldBe(expected);
    }

    #endregion

    #region integer-equal

    [Theory]
    [InlineData(42, 42, true)]
    [InlineData(0, 0, true)]
    [InlineData(1, 2, false)]
    [InlineData(-1, 1, false)]
    public void IntegerEqual_ReturnsExpected(int a, int b, bool expected)
    {
        Eval(XACMLFunctionIds.IntegerEqual, a, b).ShouldBe(expected);
    }

    [Fact]
    public void IntegerEqual_StringCoercion_Works()
    {
        Eval(XACMLFunctionIds.IntegerEqual, "42", 42).ShouldBe(true);
    }

    #endregion

    #region double-equal

    [Theory]
    [InlineData(3.14, 3.14, true)]
    [InlineData(0.0, 0.0, true)]
    [InlineData(1.0, 2.0, false)]
    public void DoubleEqual_ReturnsExpected(double a, double b, bool expected)
    {
        Eval(XACMLFunctionIds.DoubleEqual, a, b).ShouldBe(expected);
    }

    #endregion

    #region date-equal

    [Fact]
    public void DateEqual_SameDate_ReturnsTrue()
    {
        var date = new DateOnly(2026, 3, 8);
        Eval(XACMLFunctionIds.DateEqual, date, date).ShouldBe(true);
    }

    [Fact]
    public void DateEqual_DifferentDates_ReturnsFalse()
    {
        var a = new DateOnly(2026, 1, 1);
        var b = new DateOnly(2026, 12, 31);
        Eval(XACMLFunctionIds.DateEqual, a, b).ShouldBe(false);
    }

    #endregion

    #region dateTime-equal

    [Fact]
    public void DateTimeEqual_SameDateTime_ReturnsTrue()
    {
        var dt = new DateTime(2026, 3, 8, 12, 0, 0, DateTimeKind.Utc);
        Eval(XACMLFunctionIds.DateTimeEqual, dt, dt).ShouldBe(true);
    }

    [Fact]
    public void DateTimeEqual_DifferentDateTimes_ReturnsFalse()
    {
        var a = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var b = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc);
        Eval(XACMLFunctionIds.DateTimeEqual, a, b).ShouldBe(false);
    }

    #endregion

    #region time-equal

    [Fact]
    public void TimeEqual_SameTime_ReturnsTrue()
    {
        var t = TimeSpan.FromHours(14);
        Eval(XACMLFunctionIds.TimeEqual, t, t).ShouldBe(true);
    }

    [Fact]
    public void TimeEqual_DifferentTimes_ReturnsFalse()
    {
        var a = TimeSpan.FromHours(9);
        var b = TimeSpan.FromHours(17);
        Eval(XACMLFunctionIds.TimeEqual, a, b).ShouldBe(false);
    }

    #endregion

    #region Return Type

    [Fact]
    public void AllEqualityFunctions_ReturnBoolean()
    {
        var ids = new[]
        {
            XACMLFunctionIds.StringEqual,
            XACMLFunctionIds.BooleanEqual,
            XACMLFunctionIds.IntegerEqual,
            XACMLFunctionIds.DoubleEqual,
            XACMLFunctionIds.DateEqual,
            XACMLFunctionIds.DateTimeEqual,
            XACMLFunctionIds.TimeEqual
        };

        foreach (var id in ids)
        {
            _registry.GetFunction(id)!.ReturnType.ShouldBe(XACMLDataTypes.Boolean,
                $"Function '{id}' should return Boolean");
        }
    }

    #endregion
}
