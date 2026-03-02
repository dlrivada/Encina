using System.Data;
using Dapper;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Retention;

/// <summary>
/// Dapper implementation of <see cref="IRetentionAuditStore"/> for PostgreSQL.
/// Provides immutable audit trail for GDPR Article 5(2) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of the data retention measures applied and may be required during regulatory
/// audits or DPIA reviews (Article 35).
/// </para>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase).</description></item>
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (TIMESTAMP type).</description></item>
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
                (id, action, entityid, datacategory, detail, performedbyuserid, occurredatutc)
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
                OccurredAtUtc = entity.OccurredAtUtc.UtcDateTime
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
                SELECT id, action, entityid, datacategory, detail, performedbyuserid, occurredatutc
                FROM {_tableName}
                WHERE entityid = @EntityId
                ORDER BY occurredatutc DESC";

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
                SELECT id, action, entityid, datacategory, detail, performedbyuserid, occurredatutc
                FROM {_tableName}
                ORDER BY occurredatutc DESC";

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
            Id = (string)row.id,
            Action = (string)row.action,
            EntityId = row.entityid is null or DBNull ? null : (string)row.entityid,
            DataCategory = row.datacategory is null or DBNull ? null : (string)row.datacategory,
            Detail = row.detail is null or DBNull ? null : (string)row.detail,
            PerformedByUserId = row.performedbyuserid is null or DBNull ? null : (string)row.performedbyuserid,
            OccurredAtUtc = new DateTimeOffset((DateTime)row.occurredatutc, TimeSpan.Zero)
        };
    }
}
