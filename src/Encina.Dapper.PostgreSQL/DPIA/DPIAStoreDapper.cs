using System.Data;
using Dapper;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.DPIA;

/// <summary>
/// Dapper implementation of <see cref="IDPIAStore"/> for PostgreSQL.
/// Manages DPIA assessment persistence per GDPR Article 35.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native TIMESTAMPTZ support).</description></item>
/// <item><description>Integer and long types are cast directly from dynamic rows.</description></item>
/// <item><description>Column names are lowercase; Dapper returns lowercase property names.</description></item>
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
                (id, requesttypename, statusvalue, processingtype, reason, resultjson, dpoconsultationjson, createdatutc, approvedatutc, nextreviewatutc)
                VALUES
                (@Id, @RequestTypeName, @StatusValue, @ProcessingType, @Reason, @ResultJson, @DPOConsultationJson, @CreatedAtUtc, @ApprovedAtUtc, @NextReviewAtUtc)
                ON CONFLICT (id) DO UPDATE SET
                    requesttypename = EXCLUDED.requesttypename, statusvalue = EXCLUDED.statusvalue, processingtype = EXCLUDED.processingtype,
                    reason = EXCLUDED.reason, resultjson = EXCLUDED.resultjson, dpoconsultationjson = EXCLUDED.dpoconsultationjson,
                    createdatutc = EXCLUDED.createdatutc, approvedatutc = EXCLUDED.approvedatutc, nextreviewatutc = EXCLUDED.nextreviewatutc";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.RequestTypeName,
                entity.StatusValue,
                entity.ProcessingType,
                entity.Reason,
                entity.ResultJson,
                entity.DPOConsultationJson,
                CreatedAtUtc = entity.CreatedAtUtc,
                ApprovedAtUtc = entity.ApprovedAtUtc,
                NextReviewAtUtc = entity.NextReviewAtUtc
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
                SELECT id, requesttypename, statusvalue, processingtype, reason, resultjson, dpoconsultationjson, createdatutc, approvedatutc, nextreviewatutc
                FROM {_tableName}
                WHERE requesttypename = @RequestTypeName";

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
                SELECT id, requesttypename, statusvalue, processingtype, reason, resultjson, dpoconsultationjson, createdatutc, approvedatutc, nextreviewatutc
                FROM {_tableName}
                WHERE id = @Id";

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
                SELECT id, requesttypename, statusvalue, processingtype, reason, resultjson, dpoconsultationjson, createdatutc, approvedatutc, nextreviewatutc
                FROM {_tableName}
                WHERE nextreviewatutc IS NOT NULL AND nextreviewatutc < @NowUtc AND statusvalue = @ApprovedStatus
                ORDER BY nextreviewatutc ASC";

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
                SELECT id, requesttypename, statusvalue, processingtype, reason, resultjson, dpoconsultationjson, createdatutc, approvedatutc, nextreviewatutc
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
            var sql = $@"DELETE FROM {_tableName} WHERE id = @Id";
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

    /// <summary>
    /// Maps a dynamic row from Dapper to a <see cref="DPIAAssessmentEntity"/>.
    /// DateTimeOffset values are cast directly (native PostgreSQL TIMESTAMPTZ support).
    /// Property names are lowercase because PostgreSQL returns lowercase column names.
    /// </summary>
    /// <param name="row">The dynamic row returned by Dapper.</param>
    /// <returns>A populated DPIA assessment entity.</returns>
    private static DPIAAssessmentEntity MapToEntity(dynamic row)
    {
        return new DPIAAssessmentEntity
        {
            Id = (string)row.id,
            RequestTypeName = (string)row.requesttypename,
            StatusValue = (int)row.statusvalue,
            ProcessingType = row.processingtype is null or DBNull ? null : (string)row.processingtype,
            Reason = row.reason is null or DBNull ? null : (string)row.reason,
            ResultJson = row.resultjson is null or DBNull ? null : (string)row.resultjson,
            DPOConsultationJson = row.dpoconsultationjson is null or DBNull ? null : (string)row.dpoconsultationjson,
            CreatedAtUtc = (DateTimeOffset)row.createdatutc,
            ApprovedAtUtc = row.approvedatutc is null or DBNull
                ? null
                : (DateTimeOffset?)row.approvedatutc,
            NextReviewAtUtc = row.nextreviewatutc is null or DBNull
                ? null
                : (DateTimeOffset?)row.nextreviewatutc
        };
    }
}
