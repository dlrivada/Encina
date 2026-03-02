using System.Collections.Concurrent;

using Encina.Compliance.Retention.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Retention.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IRetentionRecordStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="RetentionRecord.Id"/>
/// with LINQ-based secondary indexes on <see cref="RetentionRecord.EntityId"/>,
/// <see cref="RetentionRecord.ExpiresAtUtc"/>, and <see cref="RetentionRecord.Status"/>
/// for query operations.
/// </para>
/// <para>
/// Time-based queries use <see cref="TimeProvider.GetUtcNow()"/> for deterministic testability.
/// </para>
/// <para>
/// This store is not intended for production use. All data is lost when the process exits.
/// For production, use a database-backed implementation (ADO.NET, Dapper, EF Core, or MongoDB).
/// </para>
/// </remarks>
public sealed class InMemoryRetentionRecordStore : IRetentionRecordStore
{
    private readonly ConcurrentDictionary<string, RetentionRecord> _records = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryRetentionRecordStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRetentionRecordStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for deterministic time-based queries.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryRetentionRecordStore(
        TimeProvider timeProvider,
        ILogger<InMemoryRetentionRecordStore> logger)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of records currently stored. Useful for testing assertions.
    /// </summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!_records.TryAdd(record.Id, record))
        {
            _logger.LogWarning("Retention record '{RecordId}' already exists", record.Id);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.RecordAlreadyExists(record.Id)));
        }

        _logger.LogDebug(
            "Created retention record '{RecordId}' for entity '{EntityId}' in category '{DataCategory}'",
            record.Id, record.EntityId, record.DataCategory);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<RetentionRecord>>> GetByIdAsync(
        string recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        var result = _records.TryGetValue(recordId, out var record)
            ? Some(record)
            : Option<RetentionRecord>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<RetentionRecord>>>(
            Right<EncinaError, Option<RetentionRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        IReadOnlyList<RetentionRecord> result = _records.Values
            .Where(r => r.EntityId == entityId)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionRecord>>>(
            Right<EncinaError, IReadOnlyList<RetentionRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        IReadOnlyList<RetentionRecord> result = _records.Values
            .Where(r => r.ExpiresAtUtc < now && r.Status == RetentionStatus.Active)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionRecord>>>(
            Right<EncinaError, IReadOnlyList<RetentionRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiringWithinAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var deadline = now + within;

        IReadOnlyList<RetentionRecord> result = _records.Values
            .Where(r => r.ExpiresAtUtc >= now && r.ExpiresAtUtc <= deadline && r.Status == RetentionStatus.Active)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionRecord>>>(
            Right<EncinaError, IReadOnlyList<RetentionRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string recordId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        if (!_records.TryGetValue(recordId, out var existing))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(RetentionErrors.RecordNotFound(recordId)));
        }

        var updated = existing with { Status = newStatus };

        // If transitioning to Deleted, set the deletion timestamp
        if (newStatus == RetentionStatus.Deleted)
        {
            updated = updated with { DeletedAtUtc = _timeProvider.GetUtcNow() };
        }

        _records[recordId] = updated;
        _logger.LogDebug(
            "Updated retention record '{RecordId}' status from '{OldStatus}' to '{NewStatus}'",
            recordId, existing.Status, newStatus);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<RetentionRecord> result = _records.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<RetentionRecord>>>(
            Right<EncinaError, IReadOnlyList<RetentionRecord>>(result));
    }

    /// <summary>
    /// Returns all stored records. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all records in the store.</returns>
    public IReadOnlyList<RetentionRecord> GetAllRecords() =>
        _records.Values.ToList().AsReadOnly();

    /// <summary>
    /// Removes all records from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _records.Clear();
}
