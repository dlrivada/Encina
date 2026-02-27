using System.Collections.Concurrent;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// In-memory implementation of <see cref="IDSRRequestStore"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// DSR requests are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// the request ID, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// The store uses <see cref="TimeProvider"/> for testable time-based operations such as
/// deadline expiration checking. Inject a custom <see cref="TimeProvider"/> in tests to control time.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Records are lost when the process restarts.
/// For production use, consider database-backed implementations via one of the 13 supported providers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryDSRRequestStore(TimeProvider.System, logger);
///
/// var request = DSRRequest.Create("req-001", "subject-123", DataSubjectRight.Erasure, DateTimeOffset.UtcNow);
/// await store.CreateAsync(request);
///
/// var overdue = await store.GetOverdueRequestsAsync();
/// </code>
/// </example>
public sealed class InMemoryDSRRequestStore : IDSRRequestStore
{
    private readonly ConcurrentDictionary<string, DSRRequest> _requests = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryDSRRequestStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDSRRequestStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for deadline calculations and expiration checks.</param>
    /// <param name="logger">Logger for structured DSR request store logging.</param>
    public InMemoryDSRRequestStore(
        TimeProvider timeProvider,
        ILogger<InMemoryDSRRequestStore> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> CreateAsync(
        DSRRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_requests.TryAdd(request.Id, request))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                DSRErrors.StoreError("Create", $"A DSR request with ID '{request.Id}' already exists."));
        }

        _logger.LogDebug("DSR request '{RequestId}' created for subject '{SubjectId}', right: {RightType}",
            request.Id, request.SubjectId, request.RightType);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<DSRRequest>>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Option<DSRRequest> result = _requests.TryGetValue(id, out var request)
            ? Some(request)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<DSRRequest>>>(
            Right<EncinaError, Option<DSRRequest>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        IReadOnlyList<DSRRequest> result = _requests.Values
            .Where(r => r.SubjectId == subjectId)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DSRRequest>>>(Right(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string id,
        DSRRequestStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_requests.TryGetValue(id, out var existing))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                DSRErrors.RequestNotFound(id));
        }

        var updated = newStatus switch
        {
            DSRRequestStatus.Completed => existing with
            {
                Status = newStatus,
                CompletedAtUtc = _timeProvider.GetUtcNow()
            },
            DSRRequestStatus.Rejected => existing with
            {
                Status = newStatus,
                RejectionReason = reason,
                CompletedAtUtc = _timeProvider.GetUtcNow()
            },
            DSRRequestStatus.Extended => existing with
            {
                Status = newStatus,
                ExtensionReason = reason,
                ExtendedDeadlineAtUtc = existing.DeadlineAtUtc.AddMonths(2)
            },
            DSRRequestStatus.IdentityVerified => existing with
            {
                Status = newStatus,
                VerifiedAtUtc = _timeProvider.GetUtcNow()
            },
            _ => existing with { Status = newStatus }
        };

        _requests[id] = updated;

        _logger.LogDebug("DSR request '{RequestId}' status updated to {NewStatus}", id, newStatus);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DSRRequest> result = _requests.Values
            .Where(IsPending)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DSRRequest>>>(Right(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        IReadOnlyList<DSRRequest> result = _requests.Values
            .Where(r => IsPending(r) && GetEffectiveDeadline(r) < now)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DSRRequest>>>(Right(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        var hasRestriction = _requests.Values
            .Any(r => r.SubjectId == subjectId
                && r.RightType == DataSubjectRight.Restriction
                && IsPending(r));

        return ValueTask.FromResult<Either<EncinaError, bool>>(
            Right<EncinaError, bool>(hasRestriction));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DSRRequest> result = _requests.Values
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DSRRequest>>>(Right(result));
    }

    /// <summary>
    /// Gets all DSR requests in the store.
    /// </summary>
    /// <returns>All stored DSR requests.</returns>
    /// <remarks>Intended for testing and diagnostics only.</remarks>
    public IReadOnlyList<DSRRequest> GetAllRecords() =>
        _requests.Values.ToList();

    /// <summary>
    /// Clears all DSR requests from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear() => _requests.Clear();

    /// <summary>
    /// Gets the number of DSR requests in the store.
    /// </summary>
    public int Count => _requests.Count;

    private static bool IsPending(DSRRequest request) =>
        request.Status is DSRRequestStatus.Received
            or DSRRequestStatus.IdentityVerified
            or DSRRequestStatus.InProgress
            or DSRRequestStatus.Extended;

    private static DateTimeOffset GetEffectiveDeadline(DSRRequest request) =>
        request.ExtendedDeadlineAtUtc ?? request.DeadlineAtUtc;
}
