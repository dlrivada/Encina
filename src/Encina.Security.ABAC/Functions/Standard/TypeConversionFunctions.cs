using System.Globalization;

namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Type conversion functions between string,
/// integer, double, boolean, and dateTime.
/// </summary>
internal static class TypeConversionFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── string-from-integer ─────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringFromInteger,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringFromInteger);
                var value = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.StringFromInteger, 0);
                return value.ToString(CultureInfo.InvariantCulture);
            });

        // ── integer-from-string ─────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.IntegerFromString,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.IntegerFromString);
                var str = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.IntegerFromString, 0);
                if (!int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.IntegerFromString}': cannot parse '{str}' as integer.");
                }

                return result;
            });

        // ── double-from-string ──────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.DoubleFromString,
            XACMLDataTypes.Double,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.DoubleFromString);
                var str = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.DoubleFromString, 0);
                if (!double.TryParse(str, NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture, out var result))
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.DoubleFromString}': cannot parse '{str}' as double.");
                }

                return result;
            });

        // ── boolean-from-string ─────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.BooleanFromString,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.BooleanFromString);
                var str = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.BooleanFromString, 0);
                if (!bool.TryParse(str, out var result))
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.BooleanFromString}': cannot parse '{str}' as boolean.");
                }

                return result;
            });

        // ── string-from-boolean ─────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringFromBoolean,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringFromBoolean);
                var value = FunctionHelpers.CoerceToBool(args[0], XACMLFunctionIds.StringFromBoolean, 0);
                return value ? "true" : "false";
            });

        // ── string-from-double ──────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringFromDouble,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringFromDouble);
                var value = FunctionHelpers.CoerceToDouble(args[0], XACMLFunctionIds.StringFromDouble, 0);
                return value.ToString(CultureInfo.InvariantCulture);
            });

        // ── string-from-dateTime ────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringFromDateTime,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringFromDateTime);
                var value = FunctionHelpers.CoerceToDateTime(args[0], XACMLFunctionIds.StringFromDateTime, 0);
                return value.ToString("o", CultureInfo.InvariantCulture);
            });
    }
}
