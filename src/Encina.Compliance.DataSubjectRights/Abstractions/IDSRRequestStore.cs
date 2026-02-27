using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Store for managing Data Subject Rights request lifecycle and persistence.
/// </summary>
/// <remarks>
/// <para>
/// The DSR request store provides CRUD operations for <see cref="DSRRequest"/> records,
/// supporting the full request lifecycle: creation, identity verification, processing,
/// completion, rejection, extension, and expiration tracking.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store requests in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// <para>
/// Per GDPR Article 12(3), controllers must respond to data subject requests without undue
/// delay and within one month. The <see cref="GetOverdueRequestsAsync"/> method supports
/// compliance monitoring by identifying requests that have exceeded their deadline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a new DSR request
/// var request = DSRRequest.Create("req-001", "subject-123", DataSubjectRight.Erasure, DateTimeOffset.UtcNow);
/// await store.CreateAsync(request, cancellationToken);
///
/// // Check for overdue requests
/// var overdue = await store.GetOverdueRequestsAsync(cancellationToken);
/// </code>
/// </example>
public interface IDSRRequestStore
{
    /// <summary>
    /// Creates a new DSR request record.
    /// </summary>
    /// <param name="request">The DSR request to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the request
    /// could not be stored (e.g., duplicate ID).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CreateAsync(
        DSRRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a DSR request by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(request)</c> if a request with the given ID exists,
    /// <c>None</c> if no request is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<DSRRequest>>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests for a specific data subject.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all DSR requests for the subject, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no requests exist for the subject.
    /// </returns>
    /// <remarks>
    /// Useful for building a request history dashboard where data subjects or administrators
    /// can review all past and pending requests.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an existing DSR request.
    /// </summary>
    /// <param name="id">The unique identifier of the request to update.</param>
    /// <param name="newStatus">The new status to set.</param>
    /// <param name="reason">Optional reason for the status change (e.g., rejection reason, extension justification).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the request was not
    /// found or the status transition is invalid.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string id,
        DSRRequestStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests that are currently pending (not yet completed, rejected, or expired).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of pending DSR requests (status: Received, IdentityVerified, InProgress, or Extended),
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used for operational dashboards to monitor active request workload.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests that have exceeded their deadline without completion.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of overdue DSR requests, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Per Article 12(3), controllers must respond within one month (extendable by two months
    /// for complex requests). This method identifies requests that have exceeded their
    /// <see cref="DSRRequest.DeadlineAtUtc"/> (or <see cref="DSRRequest.ExtendedDeadlineAtUtc"/>
    /// if extended) and are still in a non-terminal status.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the specified data subject has an active processing restriction.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if an active restriction exists for the subject, <c>false</c> otherwise,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is designed to be lightweight and fast as it is called from the
    /// <c>RestrictionCheckPipelineBehavior</c> on every request decorated with
    /// <see cref="RestrictProcessingAttribute"/>.
    /// </para>
    /// <para>
    /// An active restriction exists when there is a DSR request of type
    /// <see cref="DataSubjectRight.Restriction"/> in a non-terminal status
    /// (Received, IdentityVerified, InProgress, or Extended).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DSR requests across all subjects.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all DSR requests, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and auditing. For large datasets, consider
    /// implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
