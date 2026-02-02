using System.Collections.Concurrent;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.Audit;

/// <summary>
/// In-memory implementation of <see cref="IAuditStore"/> for testing and development scenarios.
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
/// var store = new InMemoryAuditStore();
///
/// await store.RecordAsync(entry, CancellationToken.None);
///
/// // Assert audit entries were recorded
/// var entries = store.GetAllEntries();
/// entries.Should().ContainSingle(e => e.Action == "Create");
/// </code>
/// </example>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentDictionary<Guid, AuditEntry> _entries = new();

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
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
    public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            .Where(e => entityId is null || e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(Right<EncinaError, IReadOnlyList<AuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.UserId == userId)
            .Where(e => fromUtc is null || e.TimestampUtc >= fromUtc.Value)
            .Where(e => toUtc is null || e.TimestampUtc <= toUtc.Value)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(Right<EncinaError, IReadOnlyList<AuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.TimestampUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AuditEntry>>>(Right<EncinaError, IReadOnlyList<AuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, PagedResult<AuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        // Validate pagination
        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, AuditQuery.MaxPageSize);

        // Apply all filters
        var filtered = _entries.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            filtered = filtered.Where(e => e.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            filtered = filtered.Where(e => e.TenantId == query.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            filtered = filtered.Where(e => e.EntityType.Equals(query.EntityType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            filtered = filtered.Where(e => e.EntityId == query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            filtered = filtered.Where(e => e.Action.Equals(query.Action, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Outcome.HasValue)
        {
            filtered = filtered.Where(e => e.Outcome == query.Outcome.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            filtered = filtered.Where(e => e.CorrelationId == query.CorrelationId);
        }

        if (query.FromUtc.HasValue)
        {
            filtered = filtered.Where(e => e.TimestampUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            filtered = filtered.Where(e => e.TimestampUtc <= query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            filtered = filtered.Where(e => e.IpAddress == query.IpAddress);
        }

        if (query.MinDuration.HasValue)
        {
            filtered = filtered.Where(e => e.Duration >= query.MinDuration.Value);
        }

        if (query.MaxDuration.HasValue)
        {
            filtered = filtered.Where(e => e.Duration <= query.MaxDuration.Value);
        }

        // Get total count before pagination
        var allResults = filtered.ToList();
        var totalCount = allResults.Count;

        // Apply ordering and pagination
        var items = allResults
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = PagedResult<AuditEntry>.Create(items, totalCount, pageNumber, pageSize);
        return ValueTask.FromResult<Either<EncinaError, PagedResult<AuditEntry>>>(Right(result));
    }

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
    public IReadOnlyList<AuditEntry> GetAllEntries()
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
