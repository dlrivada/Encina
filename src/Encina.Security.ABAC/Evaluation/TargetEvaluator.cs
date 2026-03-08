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
    /// Evaluates a single <see cref="Match"/> element by resolving the attribute
    /// from the context and applying the comparison function.
    /// </summary>
    private Effect EvaluateMatch(Match match, PolicyEvaluationContext context)
    {
        // Resolve the attribute bag for the designator's category
        var bag = ResolveCategoryBag(match.AttributeDesignator.Category, context);

        if (bag.IsEmpty)
        {
            // If MustBePresent and bag is empty, it's an error
            return match.AttributeDesignator.MustBePresent
                ? Effect.Indeterminate
                : Effect.NotApplicable;
        }

        // Get the comparison function
        var function = _functionRegistry.GetFunction(match.FunctionId);
        if (function is null)
        {
            return Effect.Indeterminate;
        }

        // Evaluate the function with the bag value and the literal match value
        try
        {
            // Single value → compare directly
            if (bag.Count == 1)
            {
                var result = function.Evaluate([bag.Values[0].Value, match.AttributeValue.Value]);
                return result is true ? Effect.Permit : Effect.NotApplicable;
            }

            // Multi-valued bag → any value must match (XACML bag semantics)
            foreach (var bagValue in bag.Values)
            {
                var result = function.Evaluate([bagValue.Value, match.AttributeValue.Value]);
                if (result is true)
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

    /// <summary>
    /// Resolves the <see cref="AttributeBag"/> for the given <see cref="AttributeCategory"/>
    /// from the evaluation context.
    /// </summary>
    private static AttributeBag ResolveCategoryBag(
        AttributeCategory category,
        PolicyEvaluationContext context) =>
        category switch
        {
            AttributeCategory.Subject => context.SubjectAttributes,
            AttributeCategory.Resource => context.ResourceAttributes,
            AttributeCategory.Environment => context.EnvironmentAttributes,
            AttributeCategory.Action => context.ActionAttributes,
            _ => AttributeBag.Empty
        };
}
