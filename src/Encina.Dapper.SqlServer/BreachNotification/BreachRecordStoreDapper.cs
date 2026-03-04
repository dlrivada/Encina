using System.Data;
using Dapper;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.BreachNotification;

/// <summary>
/// Dapper implementation of <see cref="IBreachRecordStore"/> for SQL Server.
/// Manages breach records and phased reports for GDPR Articles 33-34 compliance.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native DATETIMEOFFSET support).</description></item>
/// <item><description>Integer and long types are cast directly from dynamic rows.</description></item>
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
                .Select(row => BreachRecordMapper.ToDomain(MapToBreachRecordEntity(row)))
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
                NowUtc = nowUtc
            });

            var records = rows
                .Select(row => BreachRecordMapper.ToDomain(MapToBreachRecordEntity(row)))
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
                NowUtc = nowUtc,
                ThresholdUtc = thresholdUtc
            });

            var results = new List<DeadlineStatus>();
            foreach (var row in rows)
            {
                var statusValue = (int)row.StatusValue;
                if (!Enum.IsDefined(typeof(BreachStatus), statusValue))
                    continue;

                var detectedAtUtc = (DateTimeOffset)row.DetectedAtUtc;
                var deadlineUtc = (DateTimeOffset)row.NotificationDeadlineUtc;
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
                entity.SubmittedAtUtc,
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
                .Select(row => BreachRecordMapper.ToDomain(MapToBreachRecordEntity(row)))
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

    /// <summary>
    /// Loads phased reports for a specific breach from the phased reports table.
    /// </summary>
    /// <param name="breachId">The breach identifier.</param>
    /// <returns>A list of phased reports ordered by report number.</returns>
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

    /// <summary>
    /// Creates the parameter object for breach record SQL commands.
    /// DateTimeOffset values are passed directly (SQL Server native DATETIMEOFFSET support).
    /// </summary>
    /// <param name="entity">The breach record entity.</param>
    /// <returns>An anonymous object with all parameters.</returns>
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
            entity.DetectedAtUtc,
            entity.NotificationDeadlineUtc,
            entity.NotifiedAuthorityAtUtc,
            entity.NotifiedSubjectsAtUtc,
            entity.SeverityValue,
            entity.StatusValue,
            entity.DelayReason,
            entity.SubjectNotificationExemptionValue,
            entity.ResolvedAtUtc,
            entity.ResolutionSummary
        };
    }

    /// <summary>
    /// Maps a dynamic Dapper row to a <see cref="BreachRecordEntity"/>.
    /// Uses direct casting for SQL Server native types (DateTimeOffset, int).
    /// </summary>
    /// <param name="row">The dynamic row from Dapper query.</param>
    /// <returns>A populated breach record entity.</returns>
    private static BreachRecordEntity MapToBreachRecordEntity(dynamic row)
    {
        return new BreachRecordEntity
        {
            Id = (string)row.Id,
            Nature = (string)row.Nature,
            ApproximateSubjectsAffected = (int)row.ApproximateSubjectsAffected,
            CategoriesOfDataAffected = (string)row.CategoriesOfDataAffected,
            DPOContactDetails = (string)row.DPOContactDetails,
            LikelyConsequences = (string)row.LikelyConsequences,
            MeasuresTaken = (string)row.MeasuresTaken,
            DetectedAtUtc = (DateTimeOffset)row.DetectedAtUtc,
            NotificationDeadlineUtc = (DateTimeOffset)row.NotificationDeadlineUtc,
            NotifiedAuthorityAtUtc = row.NotifiedAuthorityAtUtc is null or DBNull
                ? null
                : (DateTimeOffset?)row.NotifiedAuthorityAtUtc,
            NotifiedSubjectsAtUtc = row.NotifiedSubjectsAtUtc is null or DBNull
                ? null
                : (DateTimeOffset?)row.NotifiedSubjectsAtUtc,
            SeverityValue = (int)row.SeverityValue,
            StatusValue = (int)row.StatusValue,
            DelayReason = row.DelayReason is null or DBNull ? null : (string)row.DelayReason,
            SubjectNotificationExemptionValue = (int)row.SubjectNotificationExemptionValue,
            ResolvedAtUtc = row.ResolvedAtUtc is null or DBNull
                ? null
                : (DateTimeOffset?)row.ResolvedAtUtc,
            ResolutionSummary = row.ResolutionSummary is null or DBNull ? null : (string)row.ResolutionSummary
        };
    }

    /// <summary>
    /// Maps a dynamic Dapper row to a <see cref="PhasedReportEntity"/>.
    /// Uses direct casting for SQL Server native types (DateTimeOffset, int).
    /// </summary>
    /// <param name="row">The dynamic row from Dapper query.</param>
    /// <returns>A populated phased report entity.</returns>
    private static PhasedReportEntity MapToPhasedReportEntity(dynamic row)
    {
        return new PhasedReportEntity
        {
            Id = (string)row.Id,
            BreachId = (string)row.BreachId,
            ReportNumber = (int)row.ReportNumber,
            Content = (string)row.Content,
            SubmittedAtUtc = (DateTimeOffset)row.SubmittedAtUtc,
            SubmittedByUserId = row.SubmittedByUserId is null or DBNull ? null : (string)row.SubmittedByUserId
        };
    }
}
