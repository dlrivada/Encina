using System.Data;
using System.Globalization;
using Dapper;
using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.DataSubjectRights;

/// <summary>
/// Dapper implementation of <see cref="IDSRAuditStore"/> for SQLite.
/// Provides immutable audit trail for GDPR Data Subject Rights compliance.
/// </summary>
/// <remarks>
/// <para>
/// DateTime values are stored as ISO 8601 text strings using <c>.ToString("O")</c> for writes
/// and <see cref="DateTimeOffset.Parse(string, IFormatProvider?, DateTimeStyles)"/> with
/// <see cref="DateTimeStyles.RoundtripKind"/> for reads, ensuring roundtrip fidelity in SQLite.
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
                OccurredAtUtc = entity.OccurredAtUtc.ToString("O")
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
                OccurredAtUtc = DateTimeOffset.Parse((string)row.OccurredAtUtc, null, DateTimeStyles.RoundtripKind)
            })).ToList();

            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }
}
