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
/// ADO.NET implementation of <see cref="IOperationAuditStore"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses raw NpgsqlCommand and NpgsqlDataReader for maximum performance.
/// SQL statements use PostgreSQL-specific syntax:
/// <list type="bullet">
/// <item><description>Double-quote identifier quoting (e.g., "EntityType")</description></item>
/// <item><description>UUID native type for GUID storage</description></item>
/// <item><description>TIMESTAMPTZ for <c>TimestampUtc</c>, <c>StartedAtUtc</c> and <c>CompletedAtUtc</c> (all UTC, timezone-aware)</description></item>
/// <item><description>LIMIT/OFFSET for pagination</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="RecordAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class OperationAuditStoreADO : IOperationAuditStore
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
    /// Initializes a new instance of the <see cref="OperationAuditStoreADO"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: OperationAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public OperationAuditStoreADO(IDbConnection connection, string tableName = "OperationAuditEntries")
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
            WHERE ""EntityType"" = @EntityType AND (@EntityId::text IS NULL OR ""EntityId"" = @EntityId)
            ORDER BY ""TimestampUtc"" DESC";

        _selectByUserSql = $@"
            SELECT ""Id"", ""CorrelationId"", ""UserId"", ""TenantId"", ""Action"", ""EntityType"", ""EntityId"",
                   ""Outcome"", ""ErrorMessage"", ""TimestampUtc"", ""StartedAtUtc"", ""CompletedAtUtc"",
                   ""IpAddress"", ""UserAgent"", ""RequestPayloadHash"", ""RequestPayload"", ""ResponsePayload"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""UserId"" = @UserId
              AND (@FromUtc::timestamptz IS NULL OR ""TimestampUtc"" >= @FromUtc)
              AND (@ToUtc::timestamptz IS NULL OR ""TimestampUtc"" <= @ToUtc)
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
        OperationAuditEntry entry,
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
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByEntityAsync(
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

            var entries = new List<OperationAuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByUserAsync(
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

            var entries = new List<OperationAuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<OperationAuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = _selectByCorrelationIdSql;
            AddParameter(command, "@CorrelationId", correlationId);

            var entries = new List<OperationAuditEntry>();

            if (_connection.State != ConnectionState.Open)
                await OpenConnectionAsync(cancellationToken);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(
                EncinaError.New($"Failed to query audit entries: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryAsync(
        OperationAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, OperationAuditQuery.MaxPageSize);
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

            var entries = new List<OperationAuditEntry>();
            using var reader = await ExecuteReaderAsync(selectCommand, cancellationToken);
            while (await ReadAsync(reader, cancellationToken))
            {
                entries.Add(MapToEntry(reader));
            }

            entries = ApplyDurationFilter(entries, query);

            countCommand.Dispose();
            return Right(PagedResult<OperationAuditEntry>.Create(entries, totalCount, pageNumber, pageSize));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, PagedResult<OperationAuditEntry>>(
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

    private (string whereClause, IDbCommand command) BuildWhereClauseAndCommand(OperationAuditQuery query)
    {
        var whereClause = new StringBuilder("WHERE 1=1");
        var command = _connection.CreateCommand();

        AddText(whereClause, command, "UserId", query.UserId);
        AddText(whereClause, command, "TenantId", query.TenantId);
        AddText(whereClause, command, "EntityType", query.EntityType);
        AddText(whereClause, command, "EntityId", query.EntityId);
        AddText(whereClause, command, "Action", query.Action);
        AddCriterion(whereClause, command, "Outcome", "=", "Outcome", query.Outcome is { } outcome ? (int)outcome : null);
        AddText(whereClause, command, "CorrelationId", query.CorrelationId);
        AddCriterion(whereClause, command, "TimestampUtc", ">=", "FromUtc", query.FromUtc);
        AddCriterion(whereClause, command, "TimestampUtc", "<=", "ToUtc", query.ToUtc);
        AddText(whereClause, command, "IpAddress", query.IpAddress);

        return (whereClause.ToString(), command);
    }

    private static void AddText(StringBuilder whereClause, IDbCommand command, string column, string? value) =>
        AddCriterion(whereClause, command, column, "=", column, string.IsNullOrWhiteSpace(value) ? null : value);

    private static void AddCriterion(
        StringBuilder whereClause,
        IDbCommand command,
        string column,
        string comparison,
        string parameterName,
        object? value)
    {
        if (value is null)
        {
            return;
        }

        whereClause.Append(" AND \"").Append(column).Append("\" ").Append(comparison).Append(" @").Append(parameterName);
        AddParameter(command, "@" + parameterName, value);
    }

    // Duration is computed, not stored, so it is filtered in memory.
    private static List<OperationAuditEntry> ApplyDurationFilter(IEnumerable<OperationAuditEntry> entries, OperationAuditQuery query) =>
        entries
            .Where(e => !query.MinDuration.HasValue || e.Duration >= query.MinDuration.Value)
            .Where(e => !query.MaxDuration.HasValue || e.Duration <= query.MaxDuration.Value)
            .ToList();

    private static string? GetNullableString(IDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
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

    private static OperationAuditEntry MapToEntry(IDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        CorrelationId = reader.GetString(reader.GetOrdinal("CorrelationId")),
        UserId = GetNullableString(reader, "UserId"),
        TenantId = GetNullableString(reader, "TenantId"),
        Action = reader.GetString(reader.GetOrdinal("Action")),
        EntityType = reader.GetString(reader.GetOrdinal("EntityType")),
        EntityId = GetNullableString(reader, "EntityId"),
        Outcome = (AuditOutcome)reader.GetInt32(reader.GetOrdinal("Outcome")),
        ErrorMessage = GetNullableString(reader, "ErrorMessage"),
        TimestampUtc = reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
        StartedAtUtc = GetDateTimeOffset(reader, reader.GetOrdinal("StartedAtUtc")),
        CompletedAtUtc = GetDateTimeOffset(reader, reader.GetOrdinal("CompletedAtUtc")),
        IpAddress = GetNullableString(reader, "IpAddress"),
        UserAgent = GetNullableString(reader, "UserAgent"),
        RequestPayloadHash = GetNullableString(reader, "RequestPayloadHash"),
        RequestPayload = GetNullableString(reader, "RequestPayload"),
        ResponsePayload = GetNullableString(reader, "ResponsePayload"),
        Metadata = DeserializeMetadata(GetNullableString(reader, "Metadata"))
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
