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
