using System.Collections.Concurrent;

using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IBreachAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="BreachAuditEntry.Id"/>
/// with LINQ-based secondary filtering on <see cref="BreachAuditEntry.BreachId"/>
/// and ordering by <see cref="BreachAuditEntry.OccurredAtUtc"/>.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability) and Article 33(5) (documentation),
/// audit entries are append-only. This store does not support deletion of individual entries.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryBreachAuditStore : IBreachAuditStore
{
    private readonly ConcurrentDictionary<string, BreachAuditEntry> _entries = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryBreachAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBreachAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryBreachAuditStore(ILogger<InMemoryBreachAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of audit entries currently stored. Useful for testing assertions.
    /// </summary>
    public int Count => _entries.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries[entry.Id] = entry;

        _logger.LogDebug(
            "Recorded audit entry '{EntryId}': {Action} for breach '{BreachId}'",
            entry.Id, entry.Action, entry.BreachId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        IReadOnlyList<BreachAuditEntry> result = _entries.Values
            .Where(e => e.BreachId == breachId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<BreachAuditEntry>>(result));
    }

    /// <summary>
    /// Returns all stored audit entries. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all audit entries in the store.</returns>
    public IReadOnlyList<BreachAuditEntry> GetAllEntries() =>
        _entries.Values.OrderByDescending(e => e.OccurredAtUtc).ToList().AsReadOnly();

    /// <summary>
    /// Removes all audit entries from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _entries.Clear();
}
