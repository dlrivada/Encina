using System.Collections.Concurrent;

namespace Encina.DomainModeling.Auditing;

/// <summary>
/// An in-memory implementation of <see cref="IAuditLogStore"/> for testing and development purposes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Warning</b>: This implementation stores audit logs in memory only. All data is lost when
/// the application restarts. This is intended for testing, development, and prototyping only.
/// </para>
/// <para>
/// For production use, implement <see cref="IAuditLogStore"/> with a persistent store such as
/// a database, event store, or external audit service.
/// </para>
/// <para>
/// This implementation is thread-safe and can be used in concurrent scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration for testing
/// services.AddSingleton&lt;IAuditLogStore, InMemoryAuditLogStore&gt;();
///
/// // Usage
/// var store = new InMemoryAuditLogStore();
/// await store.LogAsync(new AuditLogEntry(
///     Id: Guid.NewGuid().ToString(),
///     EntityType: "Order",
///     EntityId: "123",
///     Action: AuditAction.Created,
///     UserId: "user-1",
///     TimestampUtc: DateTime.UtcNow,
///     OldValues: null,
///     NewValues: "{\"Total\": 100}",
///     CorrelationId: null));
///
/// var history = await store.GetHistoryAsync("Order", "123");
/// </code>
/// </example>
public sealed class InMemoryAuditLogStore : IAuditLogStore
{
    private readonly ConcurrentDictionary<(string EntityType, string EntityId), List<AuditLogEntry>> _entries = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var key = (entry.EntityType, entry.EntityId);

        lock (_lock)
        {
            var list = _entries.GetOrAdd(key, _ => []);
            list.Add(entry);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityType"/> or <paramref name="entityId"/> is <c>null</c>.</exception>
    public Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entityId);

        var key = (entityType, entityId);

        if (_entries.TryGetValue(key, out var list))
        {
            lock (_lock)
            {
                // Return a copy ordered by timestamp descending (most recent first)
                var result = list
                    .OrderByDescending(e => e.TimestampUtc)
                    .ToList();
                return Task.FromResult<IEnumerable<AuditLogEntry>>(result);
            }
        }

        return Task.FromResult<IEnumerable<AuditLogEntry>>([]);
    }

    /// <summary>
    /// Clears all stored audit log entries.
    /// </summary>
    /// <remarks>
    /// This method is useful for resetting state between tests.
    /// </remarks>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Gets the total count of audit log entries stored.
    /// </summary>
    /// <returns>The total number of entries across all entities.</returns>
    public int GetTotalCount()
    {
        lock (_lock)
        {
            return _entries.Values.Sum(list => list.Count);
        }
    }
}
