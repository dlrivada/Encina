using Encina.Security.ABAC;

using Shouldly;

namespace Encina.GuardTests.Security.ABAC.Functions.Standard;

/// <summary>
/// Guard clause tests for equality functions (string, boolean, integer, double, date, dateTime, time).
/// Covers wrong arg count, null args, and correct equality evaluation.
/// </summary>
public class EqualityFunctionsGuardTests
{
    private readonly DefaultFunctionRegistry _registry = new();

    #region StringEqual

    [Fact]
    public void StringEqual_WrongArgCount_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;

        var act = () => fn.Evaluate(["only-one"]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("exactly");
    }

    [Fact]
    public void StringEqual_SameStrings_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;

        fn.Evaluate(["admin", "admin"]).ShouldBe(true);
    }

    [Fact]
    public void StringEqual_DifferentStrings_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;

        fn.Evaluate(["admin", "user"]).ShouldBe(false);
    }

    [Fact]
    public void StringEqual_CaseSensitive_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;

        fn.Evaluate(["Admin", "admin"]).ShouldBe(false);
    }

    [Fact]
    public void StringEqual_NullArgs_CoerceToEmptyString()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.StringEqual)!;

        fn.Evaluate([null, null]).ShouldBe(true);
    }

    #endregion

    #region BooleanEqual

    [Fact]
    public void BooleanEqual_SameValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanEqual)!;

        fn.Evaluate([true, true]).ShouldBe(true);
    }

    [Fact]
    public void BooleanEqual_DifferentValues_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanEqual)!;

        fn.Evaluate([true, false]).ShouldBe(false);
    }

    [Fact]
    public void BooleanEqual_NullArg_ThrowsInvalidOperationException()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.BooleanEqual)!;

        var act = () => fn.Evaluate([null, true]);

        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("must");
    }

    #endregion

    #region IntegerEqual

    [Fact]
    public void IntegerEqual_SameValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerEqual)!;

        fn.Evaluate([42, 42]).ShouldBe(true);
    }

    [Fact]
    public void IntegerEqual_DifferentValues_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerEqual)!;

        fn.Evaluate([42, 43]).ShouldBe(false);
    }

    [Fact]
    public void IntegerEqual_StringCoercion_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.IntegerEqual)!;

        fn.Evaluate(["42", 42]).ShouldBe(true);
    }

    #endregion

    #region DoubleEqual

    [Fact]
    public void DoubleEqual_SameValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleEqual)!;

        fn.Evaluate([3.14, 3.14]).ShouldBe(true);
    }

    [Fact]
    public void DoubleEqual_DifferentValues_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DoubleEqual)!;

        fn.Evaluate([3.14, 2.71]).ShouldBe(false);
    }

    #endregion

    #region DateEqual

    [Fact]
    public void DateEqual_SameDates_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateEqual)!;
        var date = new DateOnly(2026, 4, 3);

        fn.Evaluate([date, date]).ShouldBe(true);
    }

    [Fact]
    public void DateEqual_DifferentDates_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateEqual)!;

        fn.Evaluate([new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)]).ShouldBe(false);
    }

    #endregion

    #region DateTimeEqual

    [Fact]
    public void DateTimeEqual_SameValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.DateTimeEqual)!;
        var dt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);

        fn.Evaluate([dt, dt]).ShouldBe(true);
    }

    #endregion

    #region TimeEqual

    [Fact]
    public void TimeEqual_SameValues_ReturnsTrue()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.TimeEqual)!;
        var ts = TimeSpan.FromHours(14);

        fn.Evaluate([ts, ts]).ShouldBe(true);
    }

    [Fact]
    public void TimeEqual_DifferentValues_ReturnsFalse()
    {
        var fn = _registry.GetFunction(XACMLFunctionIds.TimeEqual)!;

        fn.Evaluate([TimeSpan.FromHours(9), TimeSpan.FromHours(17)]).ShouldBe(false);
    }

    #endregion
}
