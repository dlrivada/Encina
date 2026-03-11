using System.Data;
using Dapper;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.DPIA;

/// <summary>
/// Dapper implementation of <see cref="IDPIAAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for DPIA assessment actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native TIMESTAMPTZ support).</description></item>
/// <item><description>Column names are lowercase; Dapper returns lowercase property names.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DPIAAuditStoreDapper : IDPIAAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: DPIAAuditEntries).</param>
    public DPIAAuditStoreDapper(
        IDbConnection connection,
        string tableName = "DPIAAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(
        DPIAAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = DPIAAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, assessmentid, action, performedby, occurredatutc, details)
                VALUES
                (@Id, @AssessmentId, @Action, @PerformedBy, @OccurredAtUtc, @Details)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.AssessmentId,
                entity.Action,
                entity.PerformedBy,
                OccurredAtUtc = entity.OccurredAtUtc,
                entity.Details
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to record DPIA audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = entry.AssessmentId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, assessmentid, action, performedby, occurredatutc, details
                FROM {_tableName}
                WHERE assessmentid = @AssessmentId
                ORDER BY occurredatutc ASC";

            var rows = await _connection.QueryAsync(sql, new { AssessmentId = assessmentId.ToString("D") });
            var entries = rows
                .Select(row => DPIAAuditEntryMapper.ToDomain(MapToEntity(row)))
                .Where(e => e is not null)
                .Cast<DPIAAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DPIAAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "dpia.audit_store_error",
                message: $"Failed to get DPIA audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["assessmentId"] = assessmentId }));
        }
    }

    /// <summary>
    /// Maps a dynamic row from Dapper to a <see cref="DPIAAuditEntryEntity"/>.
    /// DateTimeOffset values are cast directly (native PostgreSQL TIMESTAMPTZ support).
    /// Property names are lowercase because PostgreSQL returns lowercase column names.
    /// </summary>
    /// <param name="row">The dynamic row returned by Dapper.</param>
    /// <returns>A populated DPIA audit entry entity.</returns>
    private static DPIAAuditEntryEntity MapToEntity(dynamic row)
    {
        return new DPIAAuditEntryEntity
        {
            Id = (string)row.id,
            AssessmentId = (string)row.assessmentid,
            Action = (string)row.action,
            PerformedBy = row.performedby is null or DBNull ? null : (string)row.performedby,
            OccurredAtUtc = (DateTimeOffset)row.occurredatutc,
            Details = row.details is null or DBNull ? null : (string)row.details
        };
    }
}
