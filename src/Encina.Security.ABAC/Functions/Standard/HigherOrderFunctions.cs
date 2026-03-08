namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A.3 — Higher-order functions: any-of, all-of, any-of-any,
/// all-of-any, all-of-all, map.
/// </summary>
/// <remarks>
/// Higher-order functions take a function identifier as their first argument,
/// resolve it from the registry, and apply it to bag elements.
/// </remarks>
internal static class HigherOrderFunctions
{
    internal static void Register(DefaultFunctionRegistry registry)
    {
        // ── any-of ──────────────────────────────────────────────────
        // any-of(functionId, value, bag) → bool
        // Returns true if the function returns true for value and ANY element in the bag.
        registry.RegisterFunction(
            XACMLFunctionIds.AnyOfFunc,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 3, XACMLFunctionIds.AnyOfFunc);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.AnyOfFunc, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.AnyOfFunc);
                var value = args[1];
                var bag = FunctionHelpers.CoerceToBag(args[2], XACMLFunctionIds.AnyOfFunc, 2);

                foreach (var item in bag.Values)
                {
                    var result = fn.Evaluate([value, item.Value]);
                    if (FunctionHelpers.CoerceToBool(result, XACMLFunctionIds.AnyOfFunc, -1))
                    {
                        return true;
                    }
                }

                return false;
            });

        // ── all-of ──────────────────────────────────────────────────
        // all-of(functionId, value, bag) → bool
        // Returns true if the function returns true for value and ALL elements in the bag.
        registry.RegisterFunction(
            XACMLFunctionIds.AllOfFunc,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateMinArgCount(args, 3, XACMLFunctionIds.AllOfFunc);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.AllOfFunc, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.AllOfFunc);
                var value = args[1];
                var bag = FunctionHelpers.CoerceToBag(args[2], XACMLFunctionIds.AllOfFunc, 2);

                foreach (var item in bag.Values)
                {
                    var result = fn.Evaluate([value, item.Value]);
                    if (!FunctionHelpers.CoerceToBool(result, XACMLFunctionIds.AllOfFunc, -1))
                    {
                        return false;
                    }
                }

                return true;
            });

        // ── any-of-any ──────────────────────────────────────────────
        // any-of-any(functionId, bag1, bag2) → bool
        // Returns true if the function returns true for ANY pair (v1, v2)
        // where v1 ∈ bag1 and v2 ∈ bag2.
        registry.RegisterFunction(
            XACMLFunctionIds.AnyOfAny,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 3, XACMLFunctionIds.AnyOfAny);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.AnyOfAny, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.AnyOfAny);
                var bag1 = FunctionHelpers.CoerceToBag(args[1], XACMLFunctionIds.AnyOfAny, 1);
                var bag2 = FunctionHelpers.CoerceToBag(args[2], XACMLFunctionIds.AnyOfAny, 2);

                foreach (var v1 in bag1.Values)
                {
                    foreach (var v2 in bag2.Values)
                    {
                        var result = fn.Evaluate([v1.Value, v2.Value]);
                        if (FunctionHelpers.CoerceToBool(result, XACMLFunctionIds.AnyOfAny, -1))
                        {
                            return true;
                        }
                    }
                }

                return false;
            });

        // ── all-of-any ──────────────────────────────────────────────
        // all-of-any(functionId, bag1, bag2) → bool
        // Returns true if for EVERY v1 ∈ bag1 there exists at least one v2 ∈ bag2
        // such that function(v1, v2) returns true.
        registry.RegisterFunction(
            XACMLFunctionIds.AllOfAny,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 3, XACMLFunctionIds.AllOfAny);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.AllOfAny, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.AllOfAny);
                var bag1 = FunctionHelpers.CoerceToBag(args[1], XACMLFunctionIds.AllOfAny, 1);
                var bag2 = FunctionHelpers.CoerceToBag(args[2], XACMLFunctionIds.AllOfAny, 2);

                foreach (var v1 in bag1.Values)
                {
                    var found = false;
                    foreach (var v2 in bag2.Values)
                    {
                        var result = fn.Evaluate([v1.Value, v2.Value]);
                        if (FunctionHelpers.CoerceToBool(result, XACMLFunctionIds.AllOfAny, -1))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }

                return true;
            });

        // ── all-of-all ──────────────────────────────────────────────
        // all-of-all(functionId, bag1, bag2) → bool
        // Returns true if function(v1, v2) returns true for ALL pairs
        // where v1 ∈ bag1 and v2 ∈ bag2.
        registry.RegisterFunction(
            XACMLFunctionIds.AllOfAll,
            XACMLDataTypes.Boolean,
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 3, XACMLFunctionIds.AllOfAll);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.AllOfAll, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.AllOfAll);
                var bag1 = FunctionHelpers.CoerceToBag(args[1], XACMLFunctionIds.AllOfAll, 1);
                var bag2 = FunctionHelpers.CoerceToBag(args[2], XACMLFunctionIds.AllOfAll, 2);

                foreach (var v1 in bag1.Values)
                {
                    foreach (var v2 in bag2.Values)
                    {
                        var result = fn.Evaluate([v1.Value, v2.Value]);
                        if (!FunctionHelpers.CoerceToBool(result, XACMLFunctionIds.AllOfAll, -1))
                        {
                            return false;
                        }
                    }
                }

                return true;
            });

        // ── map ─────────────────────────────────────────────────────
        // map(functionId, bag) → bag
        // Applies the function to each element in the bag and returns a new bag
        // containing the results.
        registry.RegisterFunction(
            XACMLFunctionIds.Map,
            XACMLDataTypes.String, // Return type depends on the mapped function; use String as default
            args =>
            {
                FunctionHelpers.ValidateArgCount(args, 2, XACMLFunctionIds.Map);
                var functionId = FunctionHelpers.CoerceToStringStrict(args[0], XACMLFunctionIds.Map, 0);
                var fn = ResolveFunction(registry, functionId, XACMLFunctionIds.Map);
                var bag = FunctionHelpers.CoerceToBag(args[1], XACMLFunctionIds.Map, 1);

                var results = new AttributeValue[bag.Count];
                for (var i = 0; i < bag.Count; i++)
                {
                    var result = fn.Evaluate([bag.Values[i].Value]);
                    results[i] = new AttributeValue
                    {
                        DataType = fn.ReturnType,
                        Value = result
                    };
                }

                return AttributeBag.Of(results);
            });
    }

    /// <summary>
    /// Resolves a function from the registry by ID, throwing if not found.
    /// </summary>
    private static IXACMLFunction ResolveFunction(
        DefaultFunctionRegistry registry,
        string functionId,
        string callerFunctionId)
    {
        var fn = registry.GetFunction(functionId);
        if (fn is null)
        {
            throw new InvalidOperationException(
                $"'{callerFunctionId}': referenced function '{functionId}' is not registered.");
        }

        return fn;
    }
}
