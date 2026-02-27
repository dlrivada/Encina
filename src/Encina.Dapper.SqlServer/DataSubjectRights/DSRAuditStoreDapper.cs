using System.Data;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRAuditStore"/> for SQL Server.
/// Provides immutable audit trail for GDPR Data Subject Rights compliance.
/// </summary>
/// <remarks>
/// <para>
/// SQL Server natively supports <see cref="DateTimeOffset"/> via <c>DATETIME2(7)</c> columns,
/// so values are passed directly without string conversion.
/// </para>
/// <para>
/// Audit entries are immutable once recorded, supporting the GDPR accountability principle (Article 5(2)).
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
                entity.OccurredAtUtc
            });

            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("RecordAudit", ex.Message));
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

            var entries = rows.Select(row => DSRAuditEntryMapper.ToDomain(new DSRAuditEntryEntity
            {
                Id = (string)row.Id,
                DSRRequestId = (string)row.DSRRequestId,
                Action = (string)row.Action,
                Detail = row.Detail is null or DBNull ? null : (string)row.Detail,
                PerformedByUserId = row.PerformedByUserId is null or DBNull ? null : (string)row.PerformedByUserId,
                OccurredAtUtc = (DateTimeOffset)row.OccurredAtUtc
            })).ToList();

            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }
}
