namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AttributeAssignment — a named value included in an
/// <see cref="Obligation"/> or <see cref="AdviceExpression"/> to parameterize
/// the post-decision action.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Attribute assignments carry metadata from the policy definition
/// to the PEP obligation/advice handler. The <see cref="Value"/> is an <see cref="IExpression"/>
/// that can be a literal <see cref="AttributeValue"/>, an <see cref="AttributeDesignator"/>
/// resolved at evaluation time, or a computed <see cref="Apply"/> expression.
/// </para>
/// <para>
/// The optional <see cref="Category"/> indicates which attribute category the assignment
/// relates to, providing context to the obligation handler.
/// </para>
/// </remarks>
public sealed record AttributeAssignment
{
    /// <summary>
    /// The identifier of the attribute being assigned (e.g., <c>"reason"</c>, <c>"resourceId"</c>).
    /// </summary>
    public required string AttributeId { get; init; }

    /// <summary>
    /// The optional attribute category that this assignment relates to.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the assignment is not category-specific (e.g., a generic reason message).
    /// </remarks>
    public AttributeCategory? Category { get; init; }

    /// <summary>
    /// The expression that computes the value for this assignment.
    /// </summary>
    /// <remarks>
    /// Can be a literal <see cref="AttributeValue"/>, an <see cref="AttributeDesignator"/>
    /// resolved from the evaluation context, or a computed <see cref="Apply"/> expression.
    /// </remarks>
    public required IExpression Value { get; init; }
}
