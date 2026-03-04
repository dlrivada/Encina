using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.BreachNotification;

/// <summary>
/// MongoDB implementation of <see cref="IBreachRecordStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Manages <see cref="BreachRecord"/> instances throughout the entire breach notification
/// lifecycle: from initial detection through authority notification, data subject notification,
/// phased reporting, and resolution per GDPR Articles 33 and 34.
/// </para>
/// <para>
/// Breach records and phased reports are stored in separate MongoDB collections. When
/// retrieving a breach by ID, phased reports are loaded from their collection and composed
/// into the breach record using the <c>with</c> expression.
/// </para>
/// <para>
/// Per GDPR Article 33(5), the controller must document "any personal data breaches,
/// comprising the facts relating to the personal data breach, its effects and the
/// remedial action taken." This store provides the persistence layer for that
/// documentation requirement.
/// </para>
/// <para>
/// Uses <see cref="TimeProvider"/> for time-based queries (overdue breaches, approaching
/// deadlines) to support deterministic testing and consistent UTC time resolution.
/// </para>
/// </remarks>
public sealed class BreachRecordStoreMongoDB : IBreachRecordStore
{
    private readonly IMongoCollection<BreachRecordDocument> _breachCollection;
    private readonly IMongoCollection<PhasedReportDocument> _phasedReportsCollection;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BreachRecordStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachRecordStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for time-based queries. Defaults to <see cref="TimeProvider.System"/>.</param>
    public BreachRecordStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<BreachRecordStoreMongoDB> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _breachCollection = database.GetCollection<BreachRecordDocument>(config.Collections.BreachRecords);
        _phasedReportsCollection = database.GetCollection<PhasedReportDocument>(config.Collections.BreachPhasedReports);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var document = BreachRecordDocument.FromRecord(breach);
            await _breachCollection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded breach '{BreachId}' with severity {Severity} and status {Status}",
                breach.Id, breach.Severity, breach.Status);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create("breachnotification.store_error",
                $"Failed to record breach: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<BreachRecord>>> GetBreachAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var filter = Builders<BreachRecordDocument>.Filter.Eq(d => d.Id, breachId);
            var document = await _breachCollection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (document is null)
            {
                return Right<EncinaError, Option<BreachRecord>>(None);
            }

            var record = document.ToRecord();
            if (record is null)
            {
                return Right<EncinaError, Option<BreachRecord>>(None);
            }

            // Load phased reports from the separate collection
            var reportsFilter = Builders<PhasedReportDocument>.Filter.Eq(d => d.BreachId, breachId);
            var reportDocs = await _phasedReportsCollection
                .Find(reportsFilter)
                .SortBy(d => d.ReportNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var reports = reportDocs.Select(d => d.ToReport()).ToList();
            var composedRecord = record with { PhasedReports = reports };

            return Right<EncinaError, Option<BreachRecord>>(Some(composedRecord));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<BreachRecord>>(
                EncinaErrors.Create("breachnotification.store_error",
                    $"Failed to get breach '{breachId}': {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var document = BreachRecordDocument.FromRecord(breach);
            var filter = Builders<BreachRecordDocument>.Filter.Eq(d => d.Id, breach.Id);
            await _breachCollection.ReplaceOneAsync(filter, document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Updated breach record '{BreachId}' to status {Status}", breach.Id, breach.Status);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create("breachnotification.store_error",
                $"Failed to update breach '{breach.Id}': {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<BreachRecordDocument>.Filter.Eq(d => d.StatusValue, (int)status);
            var documents = await _breachCollection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents
                .Select(d => d.ToRecord())
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(
                EncinaErrors.Create("breachnotification.store_error",
                    $"Failed to get breaches by status '{status}': {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetOverdueBreachesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var filterBuilder = Builders<BreachRecordDocument>.Filter;
            var filter = filterBuilder.Lt(d => d.NotificationDeadlineUtc, nowUtc)
                & filterBuilder.Eq(d => d.NotifiedAuthorityAtUtc, null);

            var documents = await _breachCollection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents
                .Select(d => d.ToRecord())
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            _logger.LogDebug("Retrieved {Count} overdue breach records", records.Count);
            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(
                EncinaErrors.Create("breachnotification.store_error",
                    $"Failed to get overdue breaches: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DeadlineStatus>>> GetApproachingDeadlineAsync(
        int hoursRemaining,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var thresholdUtc = nowUtc.AddHours(hoursRemaining);
            var filterBuilder = Builders<BreachRecordDocument>.Filter;

            var filter = filterBuilder.Eq(d => d.NotifiedAuthorityAtUtc, null)
                & filterBuilder.Gt(d => d.NotificationDeadlineUtc, nowUtc)
                & filterBuilder.Lt(d => d.NotificationDeadlineUtc, thresholdUtc);

            var documents = await _breachCollection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var now = _timeProvider.GetUtcNow();
            var deadlineStatuses = documents
                .Select(d => d.ToRecord())
                .Where(r => r is not null)
                .Cast<BreachRecord>()
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
                .ToList();

            return Right<EncinaError, IReadOnlyList<DeadlineStatus>>(deadlineStatuses);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DeadlineStatus>>(
                EncinaErrors.Create("breachnotification.store_error",
                    $"Failed to get approaching deadline breaches: {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        string breachId,
        PhasedReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(report);

        try
        {
            var document = PhasedReportDocument.FromReport(breachId, report);
            await _phasedReportsCollection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Added phased report #{ReportNumber} to breach '{BreachId}'",
                report.ReportNumber, breachId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create("breachnotification.store_error",
                $"Failed to add phased report to breach '{breachId}': {ex.Message}", ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await _breachCollection
                .Find(FilterDefinition<BreachRecordDocument>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var records = documents
                .Select(d => d.ToRecord())
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<BreachRecord>>(
                EncinaErrors.Create("breachnotification.store_error",
                    $"Failed to get all breach records: {ex.Message}", ex));
        }
    }
}
