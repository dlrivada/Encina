using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.DPIA;

/// <summary>
/// Dapper implementation of <see cref="IDPIAAuditStore"/> for SQLite.
/// Provides immutable audit trail for DPIA assessment actions per GDPR Article 5(2).
/// </summary>
/// <remarks>
/// <para>
/// SQLite-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are stored as ISO 8601 text via <c>.ToString("O")</c>.</description></item>
/// <item><description>Never uses <c>datetime('now')</c>; always uses parameterized values.</description></item>
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
                OccurredAtUtc = entity.OccurredAtUtc.ToString("O"),
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
            OccurredAtUtc = DateTimeOffset.Parse((string)row.OccurredAtUtc, null, DateTimeStyles.RoundtripKind),
            Details = row.Details is null or DBNull ? null : (string)row.Details
        };
    }
}
