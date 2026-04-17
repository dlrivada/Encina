using Encina.Security.ABAC;

using Shouldly;

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

        var act = (Action)(() => FunctionHelpers.ValidateArgCount(args, 2, TestFn));

        Should.NotThrow(act);
    }

    [Fact]
    public void ValidateArgCount_TooFewArgs_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a" };

        var act = (Action)(() => FunctionHelpers.ValidateArgCount(args, 2, TestFn));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"exactly 2 argument\(s\).*received 1");
    }

    [Fact]
    public void ValidateArgCount_TooManyArgs_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a", "b", "c" };

        var act = (Action)(() => FunctionHelpers.ValidateArgCount(args, 2, TestFn));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"exactly 2 argument\(s\).*received 3");
    }

    #endregion

    #region ValidateMinArgCount

    [Fact]
    public void ValidateMinArgCount_ExactMinimum_DoesNotThrow()
    {
        var args = new object?[] { "a", "b" };

        var act = (Action)(() => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn));

        Should.NotThrow(act);
    }

    [Fact]
    public void ValidateMinArgCount_AboveMinimum_DoesNotThrow()
    {
        var args = new object?[] { "a", "b", "c" };

        var act = (Action)(() => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn));

        Should.NotThrow(act);
    }

    [Fact]
    public void ValidateMinArgCount_BelowMinimum_ThrowsInvalidOperationException()
    {
        var args = new object?[] { "a" };

        var act = (Action)(() => FunctionHelpers.ValidateMinArgCount(args, 2, TestFn));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"at least 2 argument\(s\).*received 1");
    }

    #endregion

    #region CoerceToString

    [Fact]
    public void CoerceToString_NullArg_ReturnsEmptyString()
    {
        FunctionHelpers.CoerceToString(null).ShouldBe(string.Empty);
    }

    [Fact]
    public void CoerceToString_StringArg_ReturnsSameString()
    {
        FunctionHelpers.CoerceToString("hello").ShouldBe("hello");
    }

    [Fact]
    public void CoerceToString_IntArg_ReturnsStringRepresentation()
    {
        FunctionHelpers.CoerceToString(42).ShouldBe("42");
    }

    #endregion

    #region CoerceToStringStrict

    [Fact]
    public void CoerceToStringStrict_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToStringStrict(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToStringStrict_StringArg_ReturnsSameString()
    {
        FunctionHelpers.CoerceToStringStrict("hello", TestFn, 0).ShouldBe("hello");
    }

    [Fact]
    public void CoerceToStringStrict_NonStringArg_ReturnsToStringResult()
    {
        FunctionHelpers.CoerceToStringStrict(42, TestFn, 0).ShouldBe("42");
    }

    #endregion

    #region CoerceToBool

    [Fact]
    public void CoerceToBool_TrueBool_ReturnsTrue()
    {
        FunctionHelpers.CoerceToBool(true, TestFn, 0).ShouldBeTrue();
    }

    [Fact]
    public void CoerceToBool_FalseBool_ReturnsFalse()
    {
        FunctionHelpers.CoerceToBool(false, TestFn, 0).ShouldBeFalse();
    }

    [Fact]
    public void CoerceToBool_StringTrue_ReturnsTrue()
    {
        FunctionHelpers.CoerceToBool("true", TestFn, 0).ShouldBeTrue();
    }

    [Fact]
    public void CoerceToBool_StringFalse_ReturnsFalse()
    {
        FunctionHelpers.CoerceToBool("false", TestFn, 0).ShouldBeFalse();
    }

    [Fact]
    public void CoerceToBool_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToBool(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToBool_InvalidType_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToBool(42, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*cannot convert.*Boolean");
    }

    [Fact]
    public void CoerceToBool_InvalidString_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToBool("notabool", TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*cannot convert.*Boolean");
    }

    #endregion

    #region CoerceToInt

    [Fact]
    public void CoerceToInt_IntArg_ReturnsSameInt()
    {
        FunctionHelpers.CoerceToInt(42, TestFn, 0).ShouldBe(42);
    }

    [Fact]
    public void CoerceToInt_LongArg_ConvertsToInt()
    {
        FunctionHelpers.CoerceToInt(42L, TestFn, 0).ShouldBe(42);
    }

    [Fact]
    public void CoerceToInt_DoubleArg_ConvertsToInt()
    {
        FunctionHelpers.CoerceToInt(42.0, TestFn, 0).ShouldBe(42);
    }

    [Fact]
    public void CoerceToInt_StringArg_ParsesInt()
    {
        FunctionHelpers.CoerceToInt("42", TestFn, 0).ShouldBe(42);
    }

    [Fact]
    public void CoerceToInt_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToInt(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToInt_InvalidString_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToInt("abc", TestFn, 0));

        Should.Throw<InvalidOperationException>(act);
    }

    #endregion

    #region CoerceToDouble

    [Fact]
    public void CoerceToDouble_DoubleArg_ReturnsSameDouble()
    {
        FunctionHelpers.CoerceToDouble(3.14, TestFn, 0).ShouldBe(3.14);
    }

    [Fact]
    public void CoerceToDouble_IntArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(42, TestFn, 0).ShouldBe(42.0);
    }

    [Fact]
    public void CoerceToDouble_LongArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(42L, TestFn, 0).ShouldBe(42.0);
    }

    [Fact]
    public void CoerceToDouble_FloatArg_ConvertsToDouble()
    {
        FunctionHelpers.CoerceToDouble(3.14f, TestFn, 0).ShouldBeInRange(3.14 - 0.01, 3.14 + 0.01);
    }

    [Fact]
    public void CoerceToDouble_StringArg_ParsesDouble()
    {
        FunctionHelpers.CoerceToDouble("3.14", TestFn, 0).ShouldBe(3.14);
    }

    [Fact]
    public void CoerceToDouble_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToDouble(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    #endregion

    #region CoerceToDateTime

    [Fact]
    public void CoerceToDateTime_DateTimeArg_ReturnsSame()
    {
        var dt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        FunctionHelpers.CoerceToDateTime(dt, TestFn, 0).ShouldBe(dt);
    }

    [Fact]
    public void CoerceToDateTime_DateTimeOffsetArg_ReturnsUtcDateTime()
    {
        var dto = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        FunctionHelpers.CoerceToDateTime(dto, TestFn, 0).ShouldBe(dto.UtcDateTime);
    }

    [Fact]
    public void CoerceToDateTime_StringArg_Parses()
    {
        var result = FunctionHelpers.CoerceToDateTime("2026-04-03T12:00:00Z", TestFn, 0);
        result.ShouldBe(new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void CoerceToDateTime_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToDateTime(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToDateTime_InvalidType_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToDateTime(42, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*cannot convert.*DateTime");
    }

    #endregion

    #region CoerceToTime

    [Fact]
    public void CoerceToTime_TimeSpanArg_ReturnsSame()
    {
        var ts = TimeSpan.FromHours(14);
        FunctionHelpers.CoerceToTime(ts, TestFn, 0).ShouldBe(ts);
    }

    [Fact]
    public void CoerceToTime_DateTimeArg_ReturnsTimeOfDay()
    {
        var dt = new DateTime(2026, 4, 3, 14, 30, 0);
        FunctionHelpers.CoerceToTime(dt, TestFn, 0).ShouldBe(dt.TimeOfDay);
    }

    [Fact]
    public void CoerceToTime_StringArg_Parses()
    {
        FunctionHelpers.CoerceToTime("14:30:00", TestFn, 0).ShouldBe(new TimeSpan(14, 30, 0));
    }

    [Fact]
    public void CoerceToTime_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToTime(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToTime_InvalidType_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToTime(42, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*cannot convert.*Time");
    }

    #endregion

    #region CoerceToDate

    [Fact]
    public void CoerceToDate_DateOnlyArg_ReturnsSame()
    {
        var d = new DateOnly(2026, 4, 3);
        FunctionHelpers.CoerceToDate(d, TestFn, 0).ShouldBe(d);
    }

    [Fact]
    public void CoerceToDate_DateTimeArg_ReturnsDateOnly()
    {
        var dt = new DateTime(2026, 4, 3, 12, 0, 0);
        FunctionHelpers.CoerceToDate(dt, TestFn, 0).ShouldBe(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_DateTimeOffsetArg_ReturnsDateOnly()
    {
        var dto = new DateTimeOffset(2026, 4, 3, 12, 0, 0, TimeSpan.Zero);
        FunctionHelpers.CoerceToDate(dto, TestFn, 0).ShouldBe(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_StringArg_Parses()
    {
        FunctionHelpers.CoerceToDate("2026-04-03", TestFn, 0).ShouldBe(new DateOnly(2026, 4, 3));
    }

    [Fact]
    public void CoerceToDate_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToDate(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("argument 0 must not be null");
    }

    [Fact]
    public void CoerceToDate_InvalidType_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToDate(42, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*cannot convert.*Date");
    }

    #endregion

    #region CoerceToBag

    [Fact]
    public void CoerceToBag_ValidBag_ReturnsSame()
    {
        var bag = AttributeBag.Of(new AttributeValue { DataType = XACMLDataTypes.String, Value = "x" });

        FunctionHelpers.CoerceToBag(bag, TestFn, 0).ShouldBeSameAs(bag);
    }

    [Fact]
    public void CoerceToBag_NullArg_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToBag(null, TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0 must not be null.*AttributeBag");
    }

    [Fact]
    public void CoerceToBag_NonBagType_ThrowsInvalidOperationException()
    {
        var act = (Action)(() => FunctionHelpers.CoerceToBag("not a bag", TestFn, 0));

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"argument 0.*expected AttributeBag");
    }

    #endregion
}
