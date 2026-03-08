namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Comparison functions (greater-than, less-than, etc.)
/// for integer, double, string, date, dateTime, and time types.
/// </summary>
internal static class ComparisonFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── Integer comparisons ─────────────────────────────────────
        RegisterIntegerComparisons(registry);

        // ── Double comparisons ──────────────────────────────────────
        RegisterDoubleComparisons(registry);

        // ── String comparisons (lexicographic, ordinal) ─────────────
        RegisterStringComparisons(registry);

        // ── Date comparisons ────────────────────────────────────────
        RegisterDateComparisons(registry);

        // ── DateTime comparisons ────────────────────────────────────
        RegisterDateTimeComparisons(registry);

        // ── Time comparisons ────────────────────────────────────────
        RegisterTimeComparisons(registry);
    }

    private static void RegisterIntegerComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.IntegerGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerGreaterThan);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerGreaterThan, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerGreaterThan, 1);
                return a > b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerLessThan);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerLessThan, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerLessThan, 1);
                return a < b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerGreaterThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerGreaterThanOrEqual, 1);
                return a >= b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.IntegerLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerLessThanOrEqual);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerLessThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerLessThanOrEqual, 1);
                return a <= b;
            });
    }

    private static void RegisterDoubleComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.DoubleGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleGreaterThan);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleGreaterThan, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleGreaterThan, 1);
                return a > b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleLessThan);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleLessThan, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleLessThan, 1);
                return a < b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleGreaterThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleGreaterThanOrEqual, 1);
                return a >= b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DoubleLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleLessThanOrEqual);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleLessThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleLessThanOrEqual, 1);
                return a <= b;
            });
    }

    private static void RegisterStringComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.StringGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringGreaterThan);
                var a = FunctionHelpers.CoerceToString(args[0]);
                var b = FunctionHelpers.CoerceToString(args[1]);
                return string.Compare(a, b, StringComparison.Ordinal) > 0;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.StringLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringLessThan);
                var a = FunctionHelpers.CoerceToString(args[0]);
                var b = FunctionHelpers.CoerceToString(args[1]);
                return string.Compare(a, b, StringComparison.Ordinal) < 0;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.StringGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToString(args[0]);
                var b = FunctionHelpers.CoerceToString(args[1]);
                return string.Compare(a, b, StringComparison.Ordinal) >= 0;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.StringLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringLessThanOrEqual);
                var a = FunctionHelpers.CoerceToString(args[0]);
                var b = FunctionHelpers.CoerceToString(args[1]);
                return string.Compare(a, b, StringComparison.Ordinal) <= 0;
            });
    }

    private static void RegisterDateComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.DateGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateGreaterThan);
                var a = FunctionHelpers.CoerceToDate(args[0], XACMLFunctionIds.DateGreaterThan, 0);
                var b = FunctionHelpers.CoerceToDate(args[1], XACMLFunctionIds.DateGreaterThan, 1);
                return a > b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateLessThan);
                var a = FunctionHelpers.CoerceToDate(args[0], XACMLFunctionIds.DateLessThan, 0);
                var b = FunctionHelpers.CoerceToDate(args[1], XACMLFunctionIds.DateLessThan, 1);
                return a < b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToDate(args[0], XACMLFunctionIds.DateGreaterThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDate(args[1], XACMLFunctionIds.DateGreaterThanOrEqual, 1);
                return a >= b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateLessThanOrEqual);
                var a = FunctionHelpers.CoerceToDate(args[0], XACMLFunctionIds.DateLessThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDate(args[1], XACMLFunctionIds.DateLessThanOrEqual, 1);
                return a <= b;
            });
    }

    private static void RegisterDateTimeComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.DateTimeGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateTimeGreaterThan);
                var a = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.DateTimeGreaterThan, 0);
                var b = FunctionHelpers.CoerceToDateTime(args[1], XACMLFunctionIds.DateTimeGreaterThan, 1);
                return a > b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateTimeLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateTimeLessThan);
                var a = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.DateTimeLessThan, 0);
                var b = FunctionHelpers.CoerceToDateTime(args[1], XACMLFunctionIds.DateTimeLessThan, 1);
                return a < b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateTimeGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateTimeGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.DateTimeGreaterThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDateTime(args[1], XACMLFunctionIds.DateTimeGreaterThanOrEqual, 1);
                return a >= b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.DateTimeLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateTimeLessThanOrEqual);
                var a = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.DateTimeLessThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToDateTime(args[1], XACMLFunctionIds.DateTimeLessThanOrEqual, 1);
                return a <= b;
            });
    }

    private static void RegisterTimeComparisons(DefaultFunctionRegistry registry)
    {
        registry.RegisterFunction(
            XACMLFunctionIds.TimeGreaterThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.TimeGreaterThan);
                var a = FunctionHelpers.CoerceToTime(args[0], XACMLFunctionIds.TimeGreaterThan, 0);
                var b = FunctionHelpers.CoerceToTime(args[1], XACMLFunctionIds.TimeGreaterThan, 1);
                return a > b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.TimeLessThan,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.TimeLessThan);
                var a = FunctionHelpers.CoerceToTime(args[0], XACMLFunctionIds.TimeLessThan, 0);
                var b = FunctionHelpers.CoerceToTime(args[1], XACMLFunctionIds.TimeLessThan, 1);
                return a < b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.TimeGreaterThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.TimeGreaterThanOrEqual);
                var a = FunctionHelpers.CoerceToTime(args[0], XACMLFunctionIds.TimeGreaterThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToTime(args[1], XACMLFunctionIds.TimeGreaterThanOrEqual, 1);
                return a >= b;
            });

        registry.RegisterFunction(
            XACMLFunctionIds.TimeLessThanOrEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.TimeLessThanOrEqual);
                var a = FunctionHelpers.CoerceToTime(args[0], XACMLFunctionIds.TimeLessThanOrEqual, 0);
                var b = FunctionHelpers.CoerceToTime(args[1], XACMLFunctionIds.TimeLessThanOrEqual, 1);
                return a <= b;
            });
    }
}
