using Encina.Security.Audit;
using LanguageExt;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Persistence of break-the-glass elevations.
/// </summary>
/// <remarks>
/// <para>
/// Every operation is tenant-scoped (a <see langword="null"/> tenant means tenancy is off) except
/// the two sweeper operations, which run across tenants. Status and review changes are
/// <b>conditional</b>: they apply once, so several hosts can run the sweeper and several reviewers
/// can race without a distributed lock.
/// </para>
/// <para>Every method returns <c>Left</c> on a store failure and never lets a provider exception escape.</para>
/// </remarks>
public interface IBreakTheGlassStore
{
    /// <summary>Adds a new elevation only when no effective elevation exists for the same tenant, user, subject and scope.</summary>
    /// <remarks>
    /// The add is an <b>atomic conditional insert</b>: the check and the write cannot interleave with a
    /// concurrent add, whatever the host or the process. An elevation is effective for this check when its
    /// status is Active and its <c>ExpiresAtUtc</c> is later than <c>elevation.OpenedAtUtc</c>. Every provider
    /// MUST guarantee this itself (for example with a unique constraint or an atomic upsert); a read followed
    /// by a write is not enough.
    /// </remarks>
    /// <param name="elevation">The elevation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit on success; <c>break_glass.already_active</c> (nothing written) when an effective elevation exists.</returns>
    ValueTask<Either<EncinaError, Unit>> AddAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken = default);

    /// <summary>Gets an elevation by id.</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="elevationId">The elevation id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The elevation, or None when the tenant has none with that id.</returns>
    ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> GetByIdAsync(string? tenantId, Guid elevationId, CancellationToken cancellationToken = default);

    /// <summary>Finds the elevation that grants access at <paramref name="nowUtc"/> (status Active and not expired).</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="userId">The user.</param>
    /// <param name="subjectId">The data subject.</param>
    /// <param name="scope">The scope.</param>
    /// <param name="nowUtc">The instant to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The effective elevation, or None.</returns>
    ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> FindEffectiveAsync(
        string? tenantId, string userId, string subjectId, string scope, DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Closes an elevation, only when it is still <see cref="BreakTheGlassStatus.Active"/>.</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="elevationId">The elevation id.</param>
    /// <param name="newStatus">The closing status (Expired or Revoked).</param>
    /// <param name="reason">A fixed reason code.</param>
    /// <param name="closedAtUtc">The closing instant.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when this call closed it; <see langword="false"/> when it was not active.</returns>
    ValueTask<Either<EncinaError, bool>> TryCloseAsync(
        string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Lists elevations that are Active past their expiry, or Expired without a recorded expiry (all tenants; sweeper only).</summary>
    /// <param name="nowUtc">The instant to compare with.</param>
    /// <param name="batchSize">The maximum number of rows.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows still to close or to record.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(
        DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Marks the expiry of an Expired elevation as recorded in the audit trail.</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="elevationId">The elevation id.</param>
    /// <param name="recordedAtUtc">The instant the audit entry was written.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Records a review, only when the elevation is still unreviewed.</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="elevationId">The elevation id.</param>
    /// <param name="outcome">The ruling.</param>
    /// <param name="reviewerUserId">The reviewer.</param>
    /// <param name="note">An optional note.</param>
    /// <param name="reviewedAtUtc">The review instant.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when this call recorded the review; <see langword="false"/> when it was already reviewed (or does not exist).</returns>
    ValueTask<Either<EncinaError, bool>> TryRecordReviewAsync(
        string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Queries the review queue: unreviewed elevations, active or closed, oldest first.</summary>
    /// <param name="tenantId">The tenant, or <see langword="null"/>.</param>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>One page.</returns>
    ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> QueryReviewQueueAsync(
        string? tenantId, BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default);

    /// <summary>Deletes elevations that are closed and reviewed, reviewed before the cutoff (all tenants).</summary>
    /// <param name="reviewedBeforeUtc">The cutoff.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows deleted.</returns>
    ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default);

    /// <summary>Checks that the store is reachable (health check).</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Unit when reachable.</returns>
    ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default);
}
