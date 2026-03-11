using System.Data;
using Dapper;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.DPIA;

/// <summary>
/// Dapper implementation of <see cref="IDPIAStore"/> for MySQL.
/// Manages DPIA assessment persistence per GDPR Article 35.
/// </summary>
/// <remarks>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native DATETIME support).</description></item>
/// <item><description>Integer and long types are cast directly from dynamic rows.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPIAStoreDapper : IDPIAStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The assessments table name (default: DPIAAssessments).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>).</param>
    public DPIAStoreDapper(
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
                INSERT INTO {_tableName}
                (Id, RequestTypeName, StatusValue, ProcessingType, Reason, ResultJson, DPOConsultationJson, CreatedAtUtc, ApprovedAtUtc, NextReviewAtUtc)
                VALUES
                (@Id, @RequestTypeName, @StatusValue, @ProcessingType, @Reason, @ResultJson, @DPOConsultationJson, @CreatedAtUtc, @ApprovedAtUtc, @NextReviewAtUtc)
                ON DUPLICATE KEY UPDATE
                    RequestTypeName = VALUES(RequestTypeName), StatusValue = VALUES(StatusValue), ProcessingType = VALUES(ProcessingType),
                    Reason = VALUES(Reason), ResultJson = VALUES(ResultJson), DPOConsultationJson = VALUES(DPOConsultationJson),
                    CreatedAtUtc = VALUES(CreatedAtUtc), ApprovedAtUtc = VALUES(ApprovedAtUtc), NextReviewAtUtc = VALUES(NextReviewAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.RequestTypeName,
                entity.StatusValue,
                entity.ProcessingType,
                entity.Reason,
                entity.ResultJson,
                entity.DPOConsultationJson,
                entity.CreatedAtUtc,
                entity.ApprovedAtUtc,
                entity.NextReviewAtUtc
            });

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

            var rows = await _connection.QueryAsync(sql, new { RequestTypeName = requestTypeName });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            var entity = MapToEntity(row);
            return Right<EncinaError, Option<DPIAAssessment>>(Some(DPIAAssessmentMapper.ToDomain(entity)!));
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

            var rows = await _connection.QueryAsync(sql, new { Id = assessmentId.ToString("D") });
            var row = rows.FirstOrDefault();

            if (row is null)
                return Right<EncinaError, Option<DPIAAssessment>>(None);

            var entity = MapToEntity(row);
            return Right<EncinaError, Option<DPIAAssessment>>(Some(DPIAAssessmentMapper.ToDomain(entity)!));
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

            var rows = await _connection.QueryAsync(sql, new
            {
                NowUtc = nowUtc.UtcDateTime,
                ApprovedStatus = (int)DPIAAssessmentStatus.Approved
            });

            var results = rows
                .Select(row => DPIAAssessmentMapper.ToDomain(MapToEntity(row)))
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

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

            var rows = await _connection.QueryAsync(sql);
            var results = rows
                .Select(row => DPIAAssessmentMapper.ToDomain(MapToEntity(row)))
                .Where(a => a is not null)
                .Cast<DPIAAssessment>()
                .ToList();

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
            await _connection.ExecuteAsync(sql, new { Id = assessmentId.ToString("D") });
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

    private static DPIAAssessmentEntity MapToEntity(dynamic row)
    {
        return new DPIAAssessmentEntity
        {
            Id = (string)row.Id,
            RequestTypeName = (string)row.RequestTypeName,
            StatusValue = (int)row.StatusValue,
            ProcessingType = row.ProcessingType is null or DBNull ? null : (string)row.ProcessingType,
            Reason = row.Reason is null or DBNull ? null : (string)row.Reason,
            ResultJson = row.ResultJson is null or DBNull ? null : (string)row.ResultJson,
            DPOConsultationJson = row.DPOConsultationJson is null or DBNull ? null : (string)row.DPOConsultationJson,
            CreatedAtUtc = (DateTimeOffset)row.CreatedAtUtc,
            ApprovedAtUtc = row.ApprovedAtUtc is null or DBNull ? null : (DateTimeOffset?)row.ApprovedAtUtc,
            NextReviewAtUtc = row.NextReviewAtUtc is null or DBNull ? null : (DateTimeOffset?)row.NextReviewAtUtc
        };
    }
}
