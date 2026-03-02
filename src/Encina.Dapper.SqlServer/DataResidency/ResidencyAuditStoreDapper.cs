using System.Data;

using Dapper;

using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using Encina.Messaging;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.DataResidency;

/// <summary>
/// Dapper implementation of <see cref="IResidencyAuditStore"/> for SQL Server.
/// Provides an immutable audit trail for all data residency enforcement decisions.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Articles 44-49 (Chapter V - Transfers of personal data to third countries),
/// controllers must ensure that cross-border data transfers comply with the conditions
/// set out in the Regulation. This store records all residency enforcement decisions,
/// cross-border transfer validations, region routing outcomes, and policy violations
/// to demonstrate compliance per Article 5(2) (accountability principle).
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of data residency compliance and may be required during regulatory audits or
/// supervisory authority inquiries (Article 58).
/// </para>
/// <para>
/// SQL Server-specific considerations:
/// <list type="bullet">
/// <item><description>DateTimeOffset values are read using <c>new DateTimeOffset((DateTime)row.Column, TimeSpan.Zero)</c>.</description></item>
/// <item><description>Integer values are read using <c>Convert.ToInt32(row.Column)</c>.</description></item>
/// <item><description>Nullable string values use <c>row.Column is null or DBNull</c> pattern.</description></item>
/// <item><description>Column names use PascalCase with [bracket] identifiers.</description></item>
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
                ([Id], [EntityId], [DataCategory], [SourceRegion], [TargetRegion], [ActionValue], [OutcomeValue], [LegalBasis], [RequestType], [UserId], [TimestampUtc], [Details])
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
                entity.TimestampUtc,
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
                SELECT [Id], [EntityId], [DataCategory], [SourceRegion], [TargetRegion], [ActionValue], [OutcomeValue], [LegalBasis], [RequestType], [UserId], [TimestampUtc], [Details]
                FROM {_tableName}
                WHERE [EntityId] = @EntityId
                ORDER BY [TimestampUtc] DESC";

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
                SELECT [Id], [EntityId], [DataCategory], [SourceRegion], [TargetRegion], [ActionValue], [OutcomeValue], [LegalBasis], [RequestType], [UserId], [TimestampUtc], [Details]
                FROM {_tableName}
                WHERE [TimestampUtc] >= @FromUtc AND [TimestampUtc] <= @ToUtc
                ORDER BY [TimestampUtc] DESC";

            var rows = await _connection.QueryAsync(sql, new { FromUtc = fromUtc, ToUtc = toUtc });
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
                details: new Dictionary<string, object?> { ["fromUtc"] = fromUtc.ToString("O"), ["toUtc"] = toUtc.ToString("O") }));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<ResidencyAuditEntry>>> GetViolationsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT [Id], [EntityId], [DataCategory], [SourceRegion], [TargetRegion], [ActionValue], [OutcomeValue], [LegalBasis], [RequestType], [UserId], [TimestampUtc], [Details]
                FROM {_tableName}
                WHERE [OutcomeValue] = @BlockedOutcome
                ORDER BY [TimestampUtc] DESC";

            var rows = await _connection.QueryAsync(sql, new { BlockedOutcome = (int)ResidencyOutcome.Blocked });
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
                message: $"Failed to get residency violation entries: {ex.Message}",
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
