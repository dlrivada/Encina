using System.Collections.Concurrent;
using Encina.Security.Audit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// In-memory <see cref="IBreakTheGlassStore"/> for development and tests.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not suitable for production</b>: elevations (and therefore the review queue) are lost when the
/// process restarts. Use a database store.
/// </para>
/// <para>
/// Thread-safe. Conditional updates are compare-and-swap loops on the row, so a close or a review
/// applies once even when callers race. The store receives every timestamp from its caller and never
/// reads the clock.
/// </para>
/// </remarks>
public sealed class InMemoryBreakTheGlassStore : IBreakTheGlassStore
{
    private readonly ConcurrentDictionary<(string? TenantId, Guid ElevationId), BreakTheGlassElevation> _rows = new();
    private readonly object _addLock = new();

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddAsync(BreakTheGlassElevation elevation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(elevation);
        cancellationToken.ThrowIfCancellationRequested();

        // Check and insert under one lock: the conditional add is atomic.
        lock (_addLock)
        {
            if (_rows.Values.Any(row => ConflictsWith(row, elevation)))
            {
                return Done(Left<EncinaError, Unit>(BreakTheGlassErrors.AlreadyActive()));
            }

            _rows[(elevation.TenantId, elevation.ElevationId)] = elevation;
        }

        return Done(Right<EncinaError, Unit>(unit));
    }

    // Closed and reviewed; an Expired row also needs its expiry recorded, or the sweeper would never write its audit entry.
    private static bool IsPurgeable(BreakTheGlassElevation row, DateTimeOffset reviewedBeforeUtc) =>
        row.Status != BreakTheGlassStatus.Active
        && row.ReviewedAtUtc < reviewedBeforeUtc
        && (row.Status != BreakTheGlassStatus.Expired || row.ExpiryRecordedAtUtc is not null);

    private static bool ConflictsWith(BreakTheGlassElevation row, BreakTheGlassElevation added) =>
        row.TenantId == added.TenantId
        && string.Equals(row.UserId, added.UserId, StringComparison.Ordinal)
        && string.Equals(row.SubjectId, added.SubjectId, StringComparison.Ordinal)
        && string.Equals(row.Scope, added.Scope, StringComparison.Ordinal)
        && row.IsEffectiveAt(added.OpenedAtUtc);

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> GetByIdAsync(string? tenantId, Guid elevationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Done(Right<EncinaError, Option<BreakTheGlassElevation>>(
            _rows.TryGetValue((tenantId, elevationId), out var row) ? Some(row) : None));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreakTheGlassElevation>>> FindEffectiveAsync(
        string? tenantId, string userId, string subjectId, string scope, DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(subjectId);
        ArgumentNullException.ThrowIfNull(scope);
        cancellationToken.ThrowIfCancellationRequested();

        var found = _rows.Values
            .Where(row => row.TenantId == tenantId
                && string.Equals(row.UserId, userId, StringComparison.Ordinal)
                && string.Equals(row.SubjectId, subjectId, StringComparison.Ordinal)
                && string.Equals(row.Scope, scope, StringComparison.Ordinal)
                && row.IsEffectiveAt(nowUtc))
            .OrderByDescending(row => row.OpenedAtUtc)
            .FirstOrDefault();

        return Done(Right<EncinaError, Option<BreakTheGlassElevation>>(found is null ? None : Some(found)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryCloseAsync(
        string? tenantId, Guid elevationId, BreakTheGlassStatus newStatus, string reason, DateTimeOffset closedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reason);
        cancellationToken.ThrowIfCancellationRequested();

        var applied = TryUpdate(
            (tenantId, elevationId),
            row => row.Status == BreakTheGlassStatus.Active,
            row => row with { Status = newStatus, CloseReason = reason, ClosedAtUtc = closedAtUtc });

        return Done(Right<EncinaError, bool>(applied));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreakTheGlassElevation>>> GetExpiredUnrecordedAsync(
        DateTimeOffset nowUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<BreakTheGlassElevation> rows = _rows.Values
            .Where(row => IsPendingExpiry(row, nowUtc))
            .OrderBy(row => row.ExpiresAtUtc)
            .ThenBy(row => row.ElevationId)
            .Take(batchSize)
            .ToList();

        return Done(Right<EncinaError, IReadOnlyList<BreakTheGlassElevation>>(rows));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> MarkExpiryRecordedAsync(string? tenantId, Guid elevationId, DateTimeOffset recordedAtUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TryUpdate(
            (tenantId, elevationId),
            row => row.Status == BreakTheGlassStatus.Expired,
            row => row with { ExpiryRecordedAtUtc = recordedAtUtc });

        return Done(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> TryRecordReviewAsync(
        string? tenantId, Guid elevationId, BreakTheGlassReviewOutcome outcome, string reviewerUserId, string? note, DateTimeOffset reviewedAtUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewerUserId);
        cancellationToken.ThrowIfCancellationRequested();

        var applied = TryUpdate(
            (tenantId, elevationId),
            row => row.ReviewOutcome is null,
            row => row with { ReviewOutcome = outcome, ReviewedByUserId = reviewerUserId, ReviewNote = note, ReviewedAtUtc = reviewedAtUtc });

        return Done(Right<EncinaError, bool>(applied));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, PagedResult<BreakTheGlassElevation>>> QueryReviewQueueAsync(
        string? tenantId, BreakTheGlassReviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, BreakTheGlassReviewQuery.MaxPageSize);

        var matching = _rows.Values
            .Where(row => row.TenantId == tenantId && row.ReviewOutcome is null && MatchesQuery(row, query))
            .OrderBy(row => row.OpenedAtUtc)
            .ThenBy(row => row.ElevationId)
            .ToList();

        var items = matching.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Done(Right<EncinaError, PagedResult<BreakTheGlassElevation>>(
            PagedResult<BreakTheGlassElevation>.Create(items, matching.Count, pageNumber, pageSize)));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, int>> PurgeAsync(DateTimeOffset reviewedBeforeUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removed = 0;
        foreach (var (key, row) in _rows)
        {
            if (IsPurgeable(row, reviewedBeforeUtc) && _rows.TryRemove(new KeyValuePair<(string?, Guid), BreakTheGlassElevation>(key, row)))
            {
                removed++;
            }
        }

        return Done(Right<EncinaError, int>(removed));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Done(Right<EncinaError, Unit>(unit));
    }

    private static bool IsPendingExpiry(BreakTheGlassElevation row, DateTimeOffset nowUtc) =>
        (row.Status == BreakTheGlassStatus.Active && nowUtc >= row.ExpiresAtUtc)
        || (row.Status == BreakTheGlassStatus.Expired && row.ExpiryRecordedAtUtc is null);

    private static bool MatchesQuery(BreakTheGlassElevation row, BreakTheGlassReviewQuery query) =>
        MatchesScopeAndStatus(row, query) && MatchesWindow(row, query);

    private static bool MatchesScopeAndStatus(BreakTheGlassElevation row, BreakTheGlassReviewQuery query) =>
        (query.Scope is null || string.Equals(row.Scope, query.Scope, StringComparison.Ordinal))
        && (query.Status is null || row.Status == query.Status);

    private static bool MatchesWindow(BreakTheGlassElevation row, BreakTheGlassReviewQuery query) =>
        (query.OpenedFromUtc is null || row.OpenedAtUtc >= query.OpenedFromUtc)
        && (query.OpenedToUtc is null || row.OpenedAtUtc < query.OpenedToUtc);

    // Applies the update once: false when the row is missing or the condition no longer holds.
    private bool TryUpdate(
        (string? TenantId, Guid ElevationId) key,
        Func<BreakTheGlassElevation, bool> condition,
        Func<BreakTheGlassElevation, BreakTheGlassElevation> update)
    {
        while (_rows.TryGetValue(key, out var current))
        {
            if (!condition(current))
            {
                return false;
            }

            if (_rows.TryUpdate(key, update(current), current))
            {
                return true;
            }
        }

        return false;
    }

    private static ValueTask<Either<EncinaError, T>> Done<T>(Either<EncinaError, T> result) => ValueTask.FromResult(result);
}
