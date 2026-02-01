using System.Data;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Microsoft.Data.Sqlite;

namespace Encina.ADO.Sqlite.Auditing;

/// <summary>
/// ADO.NET implementation of <see cref="IAuditLogStore"/> for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses raw SqliteCommand and SqliteDataReader for maximum performance.
/// SQL statements use SQLite-specific syntax with double-quote identifier quoting.
/// </para>
/// <para>
/// Each call to <see cref="LogAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class AuditLogStoreADO : IAuditLogStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _insertSql;
    private readonly string _selectSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit log table name (default: AuditLogs).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public AuditLogStoreADO(IDbConnection connection, string tableName = "AuditLogs")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements with double-quote quoting
        _insertSql = $@"
            INSERT INTO ""{_tableName}""
            (""Id"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""TimestampUtc"", ""OldValues"", ""NewValues"", ""CorrelationId"")
            VALUES
            (@Id, @EntityType, @EntityId, @Action, @UserId, @TimestampUtc, @OldValues, @NewValues, @CorrelationId)";

        _selectSql = $@"
            SELECT ""Id"", ""EntityType"", ""EntityId"", ""Action"", ""UserId"", ""TimestampUtc"", ""OldValues"", ""NewValues"", ""CorrelationId""
            FROM ""{_tableName}""
            WHERE ""EntityType"" = @EntityType AND ""EntityId"" = @EntityId
            ORDER BY ""TimestampUtc"" DESC";
    }

    /// <inheritdoc/>
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var command = _connection.CreateCommand();
        command.CommandText = _insertSql;
        AddParameter(command, "@Id", entry.Id);
        AddParameter(command, "@EntityType", entry.EntityType);
        AddParameter(command, "@EntityId", entry.EntityId);
        AddParameter(command, "@Action", (int)entry.Action);
        AddParameter(command, "@UserId", entry.UserId);
        AddParameter(command, "@TimestampUtc", entry.TimestampUtc);
        AddParameter(command, "@OldValues", entry.OldValues);
        AddParameter(command, "@NewValues", entry.NewValues);
        AddParameter(command, "@CorrelationId", entry.CorrelationId);

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(entityId);

        using var command = _connection.CreateCommand();
        command.CommandText = _selectSql;
        AddParameter(command, "@EntityType", entityType);
        AddParameter(command, "@EntityId", entityId);

        var entries = new List<AuditLogEntry>();

        if (_connection.State != ConnectionState.Open)
            await OpenConnectionAsync(cancellationToken);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        while (await ReadAsync(reader, cancellationToken))
        {
            entries.Add(new AuditLogEntry(
                Id: reader.GetString(reader.GetOrdinal("Id")),
                EntityType: reader.GetString(reader.GetOrdinal("EntityType")),
                EntityId: reader.GetString(reader.GetOrdinal("EntityId")),
                Action: (AuditAction)reader.GetInt32(reader.GetOrdinal("Action")),
                UserId: reader.IsDBNull(reader.GetOrdinal("UserId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("UserId")),
                TimestampUtc: reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
                OldValues: reader.IsDBNull(reader.GetOrdinal("OldValues"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("OldValues")),
                NewValues: reader.IsDBNull(reader.GetOrdinal("NewValues"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("NewValues")),
                CorrelationId: reader.IsDBNull(reader.GetOrdinal("CorrelationId"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("CorrelationId"))));
        }

        return entries;
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
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqliteCommand sqliteCommand)
            return await sqliteCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqliteDataReader sqliteReader)
            return await sqliteReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
