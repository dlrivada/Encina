using Encina.Compliance.DataSubjectRights.Projections;
using LanguageExt;

namespace Encina.Compliance.DataSubjectRights.Abstractions;

/// <summary>
/// Unified service interface for managing Data Subject Rights request lifecycle,
/// handler operations, and queries via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the previous <c>IDSRRequestStore</c>, <c>IDSRAuditStore</c>, and
/// <c>IDataSubjectRightsHandler</c> with a single cohesive API backed by Marten event sourcing.
/// The event stream itself serves as the immutable audit trail, eliminating the need for a
/// separate audit store.
/// </para>
/// <para>
/// <b>Lifecycle Commands</b> (write-side via <c>DSRRequestAggregate</c>):
/// <list type="bullet">
///   <item><description><see cref="SubmitRequestAsync"/> — Creates a new DSR request (30-day clock starts)</description></item>
///   <item><description><see cref="VerifyIdentityAsync"/> — Records identity verification (Article 12(6))</description></item>
///   <item><description><see cref="StartProcessingAsync"/> — Begins executing the requested right</description></item>
///   <item><description><see cref="CompleteRequestAsync"/> — Marks request as fulfilled</description></item>
///   <item><description><see cref="DenyRequestAsync"/> — Rejects request with reason (Article 12(4))</description></item>
///   <item><description><see cref="ExtendDeadlineAsync"/> — Extends deadline by up to 2 months (Article 12(3))</description></item>
///   <item><description><see cref="ExpireRequestAsync"/> — Marks request as expired (compliance violation)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Handler Operations</b> (orchestrate right-specific processing):
/// <list type="bullet">
///   <item><description><see cref="HandleAccessAsync"/> — Locates personal data (Article 15)</description></item>
///   <item><description><see cref="HandleRectificationAsync"/> — Records rectification audit trail (Article 16)</description></item>
///   <item><description><see cref="HandleErasureAsync"/> — Executes data erasure (Article 17)</description></item>
///   <item><description><see cref="HandleRestrictionAsync"/> — Applies processing restriction (Article 18)</description></item>
///   <item><description><see cref="HandlePortabilityAsync"/> — Exports data in portable format (Article 20)</description></item>
///   <item><description><see cref="HandleObjectionAsync"/> — Records processing objection (Article 21)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read-side via <c>DSRRequestReadModel</c>):
/// <list type="bullet">
///   <item><description><see cref="GetRequestAsync"/> — Retrieves a request by ID</description></item>
///   <item><description><see cref="GetRequestsBySubjectAsync"/> — Lists all requests for a subject</description></item>
///   <item><description><see cref="GetPendingRequestsAsync"/> — Lists active requests</description></item>
///   <item><description><see cref="GetOverdueRequestsAsync"/> — Identifies compliance deadline violations</description></item>
///   <item><description><see cref="HasActiveRestrictionAsync"/> — Checks for active Article 18 restrictions</description></item>
///   <item><description><see cref="GetRequestHistoryAsync"/> — Returns raw event stream (audit trail)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDSRService
{
    // ========================================================================
    // Lifecycle commands (write-side via DSRRequestAggregate)
    // ========================================================================

    /// <summary>
    /// Submits a new Data Subject Rights request, starting the GDPR Article 12(3) response clock.
    /// </summary>
    /// <param name="subjectId">Stable identifier of the data subject submitting the request.</param>
    /// <param name="rightType">The GDPR right being exercised (Articles 15-22).</param>
    /// <param name="requestDetails">Additional context provided by the data subject, or <c>null</c>.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created DSR request aggregate.</returns>
    ValueTask<Either<EncinaError, Guid>> SubmitRequestAsync(
        string subjectId,
        DataSubjectRight rightType,
        string? requestDetails = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the data subject's identity has been verified.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="verifiedBy">Identifier of the person or system that verified the identity.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 12(6), the controller may request additional information necessary to
    /// confirm the identity of the data subject before processing the request.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> VerifyIdentityAsync(
        Guid requestId,
        string verifiedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts processing the DSR request.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="processedByUserId">Identifier of the user or system processing the request, or <c>null</c> for automated processing.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> StartProcessingAsync(
        Guid requestId,
        string? processedByUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the DSR request as completed successfully.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> CompleteRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Denies the DSR request with a stated reason.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="rejectionReason">Explanation of why the request is denied (required for Article 12(4)).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 12(4), the controller must inform the data subject of the reasons for
    /// not taking action, the possibility of lodging a complaint with a supervisory authority
    /// (Article 77), and the right to seek a judicial remedy (Article 79).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DenyRequestAsync(
        Guid requestId,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extends the response deadline by up to 2 additional months.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="extensionReason">Explanation of why additional time is needed (required for Article 12(3)).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 12(3), the controller may extend the response period by up to two
    /// further months, taking into account the complexity and number of requests.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ExtendDeadlineAsync(
        Guid requestId,
        string extensionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the DSR request as expired because the deadline passed without completion.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// An expired request indicates a potential GDPR compliance violation. Typically called
    /// by background processors or deadline monitoring services.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ExpireRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Handler operations (orchestrate right-specific processing)
    // ========================================================================

    /// <summary>
    /// Handles a data access request (Article 15 — Right of access by the data subject).
    /// </summary>
    /// <param name="request">The access request containing the subject identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="AccessResponse"/> containing all personal data locations and optionally
    /// processing activities, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, AccessResponse>> HandleAccessAsync(
        AccessRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data rectification request (Article 16 — Right to rectification).
    /// </summary>
    /// <param name="request">The rectification request specifying the field and new value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="Unit"/> on success, or an <see cref="EncinaError"/> on failure.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleRectificationAsync(
        RectificationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data erasure request (Article 17 — Right to erasure / "Right to be forgotten").
    /// </summary>
    /// <param name="request">The erasure request specifying the reason and optional scope.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="ErasureResult"/> detailing which fields were erased, retained, or failed,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, ErasureResult>> HandleErasureAsync(
        ErasureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a processing restriction request (Article 18 — Right to restriction of processing).
    /// </summary>
    /// <param name="request">The restriction request specifying the reason and optional scope.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="Unit"/> on success, or an <see cref="EncinaError"/> on failure.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleRestrictionAsync(
        RestrictionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles a data portability request (Article 20 — Right to data portability).
    /// </summary>
    /// <param name="request">The portability request specifying the format and optional category filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="PortabilityResponse"/> containing the exported data in the requested format,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, PortabilityResponse>> HandlePortabilityAsync(
        PortabilityRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an objection to processing request (Article 21 — Right to object).
    /// </summary>
    /// <param name="request">The objection request specifying the processing purpose and reason.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see cref="Unit"/> on success, or an <see cref="EncinaError"/> on failure.</returns>
    ValueTask<Either<EncinaError, Unit>> HandleObjectionAsync(
        ObjectionRequest request,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via DSRRequestReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a DSR request by its aggregate identifier.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the DSR request read model.</returns>
    ValueTask<Either<EncinaError, DSRRequestReadModel>> GetRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests for a specific data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of DSR request read models, or an empty list if none exist.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetRequestsBySubjectAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests that are currently pending (not yet completed, rejected, or expired).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A read-only list of pending DSR requests (status: Received, IdentityVerified, InProgress, or Extended).
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests that have exceeded their deadline without completion.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of overdue DSR requests.</returns>
    /// <remarks>
    /// Per Article 12(3), controllers must respond within one month (extendable by two months
    /// for complex requests). This method identifies requests that have exceeded their effective
    /// deadline and are still in a non-terminal status.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified data subject has an active processing restriction.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><c>true</c> if an active restriction exists, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Called by <c>RestrictionCheckPipelineBehavior</c> on every request decorated with
    /// <see cref="RestrictProcessingAttribute"/>. An active restriction exists when there is
    /// a DSR request of type <see cref="DataSubjectRight.Restriction"/> in a non-terminal status.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a DSR request aggregate.
    /// </summary>
    /// <param name="requestId">The DSR request aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events applied to this request,
    /// ordered chronologically. The event stream provides a complete audit trail
    /// for GDPR Article 5(2) accountability requirements, replacing the previous
    /// <c>IDSRAuditStore</c>.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetRequestHistoryAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);
}
