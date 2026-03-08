namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 AdviceExpression — an optional recommendation that the PEP
/// may choose to act on after the authorization decision.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Unlike <see cref="Obligation"/>, advice is <b>optional</b>:
/// the PEP may ignore advice without affecting the authorization decision. Advice is
/// useful for non-mandatory recommendations such as logging suggestions, user notifications,
/// or UI guidance.
/// </para>
/// <para>
/// Advice expressions use the same conditional mechanism as obligations: they are only
/// included in the response when the decision matches <see cref="AppliesTo"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var notifyAdvice = new AdviceExpression
/// {
///     Id = "notify-user",
///     AppliesTo = FulfillOn.Deny,
///     AttributeAssignments =
///     [
///         new AttributeAssignment
///         {
///             AttributeId = "message",
///             Value = new AttributeValue
///             {
///                 DataType = "string",
///                 Value = "Contact your manager to request access."
///             }
///         }
///     ]
/// };
/// </code>
/// </example>
public sealed record AdviceExpression
{
    /// <summary>
    /// The unique identifier for this advice expression.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Specifies on which decision effect this advice applies.
    /// </summary>
    public required FulfillOn AppliesTo { get; init; }

    /// <summary>
    /// The attribute assignments that parameterize this advice.
    /// </summary>
    public required IReadOnlyList<AttributeAssignment> AttributeAssignments { get; init; }
}
