namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 VariableDefinition — a named, reusable sub-expression
/// within a <see cref="Policy"/>.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.8 — Variable definitions allow factoring out common sub-expressions
/// to avoid duplication in policy conditions. A variable is defined once within a policy
/// and can be referenced from multiple conditions via <see cref="VariableReference"/>.
/// </para>
/// <para>
/// Variable scope is limited to the containing <see cref="Policy"/>. Variable definitions
/// are evaluated lazily when first referenced and their results can be cached for the
/// duration of a single policy evaluation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a reusable expression
/// var varDef = new VariableDefinition
/// {
///     VariableId = "isHighValue",
///     Expression = new Apply
///     {
///         FunctionId = "integer-greater-than",
///         Arguments =
///         [
///             new AttributeDesignator
///             {
///                 Category = AttributeCategory.Resource,
///                 AttributeId = "amount",
///                 DataType = "integer"
///             },
///             new AttributeValue { DataType = "integer", Value = 10000 }
///         ]
///     }
/// };
/// </code>
/// </example>
public sealed record VariableDefinition
{
    /// <summary>
    /// The unique identifier for this variable within the containing policy.
    /// </summary>
    public required string VariableId { get; init; }

    /// <summary>
    /// The expression that computes the variable's value when evaluated.
    /// </summary>
    public required IExpression Expression { get; init; }
}
