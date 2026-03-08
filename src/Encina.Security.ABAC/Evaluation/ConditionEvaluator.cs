using LanguageExt;

namespace Encina.Security.ABAC.Evaluation;

/// <summary>
/// Recursively evaluates XACML 3.0 <see cref="IExpression"/> trees against a
/// <see cref="PolicyEvaluationContext"/> to produce a value or an error.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.7 — The condition evaluator implements the recursive evaluation
/// algorithm for expression trees composed of <see cref="Apply"/>,
/// <see cref="AttributeDesignator"/>, <see cref="AttributeValue"/>, and
/// <see cref="VariableReference"/> nodes.
/// </para>
/// <para>
/// Results follow Railway Oriented Programming: <c>Either&lt;EncinaError, object?&gt;</c>.
/// Left represents an evaluation error (Indeterminate); Right represents the successfully
/// computed value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var evaluator = new ConditionEvaluator(functionRegistry);
/// var condition = ConditionBuilder.Equal(
///     ConditionBuilder.Attribute(AttributeCategory.Subject, "role", XACMLDataTypes.String),
///     ConditionBuilder.StringValue("Admin"));
///
/// var result = evaluator.Evaluate(condition, context);
/// result.Match(
///     error => Console.WriteLine($"Evaluation failed: {error.Message}"),
///     value => Console.WriteLine($"Result: {value}"));
/// </code>
/// </example>
public sealed class ConditionEvaluator(IFunctionRegistry functionRegistry)
{
    private readonly IFunctionRegistry _functionRegistry = functionRegistry
        ?? throw new ArgumentNullException(nameof(functionRegistry));

    /// <summary>
    /// Evaluates an <see cref="IExpression"/> tree against the provided evaluation context.
    /// </summary>
    /// <param name="expression">The expression tree to evaluate.</param>
    /// <param name="context">The attribute context for resolving <see cref="AttributeDesignator"/> references.</param>
    /// <param name="variables">
    /// Optional dictionary of <see cref="VariableDefinition"/> instances for resolving
    /// <see cref="VariableReference"/> nodes. Scoped to the containing policy.
    /// </param>
    /// <returns>
    /// <c>Right(value)</c> on success with the computed value, or
    /// <c>Left(error)</c> if evaluation fails (attribute resolution, function error, etc.).
    /// </returns>
    public Either<EncinaError, object?> Evaluate(
        IExpression expression,
        PolicyEvaluationContext context,
        IReadOnlyDictionary<string, VariableDefinition>? variables = null)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(context);

        return expression switch
        {
            AttributeValue attrValue => EvaluateAttributeValue(attrValue),
            AttributeDesignator designator => EvaluateAttributeDesignator(designator, context),
            VariableReference varRef => EvaluateVariableReference(varRef, context, variables),
            Apply apply => EvaluateApply(apply, context, variables),
            _ => ABACErrors.InvalidCondition(
                expression.GetType().Name,
                $"Unsupported expression type: {expression.GetType().Name}")
        };
    }

    /// <summary>
    /// Evaluates a literal <see cref="AttributeValue"/> — simply returns its value.
    /// </summary>
    private static Either<EncinaError, object?> EvaluateAttributeValue(AttributeValue attrValue) =>
        attrValue.Value;

    /// <summary>
    /// Evaluates an <see cref="AttributeDesignator"/> by resolving the attribute bag
    /// from the evaluation context.
    /// </summary>
    private static Either<EncinaError, object?> EvaluateAttributeDesignator(
        AttributeDesignator designator,
        PolicyEvaluationContext context)
    {
        var bag = ResolveCategoryBag(designator.Category, context);

        if (bag.IsEmpty)
        {
            return designator.MustBePresent
                ? ABACErrors.AttributeResolutionFailed(designator.AttributeId, designator.Category)
                : (Either<EncinaError, object?>)AttributeBag.Empty;
        }

        // Single value → unwrap for direct use in comparisons
        if (bag.Count == 1)
        {
            return bag.Values[0].Value;
        }

        // Multi-valued → return the whole bag for bag functions
        return bag;
    }

    /// <summary>
    /// Evaluates a <see cref="VariableReference"/> by looking up and evaluating the
    /// referenced <see cref="VariableDefinition"/>.
    /// </summary>
    private Either<EncinaError, object?> EvaluateVariableReference(
        VariableReference varRef,
        PolicyEvaluationContext context,
        IReadOnlyDictionary<string, VariableDefinition>? variables)
    {
        if (variables is null || !variables.TryGetValue(varRef.VariableId, out var variableDefinition))
        {
            return ABACErrors.VariableNotFound(varRef.VariableId);
        }

        // Recursively evaluate the variable's expression
        return Evaluate(variableDefinition.Expression, context, variables);
    }

    /// <summary>
    /// Evaluates an <see cref="Apply"/> node by recursively evaluating all arguments,
    /// resolving the function from the registry, and invoking it.
    /// </summary>
    private Either<EncinaError, object?> EvaluateApply(
        Apply apply,
        PolicyEvaluationContext context,
        IReadOnlyDictionary<string, VariableDefinition>? variables)
    {
        // Resolve the function
        var function = _functionRegistry.GetFunction(apply.FunctionId);
        if (function is null)
        {
            return ABACErrors.FunctionNotFound(apply.FunctionId);
        }

        // Evaluate all arguments recursively
        var evaluatedArgs = new List<object?>(apply.Arguments.Count);
        foreach (var arg in apply.Arguments)
        {
            var argResult = Evaluate(arg, context, variables);

            if (argResult.IsLeft)
            {
                // Short-circuit on first error
                return argResult;
            }

            evaluatedArgs.Add(argResult.Match(Left: _ => (object?)null, Right: v => v));
        }

        // Invoke the function
        try
        {
            var result = function.Evaluate(evaluatedArgs);
            return result;
        }
        catch (Exception ex)
        {
            return ABACErrors.FunctionError(apply.FunctionId, ex);
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
