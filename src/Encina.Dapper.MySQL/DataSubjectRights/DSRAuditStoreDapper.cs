using System.Data;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRAuditStore"/> for MySQL.
/// Provides immutable audit trail persistence for DSR request processing.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MySQL-specific features:
/// <list type="bullet">
/// <item><description>PascalCase column identifiers</description></item>
/// <item><description>DATETIME for UTC datetime storage</description></item>
/// </list>
/// </para>
/// <para>
/// Audit entries should never be modified or deleted. They serve as legal evidence
/// of DSR processing and may be required during regulatory audits or supervisory
/// authority investigations (Article 5(2) accountability principle).
/// </para>
/// </remarks>
public sealed class DSRAuditStoreDapper : IDSRAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DSRAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The DSR audit entries table name (default: DSRAuditEntries).</param>
    public DSRAuditStoreDapper(
        IDbConnection connection,
        string tableName = "DSRAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = DSRAuditEntryMapper.ToEntity(entry);

            var sql = $@"
                INSERT INTO {_tableName}
                (Id, DSRRequestId, Action, Detail, PerformedByUserId, OccurredAtUtc)
                VALUES
                (@Id, @DSRRequestId, @Action, @Detail, @PerformedByUserId, @OccurredAtUtc)";

            await _connection.ExecuteAsync(sql, new
            {
                entity.Id,
                entity.DSRRequestId,
                entity.Action,
                entity.Detail,
                entity.PerformedByUserId,
                OccurredAtUtc = entity.OccurredAtUtc.UtcDateTime
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Record", ex.Message));
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsrRequestId);

        try
        {
            var sql = $@"
                SELECT Id, DSRRequestId, Action, Detail, PerformedByUserId, OccurredAtUtc
                FROM {_tableName}
                WHERE DSRRequestId = @DSRRequestId
                ORDER BY OccurredAtUtc";

            var rows = await _connection.QueryAsync(sql, new { DSRRequestId = dsrRequestId });
            var entries = rows.Select(MapToDomain).ToList();

            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }

    private static DSRAuditEntry MapToDomain(dynamic row)
    {
        var entity = new DSRAuditEntryEntity
        {
            Id = (string)row.Id,
            DSRRequestId = (string)row.DSRRequestId,
            Action = (string)row.Action,
            Detail = row.Detail is null or DBNull ? null : (string)row.Detail,
            PerformedByUserId = row.PerformedByUserId is null or DBNull ? null : (string)row.PerformedByUserId,
            OccurredAtUtc = new DateTimeOffset((DateTime)row.OccurredAtUtc, TimeSpan.Zero)
        };

        return DSRAuditEntryMapper.ToDomain(entity);
    }
}
