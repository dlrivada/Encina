namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// Evaluates XACML 3.0 <see cref="Target"/> structures against a
/// <see cref="PolicyEvaluationContext"/> to determine whether a policy, policy set,
/// or rule applies to the current access request.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.6 — The target uses a triple-nesting structure:
/// <c>Target → AnyOf (AND) → AllOf (OR) → Match (AND)</c>.
/// All <see cref="AnyOf"/> elements must match (logical AND), any <see cref="AllOf"/>
/// within an AnyOf can match (logical OR), and all <see cref="Match"/> elements within
/// an AllOf must match (logical AND).
/// </para>
/// <para>
/// A <c>null</c> target or a target with empty <see cref="Target.AnyOfElements"/>
/// matches all requests (unconditional applicability), returning <see cref="Effect.Permit"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluator = new TargetEvaluator(functionRegistry);
/// var result = evaluator.EvaluateTarget(policy.Target, context);
/// // result is Effect.Permit (matches), Effect.NotApplicable (no match),
/// // or Effect.Indeterminate (error during evaluation)
/// </code>
/// </example>
public sealed class TargetEvaluator(IFunctionRegistry functionRegistry)
{
    private readonly IFunctionRegistry _functionRegistry = functionRegistry
        ?? throw new ArgumentNullException(nameof(functionRegistry));

    /// <summary>
    /// Evaluates a <see cref="Target"/> against the given evaluation context.
    /// </summary>
    /// <param name="target">
    /// The target to evaluate, or <c>null</c> for unconditional match.
    /// </param>
    /// <param name="context">The attribute context for resolving designator references.</param>
    /// <returns>
    /// <see cref="Effect.Permit"/> if the target matches,
    /// <see cref="Effect.NotApplicable"/> if it does not match, or
    /// <see cref="Effect.Indeterminate"/> if an error occurred during evaluation.
    /// </returns>
    public Effect EvaluateTarget(Target? target, PolicyEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Null target or empty AnyOfElements = matches all requests
        if (target is null || target.AnyOfElements.Count == 0)
        {
            return Effect.Permit;
        }

        // All AnyOf elements must match (AND)
        foreach (var anyOf in target.AnyOfElements)
        {
            var anyOfResult = EvaluateAnyOf(anyOf, context);

            if (anyOfResult != Effect.Permit)
            {
                return anyOfResult;
            }
        }

        return Effect.Permit;
    }

    /// <summary>
    /// Evaluates an <see cref="AnyOf"/> element — at least one <see cref="AllOf"/>
    /// must match (logical OR).
    /// </summary>
    private Effect EvaluateAnyOf(AnyOf anyOf, PolicyEvaluationContext context)
    {
        if (anyOf.AllOfElements.Count == 0)
        {
            return Effect.Permit;
        }

        var atLeastOneIndeterminate = false;

        foreach (var allOf in anyOf.AllOfElements)
        {
            var allOfResult = EvaluateAllOf(allOf, context);

            switch (allOfResult)
            {
                case Effect.Permit:
                    // At least one AllOf matched — AnyOf is satisfied
                    return Effect.Permit;

                case Effect.Indeterminate:
                    atLeastOneIndeterminate = true;
                    break;

                    // NotApplicable — continue checking other AllOf elements
            }
        }

        // If any AllOf was indeterminate and none matched, propagate indeterminate
        return atLeastOneIndeterminate ? Effect.Indeterminate : Effect.NotApplicable;
    }

    /// <summary>
    /// Evaluates an <see cref="AllOf"/> element — all <see cref="Match"/> elements
    /// must match (logical AND).
    /// </summary>
    private Effect EvaluateAllOf(AllOf allOf, PolicyEvaluationContext context)
    {
        if (allOf.Matches.Count == 0)
        {
            return Effect.Permit;
        }

        foreach (var match in allOf.Matches)
        {
            var matchResult = EvaluateMatch(match, context);

            if (matchResult != Effect.Permit)
            {
                return matchResult;
            }
        }

        return Effect.Permit;
    }

    /// <summary>
    /// Evaluates a single <see cref="Match"/> element by resolving the bag of the attribute its
    /// designator names (category, attribute identifier and data type) and applying the
    /// comparison function to each value.
    /// </summary>
    private Effect EvaluateMatch(Match match, PolicyEvaluationContext context)
    {
        var bag = AttributeDesignatorResolver.Resolve(match.AttributeDesignator, context);

        if (bag.IsEmpty)
        {
            // MustBePresent applies to this attribute only: absent is an error when required.
            return match.AttributeDesignator.MustBePresent
                ? Effect.Indeterminate
                : Effect.NotApplicable;
        }

        // An unknown comparison function cannot decide the match.
        var function = _functionRegistry.GetFunction(match.FunctionId);

        return function is null
            ? Effect.Indeterminate
            : MatchAnyValue(function, bag, match.AttributeValue.Value);
    }

    /// <summary>
    /// Applies the comparison function to each value of the attribute's bag and the literal match
    /// value: any value that matches satisfies the match (XACML 3.0 §7.6 bag semantics); a function
    /// error makes it <see cref="Effect.Indeterminate"/>.
    /// </summary>
    private static Effect MatchAnyValue(IXACMLFunction function, AttributeBag bag, object? literal)
    {
        try
        {
            foreach (var bagValue in bag.Values)
            {
                if (function.Evaluate([bagValue.Value, literal]) is true)
                {
                    return Effect.Permit;
                }
            }

            return Effect.NotApplicable;
        }
        catch
        {
            return Effect.Indeterminate;
        }
    }
}
