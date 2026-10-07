using LanguageExt;

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
/// var context = AttributeContextBuilder.Build(
///     subjectAttributes: new Dictionary&lt;string, object&gt; { ["role"] = "admin" },
///     resourceAttributes: new Dictionary&lt;string, object&gt; { ["classification"] = "financial-report" },
///     environmentAttributes: new Dictionary&lt;string, object&gt;(),
///     requestType: typeof(GetReportQuery));
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
    /// NotApplicable, or Indeterminate), along with any obligations and advice. When the policy
    /// sets or the standalone policies cannot be read, the decision is
    /// <see cref="Effect.Indeterminate"/>: no decision is made on part of the policy store.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was cancelled by the caller. A cancellation that does
    /// not come from the caller (for example a store timeout) is an
    /// <see cref="Effect.Indeterminate"/> decision.
    /// </exception>
    ValueTask<PolicyDecision> EvaluateAsync(
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a single top-level policy set or standalone policy, identified by
    /// <paramref name="policyId"/>, against the given attribute context, without evaluating the
    /// rest of the policy store.
    /// </summary>
    /// <param name="policyId">
    /// The identifier of a top-level policy set or of a standalone policy (one that no policy set
    /// contains). A policy set with this identifier is evaluated in preference to a policy with
    /// the same identifier. A policy or policy set nested inside a policy set is not found by its
    /// own identifier, because evaluating it without its parent would skip the parent's enabled
    /// flag, target, combining algorithm and obligations; name the parent policy set instead.
    /// </param>
    /// <param name="context">
    /// The evaluation context containing subject, resource, action, and environment attributes.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Right</c> with the decision of that policy set or policy (Permit, Deny, NotApplicable or
    /// Indeterminate, with the obligations and advice that match its effect); a failure to read
    /// the policy store or to evaluate the policy is an <see cref="Effect.Indeterminate"/> decision.
    /// <c>Left</c> with code <see cref="ABACErrors.PolicyNotFoundCode"/> when the store holds no
    /// top-level policy set and no standalone policy with that identifier.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> was cancelled by the caller.
    /// </exception>
    /// <remarks>
    /// Used by <see cref="ABACPipelineBehavior{TRequest, TResponse}"/> to enforce
    /// <see cref="RequirePolicyAttribute"/>: the named policy decides on its own, so the
    /// root-level combination of <see cref="EvaluateAsync"/> does not apply.
    /// </remarks>
    ValueTask<Either<EncinaError, PolicyDecision>> EvaluatePolicyAsync(
        string policyId,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);
}
