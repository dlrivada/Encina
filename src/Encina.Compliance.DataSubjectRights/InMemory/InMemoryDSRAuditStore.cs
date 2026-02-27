using System.Collections.Concurrent;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// In-memory implementation of <see cref="IDSRAuditStore"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// the DSR request ID, with each value being a thread-safe list of audit entries.
/// </para>
/// <para>
/// Entries are returned in chronological order by <see cref="DSRAuditEntry.OccurredAtUtc"/>
/// as required by the <see cref="IDSRAuditStore"/> contract.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Records are lost when the process restarts.
/// For production use, consider database-backed implementations via one of the 13 supported providers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryDSRAuditStore(logger);
///
/// var entry = new DSRAuditEntry
/// {
///     Id = Guid.NewGuid().ToString(),
///     DSRRequestId = "req-001",
///     Action = "ErasureExecuted",
///     Detail = "Erased 12 fields across 3 entities",
///     PerformedByUserId = "admin-456",
///     OccurredAtUtc = DateTimeOffset.UtcNow
/// };
///
/// await store.RecordAsync(entry);
/// </code>
/// </example>
public sealed class InMemoryDSRAuditStore : IDSRAuditStore
{
    private readonly ConcurrentDictionary<string, List<DSRAuditEntry>> _entries = new();
    private readonly ILogger<InMemoryDSRAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDSRAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured DSR audit store logging.</param>
    public InMemoryDSRAuditStore(ILogger<InMemoryDSRAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var list = _entries.GetOrAdd(entry.DSRRequestId, _ => []);

        lock (list)
        {
            list.Add(entry);
        }

        _logger.LogDebug(
            "DSR audit entry recorded for request '{RequestId}': {Action}",
            entry.DSRRequestId,
            entry.Action);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsrRequestId);

        IReadOnlyList<DSRAuditEntry> result;

        if (_entries.TryGetValue(dsrRequestId, out var list))
        {
            lock (list)
            {
                result = list
                    .OrderBy(e => e.OccurredAtUtc)
                    .ToList()
                    .AsReadOnly();
            }
        }
        else
        {
            result = [];
        }

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>>(Right(result));
    }

    /// <summary>
    /// Gets all audit entries in the store.
    /// </summary>
    /// <returns>All stored audit entries.</returns>
    /// <remarks>Intended for testing and diagnostics only.</remarks>
    public IReadOnlyList<DSRAuditEntry> GetAllEntries() =>
        _entries.Values
            .SelectMany(list =>
            {
                lock (list)
                {
                    return list.ToList();
                }
            })
            .OrderBy(e => e.OccurredAtUtc)
            .ToList();

    /// <summary>
    /// Clears all audit entries from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear() => _entries.Clear();

    /// <summary>
    /// Gets the total number of audit entries across all requests.
    /// </summary>
    public int Count => _entries.Values.Sum(list =>
    {
        lock (list)
        {
            return list.Count;
        }
    });
}
