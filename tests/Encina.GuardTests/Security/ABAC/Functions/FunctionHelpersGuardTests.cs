using Encina.Security.ABAC;

using FluentAssertions;

namespace Encina.GuardTests.Security.ABAC.Functions;

/// <summary>
/// Guard clause tests for <see cref="FunctionHelpers"/>.
/// Covers argument validation, type coercion (string, bool, int, double, date, time, bag),
/// null handling, and invalid type errors for all Coerce* methods.
/// </summary>
public class FunctionHelpersGuardTests
{
    private const string TestFn = "test-function";

    #region ValidateArgCount

    [Fact]
    public void ValidateArgCount_CorrectCount_DoesNotThrow()
    {
        var args = new object?[] { "a", "b" };

        var act = () => FunctionHelpers.ValidateArgCount(args, 2, TestFn);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateArgCount_TooFewArgs_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a" };

        var act = () => FunctionHelpers.ValidateArgCount(args, 2, TestFn);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*exactly 2*received 1*");
    }

    [Fact]
    public void ValidateArgCount_TooManyArgs_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a", "b", "c" };

        var act = () => FunctionHelpers.ValidateArgCount(args, 2, TestFn);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*exactly 2*received 3*");
    }

    #endregion

    #region ValidateMinArgCount

    [Fact]
    public void ValidateMinArgCount_ExactMinimum_DoesNotThrow()
    {
        var args = new object?[] { "a", "b" };

        var act = () => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateMinArgCount_AboveMinimum_DoesNotThrow()
    {
        var args = new object?[] { "a", "b", "c" };

        var act = () => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateMinArgCount_BelowMinimum_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a" };

        var act = () => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*at least 2*received 1*");
    }

    #endregion

    #region CoerceToString

    [Fact]
    public void CoerceToString_NullArg_ReturnsEmptyString()
    {
        FunctionHelpers.CoerceToString(null).Should().Be(string.Empty);
    }

    [Fact]
    public void CoerceToString_StringArg_ReturnsSameString()
    {
        FunctionHelpers.CoerceToString("hello").Should().Be("hello");
    }

    [Fact]
    public void CoerceToString_IntArg_ReturnsStringRepresentation()
    {
        FunctionHelpers.CoerceToString(42).Should().Be("42");
    }

    #endregion

    #region CoerceToStringStrict

    [Fact]
    public void CoerceToStringStrict_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToStringStrict(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToStringStrict_StringArg_ReturnsSameString()
    {
        FunctionHelpers.CoerceToStringStrict("hello", TestFn, 0).Should().Be("hello");
    }

    [Fact]
    public void CoerceToStringStrict_NonStringArg_ReturnsToStringResult()
    {
        FunctionHelpers.CoerceToStringStrict(42, TestFn, 0).Should().Be("42");
    }

    #endregion

    #region CoerceToBool

    [Fact]
    public void CoerceToBool_TrueBool_ReturnsTrue()
    {
        FunctionHelpers.CoerceToBool(true, TestFn, 0).Should().BeTrue();
    }

    [Fact]
    public void CoerceToBool_FalseBool_ReturnsFalse()
    {
        FunctionHelpers.CoerceToBool(false, TestFn, 0).Should().BeFalse();
    }

    [Fact]
    public void CoerceToBool_StringTrue_ReturnsTrue()
    {
        FunctionHelpers.CoerceToBool("true", TestFn, 0).Should().BeTrue();
    }

    [Fact]
    public void CoerceToBool_StringFalse_ReturnsFalse()
    {
        FunctionHelpers.CoerceToBool("false", TestFn, 0).Should().BeFalse();
    }

    [Fact]
    public void CoerceToBool_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToBool(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToBool_InvalidType_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToBool(42, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*cannot convert*Boolean*");
    }

    [Fact]
    public void CoerceToBool_InvalidString_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToBool("notabool", TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*cannot convert*Boolean*");
    }

    #endregion

    #region CoerceToInt

    [Fact]
    public void CoerceToInt_IntArg_ReturnsSameInt()
    {
        FunctionHelpers.CoerceToInt(42, TestFn, 0).Should().Be(42);
    }

    [Fact]
    public void CoerceToInt_LongArg_ConvertsToInt()
    {
        FunctionHelpers.CoerceToInt(42L, TestFn, 0).Should().Be(42);
    }

    [Fact]
    public void CoerceToInt_DoubleArg_ConvertsToInt()
    {
        FunctionHelpers.CoerceToInt(42.0, TestFn, 0).Should().Be(42);
    }

    [Fact]
    public void CoerceToInt_StringArg_ParsesInt()
    {
        FunctionHelpers.CoerceToInt("42", TestFn, 0).Should().Be(42);
    }

    [Fact]
    public void CoerceToInt_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToInt(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToInt_InvalidString_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToInt("abc", TestFn, 0);

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region CoerceToDouble

    [Fact]
    public void CoerceToDouble_DoubleArg_ReturnsSameDouble()
    {
        FunctionHelpers.CoerceToDouble(3.14, TestFn, 0).Should().Be(3.14);
    }

    [Fact]
    public void CoerceToDouble_IntArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(42, TestFn, 0).Should().Be(42.0);
    }

    [Fact]
    public void CoerceToDouble_LongArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(42L, TestFn, 0).Should().Be(42.0);
    }

    [Fact]
    public void CoerceToDouble_FloatArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(3.14f, TestFn, 0).Should().BeApproximately(3.14, 0.01);
    }

    [Fact]
    public void CoerceToDouble_StringArg_ParsesDouble()
    {
        FunctionHelpers.CoerceToDouble("3.14", TestFn, 0).Should().Be(3.14);
    }

    [Fact]
    public void CoerceToDouble_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToDouble(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    #endregion

    #region CoerceToDateTime

    [Fact]
    public void CoerceToDateTime_DateTimeArg_ReturnsSame()
    {
        var dt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        FunctionHelpers.CoerceToDateTime(dt, TestFn, 0).Should().Be(dt);
    }

    [Fact]
    public void CoerceToDateTime_DateTimeOffsetArg_ReturnsUtcDateTime()
    {
        var dto = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        FunctionHelpers.CoerceToDateTime(dto, TestFn, 0).Should().Be(dto.UtcDateTime);
    }

    [Fact]
    public void CoerceToDateTime_StringArg_Parses()
    {
        var result = FunctionHelpers.CoerceToDateTime("2026-04-03T12:00:00Z", TestFn, 0);
        result.Should().Be(new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CoerceToDateTime_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToDateTime(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToDateTime_InvalidType_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToDateTime(42, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*cannot convert*DateTime*");
    }

    #endregion

    #region CoerceToTime

    [Fact]
    public void CoerceToTime_TimeSpanArg_ReturnsSame()
    {
        var ts = TimeSpan.FromHours(14);
        FunctionHelpers.CoerceToTime(ts, TestFn, 0).Should().Be(ts);
    }

    [Fact]
    public void CoerceToTime_DateTimeArg_ReturnsTimeOfDay()
    {
        var dt = new DateTime(2026, 4, 3, 14, 30, 0);
        FunctionHelpers.CoerceToTime(dt, TestFn, 0).Should().Be(dt.TimeOfDay);
    }

    [Fact]
    public void CoerceToTime_StringArg_Parses()
    {
        FunctionHelpers.CoerceToTime("14:30:00", TestFn, 0).Should().Be(new TimeSpan(14, 30, 0));
    }

    [Fact]
    public void CoerceToTime_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToTime(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToTime_InvalidType_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToTime(42, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*cannot convert*Time*");
    }

    #endregion

    #region CoerceToDate

    [Fact]
    public void CoerceToDate_DateOnlyArg_ReturnsSame()
    {
        var d = new DateOnly(2026, 4, 3);
        FunctionHelpers.CoerceToDate(d, TestFn, 0).Should().Be(d);
    }

    [Fact]
    public void CoerceToDate_DateTimeArg_ReturnsDateOnly()
    {
        var dt = new DateTime(2026, 4, 3, 12, 0, 0);
        FunctionHelpers.CoerceToDate(dt, TestFn, 0).Should().Be(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_DateTimeOffsetArg_ReturnsDateOnly()
    {
        var dto = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        FunctionHelpers.CoerceToDate(dto, TestFn, 0).Should().Be(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_StringArg_Parses()
    {
        FunctionHelpers.CoerceToDate("2026-04-03", TestFn, 0).Should().Be(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToDate(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*");
    }

    [Fact]
    public void CoerceToDate_InvalidType_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToDate(42, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*cannot convert*Date*");
    }

    #endregion

    #region CoerceToBag

    [Fact]
    public void CoerceToBag_ValidBag_ReturnsSame()
    {
        var bag = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" });

        FunctionHelpers.CoerceToBag(bag, TestFn, 0).Should().BeSameAs(bag);
    }

    [Fact]
    public void CoerceToBag_NullArg_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToBag(null, TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*must not be null*expected AttributeBag*");
    }

    [Fact]
    public void CoerceToBag_NonBagType_ThrowsInvalidOperationException()
    {
        var act = () => FunctionHelpers.CoerceToBag("not a bag", TestFn, 0);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{TestFn}*argument 0*expected AttributeBag*String*");
    }

    #endregion
}
