using System.Collections.Concurrent;

using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.BreachNotification.InMemory;

/// <summary>
/// In-memory implementation of <see cref="IBreachRecordStore"/> for development and testing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by <see cref="BreachRecord.Id"/>
/// with LINQ-based secondary indexes on <see cref="BreachRecord.Status"/>,
/// <see cref="BreachRecord.NotificationDeadlineUtc"/>, and <see cref="BreachRecord.NotifiedAuthorityAtUtc"/>
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
public sealed class InMemoryBreachRecordStore : IBreachRecordStore
{
    private readonly ConcurrentDictionary<string, BreachRecord> _records = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryBreachRecordStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryBreachRecordStore"/> class.
    /// </summary>
    /// <param name="timeProvider">Time provider for deterministic time-based queries.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    public InMemoryBreachRecordStore(
        TimeProvider timeProvider,
        ILogger<InMemoryBreachRecordStore> logger)
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
    public ValueTask<Either<EncinaError, Unit>> RecordBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        if (!_records.TryAdd(breach.Id, breach))
        {
            _logger.LogWarning("Breach record '{BreachId}' already exists", breach.Id);
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(BreachNotificationErrors.AlreadyExists(breach.Id)));
        }

        _logger.LogDebug(
            "Recorded breach '{BreachId}' with severity {Severity} and status {Status}",
            breach.Id, breach.Severity, breach.Status);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<BreachRecord>>> GetBreachAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        var result = _records.TryGetValue(breachId, out var record)
            ? Some(record)
            : Option<BreachRecord>.None;

        return ValueTask.FromResult<Either<EncinaError, Option<BreachRecord>>>(
            Right<EncinaError, Option<BreachRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> UpdateBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        if (!_records.ContainsKey(breach.Id))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(BreachNotificationErrors.NotFound(breach.Id)));
        }

        _records[breach.Id] = breach;

        _logger.LogDebug("Updated breach record '{BreachId}' to status {Status}", breach.Id, breach.Status);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BreachRecord> result = _records.Values
            .Where(r => r.Status == status)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<BreachRecord>>>(
            Right<EncinaError, IReadOnlyList<BreachRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetOverdueBreachesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        IReadOnlyList<BreachRecord> result = _records.Values
            .Where(r => r.NotificationDeadlineUtc < now
                && r.NotifiedAuthorityAtUtc is null
                && r.Status != BreachStatus.Resolved
                && r.Status != BreachStatus.Closed)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<BreachRecord>>>(
            Right<EncinaError, IReadOnlyList<BreachRecord>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DeadlineStatus>>> GetApproachingDeadlineAsync(
        int hoursRemaining,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        IReadOnlyList<DeadlineStatus> result = _records.Values
            .Where(r => r.NotifiedAuthorityAtUtc is null
                && r.Status != BreachStatus.Resolved
                && r.Status != BreachStatus.Closed)
            .Select(r =>
            {
                var remaining = (r.NotificationDeadlineUtc - now).TotalHours;
                return new DeadlineStatus
                {
                    BreachId = r.Id,
                    DetectedAtUtc = r.DetectedAtUtc,
                    DeadlineUtc = r.NotificationDeadlineUtc,
                    RemainingHours = remaining,
                    IsOverdue = remaining < 0,
                    Status = r.Status
                };
            })
            .Where(d => d.RemainingHours < hoursRemaining)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DeadlineStatus>>>(
            Right<EncinaError, IReadOnlyList<DeadlineStatus>>(result));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        string breachId,
        PhasedReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(report);

        if (!_records.TryGetValue(breachId, out var existing))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(BreachNotificationErrors.NotFound(breachId)));
        }

        var updatedReports = existing.PhasedReports.Append(report).ToList().AsReadOnly();
        _records[breachId] = existing with { PhasedReports = updatedReports };

        _logger.LogDebug(
            "Added phased report #{ReportNumber} to breach '{BreachId}'",
            report.ReportNumber, breachId);

        return ValueTask.FromResult<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BreachRecord> result = _records.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<BreachRecord>>>(
            Right<EncinaError, IReadOnlyList<BreachRecord>>(result));
    }

    /// <summary>
    /// Returns all stored records. Useful for testing assertions.
    /// </summary>
    /// <returns>A read-only list of all records in the store.</returns>
    public IReadOnlyList<BreachRecord> GetAllRecords() =>
        _records.Values.ToList().AsReadOnly();

    /// <summary>
    /// Removes all records from the store. Useful for test cleanup.
    /// </summary>
    public void Clear() => _records.Clear();
}
