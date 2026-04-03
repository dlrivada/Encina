using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for comparison functions (integer, double, string, date, dateTime, time).
/// Covers wrong arg count, null args, type mismatch, and correct evaluation for each comparison variant.
/// </summary>
public class ComparisonFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region Integer Comparisons

    [Fact]
    public void IntegerGreaterThan_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerGreaterThan)!;

        var act = () => fn.Evaluate([10]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 2*received 1*");
    }

    [Fact]
    public void IntegerGreaterThan_NullFirstArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerGreaterThan)!;

        var act = () => fn.Evaluate([null, 5]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(5, 10, false)]
    [InlineData(5, 5, false)]
    public void IntegerGreaterThan_ValidArgs_ReturnsExpected(int a, int b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerGreaterThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 10, true)]
    [InlineData(10, 3, false)]
    [InlineData(5, 5, false)]
    public void IntegerLessThan_ValidArgs_ReturnsExpected(int a, int b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerLessThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(3, 5, false)]
    public void IntegerGreaterThanOrEqual_ValidArgs_ReturnsExpected(int a, int b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerGreaterThanOrEqual)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(3, 5, true)]
    [InlineData(5, 5, true)]
    [InlineData(10, 5, false)]
    public void IntegerLessThanOrEqual_ValidArgs_ReturnsExpected(int a, int b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerLessThanOrEqual)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    #endregion

    #region Double Comparisons

    [Fact]
    public void DoubleGreaterThan_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleGreaterThan)!;

        var act = () => fn.Evaluate([null, 5.0]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Theory]
    [InlineData(3.14, 2.71, true)]
    [InlineData(2.71, 3.14, false)]
    [InlineData(3.14, 3.14, false)]
    public void DoubleGreaterThan_ValidArgs_ReturnsExpected(double a, double b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleGreaterThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(2.0, 3.0, true)]
    [InlineData(3.0, 2.0, false)]
    public void DoubleLessThan_ValidArgs_ReturnsExpected(double a, double b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleLessThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Fact]
    public void DoubleGreaterThanOrEqual_EqualValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleGreaterThanOrEqual)!;

        var result = fn.Evaluate([5.0, 5.0]);

        result.Should().Be(true);
    }

    [Fact]
    public void DoubleLessThanOrEqual_EqualValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleLessThanOrEqual)!;

        var result = fn.Evaluate([5.0, 5.0]);

        result.Should().Be(true);
    }

    #endregion

    #region String Comparisons (Ordinal)

    [Fact]
    public void StringGreaterThan_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringGreaterThan)!;

        var act = () => fn.Evaluate(["a", "b", "c"]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exactly 2*received 3*");
    }

    [Theory]
    [InlineData("banana", "apple", true)]
    [InlineData("apple", "banana", false)]
    [InlineData("apple", "apple", false)]
    public void StringGreaterThan_ValidArgs_ReturnsExpected(string a, string b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringGreaterThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("apple", "banana", true)]
    [InlineData("banana", "apple", false)]
    public void StringLessThan_ValidArgs_ReturnsExpected(string a, string b, bool expected)
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringLessThan)!;

        var result = fn.Evaluate([a, b]);

        result.Should().Be(expected);
    }

    [Fact]
    public void StringGreaterThanOrEqual_EqualStrings_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringGreaterThanOrEqual)!;

        var result = fn.Evaluate(["hello", "hello"]);

        result.Should().Be(true);
    }

    [Fact]
    public void StringLessThanOrEqual_EqualStrings_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringLessThanOrEqual)!;

        var result = fn.Evaluate(["hello", "hello"]);

        result.Should().Be(true);
    }

    #endregion

    #region Date Comparisons

    [Fact]
    public void DateGreaterThan_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateGreaterThan)!;

        var act = () => fn.Evaluate([null, new DateOnly(2026, 1, 1)]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void DateGreaterThan_LaterDate_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateGreaterThan)!;

        var result = fn.Evaluate([new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1)]);

        result.Should().Be(true);
    }

    [Fact]
    public void DateLessThan_EarlierDate_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateLessThan)!;

        var result = fn.Evaluate([new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)]);

        result.Should().Be(true);
    }

    #endregion

    #region DateTime Comparisons

    [Fact]
    public void DateTimeGreaterThan_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateTimeGreaterThan)!;

        var act = () => fn.Evaluate([null, DateTime.UtcNow]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void DateTimeGreaterThan_LaterDateTime_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateTimeGreaterThan)!;
        var later = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var earlier = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = fn.Evaluate([later, earlier]);

        result.Should().Be(true);
    }

    #endregion

    #region Time Comparisons

    [Fact]
    public void TimeGreaterThan_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.TimeGreaterThan)!;

        var act = () => fn.Evaluate([null, TimeSpan.FromHours(12)]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*must not be null*");
    }

    [Fact]
    public void TimeGreaterThan_LaterTime_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.TimeGreaterThan)!;

        var result = fn.Evaluate([TimeSpan.FromHours(18), TimeSpan.FromHours(9)]);

        result.Should().Be(true);
    }

    [Fact]
    public void TimeLessThan_EarlierTime_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.TimeLessThan)!;

        var result = fn.Evaluate([TimeSpan.FromHours(9), TimeSpan.FromHours(18)]);

        result.Should().Be(true);
    }

    #endregion
}
