using System.Collections.Concurrent;

using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataResidency.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IResidencyAuditStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="ResidencyAuditEntry.Id"/> with LINQ-based secondary filtering
/// on <see cref="ResidencyAuditEntry.EntityId"/> and ordering by
/// <see cref="ResidencyAuditEntry.TimestampUtc"/>.
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
public sealed class InMemoryResidencyAuditStore : IResidencyAuditStore
{
    private readonly ConcurrentDictionary<string, ResidencyAuditEntry> _entries = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryResidencyAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryResidencyAuditStore"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryResidencyAuditStore(ILogger<InMemoryResidencyAuditStore> logger)
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
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries[entry.Id] = entry;

        _logger.LogDebug(
            "Recorded residency audit entry '{EntryId}': {Action} ({Outcome}) for category '{DataCategory}'",
            entry.Id, entry.Action, entry.Outcome, entry.DataCategory);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<ResidencyAuditEntry> result = _entries.Values
            .Where(e => e.EntityId == entityId)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ResidencyAuditEntry> result = _entries.Values
            .Where(e => e.TimestampUtc >= fromUtc && e.TimestampUtc <= toUtc)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ResidencyAuditEntry> result = _entries.Values
            .Where(e => e.Outcome == ResidencyOutcome.Blocked)
            .OrderByDescending(e => e.TimestampUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(result));
    }

    /// <summary>
    /// Returns all stored audit entries. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all audit entries in the store.</returns>
    public IReadOnlyList<ResidencyAuditEntry> GetAllEntries() =>
        _entries.Values.OrderByDescending(e => e.TimestampUtc).ToList().AsReadOnly();

    /// <summary>
    /// Removes all audit entries from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _entries.Clear();
}
