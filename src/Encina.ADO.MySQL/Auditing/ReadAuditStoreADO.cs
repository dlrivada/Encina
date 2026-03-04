using System.Data;
using System.Globalization;
using System.Text;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.Auditing;

/// <summary>
/// ADO.NET implementation of <see cref="IReadAuditStore"/> for MySQL/MariaDB.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses raw MySqlCommand and MySqlDataReader for maximum performance.
/// SQL statements use MySQL-specific syntax:
/// <list type="bullet">
/// <item><description>Backtick identifier quoting (e.g., `EntityType`)</description></item>
/// <item><description>CHAR(36) for GUID storage</description></item>
/// <item><description>DATETIME(6) for timestamps with microsecond precision</description></item>
/// <item><description>INTEGER for enum storage (AccessMethod ordinal)</description></item>
/// <item><description>CONCAT for LIKE pattern construction</description></item>
/// <item><description>LIMIT/OFFSET for pagination</description></item>
/// </list>
/// </para>
/// <para>
/// Maps between <see cref="ReadAuditEntry"/> domain records and database rows using
/// <see cref="ReadAuditEntryMapper"/> for consistent serialization across providers.
/// </para>
/// </remarks>
public sealed class ReadAuditStoreADO : IReadAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _insertSql;
    private readonly string _selectByEntitySql;
    private readonly string _selectByUserSql;
    private readonly string _purgeSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The read audit entries table name (default: ReadAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public ReadAuditStoreADO(IDbConnection connection, string tableName = "ReadAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements with backtick quoting
        _insertSql = $@"
            INSERT INTO `{_tableName}`
            (`Id`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessedAtUtc`,
             `CorrelationId`, `Purpose`, `AccessMethod`, `EntityCount`, `Metadata`)
            VALUES
            (@Id, @EntityType, @EntityId, @UserId, @TenantId, @AccessedAtUtc,
             @CorrelationId, @Purpose, @AccessMethod, @EntityCount, @Metadata)";

        _selectByEntitySql = $@"
            SELECT `Id`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessedAtUtc`,
                   `CorrelationId`, `Purpose`, `AccessMethod`, `EntityCount`, `Metadata`
            FROM `{_tableName}`
            WHERE `EntityType` = @EntityType AND `EntityId` = @EntityId
            ORDER BY `AccessedAtUtc` DESC";

        _selectByUserSql = $@"
            SELECT `Id`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessedAtUtc`,
                   `CorrelationId`, `Purpose`, `AccessMethod`, `EntityCount`, `Metadata`
            FROM `{_tableName}`
            WHERE `UserId` = @UserId
              AND `AccessedAtUtc` >= @FromUtc
              AND `AccessedAtUtc` <= @ToUtc
            ORDER BY `AccessedAtUtc` DESC";

        _purgeSql = $@"
            DELETE FROM `{_tableName}`
            WHERE `AccessedAtUtc` < @OlderThanUtc";
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> LogReadAsync(
        ReadAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = ReadAuditEntryMapper.MapToEntity(entry);

            using var command = _connection.CreateCommand();
            command.CommandText = _insertSql;
            AddParameter(command, "@Id", entity.Id);
            AddParameter(command, "@EntityType", entity.EntityType);
            AddParameter(command, "@EntityId", entity.EntityId);
            AddParameter(command, "@UserId", entity.UserId);
            AddParameter(command, "@TenantId", entity.TenantId);
            AddParameter(command, "@AccessedAtUtc", entity.AccessedAtUtc.UtcDateTime);
            AddParameter(command, "@CorrelationId", entity.CorrelationId);
            AddParameter(command, "@Purpose", entity.Purpose);
            AddParameter(command, "@AccessMethod", entity.AccessMethod);
            AddParameter(command, "@EntityCount", entity.EntityCount);
            AddParameter(command, "@Metadata", entity.Metadata);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(ReadAuditErrors.StoreError("LogRead", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByEntitySql;
            AddParameter(command, "@EntityType", entityType);
            AddParameter(command, "@EntityId", entityId);

            var entries = new List<ReadAuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetAccessHistory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
        string userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByUserSql;
            AddParameter(command, "@UserId", userId);
            AddParameter(command, "@FromUtc", fromUtc.UtcDateTime);
            AddParameter(command, "@ToUtc", toUtc.UtcDateTime);

            var entries = new List<ReadAuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetUserAccessHistory", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
        ReadAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, ReadAuditQuery.MaxPageSize);
            var offset = (pageNumber - 1) * pageSize;

            // Build dynamic WHERE clause
            var (whereClauseStr, countCommand) = BuildWhereClauseAndCommand(query);

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM `{_tableName}` {whereClauseStr}";
            countCommand.CommandText = countSql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var totalCount = Convert.ToInt32(await ExecuteScalarAsync(countCommand, cancellationToken), CultureInfo.InvariantCulture);

            // Get paginated results using LIMIT/OFFSET
            var selectSql = $@"
                SELECT `Id`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessedAtUtc`,
                       `CorrelationId`, `Purpose`, `AccessMethod`, `EntityCount`, `Metadata`
                FROM `{_tableName}`
                {whereClauseStr}
                ORDER BY `AccessedAtUtc` DESC
                LIMIT @PageSize OFFSET @Offset";

            using var selectCommand = _connection.CreateCommand();
            selectCommand.CommandText = selectSql;

            // Copy parameters from count command
            CopyParameters(countCommand, selectCommand);
            AddParameter(selectCommand, "@Offset", offset);
            AddParameter(selectCommand, "@PageSize", pageSize);

            var entries = new List<ReadAuditEntry>();
            using var reader = await ExecuteReaderAsync(selectCommand, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            countCommand.Dispose();
            return Right(PagedResult<ReadAuditEntry>.Create(entries, totalCount, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("Query", ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _purgeSql;
            AddParameter(command, "@OlderThanUtc", olderThanUtc.UtcDateTime);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            // MySQL returns affected rows from ExecuteNonQuery
            var purgedCount = await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(purgedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.PurgeFailed(ex.Message, ex));
        }
    }

    private (string whereClause, IDbCommand command) BuildWhereClauseAndCommand(ReadAuditQuery query)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var command = _connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            whereClause.Append(" AND `UserId` = @UserId");
            AddParameter(command, "@UserId", query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            whereClause.Append(" AND `TenantId` = @TenantId");
            AddParameter(command, "@TenantId", query.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            whereClause.Append(" AND `EntityType` = @EntityType");
            AddParameter(command, "@EntityType", query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            whereClause.Append(" AND `EntityId` = @EntityId");
            AddParameter(command, "@EntityId", query.EntityId);
        }

        if (query.AccessMethod.HasValue)
        {
            whereClause.Append(" AND `AccessMethod` = @AccessMethod");
            AddParameter(command, "@AccessMethod", (int)query.AccessMethod.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Purpose))
        {
            whereClause.Append(" AND `Purpose` LIKE CONCAT('%', @Purpose, '%')");
            AddParameter(command, "@Purpose", query.Purpose);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            whereClause.Append(" AND `CorrelationId` = @CorrelationId");
            AddParameter(command, "@CorrelationId", query.CorrelationId);
        }

        if (query.FromUtc.HasValue)
        {
            whereClause.Append(" AND `AccessedAtUtc` >= @FromUtc");
            AddParameter(command, "@FromUtc", query.FromUtc.Value.UtcDateTime);
        }

        if (query.ToUtc.HasValue)
        {
            whereClause.Append(" AND `AccessedAtUtc` <= @ToUtc");
            AddParameter(command, "@ToUtc", query.ToUtc.Value.UtcDateTime);
        }

        return (whereClause.ToString(), command);
    }

    private static void CopyParameters(IDbCommand source, IDbCommand destination)
    {
        foreach (IDbDataParameter param in source.Parameters)
        {
            var newParam = destination.CreateParameter();
            newParam.ParameterName = param.ParameterName;
            newParam.Value = param.Value;
            destination.Parameters.Add(newParam);
        }
    }

    private static ReadAuditEntry MapToEntry(IDataReader reader)
    {
        var entity = new ReadAuditEntryEntity
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
            EntityId = reader.IsDBNull(reader.GetOrdinal("EntityId"))
                ? null
                : reader.GetString(reader.GetOrdinal("EntityId")),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId"))
                ? null
                : reader.GetString(reader.GetOrdinal("UserId")),
            TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
                ? null
                : reader.GetString(reader.GetOrdinal("TenantId")),
            // MySqlConnector returns DateTime; convert to DateTimeOffset
            AccessedAtUtc = new DateTimeOffset(reader.GetDateTime(reader.GetOrdinal("AccessedAtUtc")), TimeSpan.Zero),
            CorrelationId = reader.IsDBNull(reader.GetOrdinal("CorrelationId"))
                ? null
                : reader.GetString(reader.GetOrdinal("CorrelationId")),
            Purpose = reader.IsDBNull(reader.GetOrdinal("Purpose"))
                ? null
                : reader.GetString(reader.GetOrdinal("Purpose")),
            AccessMethod = reader.GetInt32(reader.GetOrdinal("AccessMethod")),
            EntityCount = reader.GetInt32(reader.GetOrdinal("EntityCount")),
            Metadata = reader.IsDBNull(reader.GetOrdinal("Metadata"))
                ? null
                : reader.GetString(reader.GetOrdinal("Metadata"))
        };

        return ReadAuditEntryMapper.MapToRecord(entity);
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

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is MySqlCommand mysqlCommand)
            return await mysqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is MySqlDataReader mysqlReader)
            return await mysqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
