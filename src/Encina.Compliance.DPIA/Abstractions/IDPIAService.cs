using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using LanguageExt;

namespace Encina.Compliance.DPIA.Abstractions;

/// <summary>
/// Service interface for managing DPIA assessment lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for creating, evaluating, approving, rejecting, and querying DPIA assessments.
/// The implementation wraps the event-sourced <c>DPIAAggregate</c> via
/// <c>IAggregateRepository&lt;DPIAAggregate&gt;</c> and queries projected read models via
/// <c>IReadModelRepository&lt;DPIAReadModel&gt;</c>.
/// </para>
/// <para>
/// Per GDPR Article 35, the controller must carry out a DPIA when processing is likely to result
/// in a high risk to the rights and freedoms of natural persons. This service manages the full
/// assessment lifecycle: creation, risk evaluation, DPO consultation, approval/rejection, revision,
/// and expiration.
/// </para>
/// </remarks>
public interface IDPIAService
{
    // ========================================================================
    // Write operations (aggregate commands)
    // ========================================================================

    /// <summary>
    /// Creates a new DPIA assessment in Draft status.
    /// </summary>
    /// <param name="requestTypeName">Fully-qualified type name of the request this assessment covers.</param>
    /// <param name="processingType">Type of processing covered (e.g., "AutomatedDecisionMaking"), or <c>null</c>.</param>
    /// <param name="reason">Justification for conducting this assessment, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created assessment.</returns>
    ValueTask<Either<EncinaError, Guid>> CreateAssessmentAsync(
        string requestTypeName,
        string? processingType = null,
        string? reason = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates an assessment using the DPIA assessment engine, transitioning it to InReview status.
    /// </summary>
    /// <param name="assessmentId">The assessment to evaluate.</param>
    /// <param name="context">The DPIA context for risk evaluation.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the evaluation result.</returns>
    /// <remarks>
    /// Calls <see cref="IDPIAAssessmentEngine.AssessAsync"/> to produce the risk evaluation,
    /// then applies it to the aggregate via <see cref="Aggregates.DPIAAggregate.Evaluate"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, DPIAResult>> EvaluateAssessmentAsync(
        Guid assessmentId,
        DPIAContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a DPO consultation for an assessment in InReview status.
    /// </summary>
    /// <param name="assessmentId">The assessment requiring DPO consultation.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the consultation identifier.</returns>
    /// <remarks>
    /// Per Article 35(2), the controller must seek the DPO's advice when carrying out a DPIA.
    /// DPO contact information is resolved from <see cref="DPIAOptions"/> configuration.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> RequestDPOConsultationAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the DPO's response to a consultation request.
    /// </summary>
    /// <param name="assessmentId">The assessment that was consulted on.</param>
    /// <param name="consultationId">The consultation record being responded to.</param>
    /// <param name="decision">The DPO's decision on the assessment.</param>
    /// <param name="comments">Additional comments from the DPO, or <c>null</c>.</param>
    /// <param name="conditions">Conditions for conditional approval, or <c>null</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RecordDPOResponseAsync(
        Guid assessmentId,
        Guid consultationId,
        DPOConsultationDecision decision,
        string? comments = null,
        string? conditions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves an assessment, allowing the processing operation to proceed.
    /// </summary>
    /// <param name="assessmentId">The assessment to approve.</param>
    /// <param name="approvedBy">Identifier of the person approving the assessment.</param>
    /// <param name="nextReviewAtUtc">Scheduled date for the next periodic review (Art. 35(11)), or <c>null</c>.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ApproveAssessmentAsync(
        Guid assessmentId,
        string approvedBy,
        DateTimeOffset? nextReviewAtUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects an assessment; the processing operation must not proceed.
    /// </summary>
    /// <param name="assessmentId">The assessment to reject.</param>
    /// <param name="rejectedBy">Identifier of the person rejecting the assessment.</param>
    /// <param name="reason">Explanation of why the assessment was rejected.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RejectAssessmentAsync(
        Guid assessmentId,
        string rejectedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends the assessment back for revision before it can be approved.
    /// </summary>
    /// <param name="assessmentId">The assessment requiring revision.</param>
    /// <param name="requestedBy">Identifier of the person requesting revision.</param>
    /// <param name="reason">Explanation of what revisions are needed.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> RequestRevisionAsync(
        Guid assessmentId,
        string requestedBy,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires an approved assessment whose review date has passed.
    /// </summary>
    /// <param name="assessmentId">The assessment to expire.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ExpireAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Read operations (read model queries)
    // ========================================================================

    /// <summary>
    /// Retrieves an assessment by its unique identifier.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the assessment read model.</returns>
    ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent assessment for a given request type.
    /// </summary>
    /// <param name="requestTypeName">Fully-qualified type name of the request.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the assessment read model.</returns>
    ValueTask<Either<EncinaError, DPIAReadModel>> GetAssessmentByRequestTypeAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all assessments whose review date has passed.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of expired assessments.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetExpiredAssessmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DPIA assessments.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of all assessments.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the event history for a specific assessment.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the list of domain events for the assessment.</returns>
    /// <remarks>
    /// The event stream provides the full audit trail required by Article 5(2) accountability.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetAssessmentHistoryAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}
