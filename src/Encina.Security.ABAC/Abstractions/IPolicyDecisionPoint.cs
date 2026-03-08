namespace Encina.Security.ABAC;

/// <summary>
/// Policy Decision Point (PDP) — evaluates access requests against XACML policies
/// and returns authorization decisions.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.2 — The PDP is the core evaluation engine. It receives a
/// <see cref="PolicyEvaluationContext"/> containing all resolved attributes, evaluates
/// applicable policies, applies combining algorithms, and returns a <see cref="PolicyDecision"/>
/// with one of four possible effects: Permit, Deny, NotApplicable, or Indeterminate.
/// </para>
/// <para>
/// The PDP always returns a decision — it never throws exceptions for policy evaluation
/// failures. Instead, evaluation errors produce <see cref="Effect.Indeterminate"/> with
/// a <see cref="DecisionStatus"/> describing the problem.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new PolicyEvaluationContext
/// {
///     SubjectAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "admin" }),
///     ResourceAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "financial-report" }),
///     EnvironmentAttributes = AttributeBag.Empty,
///     ActionAttributes = AttributeBag.Of(
///         new AttributeValue { DataType = "string", Value = "read" }),
///     RequestType = typeof(GetReportQuery)
/// };
///
/// PolicyDecision decision = await pdp.EvaluateAsync(context);
/// if (decision.Effect == Effect.Permit)
/// {
///     // Process obligations, then allow access
/// }
/// </code>
/// </example>
public interface IPolicyDecisionPoint
{
    /// <summary>
    /// Evaluates the given attribute context against all applicable policies and returns
    /// an authorization decision.
    /// </summary>
    /// <param name="context">
    /// The evaluation context containing subject, resource, action, and environment attributes.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="PolicyDecision"/> containing the computed effect (Permit, Deny,
    /// NotApplicable, or Indeterminate), along with any obligations and advice.
    /// </returns>
    ValueTask<PolicyDecision> EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);
}
