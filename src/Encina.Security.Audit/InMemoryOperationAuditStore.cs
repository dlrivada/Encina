using System.Collections.Concurrent;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.Audit;

/// <summary>
/// In-memory implementation of <see cref="IOperationAuditStore"/> for testing and development scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This store is designed for:
/// <list type="bullet">
/// <item>Unit and integration testing</item>
/// <item>Development and local debugging</item>
/// <item>Single-instance applications with no persistence requirements</item>
/// </list>
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Audit entries are lost when the process restarts.
/// For production use, consider database-backed implementations (SQL Server, PostgreSQL, etc.)
/// or specialized audit logging services.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For testing
/// var store = new InMemoryOperationAuditStore();
///
/// await store.RecordAsync(entry, CancellationToken.None);
///
/// // Assert audit entries were recorded
/// var entries = store.GetAllEntries();
/// entries.Should().ContainSingle(e => e.Action == "Create");
/// </code>
/// </example>
public sealed class InMemoryOperationAuditStore : IOperationAuditStore
{
    private readonly ConcurrentDictionary<Guid, OperationAuditEntry> _entries = new();

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        OperationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        if (!_entries.TryAdd(entry.Id, entry))
        {
            // Update existing entry if ID already exists
            _entries[entry.Id] = entry;
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            .Where(e => entityId is null || e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.UserId == userId)
            .Where(e => fromUtc is null || e.TimestampUtc >= fromUtc.Value)
            .Where(e => toUtc is null || e.TimestampUtc <= toUtc.Value)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>>(Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(
        OperationAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, PagedResult<OperationAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        // Validate pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, OperationAuditQuery.MaxPageSize);

        // Apply all filters
        var filtered = ApplyFilters(_entries.Values, query);

        // Get total count before pagination
        var allResults = filtered.ToList();
        var totalCount = allResults.Count;

        // Apply ordering and pagination
        var items = allResults
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = PagedResult<OperationAuditEntry>.Create(items, totalCount, pageNumber, pageSize);
        return ValueTask.FromResult<Either<EncinaError, PagedResult<OperationAuditEntry>>>(Right(result));
    }

    private static IEnumerable<OperationAuditEntry> ApplyFilters(
        IEnumerable<OperationAuditEntry> entries,
        OperationAuditQuery query)
    {
        var filtered = entries;
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.UserId), e => e.UserId == query.UserId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.TenantId), e => e.TenantId == query.TenantId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.EntityType), e => e.EntityType.Equals(query.EntityType, StringComparison.OrdinalIgnoreCase));
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.EntityId), e => e.EntityId == query.EntityId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.Action), e => e.Action.Equals(query.Action, StringComparison.OrdinalIgnoreCase));
        filtered = WhereIf(filtered, query.Outcome.HasValue, e => e.Outcome == query.Outcome!.Value);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.CorrelationId), e => e.CorrelationId == query.CorrelationId);
        filtered = WhereIf(filtered, query.FromUtc.HasValue, e => e.TimestampUtc >= query.FromUtc!.Value);
        filtered = WhereIf(filtered, query.ToUtc.HasValue, e => e.TimestampUtc <= query.ToUtc!.Value);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.IpAddress), e => e.IpAddress == query.IpAddress);
        filtered = WhereIf(filtered, query.MinDuration.HasValue, e => e.Duration >= query.MinDuration!.Value);
        return WhereIf(filtered, query.MaxDuration.HasValue, e => e.Duration <= query.MaxDuration!.Value);
    }

    private static IEnumerable<OperationAuditEntry> WhereIf(
        IEnumerable<OperationAuditEntry> source,
        bool condition,
        Func<OperationAuditEntry, bool> predicate) =>
        condition ? source.Where(predicate) : source;

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, int>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entriesToPurge = _entries
            .Where(kvp => kvp.Value.TimestampUtc < olderThanUtc)
            .Select(kvp => kvp.Key)
            .ToList();

        var purgedCount = 0;
        foreach (var id in entriesToPurge)
        {
            if (_entries.TryRemove(id, out _))
            {
                purgedCount++;
            }
        }

        return ValueTask.FromResult<Either<EncinaError, int>>(Right(purgedCount));
    }

    /// <summary>
    /// Gets all audit entries in the store.
    /// </summary>
    /// <returns>All recorded audit entries.</returns>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// Returns entries in no guaranteed order.
    /// </remarks>
    public IReadOnlyList<OperationAuditEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    /// <summary>
    /// Clears all audit entries from the store.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests.
    /// </remarks>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Gets the number of audit entries in the store.
    /// </summary>
    public int Count => _entries.Count;
}
