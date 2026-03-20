using Encina.Compliance.AIAct.Model;

using LanguageExt;

namespace Encina.Compliance.AIAct.Abstractions;

/// <summary>
/// Enforces human oversight requirements for high-risk AI systems as mandated
/// by Article 14 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 14(1) requires high-risk AI systems to be designed and developed in such
/// a way that they can be effectively overseen by natural persons during the period
/// in which they are in use, including with appropriate human-machine interface tools.
/// </para>
/// <para>
/// Article 14(4) specifies that human oversight measures shall enable the individuals
/// to whom oversight is assigned to:
/// </para>
/// <list type="bullet">
/// <item>(a) Fully understand the capacities and limitations of the AI system</item>
/// <item>(b) Correctly interpret the high-risk AI system's output</item>
/// <item>(c) Decide not to use the AI system or disregard, override, or reverse its output</item>
/// <item>(d) Intervene on the operation of the system or interrupt it</item>
/// </list>
/// <para>
/// The default implementation (<c>DefaultHumanOversightEnforcer</c>) provides in-memory
/// enforcement. Full persistence of human decision records across 13 database providers
/// is implemented in child issue #839 ("AI Act Human Oversight &amp; Decision Records").
/// </para>
/// </remarks>
public interface IHumanOversightEnforcer
{
    /// <summary>
    /// Determines whether a given request requires human review before execution.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being evaluated.</typeparam>
    /// <param name="request">The request to evaluate for human oversight requirements.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the request requires human review (e.g., targets a high-risk system
    /// or is decorated with <c>[RequireHumanOversight]</c>); <c>false</c> otherwise;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> RequiresHumanReviewAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a human oversight decision for audit trail purposes.
    /// </summary>
    /// <param name="decision">The human decision record to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the decision
    /// could not be recorded.
    /// </returns>
    /// <remarks>
    /// The decision record captures the reviewer's identity, decision, rationale, and
    /// correlation to the original request — essential for demonstrating effective human
    /// oversight under Article 14 and for audit requirements under Article 12.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RecordHumanDecisionAsync(
        HumanDecisionRecord decision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a specific human decision has been approved.
    /// </summary>
    /// <param name="decisionId">The unique identifier of the decision to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if a human decision with the specified ID exists and was approved;
    /// <c>false</c> otherwise; or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used by the pipeline behavior to verify that human oversight has been completed
    /// before allowing a high-risk AI operation to proceed.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> HasHumanApprovalAsync(
        Guid decisionId,
        CancellationToken cancellationToken = default);
}
