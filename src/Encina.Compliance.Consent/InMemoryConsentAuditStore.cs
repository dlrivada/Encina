using System.Collections.Concurrent;

using Encina.Compliance.Consent.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Consent;

/// <summary>
/// In-memory implementation of <see cref="IConsentAuditStore"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// the entry's <see cref="ConsentAuditEntry.Id"/>. Queries by subject and purpose use
/// LINQ filtering over the values collection.
/// </para>
/// <para>
/// <b>Not suitable for production</b>: Audit entries are lost when the process restarts.
/// For production use, consider database-backed implementations that ensure
/// GDPR Article 7(1) demonstrability with durable storage.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var store = new InMemoryConsentAuditStore(TimeProvider.System, logger);
///
/// var entry = new ConsentAuditEntry
/// {
///     Id = Guid.NewGuid(),
///     SubjectId = "user-123",
///     Purpose = ConsentPurposes.Marketing,
///     Action = ConsentAuditAction.Granted,
///     OccurredAtUtc = DateTimeOffset.UtcNow,
///     PerformedBy = "user-123",
///     Metadata = new Dictionary&lt;string, object?&gt;()
/// };
///
/// await store.RecordAsync(entry);
/// </code>
/// </example>
public sealed class InMemoryConsentAuditStore : IConsentAuditStore
{
    private readonly ConcurrentDictionary<Guid, ConsentAuditEntry> _entries = new();
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryConsentAuditStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConsentAuditStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for timestamp operations.</param>
    /// <param name="logger">Logger for structured audit store logging.</param>
    public InMemoryConsentAuditStore(
        TimeProvider timeProvider,
        ILogger<InMemoryConsentAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ConsentAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!_entries.TryAdd(entry.Id, entry))
        {
            // Update existing entry if ID already exists (idempotent)
            _entries[entry.Id] = entry;
        }

        _logger.AuditEntryRecorded(entry.SubjectId, entry.Action.ToString(), entry.Purpose);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>> GetAuditTrailAsync(
        string subjectId,
        string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);

        var filtered = _entries.Values
            .Where(e => e.SubjectId == subjectId);

        if (purpose is not null)
        {
            filtered = filtered.Where(e => e.Purpose.Equals(purpose, StringComparison.OrdinalIgnoreCase));
        }

        var result = filtered
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToList();

        _logger.AuditTrailFetched(subjectId, result.Count);

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ConsentAuditEntry>>>(
            Right<EncinaError, IReadOnlyList<ConsentAuditEntry>>(result));
    }

    /// <summary>
    /// Gets all audit entries in the store.
    /// </summary>
    /// <returns>All stored audit entries.</returns>
    /// <remarks>Intended for testing and diagnostics only.</remarks>
    public IReadOnlyList<ConsentAuditEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    /// <summary>
    /// Clears all audit entries from the store.
    /// </summary>
    /// <remarks>Intended for testing only to reset state between tests.</remarks>
    public void Clear()
    {
        _entries.Clear();
    }

    /// <summary>
    /// Gets the number of audit entries in the store.
    /// </summary>
    public int Count => _entries.Count;
}
