using System.Data;
using System.Globalization;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.DPIA;

/// <summary>
/// ADO.NET implementation of <see cref="IDPIAStore"/> for SQLite.
/// Manages DPIA assessment persistence per GDPR Article 35.
/// </summary>
public sealed class DPIAStoreADO : IDPIAStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The assessments table name (default: DPIAAssessments).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DPIAStoreADO(
        IDbConnection connection,
        string tableName = "DPIAAssessments",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(
        DPIAAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        try
        {
            var entity = DPIAAssessmentMapper.ToEntity(assessment);
            var sql = $@"
                INSERT OR REPLACE INTO {_tableName}
                (Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc)
                VALUES
                (@Id, @RequestTypeName, @StatusValue, @ProcessingType, @Reason, @ResultJson, @DPOConsultationJson, @CreatedAtUtc, @ApprovedAtUtc, @NextReviewAtUtc)";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@RequestTypeName", entity.RequestTypeName);
            AddParameter(command, "@StatusValue", entity.StatusValue);
            AddParameter(command, "@ProcessingType", entity.ProcessingType);
            AddParameter(command, "@Reason", entity.Reason);
            AddParameter(command, "@ResultJson", entity.ResultJson);
            AddParameter(command, "@DPOConsultationJson", entity.DPOConsultationJson);
            AddParameter(command, "@CreatedAtUtc", entity.CreatedAtUtc.ToString("O"));
            AddParameter(command, "@ApprovedAtUtc", entity.ApprovedAtUtc?.ToString("O"));
            AddParameter(command, "@NextReviewAtUtc", entity.NextReviewAtUtc?.ToString("O"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to save DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessment.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestTypeName);

        try
        {
            var sql = $@"
                SELECT Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc
                FROM {_tableName}
                WHERE RequestTypeName = @RequestTypeName";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@RequestTypeName", requestTypeName);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DPIAAssessmentMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<DPIAAssessment>>(Some(domain))
                    : Right<EncinaError, Option<DPIAAssessment>>(None);
            }

            return Right<EncinaError, Option<DPIAAssessment>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to get DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["requestTypeName"] = requestTypeName }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentByIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc
                FROM {_tableName}
                WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", assessmentId.ToString("D"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DPIAAssessmentMapper.ToDomain(entity);
                return domain is not null
                    ? Right<EncinaError, Option<DPIAAssessment>>(Some(domain))
                    : Right<EncinaError, Option<DPIAAssessment>>(None);
            }

            return Right<EncinaError, Option<DPIAAssessment>>(None);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to get DPIA assessment by ID: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc
                FROM {_tableName}
                WHERE NextReviewAtUtc IS NOT NULL AND NextReviewAtUtc < @NowUtc AND StatusValue = @ApprovedStatus
                ORDER BY NextReviewAtUtc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@NowUtc", nowUtc.ToString("O"));
            AddParameter(command, "@ApprovedStatus", (int)DPIAAssessmentStatus.Approved);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DPIAAssessment>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DPIAAssessmentMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to get expired DPIA assessments: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc
                FROM {_tableName}";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var results = new List<DPIAAssessment>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                var domain = DPIAAssessmentMapper.ToDomain(entity);
                if (domain is not null)
                    results.Add(domain);
            }

            return Right<EncinaError, IReadOnlyList<DPIAAssessment>>(results);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to get all DPIA assessments: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"DELETE FROM {_tableName} WHERE Id = @Id";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", assessmentId.ToString("D"));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.store_error",
                message: $"Failed to delete DPIA assessment: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    private static DPIAAssessmentEntity MapToEntity(IDataReader reader)
    {
        var processingTypeOrd = reader.GetOrdinal("ProcessingType");
        var reasonOrd = reader.GetOrdinal("Reason");
        var resultJsonOrd = reader.GetOrdinal("ResultJson");
        var dpoConsultationJsonOrd = reader.GetOrdinal("DPOConsultationJson");
        var approvedAtUtcOrd = reader.GetOrdinal("ApprovedAtUtc");
        var nextReviewAtUtcOrd = reader.GetOrdinal("NextReviewAtUtc");

        return new DPIAAssessmentEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            RequestTypeName = reader.GetString(reader.GetOrdinal("RequestTypeName")),
            StatusValue = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("StatusValue")), CultureInfo.InvariantCulture),
            ProcessingType = reader.IsDBNull(processingTypeOrd) ? null : reader.GetString(processingTypeOrd),
            Reason = reader.IsDBNull(reasonOrd) ? null : reader.GetString(reasonOrd),
            ResultJson = reader.IsDBNull(resultJsonOrd) ? null : reader.GetString(resultJsonOrd),
            DPOConsultationJson = reader.IsDBNull(dpoConsultationJsonOrd) ? null : reader.GetString(dpoConsultationJsonOrd),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("CreatedAtUtc")), null, DateTimeStyles.RoundtripKind),
            ApprovedAtUtc = reader.IsDBNull(approvedAtUtcOrd)
                ? null
                : DateTimeOffset.Parse(reader.GetString(approvedAtUtcOrd), null, DateTimeStyles.RoundtripKind),
            NextReviewAtUtc = reader.IsDBNull(nextReviewAtUtcOrd)
                ? null
                : DateTimeOffset.Parse(reader.GetString(nextReviewAtUtcOrd), null, DateTimeStyles.RoundtripKind)
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
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqlCommand)
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqlReader)
            return await sqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
