namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Set functions: intersection, union, subset,
/// at-least-one-member-of, set-equals for string, integer, and double types.
/// </summary>
internal static class SetFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        RegisterSetFunctionsForType(registry, XACMLDataTypes.String,
            XACMLFunctionIds.StringIntersection, XACMLFunctionIds.StringUnion,
            XACMLFunctionIds.StringSubset, XACMLFunctionIds.StringAtLeastOneMemberOf,
            XACMLFunctionIds.StringSetEquals);

        RegisterSetFunctionsForType(registry, XACMLDataTypes.Integer,
            XACMLFunctionIds.IntegerIntersection, XACMLFunctionIds.IntegerUnion,
            XACMLFunctionIds.IntegerSubset, XACMLFunctionIds.IntegerAtLeastOneMemberOf,
            XACMLFunctionIds.IntegerSetEquals);

        RegisterSetFunctionsForType(registry, XACMLDataTypes.Double,
            XACMLFunctionIds.DoubleIntersection, XACMLFunctionIds.DoubleUnion,
            XACMLFunctionIds.DoubleSubset, XACMLFunctionIds.DoubleAtLeastOneMemberOf,
            XACMLFunctionIds.DoubleSetEquals);
    }

    private static void RegisterSetFunctionsForType(
        DefaultFunctionRegistry registry,
        string dataType,
        string intersectionId,
        string unionId,
        string subsetId,
        string atLeastOneMemberOfId,
        string setEqualsId)
    {
        // ── *-intersection ──────────────────────────────────────────
        // Returns a bag containing values that exist in both bags.
        registry.RegisterFunction(
            intersectionId,
            dataType,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, intersectionId);
                var bag1 = FunctionHelpers.CoerceToBag(args[0], intersectionId, 0);
                var bag2 = FunctionHelpers.CoerceToBag(args[1], intersectionId, 1);

                var result = new List<AttributeValue>();
                foreach (var v1 in bag1.Values)
                {
                    if (bag2.Values.Any(v2 => BagFunctions.ValuesEqual(v1.Value, v2.Value)))
                    {
                        // Avoid duplicates in the result
                        if (!result.Any(r => BagFunctions.ValuesEqual(r.Value, v1.Value)))
                        {
                            result.Add(v1);
                        }
                    }
                }

                return result.Count == 0
                    ? AttributeBag.Empty
                    : AttributeBag.FromValues(result);
            });

        // ── *-union ─────────────────────────────────────────────────
        // Returns a bag containing all unique values from both bags.
        registry.RegisterFunction(
            unionId,
            dataType,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, unionId);
                var bag1 = FunctionHelpers.CoerceToBag(args[0], unionId, 0);
                var bag2 = FunctionHelpers.CoerceToBag(args[1], unionId, 1);

                var result = new List<AttributeValue>(bag1.Values);
                foreach (var v2 in bag2.Values)
                {
                    if (!result.Any(r => BagFunctions.ValuesEqual(r.Value, v2.Value)))
                    {
                        result.Add(v2);
                    }
                }

                return result.Count == 0
                    ? AttributeBag.Empty
                    : AttributeBag.FromValues(result);
            });

        // ── *-subset ────────────────────────────────────────────────
        // Returns true if bag1 is a subset of bag2
        // (every value in bag1 exists in bag2).
        registry.RegisterFunction(
            subsetId,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, subsetId);
                var bag1 = FunctionHelpers.CoerceToBag(args[0], subsetId, 0);
                var bag2 = FunctionHelpers.CoerceToBag(args[1], subsetId, 1);

                return bag1.Values.All(v1 =>
                    bag2.Values.Any(v2 => BagFunctions.ValuesEqual(v1.Value, v2.Value)));
            });

        // ── *-at-least-one-member-of ────────────────────────────────
        // Returns true if the two bags share at least one common value.
        registry.RegisterFunction(
            atLeastOneMemberOfId,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, atLeastOneMemberOfId);
                var bag1 = FunctionHelpers.CoerceToBag(args[0], atLeastOneMemberOfId, 0);
                var bag2 = FunctionHelpers.CoerceToBag(args[1], atLeastOneMemberOfId, 1);

                return bag1.Values.Any(v1 =>
                    bag2.Values.Any(v2 => BagFunctions.ValuesEqual(v1.Value, v2.Value)));
            });

        // ── *-set-equals ────────────────────────────────────────────
        // Returns true if both bags contain the same set of values
        // (bag1 ⊆ bag2 and bag2 ⊆ bag1).
        registry.RegisterFunction(
            setEqualsId,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, setEqualsId);
                var bag1 = FunctionHelpers.CoerceToBag(args[0], setEqualsId, 0);
                var bag2 = FunctionHelpers.CoerceToBag(args[1], setEqualsId, 1);

                // bag1 ⊆ bag2
                var subset1 = bag1.Values.All(v1 =>
                    bag2.Values.Any(v2 => BagFunctions.ValuesEqual(v1.Value, v2.Value)));

                // bag2 ⊆ bag1
                var subset2 = bag2.Values.All(v2 =>
                    bag1.Values.Any(v1 => BagFunctions.ValuesEqual(v1.Value, v2.Value)));

                return subset1 && subset2;
            });
    }
}
