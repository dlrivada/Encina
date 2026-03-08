using System.Globalization;

namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Equality functions for all standard data types.
/// </summary>
internal static class EqualityFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── String equality ─────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringEqual);
                var a = FunctionHelpers.CoerceToString(args[0]);
                var b = FunctionHelpers.CoerceToString(args[1]);
                return string.Equals(a, b, StringComparison.Ordinal);
            });

        // ── Boolean equality ────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.BooleanEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.BooleanEqual);
                var a = FunctionHelpers.CoerceToBool(args[0], XACMLFunctionIds.BooleanEqual, 0);
                var b = FunctionHelpers.CoerceToBool(args[1], XACMLFunctionIds.BooleanEqual, 1);
                return a == b;
            });

        // ── Integer equality ────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.IntegerEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.IntegerEqual);
                var a = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.IntegerEqual, 0);
                var b = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.IntegerEqual, 1);
                return a == b;
            });

        // ── Double equality ─────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.DoubleEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DoubleEqual);
                var a = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.DoubleEqual, 0);
                var b = FunctionHelpers.CoerceToDouble(args[1], XACMLFunctionIds.DoubleEqual, 1);
                return a.Equals(b);
            });

        // ── Date equality ───────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.DateEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateEqual);
                var a = FunctionHelpers.CoerceToDate(args[0], XACMLFunctionIds.DateEqual, 0);
                var b = FunctionHelpers.CoerceToDate(args[1], XACMLFunctionIds.DateEqual, 1);
                return a == b;
            });

        // ── DateTime equality ───────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.DateTimeEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.DateTimeEqual);
                var a = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.DateTimeEqual, 0);
                var b = FunctionHelpers.CoerceToDateTime(args[1], XACMLFunctionIds.DateTimeEqual, 1);
                return a == b;
            });

        // ── Time equality ───────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.TimeEqual,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.TimeEqual);
                var a = FunctionHelpers.CoerceToTime(args[0], XACMLFunctionIds.TimeEqual, 0);
                var b = FunctionHelpers.CoerceToTime(args[1], XACMLFunctionIds.TimeEqual, 1);
                return a == b;
            });
    }
}
