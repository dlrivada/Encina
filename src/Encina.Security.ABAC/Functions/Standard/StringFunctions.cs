using System.Globalization;
using System.Text.RegularExpressions;

namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — String manipulation functions.
/// </summary>
internal static partial class StringFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── string-concatenate ──────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringConcatenate,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 2, XACMLFunctionIds.StringConcatenate);
                var parts = new string[args.Count];
                for (var i = 0; i < args.Count; i++)
                {
                    parts[i] = FunctionHelpers.CoerceToString(args[i]);
                }

                return string.Concat(parts);
            });

        // ── string-starts-with ──────────────────────────────────────
        // XACML: string-starts-with(substring, fullString) → bool
        registry.RegisterFunction(
            XACMLFunctionIds.StringStartsWith,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringStartsWith);
                var substring = FunctionHelpers.CoerceToString(args[0]);
                var fullString = FunctionHelpers.CoerceToString(args[1]);
                return fullString.StartsWith(substring, StringComparison.Ordinal);
            });

        // ── string-ends-with ────────────────────────────────────────
        // XACML: string-ends-with(substring, fullString) → bool
        registry.RegisterFunction(
            XACMLFunctionIds.StringEndsWith,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringEndsWith);
                var substring = FunctionHelpers.CoerceToString(args[0]);
                var fullString = FunctionHelpers.CoerceToString(args[1]);
                return fullString.EndsWith(substring, StringComparison.Ordinal);
            });

        // ── string-contains ─────────────────────────────────────────
        // XACML: string-contains(substring, fullString) → bool
        registry.RegisterFunction(
            XACMLFunctionIds.StringContains,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringContains);
                var substring = FunctionHelpers.CoerceToString(args[0]);
                var fullString = FunctionHelpers.CoerceToString(args[1]);
                return fullString.Contains(substring, StringComparison.Ordinal);
            });

        // ── string-substring ────────────────────────────────────────
        // XACML: string-substring(string, beginIndex, endIndex) → string
        // Indices are 0-based. endIndex of -1 means "to end".
        registry.RegisterFunction(
            XACMLFunctionIds.StringSubstring,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 3, XACMLFunctionIds.StringSubstring);
                var str = FunctionHelpers.CoerceToString(args[0]);
                var begin = FunctionHelpers.CoerceToInt(args[1], XACMLFunctionIds.StringSubstring, 1);
                var end = FunctionHelpers.CoerceToInt(args[2], XACMLFunctionIds.StringSubstring, 2);

                if (begin < 0 || begin > str.Length)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.StringSubstring}': begin index {begin} is out of range for string of length {str.Length}.");
                }

                if (end == -1)
                {
                    return str[begin..];
                }

                if (end < begin || end > str.Length)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.StringSubstring}': end index {end} is out of range (begin={begin}, length={str.Length}).");
                }

                return str[begin..end];
            });

        // ── string-normalize-space ──────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringNormalizeSpace,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringNormalizeSpace);
                var str = FunctionHelpers.CoerceToString(args[0]);
                return NormalizeWhitespaceRegex().Replace(str.Trim(), " ");
            });

        // ── string-normalize-to-lower-case ──────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringNormalizeToLowerCase,
            XACMLDataTypes.String,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringNormalizeToLowerCase);
                var str = FunctionHelpers.CoerceToString(args[0]);
                return str.ToLowerInvariant();
            });

        // ── string-length ───────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.StringLength,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.StringLength);
                var str = FunctionHelpers.CoerceToString(args[0]);
                return str.Length;
            });
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex NormalizeWhitespaceRegex();
}
