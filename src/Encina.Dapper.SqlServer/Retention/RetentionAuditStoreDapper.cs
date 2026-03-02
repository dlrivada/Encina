using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Retention;

/// <summary>
/// Dapper implementation of <see cref="IRetentionAuditStore"/> for SQL Server.
/// Provides immutable audit trail for GDPR Article 5(2) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the data retention measures applied and may be required during regulatory
/// audits or DPIA reviews (Article 35).
/// </para>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are passed directly (native DATETIMEOFFSET support).</description></item>
/// <item><description>String and nullable string types are cast directly from dynamic rows.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RetentionAuditStoreDapper : IRetentionAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: RetentionAuditEntries).</param>
    public RetentionAuditStoreDapper(
        IDbConnection connection,
        string tableName = "RetentionAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        RetentionAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = RetentionAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @Action, @EntityId, @DataCategory, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.Action,
                entity.EntityId,
                entity.DataCategory,
                entity.Detail,
                entity.PerformedByUserId,
                entity.OccurredAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to record audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE EntityId = @EntityId
                ORDER BY OccurredAtUtc DESC";

            var rows = await _connection.QueryAsync(sql, new { EntityId = entityId });
            var entries = rows.Select(row => RetentionAuditEntryMapper.ToDomain(MapToEntity(row))).Cast<RetentionAuditEntry>().ToList();

            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to get audit entries by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT Id, Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                ORDER BY OccurredAtUtc DESC";

            var rows = await _connection.QueryAsync(sql);
            var entries = rows.Select(row => RetentionAuditEntryMapper.ToDomain(MapToEntity(row))).Cast<RetentionAuditEntry>().ToList();

            return Right<EncinaError, IReadOnlyList<RetentionAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.audit_store_error",
                message: $"Failed to get all audit entries: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static RetentionAuditEntryEntity MapToEntity(dynamic row)
    {
        return new RetentionAuditEntryEntity
        {
            Id = (string)row.Id,
            Action = (string)row.Action,
            EntityId = row.EntityId is null or DBNull ? null : (string)row.EntityId,
            DataCategory = row.DataCategory is null or DBNull ? null : (string)row.DataCategory,
            Detail = row.Detail is null or DBNull ? null : (string)row.Detail,
            PerformedByUserId = row.PerformedByUserId is null or DBNull ? null : (string)row.PerformedByUserId,
            OccurredAtUtc = (DateTimeOffset)row.OccurredAtUtc
        };
    }
}
