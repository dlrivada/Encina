using System.Data;
using Dapper;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IResidencyAuditStore"/> for PostgreSQL.
/// Provides an immutable audit trail for data residency enforcement decisions.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Articles 44-49 (Chapter V - Transfers of personal data to third countries),
/// controllers must document and justify all cross-border data transfers. This audit store
/// records every residency enforcement decision, transfer validation, and policy violation
/// to support accountability obligations under Article 5(2).
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// <para>
/// PostgreSQL-specific considerations:
/// <list type="bullet">
/// <item><description>Lowercase unquoted column identifiers (PostgreSQL folds to lowercase).</description></item>
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (TIMESTAMP type).</description></item>
/// <item><description>Integer values use <see cref="Convert.ToInt32(object)"/> for safe conversion.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ResidencyAuditStoreDapper : IResidencyAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidencyAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: ResidencyAuditEntries).</param>
    /// <param name="timeProvider">The time provider for UTC time (unused, reserved for consistency).</param>
    public ResidencyAuditStoreDapper(
        IDbConnection connection,
        string tableName = "ResidencyAuditEntries",
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        ResidencyAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ResidencyAuditEntryMapper.ToEntity(entry);
            var sql = $@"
                INSERT INTO {_tableName}
                (id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details)
                VALUES
                (@Id, @EntityId, @DataCategory, @SourceRegion, @TargetRegion, @ActionValue, @OutcomeValue, @LegalBasis, @RequestType, @UserId, @TimestampUtc, @Details)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.EntityId,
                entity.DataCategory,
                entity.SourceRegion,
                entity.TargetRegion,
                entity.ActionValue,
                entity.OutcomeValue,
                entity.LegalBasis,
                entity.RequestType,
                entity.UserId,
                TimestampUtc = entity.TimestampUtc.UtcDateTime,
                entity.Details
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record residency audit entry: {ex.Message}",
                details: new Dictionary<string, object?> { ["entryId"] = entry.Id }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE entityid = @EntityId
                ORDER BY timestamputc DESC";

            var rows = await _connection.QueryAsync(sql, new { EntityId = entityId });
            var entries = rows
                .Select(row => ResidencyAuditEntryMapper.ToDomain(MapToEntity(row)))
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency audit entries by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetByDateRangeAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE timestamputc >= @FromUtc AND timestamputc <= @ToUtc
                ORDER BY timestamputc";

            var rows = await _connection.QueryAsync(sql, new
            {
                FromUtc = fromUtc.UtcDateTime,
                ToUtc = toUtc.UtcDateTime
            });

            var entries = rows
                .Select(row => ResidencyAuditEntryMapper.ToDomain(MapToEntity(row)))
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency audit entries by date range: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT id, entityid, datacategory, sourceregion, targetregion, actionvalue, outcomevalue, legalbasis, requesttype, userid, timestamputc, details
                FROM {_tableName}
                WHERE outcomevalue = @OutcomeValue
                ORDER BY timestamputc DESC";

            var rows = await _connection.QueryAsync(sql, new { OutcomeValue = (int)ResidencyOutcome.Blocked });
            var entries = rows
                .Select(row => ResidencyAuditEntryMapper.ToDomain(MapToEntity(row)))
                .Where(e => e is not null)
                .Cast<ResidencyAuditEntry>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<ResidencyAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to get residency audit violations: {ex.Message}",
                details: new Dictionary<string, object?>()));
        }
    }

    private static ResidencyAuditEntryEntity MapToEntity(dynamic row)
    {
        return new ResidencyAuditEntryEntity
        {
            Id = (string)row.id,
            EntityId = row.entityid is null or DBNull ? null : (string)row.entityid,
            DataCategory = (string)row.datacategory,
            SourceRegion = (string)row.sourceregion,
            TargetRegion = row.targetregion is null or DBNull ? null : (string)row.targetregion,
            ActionValue = Convert.ToInt32(row.actionvalue),
            OutcomeValue = Convert.ToInt32(row.outcomevalue),
            LegalBasis = row.legalbasis is null or DBNull ? null : (string)row.legalbasis,
            RequestType = row.requesttype is null or DBNull ? null : (string)row.requesttype,
            UserId = row.userid is null or DBNull ? null : (string)row.userid,
            TimestampUtc = new DateTimeOffset((DateTime)row.timestamputc, TimeSpan.Zero),
            Details = row.details is null or DBNull ? null : (string)row.details
        };
    }
}
