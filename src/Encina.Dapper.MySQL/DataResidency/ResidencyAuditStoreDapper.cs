using System.Data;
using Dapper;
using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;
using LanguageExt;
using System.Globalization;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IResidencyAuditStore"/> for MySQL.
/// Provides immutable audit trail for GDPR Article 5(2) accountability.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// <para>
/// MySQL-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are written via <c>.UtcDateTime</c> (DATETIME type).</description></item>
/// <item><description>Integer values require <c>Convert.ToInt32()</c> for safe casting from dynamic rows.</description></item>
/// <item><description>PascalCase column names in SQL queries.</description></item>
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
    /// <param name="tableName">The residency audit entries table name (default: ResidencyAuditEntries).</param>
    /// <param name="timeProvider">The time provider for UTC time (default: <see cref="TimeProvider.System"/>). Reserved for future use.</param>
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
                (`Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`)
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
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM {_tableName}
                WHERE `EntityId` = @EntityId
                ORDER BY `TimestampUtc` DESC";

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
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM {_tableName}
                WHERE `TimestampUtc` >= @FromUtc AND `TimestampUtc` <= @ToUtc
                ORDER BY `TimestampUtc` DESC";

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
                details: new Dictionary<string, object?> { ["fromUtc"] = fromUtc.ToString("O", CultureInfo.InvariantCulture), ["toUtc"] = toUtc.ToString("O", CultureInfo.InvariantCulture) }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT `Id`, `EntityId`, `DataCategory`, `SourceRegion`, `TargetRegion`, `ActionValue`, `OutcomeValue`, `LegalBasis`, `RequestType`, `UserId`, `TimestampUtc`, `Details`
                FROM {_tableName}
                WHERE `OutcomeValue` = @OutcomeValue
                ORDER BY `TimestampUtc` DESC";

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
            Id = (string)row.Id,
            EntityId = row.EntityId is null or DBNull ? null : (string)row.EntityId,
            DataCategory = (string)row.DataCategory,
            SourceRegion = (string)row.SourceRegion,
            TargetRegion = row.TargetRegion is null or DBNull ? null : (string)row.TargetRegion,
            ActionValue = Convert.ToInt32(row.ActionValue),
            OutcomeValue = Convert.ToInt32(row.OutcomeValue),
            LegalBasis = row.LegalBasis is null or DBNull ? null : (string)row.LegalBasis,
            RequestType = row.RequestType is null or DBNull ? null : (string)row.RequestType,
            UserId = row.UserId is null or DBNull ? null : (string)row.UserId,
            TimestampUtc = new DateTimeOffset((DateTime)row.TimestampUtc, TimeSpan.Zero),
            Details = row.Details is null or DBNull ? null : (string)row.Details
        };
    }
}
