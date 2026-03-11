using System.Data;
using Dapper;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.DPIA;

/// <summary>
/// Dapper implementation of <see cref="IDPIAAuditStore"/> for SQL Server.
/// Provides immutable audit trail for DPIA assessment actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly as native parameters.</description></item>
/// <item><description>Integer and DateTimeOffset values can be cast directly without conversion helpers.</description></item>
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
                (Id, AssessmentId, Action, PerformedBy, OccurredAtUtc, Details)
                VALUES
                (@Id, @AssessmentId, @Action, @PerformedBy, @OccurredAtUtc, @Details)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.AssessmentId,
                entity.Action,
                entity.PerformedBy,
                entity.OccurredAtUtc,
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
                SELECT Id, AssessmentId, Action, PerformedBy, OccurredAtUtc, Details
                FROM {_tableName}
                WHERE AssessmentId = @AssessmentId
                ORDER BY OccurredAtUtc ASC";

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

    private static DPIAAuditEntryEntity MapToEntity(dynamic row)
    {
        return new DPIAAuditEntryEntity
        {
            Id = (string)row.Id,
            AssessmentId = (string)row.AssessmentId,
            Action = (string)row.Action,
            PerformedBy = row.PerformedBy is null or DBNull ? null : (string)row.PerformedBy,
            OccurredAtUtc = (DateTimeOffset)row.OccurredAtUtc,
            Details = row.Details is null or DBNull ? null : (string)row.Details
        };
    }
}
