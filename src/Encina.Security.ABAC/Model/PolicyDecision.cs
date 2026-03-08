namespace Encina.Security.ABAC;

/// <summary>
/// Represents the final authorization decision returned by the Policy Decision Point (PDP),
/// including the effect, obligations, advice, and diagnostic information.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.10 — The PolicyDecision is the primary output of the PDP evaluation.
/// It carries the computed <see cref="Effect"/>, any <see cref="Obligations"/> that the PEP
/// must fulfill, optional <see cref="Advice"/> that the PEP may act on, and diagnostic
/// information via <see cref="Status"/>.
/// </para>
/// <para>
/// The PEP (ABACPipelineBehavior) uses this decision to:
/// </para>
/// <list type="number">
/// <item><description>Check the <see cref="Effect"/> — if Deny, block the request</description></item>
/// <item><description>Execute <see cref="Obligations"/> matching the effect — if any fail, deny access</description></item>
/// <item><description>Process <see cref="Advice"/> — optional, failure does not affect the decision</description></item>
/// </list>
/// </remarks>
public sealed record PolicyDecision
{
    /// <summary>
    /// The authorization effect: Permit, Deny, NotApplicable, or Indeterminate.
    /// </summary>
    public required Effect Effect { get; init; }

    /// <summary>
    /// Optional status providing diagnostic information about the decision.
    /// </summary>
    /// <remarks>
    /// Typically present when the effect is <see cref="ABAC.Effect.Indeterminate"/>,
    /// providing details about what went wrong during evaluation.
    /// </remarks>
    public DecisionStatus? Status { get; init; }

    /// <summary>
    /// Mandatory obligations that the PEP must execute for the decision effect.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §7.18 — Only includes obligations whose <see cref="Obligation.FulfillOn"/>
    /// matches the <see cref="Effect"/>. If the PEP cannot fulfill any obligation,
    /// it must deny access regardless of the PDP decision.
    /// </remarks>
    public required IReadOnlyList<Obligation> Obligations { get; init; }

    /// <summary>
    /// Optional advice that the PEP may act on for the decision effect.
    /// </summary>
    /// <remarks>
    /// Only includes advice whose <see cref="AdviceExpression.AppliesTo"/> matches the
    /// <see cref="Effect"/>. Unlike obligations, the PEP may ignore advice.
    /// </remarks>
    public required IReadOnlyList<AdviceExpression> Advice { get; init; }

    /// <summary>
    /// The identifier of the policy that produced the final decision, if identifiable.
    /// </summary>
    public string? PolicyId { get; init; }

    /// <summary>
    /// The identifier of the rule that produced the final decision, if identifiable.
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// An optional human-readable explanation of why this decision was reached.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// The wall-clock time spent evaluating the policy to produce this decision.
    /// </summary>
    public required TimeSpan EvaluationDuration { get; init; }
}
