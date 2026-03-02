using System.Collections.Concurrent;

using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IRetentionAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="RetentionAuditEntry.Id"/>
/// with LINQ-based secondary filtering on <see cref="RetentionAuditEntry.EntityId"/>
/// and ordering by <see cref="RetentionAuditEntry.OccurredAtUtc"/>.
/// </para>
/// <para>
/// Per GDPR Article 5(2) (accountability), audit entries are append-only.
/// This store does not support deletion of individual entries.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryRetentionAuditStore : IRetentionAuditStore
{
    private readonly ConcurrentDictionary<string, RetentionAuditEntry> _entries = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryRetentionAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRetentionAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryRetentionAuditStore(ILogger<InMemoryRetentionAuditStore> logger)
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
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries[entry.Id] = entry;

        _logger.LogDebug(
            "Recorded audit entry '{EntryId}': {Action} for entity '{EntityId}'",
            entry.Id, entry.Action, entry.EntityId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<RetentionAuditEntry> result = _entries.Values
            .Where(e => e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RetentionAuditEntry> result = _entries.Values
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(result));
    }

    /// <summary>
    /// Returns all stored audit entries. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all audit entries in the store.</returns>
    public IReadOnlyList<RetentionAuditEntry> GetAllEntries() =>
        _entries.Values.OrderByDescending(e => e.OccurredAtUtc).ToList().AsReadOnly();

    /// <summary>
    /// Removes all audit entries from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _entries.Clear();
}
