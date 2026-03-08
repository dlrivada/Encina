namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Logical functions: and, or, not, n-of.
/// </summary>
internal static class LogicalFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── and (short-circuit) ─────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.And,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 1, XACMLFunctionIds.And);
                for (var i = 0; i < args.Count; i++)
                {
                    if (!FunctionHelpers.CoerceToBool(args[i], XACMLFunctionIds.And, i))
                    {
                        return false; // Short-circuit
                    }
                }

                return true;
            });

        // ── or (short-circuit) ──────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.Or,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 1, XACMLFunctionIds.Or);
                for (var i = 0; i < args.Count; i++)
                {
                    if (FunctionHelpers.CoerceToBool(args[i], XACMLFunctionIds.Or, i))
                    {
                        return true; // Short-circuit
                    }
                }

                return false;
            });

        // ── not ─────────────────────────────────────────────────────
        registry.RegisterFunction(
            XACMLFunctionIds.Not,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, XACMLFunctionIds.Not);
                var value = FunctionHelpers.CoerceToBool(args[0], XACMLFunctionIds.Not, 0);
                return !value;
            });

        // ── n-of ────────────────────────────────────────────────────
        // First argument is the integer N, remaining are boolean values.
        // Returns true if at least N of the boolean arguments are true.
        registry.RegisterFunction(
            XACMLFunctionIds.NOf,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 2, XACMLFunctionIds.NOf);
                var n = FunctionHelpers.CoerceToInt(args[0], XACMLFunctionIds.NOf, 0);
                var trueCount = 0;

                for (var i = 1; i < args.Count; i++)
                {
                    if (FunctionHelpers.CoerceToBool(args[i], XACMLFunctionIds.NOf, i))
                    {
                        trueCount++;
                        if (trueCount >= n)
                        {
                            return true; // Short-circuit when threshold reached
                        }
                    }
                }

                return trueCount >= n;
            });
    }
}
