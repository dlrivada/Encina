using System.Collections.Concurrent;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Security.Audit;

/// <summary>
/// In-memory implementation of <see cref="IReadAuditStore"/> for testing and development scenarios.
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
/// <b>Not suitable for production</b>: Read audit entries are lost when the process restarts.
/// For production use, consider database-backed implementations (SQL Server, PostgreSQL, etc.)
/// or specialized audit logging services.
/// </para>
/// <para>
/// Thread-safe: Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> for concurrent access.
/// Registered as the default <see cref="IReadAuditStore"/> via <c>TryAddSingleton</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For testing
/// var store = new InMemoryReadAuditStore();
///
/// await store.LogReadAsync(entry, CancellationToken.None);
///
/// // Assert read audit entries were recorded
/// var entries = store.GetAllEntries();
/// entries.Should().ContainSingle(e => e.EntityType == "Patient");
/// </code>
/// </example>
public sealed class InMemoryReadAuditStore : IReadAuditStore
{
    private readonly ConcurrentDictionary<Guid, ReadAuditEntry> _entries = new();

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> LogReadAsync(
        ReadAuditEntry entry,
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
            _entries[entry.Id] = entry;
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(unit));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.EntityId == entityId)
            .OrderByDescending(e => e.AccessedAtUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
        string userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entries = _entries.Values
            .Where(e => e.UserId == userId)
            .Where(e => e.AccessedAtUtc >= fromUtc)
            .Where(e => e.AccessedAtUtc <= toUtc)
            .OrderByDescending(e => e.AccessedAtUtc)
            .ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
        ReadAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, PagedResult<ReadAuditEntry>>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Clamp(query.PageSize, 1, ReadAuditQuery.MaxPageSize);

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

        if (query.AccessMethod.HasValue)
        {
            filtered = filtered.Where(e => e.AccessMethod == query.AccessMethod.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Purpose))
        {
            filtered = filtered.Where(e =>
                e.Purpose is not null &&
                e.Purpose.Contains(query.Purpose, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            filtered = filtered.Where(e => e.CorrelationId == query.CorrelationId);
        }

        if (query.FromUtc.HasValue)
        {
            filtered = filtered.Where(e => e.AccessedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            filtered = filtered.Where(e => e.AccessedAtUtc <= query.ToUtc.Value);
        }

        var allResults = filtered.ToList();
        var totalCount = allResults.Count;

        var items = allResults
            .OrderByDescending(e => e.AccessedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = PagedResult<ReadAuditEntry>.Create(items, totalCount, pageNumber, pageSize);
        return ValueTask.FromResult<Either<EncinaError, PagedResult<ReadAuditEntry>>>(Right(result));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<Either<EncinaError, int>>(
                Left(EncinaError.New("Operation was cancelled")));
        }

        var entriesToPurge = _entries
            .Where(kvp => kvp.Value.AccessedAtUtc < olderThanUtc)
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
    /// Gets all read audit entries in the store.
    /// </summary>
    /// <returns>All recorded read audit entries.</returns>
    /// <remarks>
    /// Intended for testing and diagnostics only.
    /// Returns entries in no guaranteed order.
    /// </remarks>
    public IReadOnlyList<ReadAuditEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    /// <summary>
    /// Clears all read audit entries from the store.
    /// </summary>
    /// <remarks>
    /// Intended for testing only to reset state between tests.
    /// </remarks>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Gets the number of read audit entries in the store.
    /// </summary>
    public int Count => _entries.Count;
}
