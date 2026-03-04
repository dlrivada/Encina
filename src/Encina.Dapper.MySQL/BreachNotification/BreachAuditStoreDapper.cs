using System.Data;
using Dapper;
using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.BreachNotification;

/// <summary>
/// Dapper implementation of <see cref="IBreachAuditStore"/> for MySQL.
/// Provides immutable audit trail for GDPR Article 33(5) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the breach notification measures applied and may be required during supervisory
/// authority inquiries (Article 58).
/// </para>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native DATETIME support).</description></item>
/// <item><description>PascalCase column names in SQL queries.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BreachAuditStoreDapper : IBreachAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreachAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: BreachAuditEntries).</param>
    public BreachAuditStoreDapper(
        IDbConnection connection,
        string tableName = "BreachAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        BreachAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = BreachAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, BreachId, Action, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @BreachId, @Action, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.BreachId,
                entity.Action,
                entity.Detail,
                entity.PerformedByUserId,
                OccurredAtUtc = entity.OccurredAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to record breach audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = entry.BreachId, ["action"] = entry.Action }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<BreachAuditEntry>>> GetAuditTrailAsync(
        string breachId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(breachId);

        try
        {
            var sql = $@"
                SELECT Id, BreachId, Action, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE BreachId = @BreachId
                ORDER BY OccurredAtUtc ASC";

            var rows = await _connection.QueryAsync(sql, new { BreachId = breachId });
            var entries = rows
                .Select(row => BreachAuditEntryMapper.ToDomain(MapToEntity(row)))
                .Cast<BreachAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<BreachAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "breachnotification.audit_store_error",
                message: $"Failed to get breach audit trail: {ex.Message}",
                details: new Dictionary<string, object?> { ["breachId"] = breachId }));
        }
    }

    private static BreachAuditEntryEntity MapToEntity(dynamic row)
    {
        return new BreachAuditEntryEntity
        {
            Id = (string)row.Id,
            BreachId = (string)row.BreachId,
            Action = (string)row.Action,
            Detail = row.Detail is null or DBNull ? null : (string)row.Detail,
            PerformedByUserId = row.PerformedByUserId is null or DBNull ? null : (string)row.PerformedByUserId,
            OccurredAtUtc = (DateTimeOffset)row.OccurredAtUtc
        };
    }
}
