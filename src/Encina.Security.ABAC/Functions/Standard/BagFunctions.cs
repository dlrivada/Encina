namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Bag functions: one-and-only, bag-size, is-in, bag
/// for all standard data types (string, boolean, integer, double, date, dateTime, time, anyURI).
/// </summary>
internal static class BagFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        RegisterBagFunctionsForType(registry, "string", XACMLDataTypes.String,
            XACMLFunctionIds.StringOneAndOnly, XACMLFunctionIds.StringBagSize,
            XACMLFunctionIds.StringIsIn, XACMLFunctionIds.StringBag);

        RegisterBagFunctionsForType(registry, "boolean", XACMLDataTypes.Boolean,
            XACMLFunctionIds.BooleanOneAndOnly, XACMLFunctionIds.BooleanBagSize,
            XACMLFunctionIds.BooleanIsIn, XACMLFunctionIds.BooleanBag);

        RegisterBagFunctionsForType(registry, "integer", XACMLDataTypes.Integer,
            XACMLFunctionIds.IntegerOneAndOnly, XACMLFunctionIds.IntegerBagSize,
            XACMLFunctionIds.IntegerIsIn, XACMLFunctionIds.IntegerBag);

        RegisterBagFunctionsForType(registry, "double", XACMLDataTypes.Double,
            XACMLFunctionIds.DoubleOneAndOnly, XACMLFunctionIds.DoubleBagSize,
            XACMLFunctionIds.DoubleIsIn, XACMLFunctionIds.DoubleBag);

        RegisterBagFunctionsForType(registry, "date", XACMLDataTypes.Date,
            XACMLFunctionIds.DateOneAndOnly, XACMLFunctionIds.DateBagSize,
            XACMLFunctionIds.DateIsIn, XACMLFunctionIds.DateBag);

        RegisterBagFunctionsForType(registry, "dateTime", XACMLDataTypes.DateTime,
            XACMLFunctionIds.DateTimeOneAndOnly, XACMLFunctionIds.DateTimeBagSize,
            XACMLFunctionIds.DateTimeIsIn, XACMLFunctionIds.DateTimeBag);

        RegisterBagFunctionsForType(registry, "time", XACMLDataTypes.Time,
            XACMLFunctionIds.TimeOneAndOnly, XACMLFunctionIds.TimeBagSize,
            XACMLFunctionIds.TimeIsIn, XACMLFunctionIds.TimeBag);

        RegisterBagFunctionsForType(registry, "anyURI", XACMLDataTypes.AnyURI,
            XACMLFunctionIds.AnyURIOneAndOnly, XACMLFunctionIds.AnyURIBagSize,
            XACMLFunctionIds.AnyURIIsIn, XACMLFunctionIds.AnyURIBag);
    }

    private static void RegisterBagFunctionsForType(
        DefaultFunctionRegistry registry,
        string typeName,
        string dataType,
        string oneAndOnlyId,
        string bagSizeId,
        string isInId,
        string bagId)
    {
        // ── *-one-and-only ──────────────────────────────────────────
        // Extracts the single value from a bag; error if bag size != 1.
        registry.RegisterFunction(
            oneAndOnlyId,
            dataType,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, oneAndOnlyId);
                var bag = FunctionHelpers.CoerceToBag(args[0], oneAndOnlyId, 0);
                return bag.SingleValue().Value;
            });

        // ── *-bag-size ──────────────────────────────────────────────
        // Returns the number of values in a bag.
        registry.RegisterFunction(
            bagSizeId,
            XACMLDataTypes.Integer,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 1, bagSizeId);
                var bag = FunctionHelpers.CoerceToBag(args[0], bagSizeId, 0);
                return bag.Count;
            });

        // ── *-is-in ─────────────────────────────────────────────────
        // Checks if a value is contained in a bag.
        // XACML: type-is-in(value, bag) → bool
        registry.RegisterFunction(
            isInId,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, isInId);
                var value = args[0];
                var bag = FunctionHelpers.CoerceToBag(args[1], isInId, 1);

                foreach (var item in bag.Values)
                {
                    if (ValuesEqual(value, item.Value))
                    {
                        return true;
                    }
                }

                return false;
            });

        // ── *-bag ───────────────────────────────────────────────────
        // Creates a bag from the given values.
        registry.RegisterFunction(
            bagId,
            dataType,
            args =>
            {
                var values = new AttributeValue[args.Count];
                for (var i = 0; i < args.Count; i++)
                {
                    values[i] = new AttributeValue
                    {
                        DataType = dataType,
                        Value = args[i]
                    };
                }

                return AttributeBag.Of(values);
            });
    }

    /// <summary>
    /// Compares two values for equality, handling type coercion for common cases.
    /// </summary>
    internal static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        // Direct equality
        if (a.Equals(b))
        {
            return true;
        }

        // String comparison (case-sensitive ordinal)
        if (a is string sa && b is string sb)
        {
            return string.Equals(sa, sb, StringComparison.Ordinal);
        }

        // Numeric coercion: int/double interop
        if (a is int ia && b is double db)
        {
            return ((double)ia).Equals(db);
        }

        if (a is double da && b is int ib)
        {
            return da.Equals((double)ib);
        }

        // ToString fallback for cross-type comparison
        return string.Equals(
            a.ToString(),
            b.ToString(),
            StringComparison.Ordinal);
    }
}
