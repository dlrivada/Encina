namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Apply element — a function application node in the
/// condition expression tree.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.7 — An Apply node invokes a function identified by <see cref="FunctionId"/>
/// with a list of <see cref="IExpression"/> arguments. Arguments can be other Apply nodes
/// (recursive composition), <see cref="AttributeDesignator"/> (attribute lookups),
/// <see cref="AttributeValue"/> (literals), or <see cref="VariableReference"/> (named variables).
/// </para>
/// <para>
/// This recursive tree structure enables arbitrarily complex conditions:
/// <c>and(string-equal(subject.department, "Finance"), integer-greater-than(resource.amount, 10000))</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Condition: string-equal(subject.department, "Finance")
/// var apply = new Apply
/// {
///     FunctionId = "string-equal",
///     Arguments =
///     [
///         new AttributeDesignator
///         {
///             Category = AttributeCategory.Subject,
///             AttributeId = "department",
///             DataType = "string"
///         },
///         new AttributeValue { DataType = "string", Value = "Finance" }
///     ]
/// };
/// </code>
/// </example>
public sealed record Apply : IExpression
{
    /// <summary>
    /// The identifier of the function to apply.
    /// </summary>
    /// <remarks>
    /// Must reference a function registered in the <c>IFunctionRegistry</c>.
    /// Common identifiers include <c>"string-equal"</c>, <c>"integer-greater-than"</c>,
    /// <c>"and"</c>, <c>"or"</c>, <c>"not"</c>.
    /// </remarks>
    public required string FunctionId { get; init; }

    /// <summary>
    /// The ordered list of arguments to pass to the function.
    /// </summary>
    /// <remarks>
    /// Each argument is an <see cref="IExpression"/> that is evaluated recursively before
    /// being passed to the function. The function validates argument count and types
    /// at evaluation time.
    /// </remarks>
    public required IReadOnlyList<IExpression> Arguments { get; init; }
}
