using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using Npgsql;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.Auditing;

/// <summary>
/// ADO.NET implementation of <see cref="IAuditStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance.
/// SQL statements use PostgreSQL-specific syntax:
/// <list type="bullet">
/// <item><description>Double-quote identifier quoting (e.g., "EntityType")</description></item>
/// <item><description>UUID native type for GUID storage</description></item>
/// <item><description>TIMESTAMPTZ for timestamps with timezone information</description></item>
/// <item><description>LIMIT/OFFSET for pagination</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="RecordAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class AuditStoreADO : IAuditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _insertSql;
    private readonly string _selectByEntitySql;
    private readonly string _selectByUserSql;
    private readonly string _selectByCorrelationIdSql;
    private readonly string _purgeSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: SecurityAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public AuditStoreADO(IDbConnection connection, string tableName = "SecurityAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements with double-quote quoting
        _insertSql = $@"
            INSERT INTO ""{_tableName}""
            (""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
             ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
             ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata"")
            VALUES
            (@Id, @CorrelationId, @UserId, @TenantId, @Action, @EntityType, @EntityId,
             @Outcome, @ErrorMessage, @TimestampUtc, @StartedAtUtc, @CompletedAtUtc,
             @IpAddress, @UserAgent, @RequestPayloadHash, @RequestPayload, @ResponsePayload, @Metadata)";

        _selectByEntitySql = $@"
            SELECT ""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
                   ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
                   ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""EntityType"" = @EntityType AND (@EntityId IS NULL OR ""EntityId"" = @EntityId)
            ORDER BY ""TimestampUtc"" DESC";

        _selectByUserSql = $@"
            SELECT ""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
                   ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
                   ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""UserId"" = @UserId
              AND (@FromUtc IS NULL OR ""TimestampUtc"" >= @FromUtc)
              AND (@ToUtc IS NULL OR ""TimestampUtc"" <= @ToUtc)
            ORDER BY ""TimestampUtc"" DESC";

        _selectByCorrelationIdSql = $@"
            SELECT ""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
                   ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
                   ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""CorrelationId"" = @CorrelationId
            ORDER BY ""TimestampUtc"" ASC";

        _purgeSql = $@"
            WITH deleted AS (
                DELETE FROM ""{_tableName}""
                WHERE ""TimestampUtc"" < @OlderThanUtc
                RETURNING 1
            )
            SELECT COUNT(*) FROM deleted";
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _insertSql;
            AddParameter(command, "@Id", entry.Id);
            AddParameter(command, "@CorrelationId", entry.CorrelationId);
            AddParameter(command, "@UserId", entry.UserId);
            AddParameter(command, "@TenantId", entry.TenantId);
            AddParameter(command, "@Action", entry.Action);
            AddParameter(command, "@EntityType", entry.EntityType);
            AddParameter(command, "@EntityId", entry.EntityId);
            AddParameter(command, "@Outcome", (int)entry.Outcome);
            AddParameter(command, "@ErrorMessage", entry.ErrorMessage);
            AddParameter(command, "@TimestampUtc", entry.TimestampUtc);
            AddParameter(command, "@StartedAtUtc", entry.StartedAtUtc);
            AddParameter(command, "@CompletedAtUtc", entry.CompletedAtUtc);
            AddParameter(command, "@IpAddress", entry.IpAddress);
            AddParameter(command, "@UserAgent", entry.UserAgent);
            AddParameter(command, "@RequestPayloadHash", entry.RequestPayloadHash);
            AddParameter(command, "@RequestPayload", entry.RequestPayload);
            AddParameter(command, "@ResponsePayload", entry.ResponsePayload);
            AddParameter(command, "@Metadata", SerializeMetadata(entry.Metadata));

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            await ExecuteNonQueryAsync(command, cancellationToken);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(EncinaError.New($"Failed to record audit entry: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByEntitySql;
            AddParameter(command, "@EntityType", entityType);
            AddParameter(command, "@EntityId", entityId);

            var entries = new List<AuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByUserSql;
            AddParameter(command, "@UserId", userId);
            AddParameter(command, "@FromUtc", fromUtc);
            AddParameter(command, "@ToUtc", toUtc);

            var entries = new List<AuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByCorrelationIdSql;
            AddParameter(command, "@CorrelationId", correlationId);

            var entries = new List<AuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, AuditQuery.MaxPageSize);
            var offset = (pageNumber - 1) * pageSize;

            // Build dynamic WHERE clause
            var (whereClauseStr, countCommand) = BuildWhereClauseAndCommand(query);

            // Get total count
            var countSql = $@"SELECT COUNT(*) FROM ""{_tableName}"" {whereClauseStr}";
            countCommand.CommandText = countSql;

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var totalCount = Convert.ToInt32(await ExecuteScalarAsync(countCommand, cancellationToken), CultureInfo.InvariantCulture);

            // Get paginated results using LIMIT/OFFSET
            var selectSql = $@"
                SELECT ""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
                       ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
                       ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata""
                FROM ""{_tableName}""
                {whereClauseStr}
                ORDER BY ""TimestampUtc"" DESC
                LIMIT @PageSize OFFSET @Offset";

            using var selectCommand = _connection.CreateCommand();
            selectCommand.CommandText = selectSql;

            // Copy parameters from count command
            CopyParameters(countCommand, selectCommand);
            AddParameter(selectCommand, "@Offset", offset);
            AddParameter(selectCommand, "@PageSize", pageSize);

            var entries = new List<AuditEntry>();
            using var reader = await ExecuteReaderAsync(selectCommand, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            // Apply duration filter in memory (Duration is computed, not stored)
            if (query.MinDuration.HasValue || query.MaxDuration.HasValue)
            {
                var filtered = entries.AsEnumerable();

                if (query.MinDuration.HasValue)
                {
                    filtered = filtered.Where(e => e.Duration >= query.MinDuration.Value);
                }

                if (query.MaxDuration.HasValue)
                {
                    filtered = filtered.Where(e => e.Duration <= query.MaxDuration.Value);
                }

                entries = filtered.ToList();
            }

            countCommand.Dispose();
            return Right(PagedResult<AuditEntry>.Create(entries, totalCount, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<AuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _purgeSql;
            AddParameter(command, "@OlderThanUtc", olderThanUtc);

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            var purgedCount = Convert.ToInt32(await ExecuteScalarAsync(command, cancellationToken), CultureInfo.InvariantCulture);
            return Right(purgedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                EncinaError.New($"Failed to purge audit entries: {ex.Message}"));
        }
    }

    private (string whereClause, IDbCommand command) BuildWhereClauseAndCommand(AuditQuery query)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var command = _connection.CreateCommand();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            whereClause.Append(@" AND ""UserId"" = @UserId");
            AddParameter(command, "@UserId", query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            whereClause.Append(@" AND ""TenantId"" = @TenantId");
            AddParameter(command, "@TenantId", query.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            whereClause.Append(@" AND ""EntityType"" = @EntityType");
            AddParameter(command, "@EntityType", query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            whereClause.Append(@" AND ""EntityId"" = @EntityId");
            AddParameter(command, "@EntityId", query.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            whereClause.Append(@" AND ""Action"" = @Action");
            AddParameter(command, "@Action", query.Action);
        }

        if (query.Outcome.HasValue)
        {
            whereClause.Append(@" AND ""Outcome"" = @Outcome");
            AddParameter(command, "@Outcome", (int)query.Outcome.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            whereClause.Append(@" AND ""CorrelationId"" = @CorrelationId");
            AddParameter(command, "@CorrelationId", query.CorrelationId);
        }

        if (query.FromUtc.HasValue)
        {
            whereClause.Append(@" AND ""TimestampUtc"" >= @FromUtc");
            AddParameter(command, "@FromUtc", query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            whereClause.Append(@" AND ""TimestampUtc"" <= @ToUtc");
            AddParameter(command, "@ToUtc", query.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            whereClause.Append(@" AND ""IpAddress"" = @IpAddress");
            AddParameter(command, "@IpAddress", query.IpAddress);
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

    private static AuditEntry MapToEntry(IDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        CorrelationId = reader.GetString(reader.GetOrdinal("CorrelationId")),
        UserId = reader.IsDBNull(reader.GetOrdinal("UserId"))
            ? null
            : reader.GetString(reader.GetOrdinal("UserId")),
        TenantId = reader.IsDBNull(reader.GetOrdinal("TenantId"))
            ? null
            : reader.GetString(reader.GetOrdinal("TenantId")),
        Action = reader.GetString(reader.GetOrdinal("Action")),
        EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
        EntityId = reader.IsDBNull(reader.GetOrdinal("EntityId"))
            ? null
            : reader.GetString(reader.GetOrdinal("EntityId")),
        Outcome = (AuditOutcome)reader.GetInt32(reader.GetOrdinal("Outcome")),
        ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
            ? null
            : reader.GetString(reader.GetOrdinal("ErrorMessage")),
        TimestampUtc = reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
        StartedAtUtc = GetDateTimeOffset(reader, reader.GetOrdinal("StartedAtUtc")),
        CompletedAtUtc = GetDateTimeOffset(reader, reader.GetOrdinal("CompletedAtUtc")),
        IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress"))
            ? null
            : reader.GetString(reader.GetOrdinal("IpAddress")),
        UserAgent = reader.IsDBNull(reader.GetOrdinal("UserAgent"))
            ? null
            : reader.GetString(reader.GetOrdinal("UserAgent")),
        RequestPayloadHash = reader.IsDBNull(reader.GetOrdinal("RequestPayloadHash"))
            ? null
            : reader.GetString(reader.GetOrdinal("RequestPayloadHash")),
        RequestPayload = reader.IsDBNull(reader.GetOrdinal("RequestPayload"))
            ? null
            : reader.GetString(reader.GetOrdinal("RequestPayload")),
        ResponsePayload = reader.IsDBNull(reader.GetOrdinal("ResponsePayload"))
            ? null
            : reader.GetString(reader.GetOrdinal("ResponsePayload")),
        Metadata = DeserializeMetadata(
            reader.IsDBNull(reader.GetOrdinal("Metadata"))
                ? null
                : reader.GetString(reader.GetOrdinal("Metadata")))
    };

    private static DateTimeOffset GetDateTimeOffset(IDataReader reader, int ordinal)
    {
        if (reader is NpgsqlDataReader npgsqlReader)
        {
            // PostgreSQL stores TIMESTAMPTZ which Npgsql returns as DateTime in UTC
            var dateTime = npgsqlReader.GetDateTime(ordinal);
            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }

        return new DateTimeOffset(reader.GetDateTime(ordinal), TimeSpan.Zero);
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
            return dict ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
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
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteReaderAsync(cancellationToken);

        return await Task.Run(command.ExecuteReader, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteNonQueryAsync(cancellationToken);

        return await Task.Run(command.ExecuteNonQuery, cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is NpgsqlCommand npgsqlCommand)
            return await npgsqlCommand.ExecuteScalarAsync(cancellationToken);

        return await Task.Run(command.ExecuteScalar, cancellationToken);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is NpgsqlDataReader npgsqlReader)
            return await npgsqlReader.ReadAsync(cancellationToken);

        return await Task.Run(reader.Read, cancellationToken);
    }
}
