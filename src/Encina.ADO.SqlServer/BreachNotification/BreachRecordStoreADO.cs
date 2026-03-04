using System.Data;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.BreachNotification;

/// <summary>
/// ADO.NET implementation of <see cref="IBreachRecordStore"/> for SQL Server.
/// Manages breach records and phased reports for GDPR Articles 33-34 compliance.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values use native DATETIME2/DATETIMEOFFSET column types with <c>.UtcDateTime</c> for parameters.</description></item>
/// <item><description>Enum values are stored as integers.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BreachRecordStoreADO : IBreachRecordStore
{
    private readonly IDbConnection _connection;
    private readonly string _breachRecordsTable;
    private readonly string _phasedReportsTable;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachRecordStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="breachRecordsTable">The breach records table name (default: BreachRecords).</param>
    /// <param name="phasedReportsTable">The phased reports table name (default: BreachPhasedReports).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public BreachRecordStoreADO(
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddBreachRecordParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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
            // Load breach record
            var sql = $@"
                SELECT Id, Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails,
                       LikelyConsequences, MeasuresTaken, DetectedAtUtc, NotificationDeadlineUtc,
                       NotifiedAuthorityAtUtc, NotifiedSubjectsAtUtc, SeverityValue, StatusValue,
                       DelayReason, SubjectNotificationExemptionValue, ResolvedAtUtc, ResolutionSummary
                FROM {_breachRecordsTable}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", breachId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            BreachRecord? record;
            using (var reader = await ExecuteReaderAsync(command, cancellationToken))
            {
                if (!await ReadAsync(reader, cancellationToken))
                    return Right<EncinaError, Option<BreachRecord>>(None);

                var entity = MapToBreachRecordEntity(reader);
                record = BreachRecordMapper.ToDomain(entity);
            }

            if (record is null)
                return Right<EncinaError, Option<BreachRecord>>(None);

            // Load phased reports
            var reports = await LoadPhasedReportsAsync(breachId, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddBreachRecordParameters(command, entity);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@StatusValue", (int)status);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<BreachRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToBreachRecordEntity(reader);
                var domain = BreachRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<BreachRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToBreachRecordEntity(reader);
                var domain = BreachRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@NowUtc", nowUtc.UtcDateTime);
            AddParameter(command, "@ThresholdUtc", thresholdUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DeadlineStatus>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var breachId = reader.GetString(reader.GetOrdinal("Id"));
                var detectedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("DetectedAtUtc")), TimeSpan.Zero);
                var deadlineUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("NotificationDeadlineUtc")), TimeSpan.Zero);
                var statusValue = reader.GetInt32(reader.GetOrdinal("StatusValue"));

                var remaining = (deadlineUtc - nowUtc).TotalHours;

                if (Enum.IsDefined(typeof(BreachStatus), statusValue))
                {
                    results.Add(new DeadlineStatus
                    {
                        BreachId = breachId,
                        DetectedAtUtc = detectedAtUtc,
                        DeadlineUtc = deadlineUtc,
                        RemainingHours = remaining,
                        IsOverdue = remaining < 0,
                        Status = (BreachStatus)statusValue
                    });
                }
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@BreachId", breachId);
            AddParameter(command, "@ReportNumber", entity.ReportNumber);
            AddParameter(command, "@Content", entity.Content);
            AddParameter(command, "@SubmittedAtUtc", entity.SubmittedAtUtc.UtcDateTime);
            AddParameter(command, "@SubmittedByUserId", entity.SubmittedByUserId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var records = new List<BreachRecord>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToBreachRecordEntity(reader);
                var domain = BreachRecordMapper.ToDomain(entity);
                if (domain is not null)
                    records.Add(domain);
            }

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

    private async Task<IReadOnlyList<PhasedReport>> LoadPhasedReportsAsync(
        string breachId,
        CancellationToken cancellationToken)
    {
        var sql = $@"
            SELECT Id, BreachId, ReportNumber, Content, SubmittedAtUtc, SubmittedByUserId
            FROM {_phasedReportsTable}
            WHERE BreachId = @BreachId
            ORDER BY ReportNumber ASC";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        AddParameter(command, "@BreachId", breachId);

        var reports = new List<PhasedReport>();
        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            var entity = MapToPhasedReportEntity(reader);
            reports.Add(PhasedReportMapper.ToDomain(entity));
        }

        return reports;
    }

    private static void AddBreachRecordParameters(IDbCommand command, BreachRecordEntity entity)
    {
        AddParameter(command, "@Id", entity.Id);
        AddParameter(command, "@Nature", entity.Nature);
        AddParameter(command, "@ApproximateSubjectsAffected", entity.ApproximateSubjectsAffected);
        AddParameter(command, "@CategoriesOfDataAffected", entity.CategoriesOfDataAffected);
        AddParameter(command, "@DPOContactDetails", entity.DPOContactDetails);
        AddParameter(command, "@LikelyConsequences", entity.LikelyConsequences);
        AddParameter(command, "@MeasuresTaken", entity.MeasuresTaken);
        AddParameter(command, "@DetectedAtUtc", entity.DetectedAtUtc.UtcDateTime);
        AddParameter(command, "@NotificationDeadlineUtc", entity.NotificationDeadlineUtc.UtcDateTime);
        AddParameter(command, "@NotifiedAuthorityAtUtc", entity.NotifiedAuthorityAtUtc?.UtcDateTime);
        AddParameter(command, "@NotifiedSubjectsAtUtc", entity.NotifiedSubjectsAtUtc?.UtcDateTime);
        AddParameter(command, "@SeverityValue", entity.SeverityValue);
        AddParameter(command, "@StatusValue", entity.StatusValue);
        AddParameter(command, "@DelayReason", entity.DelayReason);
        AddParameter(command, "@SubjectNotificationExemptionValue", entity.SubjectNotificationExemptionValue);
        AddParameter(command, "@ResolvedAtUtc", entity.ResolvedAtUtc?.UtcDateTime);
        AddParameter(command, "@ResolutionSummary", entity.ResolutionSummary);
    }

    private static BreachRecordEntity MapToBreachRecordEntity(IDataReader reader)
    {
        var notifiedAuthorityOrd = reader.GetOrdinal("NotifiedAuthorityAtUtc");
        var notifiedSubjectsOrd = reader.GetOrdinal("NotifiedSubjectsAtUtc");
        var delayReasonOrd = reader.GetOrdinal("DelayReason");
        var resolvedAtOrd = reader.GetOrdinal("ResolvedAtUtc");
        var resolutionSummaryOrd = reader.GetOrdinal("ResolutionSummary");

        return new BreachRecordEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Nature = reader.GetString(reader.GetOrdinal("Nature")),
            ApproximateSubjectsAffected = reader.GetInt32(reader.GetOrdinal("ApproximateSubjectsAffected")),
            CategoriesOfDataAffected = reader.GetString(reader.GetOrdinal("CategoriesOfDataAffected")),
            DPOContactDetails = reader.GetString(reader.GetOrdinal("DPOContactDetails")),
            LikelyConsequences = reader.GetString(reader.GetOrdinal("LikelyConsequences")),
            MeasuresTaken = reader.GetString(reader.GetOrdinal("MeasuresTaken")),
            DetectedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("DetectedAtUtc")), TimeSpan.Zero),
            NotificationDeadlineUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("NotificationDeadlineUtc")), TimeSpan.Zero),
            NotifiedAuthorityAtUtc = reader.IsDBNull(notifiedAuthorityOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(notifiedAuthorityOrd), TimeSpan.Zero),
            NotifiedSubjectsAtUtc = reader.IsDBNull(notifiedSubjectsOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(notifiedSubjectsOrd), TimeSpan.Zero),
            SeverityValue = reader.GetInt32(reader.GetOrdinal("SeverityValue")),
            StatusValue = reader.GetInt32(reader.GetOrdinal("StatusValue")),
            DelayReason = reader.IsDBNull(delayReasonOrd) ? null : reader.GetString(delayReasonOrd),
            SubjectNotificationExemptionValue = reader.GetInt32(reader.GetOrdinal("SubjectNotificationExemptionValue")),
            ResolvedAtUtc = reader.IsDBNull(resolvedAtOrd)
                ? null
                : new DateTimeOffset(reader.GetDateTime(resolvedAtOrd), TimeSpan.Zero),
            ResolutionSummary = reader.IsDBNull(resolutionSummaryOrd) ? null : reader.GetString(resolutionSummaryOrd)
        };
    }

    private static PhasedReportEntity MapToPhasedReportEntity(IDataReader reader)
    {
        var submittedByOrd = reader.GetOrdinal("SubmittedByUserId");

        return new PhasedReportEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            BreachId = reader.GetString(reader.GetOrdinal("BreachId")),
            ReportNumber = reader.GetInt32(reader.GetOrdinal("ReportNumber")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            SubmittedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("SubmittedAtUtc")), TimeSpan.Zero),
            SubmittedByUserId = reader.IsDBNull(submittedByOrd) ? null : reader.GetString(submittedByOrd)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
