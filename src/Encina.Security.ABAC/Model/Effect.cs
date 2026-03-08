namespace Encina.Security.ABAC;

/// <summary>
/// Represents the four possible effects of an XACML 3.0 policy or rule evaluation.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 defines exactly four effects (§7.1). Unlike simplified two-effect models,
/// the full set is essential for correct combining algorithm semantics and for distinguishing
/// between "explicitly denied", "not applicable", and "error during evaluation".
/// </para>
/// <para>
/// The <see cref="NotApplicable"/> and <see cref="Indeterminate"/> effects carry
/// distinct semantics that combining algorithms use to produce correct final decisions.
/// Collapsing them into a simple Permit/Deny model would silently lose information
/// and could produce incorrect authorization decisions.
/// </para>
/// </remarks>
public enum Effect
{
    /// <summary>
    /// The request is explicitly allowed by the policy or rule.
    /// </summary>
    /// <remarks>XACML 3.0 §7.1 — The authorization decision permits the requested access.</remarks>
    Permit,

    /// <summary>
    /// The request is explicitly denied by the policy or rule.
    /// </summary>
    /// <remarks>XACML 3.0 §7.1 — The authorization decision denies the requested access.</remarks>
    Deny,

    /// <summary>
    /// The policy or rule does not apply to the request.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.1 — The request does not match the target of the policy, policy set, or rule.
    /// This is distinct from <see cref="Deny"/>: a non-applicable policy simply has no opinion
    /// about the request, whereas a deny is an active refusal.
    /// </remarks>
    NotApplicable,

    /// <summary>
    /// An error occurred during policy evaluation, preventing a definitive decision.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.1 — The PDP could not evaluate the request due to missing attributes,
    /// function errors, or other evaluation failures. Combining algorithms handle indeterminate
    /// results according to their specific semantics.
    /// </remarks>
    Indeterminate
}
