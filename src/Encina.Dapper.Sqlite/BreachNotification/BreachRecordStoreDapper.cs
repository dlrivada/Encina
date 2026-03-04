using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.BreachNotification;

/// <summary>
/// Dapper implementation of <see cref="IBreachRecordStore"/> for SQLite.
/// Manages breach records and phased reports for GDPR Articles 33-34 compliance.
/// </summary>
/// <remarks>
/// <para>
/// SQLite-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as ISO 8601 text via <c>.ToString("O")</c>.</description></item>
/// <item><description>Never uses <c>datetime('now')</c>; always uses parameterized <c>@NowUtc</c>.</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BreachRecordStoreDapper : IBreachRecordStore
{
    private readonly IDbConnection _connection;
    private readonly string _breachRecordsTable;
    private readonly string _phasedReportsTable;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachRecordStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="breachRecordsTable">The breach records table name (default: BreachRecords).</param>
    /// <param name="phasedReportsTable">The phased reports table name (default: BreachPhasedReports).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public BreachRecordStoreDapper(
        IDbConnection connection,
        string breachRecordsTable = "BreachRecords",
        string phasedReportsTable = "BreachPhasedReports",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _breachRecordsTable = SqlIdentifierValidator.ValidateTableName(breachRecordsTable);
        _phasedReportsTable = SqlIdentifierValidator.ValidateTableName(phasedReportsTable);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var entity = BreachRecordMapper.ToEntity(breach);
            var sql = $@"
                INSERT INTO {_breachRecordsTable}
                (Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                 LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                 NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                 DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary)
                VALUES
                (@Id, @Nature, @ApproximateSubjectsAffected, @CategoriesOfDataAffected, @DPOContactDetails,
                 @LikelyConsequences, @MeasuresTaken, @DetectedAtUtc, @NotificationDeadlineUtc,
                 @NotifiedAuthorityAtUtc, @NotifiedSubjectsAtUtc, @SeverityValue, @StatusValue,
                 @DelayReason, @SubjectNotificationExemptionValue, @ResolvedAtUtc, @ResolutionSummary)";

            await _connection.ExecuteAsync(sql, CreateBreachRecordParams(entity));
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to record breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<BreachRecord>>> GetBreachAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var sql = $@"
                SELECT Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                       LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                       NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                       DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary
                FROM {_breachRecordsTable}
                WHERE Id = @Id";

            var rows = await _connection.QueryAsync(sql, new { Id = breachId });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<BreachRecord>>(None);

            BreachRecordEntity entity = MapToBreachRecordEntity(row);
            BreachRecord? record = BreachRecordMapper.ToDomain(entity);

            if (record is null)
                return Right<EncinaError, Option<BreachRecord>>(None);

            // Load phased reports from separate table
            var reports = await LoadPhasedReportsAsync(breachId);
            if (reports.Count > 0)
                record = record with { PhasedReports = reports };

            return Right<EncinaError, Option<BreachRecord>>(Some(record));
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> UpdateBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(breach);

        try
        {
            var entity = BreachRecordMapper.ToEntity(breach);
            var sql = $@"
                UPDATE {_breachRecordsTable}
                SET Nature = @Nature,
                    ApproximateSubjectsAffected = @ApproximateSubjectsAffected,
                    CategoriesOfDataAffected = @CategoriesOfDataAffected,
                    DPOContactDetails = @DPOContactDetails,
                    LikelyConsequences = @LikelyConsequences,
                    MeasuresTaken = @MeasuresTaken,
                    DetectedAtUtc = @DetectedAtUtc,
                    NotificationDeadlineUtc = @NotificationDeadlineUtc,
                    NotifiedAuthorityAtUtc = @NotifiedAuthorityAtUtc,
                    NotifiedSubjectsAtUtc = @NotifiedSubjectsAtUtc,
                    SeverityValue = @SeverityValue,
                    StatusValue = @StatusValue,
                    DelayReason = @DelayReason,
                    SubjectNotificationExemptionValue = @SubjectNotificationExemptionValue,
                    ResolvedAtUtc = @ResolvedAtUtc,
                    ResolutionSummary = @ResolutionSummary
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, CreateBreachRecordParams(entity));
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to update breach: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breach.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                       LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                       NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                       DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary
                FROM {_breachRecordsTable}
                WHERE StatusValue = @StatusValue";

            var rows = await _connection.QueryAsync(sql, new { StatusValue = (int)status });
            var records = rows
                .Select(row =>
                {
                    BreachRecordEntity entity = MapToBreachRecordEntity(row);
                    return BreachRecordMapper.ToDomain(entity);
                })
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get breaches by status: {ex.Message}",
                details: new Dictionary<string, object?> { ["status"] = status.ToString() }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetOverdueBreachesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var sql = $@"
                SELECT Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                       LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                       NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                       DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary
                FROM {_breachRecordsTable}
                WHERE NotificationDeadlineUtc < @NowUtc AND NotifiedAuthorityAtUtc IS NULL";

            var rows = await _connection.QueryAsync(sql, new
            {
                NowUtc = nowUtc.ToString("O")
            });

            var records = rows
                .Select(row =>
                {
                    BreachRecordEntity entity = MapToBreachRecordEntity(row);
                    return BreachRecordMapper.ToDomain(entity);
                })
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get overdue breaches: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DeadlineStatus>>> GetApproachingDeadlineAsync(
        int hoursRemaining,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var thresholdUtc = nowUtc.AddHours(hoursRemaining);

            var sql = $@"
                SELECT Id, DetectedAtUtc, NotificationDeadlineUtc, StatusValue
                FROM {_breachRecordsTable}
                WHERE NotifiedAuthorityAtUtc IS NULL
                  AND NotificationDeadlineUtc > @NowUtc
                  AND NotificationDeadlineUtc < @ThresholdUtc";

            var rows = await _connection.QueryAsync(sql, new
            {
                NowUtc = nowUtc.ToString("O"),
                ThresholdUtc = thresholdUtc.ToString("O")
            });

            var results = new List<DeadlineStatus>();
            foreach (var row in rows)
            {
                var statusValue = Convert.ToInt32(row.StatusValue);
                if (!Enum.IsDefined(typeof(BreachStatus), statusValue))
                    continue;

                var detectedAtUtc = DateTimeOffset.Parse(
                    (string)row.DetectedAtUtc, null, DateTimeStyles.RoundtripKind);
                var deadlineUtc = DateTimeOffset.Parse(
                    (string)row.NotificationDeadlineUtc, null, DateTimeStyles.RoundtripKind);
                var remaining = (deadlineUtc - nowUtc).TotalHours;

                results.Add(new DeadlineStatus
                {
                    BreachId = (string)row.Id,
                    DetectedAtUtc = detectedAtUtc,
                    DeadlineUtc = deadlineUtc,
                    RemainingHours = remaining,
                    IsOverdue = remaining < 0,
                    Status = (BreachStatus)statusValue
                });
            }

            return Right<EncinaError, IReadOnlyList<DeadlineStatus>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get approaching deadline breaches: {ex.Message}",
                details: new Dictionary<string, object?> { ["hoursRemaining"] = hoursRemaining }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        string breachId,
        PhasedReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);
        ArgumentNullException.ThrowIfNull(report);

        try
        {
            var entity = PhasedReportMapper.ToEntity(report);
            var sql = $@"
                INSERT INTO {_phasedReportsTable}
                (Id, BreachId, ReportNumber, Content, SubmittedAtUtc, SubmittedByUserId)
                VALUES
                (@Id, @BreachId, @ReportNumber, @Content, @SubmittedAtUtc, @SubmittedByUserId)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                BreachId = breachId,
                entity.ReportNumber,
                entity.Content,
                SubmittedAtUtc = entity.SubmittedAtUtc.ToString("O"),
                entity.SubmittedByUserId
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to add phased report: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId, ["reportId"] = report.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                       LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                       NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                       DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary
                FROM {_breachRecordsTable}";

            var rows = await _connection.QueryAsync(sql);
            var records = rows
                .Select(row =>
                {
                    BreachRecordEntity entity = MapToBreachRecordEntity(row);
                    return BreachRecordMapper.ToDomain(entity);
                })
                .Where(r => r is not null)
                .Cast<BreachRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachRecord>>(records);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.store_error",
                message: $"Failed to get all breaches: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private async Task<IReadOnlyList<PhasedReport>> LoadPhasedReportsAsync(string breachId)
    {
        var sql = $@"
            SELECT Id, BreachId, ReportNumber, Content, SubmittedAtUtc, SubmittedByUserId
            FROM {_phasedReportsTable}
            WHERE BreachId = @BreachId
            ORDER BY ReportNumber ASC";

        var rows = await _connection.QueryAsync(sql, new { BreachId = breachId });
        return rows
            .Select(row => PhasedReportMapper.ToDomain(MapToPhasedReportEntity(row)))
            .Cast<PhasedReport>()
            .ToList();
    }

    private static object CreateBreachRecordParams(BreachRecordEntity entity)
    {
        return new
        {
            entity.Id,
            entity.Nature,
            entity.ApproximateSubjectsAffected,
            entity.CategoriesOfDataAffected,
            entity.DPOContactDetails,
            entity.LikelyConsequences,
            entity.MeasuresTaken,
            DetectedAtUtc = entity.DetectedAtUtc.ToString("O"),
            NotificationDeadlineUtc = entity.NotificationDeadlineUtc.ToString("O"),
            NotifiedAuthorityAtUtc = entity.NotifiedAuthorityAtUtc?.ToString("O"),
            NotifiedSubjectsAtUtc = entity.NotifiedSubjectsAtUtc?.ToString("O"),
            entity.SeverityValue,
            entity.StatusValue,
            entity.DelayReason,
            entity.SubjectNotificationExemptionValue,
            ResolvedAtUtc = entity.ResolvedAtUtc?.ToString("O"),
            entity.ResolutionSummary
        };
    }

    private static BreachRecordEntity MapToBreachRecordEntity(dynamic row)
    {
        return new BreachRecordEntity
        {
            Id = (string)row.Id,
            Nature = (string)row.Nature,
            ApproximateSubjectsAffected = Convert.ToInt32(row.ApproximateSubjectsAffected),
            CategoriesOfDataAffected = (string)row.CategoriesOfDataAffected,
            DPOContactDetails = (string)row.DPOContactDetails,
            LikelyConsequences = (string)row.LikelyConsequences,
            MeasuresTaken = (string)row.MeasuresTaken,
            DetectedAtUtc = DateTimeOffset.Parse((string)row.DetectedAtUtc, null, DateTimeStyles.RoundtripKind),
            NotificationDeadlineUtc = DateTimeOffset.Parse((string)row.NotificationDeadlineUtc, null, DateTimeStyles.RoundtripKind),
            NotifiedAuthorityAtUtc = row.NotifiedAuthorityAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.NotifiedAuthorityAtUtc, null, DateTimeStyles.RoundtripKind),
            NotifiedSubjectsAtUtc = row.NotifiedSubjectsAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.NotifiedSubjectsAtUtc, null, DateTimeStyles.RoundtripKind),
            SeverityValue = Convert.ToInt32(row.SeverityValue),
            StatusValue = Convert.ToInt32(row.StatusValue),
            DelayReason = row.DelayReason is null or DBNull ? null : (string)row.DelayReason,
            SubjectNotificationExemptionValue = Convert.ToInt32(row.SubjectNotificationExemptionValue),
            ResolvedAtUtc = row.ResolvedAtUtc is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)row.ResolvedAtUtc, null, DateTimeStyles.RoundtripKind),
            ResolutionSummary = row.ResolutionSummary is null or DBNull ? null : (string)row.ResolutionSummary
        };
    }

    private static PhasedReportEntity MapToPhasedReportEntity(dynamic row)
    {
        return new PhasedReportEntity
        {
            Id = (string)row.Id,
            BreachId = (string)row.BreachId,
            ReportNumber = Convert.ToInt32(row.ReportNumber),
            Content = (string)row.Content,
            SubmittedAtUtc = DateTimeOffset.Parse((string)row.SubmittedAtUtc, null, DateTimeStyles.RoundtripKind),
            SubmittedByUserId = row.SubmittedByUserId is null or DBNull ? null : (string)row.SubmittedByUserId
        };
    }
}
