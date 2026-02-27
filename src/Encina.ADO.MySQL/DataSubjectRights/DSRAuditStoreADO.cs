using System.Data;

using Encina.Compliance.DataSubjectRights;
using Encina.Messaging;

using LanguageExt;

using MySqlConnector;

using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.DataSubjectRights;

/// <summary>
/// ADO.NET implementation of <see cref="IDSRAuditStore"/> for MySQL.
/// Provides immutable audit trail for GDPR Data Subject Rights compliance.
/// </summary>
/// <remarks>
/// <para>
/// Stores DSR audit entries in a MySQL table with PascalCase column identifiers.
/// The audit trail is append-only and provides evidence of compliance with
/// GDPR obligations under the accountability principle (Article 5(2)).
/// </para>
/// <para>
/// Timestamps are stored as <c>DATETIME(6)</c> and converted between <see cref="DateTimeOffset"/>
/// and <see cref="DateTime"/> (UTC) for parameter binding.
/// </para>
/// </remarks>
public sealed class DSRAuditStoreADO : IDSRAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the <see cref="DSRAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The DSR audit entries table name (default: DSRAuditEntries).</param>
    public DSRAuditStoreADO(
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

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@DSRRequestId", entity.DSRRequestId);
            AddParameter(command, "@Action", entity.Action);
            AddParameter(command, "@Detail", (object?)entity.Detail ?? DBNull.Value);
            AddParameter(command, "@PerformedByUserId", (object?)entity.PerformedByUserId ?? DBNull.Value);
            AddParameter(command, "@OccurredAtUtc", entity.OccurredAtUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
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
                ORDER BY OccurredAtUtc ASC";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            AddParameter(command, "@DSRRequestId", dsrRequestId);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var entries = new List<DSRAuditEntry>();
            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                var entity = MapToEntity(reader);
                entries.Add(DSRAuditEntryMapper.ToDomain(entity));
            }

            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }

    private static DSRAuditEntryEntity MapToEntity(IDataReader reader)
    {
        return new DSRAuditEntryEntity
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            DSRRequestId = reader.GetString(reader.GetOrdinal("DSRRequestId")),
            Action = reader.GetString(reader.GetOrdinal("Action")),
            Detail = reader.IsDBNull(reader.GetOrdinal("Detail"))
                ? null
                : reader.GetString(reader.GetOrdinal("Detail")),
            PerformedByUserId = reader.IsDBNull(reader.GetOrdinal("PerformedByUserId"))
                ? null
                : reader.GetString(reader.GetOrdinal("PerformedByUserId")),
            OccurredAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("OccurredAtUtc")), TimeSpan.Zero)
        };
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Task OpenConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
