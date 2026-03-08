using LanguageExt;

namespace Encina.Security.ABAC;

/// <summary>
/// Handles the execution of XACML obligations after a policy decision has been made.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.18 — Obligations are mandatory actions that the Policy Enforcement Point (PEP)
/// must execute after the PDP returns a decision. If any obligation handler returns an error,
/// the PEP <strong>must deny access</strong> regardless of the PDP's original decision.
/// </para>
/// <para>
/// Each implementation handles one or more specific obligation types, identified by
/// <see cref="Obligation.Id"/>. The PEP iterates through all obligations in the
/// <see cref="PolicyDecision"/>, finds matching handlers via <see cref="CanHandle"/>,
/// and executes them.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class AuditLogObligationHandler : IObligationHandler
/// {
///     public bool CanHandle(string obligationId)
///         => obligationId == "audit-log";
///
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(
///         Obligation obligation, PolicyEvaluationContext context, CancellationToken ct)
///     {
///         await _auditService.LogAccessAsync(context, ct);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface IObligationHandler
{
    /// <summary>
    /// Determines whether this handler can process the obligation with the specified identifier.
    /// </summary>
    /// <param name="obligationId">The obligation identifier to check.</param>
    /// <returns><c>true</c> if this handler can process the obligation; otherwise, <c>false</c>.</returns>
    bool CanHandle(string obligationId);

    /// <summary>
    /// Executes the obligation action.
    /// </summary>
    /// <param name="obligation">The obligation to handle, including its attribute assignments.</param>
    /// <param name="context">The evaluation context that produced the obligation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// <c>Unit</c> on success, or an <see cref="EncinaError"/> on failure.
    /// If this returns an error, the PEP must deny access per XACML 3.0 §7.18.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);
}
