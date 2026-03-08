namespace Encina.Security.ABAC;

/// <summary>
/// Represents an XACML 3.0 Obligation — a mandatory action that the Policy Enforcement Point (PEP)
/// must execute after the authorization decision.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Obligations are mandatory post-decision actions. If the PEP cannot
/// fulfill a mandatory obligation, it <b>must</b> deny access regardless of the PDP decision.
/// This is a critical XACML specification requirement.
/// </para>
/// <para>
/// Each obligation is conditional on the decision effect: it is only executed when the
/// decision matches the <see cref="FulfillOn"/> value (either <see cref="ABAC.FulfillOn.Permit"/>
/// or <see cref="ABAC.FulfillOn.Deny"/>).
/// </para>
/// <para>
/// Obligations carry <see cref="AttributeAssignment"/> values that parameterize the action
/// (e.g., an audit obligation might include the resource ID and user ID as attribute assignments).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var auditObligation = new Obligation
/// {
///     Id = "audit-access",
///     FulfillOn = FulfillOn.Permit,
///     AttributeAssignments =
///     [
///         new AttributeAssignment
///         {
///             AttributeId = "reason",
///             Value = new AttributeValue { DataType = "string", Value = "Sensitive resource accessed" }
///         }
///     ]
/// };
/// </code>
/// </example>
public sealed record Obligation
{
    /// <summary>
    /// The unique identifier for this obligation.
    /// </summary>
    /// <remarks>
    /// Used by <c>IObligationHandler.CanHandle(string obligationId)</c> to match
    /// obligations to their handler implementations.
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Specifies on which decision effect this obligation should be fulfilled.
    /// </summary>
    public required FulfillOn FulfillOn { get; init; }

    /// <summary>
    /// The attribute assignments that parameterize this obligation.
    /// </summary>
    public required IReadOnlyList<AttributeAssignment> AttributeAssignments { get; init; }
}
