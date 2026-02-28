using System.Collections.Concurrent;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IAnonymizationAuditStore"/> for testing and development.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> and are
/// lost when the process terminates. For production use, implement
/// <see cref="IAnonymizationAuditStore"/> backed by a persistent database.
/// </para>
/// <para>
/// Audit entries are immutable by design and should never be modified or deleted
/// once recorded, as they serve as legal evidence of data protection measures.
/// </para>
/// </remarks>
public sealed class InMemoryAnonymizationAuditStore : IAnonymizationAuditStore
{
    private readonly ConcurrentDictionary<string, AnonymizationAuditEntry> _entries = new();

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> AddEntryAsync(
        AnonymizationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _entries[entry.Id] = entry;

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subjectId);

        var entries = _entries.Values
            .Where(e => e.SubjectId == subjectId)
            .OrderByDescending(e => e.PerformedAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>(entries));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = _entries.Values
            .OrderByDescending(e => e.PerformedAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<AnonymizationAuditEntry>>(entries));
    }

    /// <summary>
    /// Gets the total number of audit entries in the store (for testing).
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Retrieves all audit entries (for testing).
    /// </summary>
    public IReadOnlyList<AnonymizationAuditEntry> GetAllEntries() =>
        _entries.Values
            .OrderByDescending(e => e.PerformedAtUtc)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Clears all audit entries from the store (for testing).
    /// </summary>
    public void Clear() => _entries.Clear();
}
