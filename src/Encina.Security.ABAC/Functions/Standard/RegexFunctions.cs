using System.Text.RegularExpressions;

namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Regular expression matching function.
/// </summary>
internal static class RegexFunctions
{
    /// <summary>
    /// Timeout for regex evaluation to prevent ReDoS attacks.
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── string-regexp-match ─────────────────────────────────────
        // XACML: string-regexp-match(pattern, string) → bool
        registry.RegisterFunction(
            XACMLFunctionIds.StringRegexpMatch,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.StringRegexpMatch);
                var pattern = FunctionHelpers.CoerceToStringStrict(
                    args[0], XACMLFunctionIds.StringRegexpMatch, 0);
                var input = FunctionHelpers.CoerceToString(args[1]);

                try
                {
                    return Regex.IsMatch(input, pattern, RegexOptions.None, RegexTimeout);
                }
                catch (RegexMatchTimeoutException)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.StringRegexpMatch}': regex evaluation timed out for pattern '{pattern}'.");
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"'{XACMLFunctionIds.StringRegexpMatch}': invalid regex pattern '{pattern}': {ex.Message}",
                        ex);
                }
            });
    }
}
