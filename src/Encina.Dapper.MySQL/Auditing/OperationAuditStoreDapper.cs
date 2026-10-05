using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Dapper;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.Auditing;

/// <summary>
/// Dapper implementation of <see cref="IOperationAuditStore"/> for MySQL/MariaDB.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses MySQL-specific syntax:
/// <list type="bullet">
/// <item><description>Backtick identifier quoting (e.g., `EntityType`)</description></item>
/// <item><description>CHAR(36) for GUID storage</description></item>
/// <item><description>DATETIME(6) for timestamps with microsecond precision</description></item>
/// <item><description>LIMIT/OFFSET for pagination</description></item>
/// </list>
/// </para>
/// <para>
/// Each call to <see cref="RecordAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class OperationAuditStoreDapper : IOperationAuditStore
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
    /// Initializes a new instance of the <see cref="OperationAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: OperationAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public OperationAuditStoreDapper(IDbConnection connection, string tableName = "OperationAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements using MySQL backtick syntax
        _insertSql = $@"
            INSERT INTO `{_tableName}`
            (`Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`,
             `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`,
             `IpAddress`, `UserAgent`, `RequestPayloadHash`, `RequestPayload`, `ResponsePayload`, `Metadata`)
            VALUES
            (@Id, @CorrelationId, @UserId, @TenantId, @Action, @EntityType, @EntityId,
             @Outcome, @ErrorMessage, @TimestampUtc, @StartedAtUtc, @CompletedAtUtc,
             @IpAddress, @UserAgent, @RequestPayloadHash, @RequestPayload, @ResponsePayload, @Metadata)";

        _selectByEntitySql = $@"
            SELECT `Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`,
                   `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`,
                   `IpAddress`, `UserAgent`, `RequestPayloadHash`, `RequestPayload`, `ResponsePayload`, `Metadata`
            FROM `{_tableName}`
            WHERE `EntityType` = @EntityType AND (@EntityId IS NULL OR `EntityId` = @EntityId)
            ORDER BY `TimestampUtc` DESC";

        _selectByUserSql = $@"
            SELECT `Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`,
                   `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`,
                   `IpAddress`, `UserAgent`, `RequestPayloadHash`, `RequestPayload`, `ResponsePayload`, `Metadata`
            FROM `{_tableName}`
            WHERE `UserId` = @UserId
              AND (@FromUtc IS NULL OR `TimestampUtc` >= @FromUtc)
              AND (@ToUtc IS NULL OR `TimestampUtc` <= @ToUtc)
            ORDER BY `TimestampUtc` DESC";

        _selectByCorrelationIdSql = $@"
            SELECT `Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`,
                   `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`,
                   `IpAddress`, `UserAgent`, `RequestPayloadHash`, `RequestPayload`, `ResponsePayload`, `Metadata`
            FROM `{_tableName}`
            WHERE `CorrelationId` = @CorrelationId
            ORDER BY `TimestampUtc` ASC";

        _purgeSql = $@"
            DELETE FROM `{_tableName}`
            WHERE `TimestampUtc` < @OlderThanUtc";
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        OperationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var parameters = new
            {
                entry.Id,
                entry.CorrelationId,
                entry.UserId,
                entry.TenantId,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                Outcome = (int)entry.Outcome,
                entry.ErrorMessage,
                entry.TimestampUtc,
                entry.StartedAtUtc,
                entry.CompletedAtUtc,
                entry.IpAddress,
                entry.UserAgent,
                entry.RequestPayloadHash,
                entry.RequestPayload,
                entry.ResponsePayload,
                Metadata = SerializeMetadata(entry.Metadata)
            };

            await _connection.ExecuteAsync(_insertSql, parameters);
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
            var rows = await _connection.QueryAsync<OperationAuditEntryRow>(
                _selectByEntitySql,
                new { EntityType = entityType, EntityId = entityId });

            var entries = rows.Select(MapToEntry).ToList();
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
            var rows = await _connection.QueryAsync<OperationAuditEntryRow>(
                _selectByUserSql,
                new { UserId = userId, FromUtc = fromUtc, ToUtc = toUtc });

            var entries = rows.Select(MapToEntry).ToList();
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
            var rows = await _connection.QueryAsync<OperationAuditEntryRow>(
                _selectByCorrelationIdSql,
                new { CorrelationId = correlationId });

            var entries = rows.Select(MapToEntry).ToList();
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

            var (whereClauseStr, parameters) = BuildWhereClause(query);

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM `{_tableName}` {whereClauseStr}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Get paginated results using MySQL LIMIT/OFFSET
            var selectSql = $@"
                SELECT `Id`, `CorrelationId`, `UserId`, `TenantId`, `Action`, `EntityType`, `EntityId`,
                       `Outcome`, `ErrorMessage`, `TimestampUtc`, `StartedAtUtc`, `CompletedAtUtc`,
                       `IpAddress`, `UserAgent`, `RequestPayloadHash`, `RequestPayload`, `ResponsePayload`, `Metadata`
                FROM `{_tableName}`
                {whereClauseStr}
                ORDER BY `TimestampUtc` DESC
                LIMIT @PageSize OFFSET @Offset";

            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var rows = await _connection.QueryAsync<OperationAuditEntryRow>(selectSql, parameters);
            var entries = ApplyDurationFilter(rows.Select(MapToEntry), query);

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
            // MySQL doesn't support RETURNING clause, use ROW_COUNT()
            var purgedCount = await _connection.ExecuteAsync(
                _purgeSql,
                new { OlderThanUtc = olderThanUtc });

            return Right(purgedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                EncinaError.New($"Failed to purge audit entries: {ex.Message}"));
        }
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(OperationAuditQuery query)
    {
        var where = new StringBuilder("WHERE 1=1");
        var parameters = new DynamicParameters();

        AddText(where, parameters, "UserId", query.UserId);
        AddText(where, parameters, "TenantId", query.TenantId);
        AddText(where, parameters, "EntityType", query.EntityType);
        AddText(where, parameters, "EntityId", query.EntityId);
        AddText(where, parameters, "Action", query.Action);
        AddCriterion(where, parameters, "Outcome", "=", "Outcome", query.Outcome is { } outcome ? (int)outcome : null);
        AddText(where, parameters, "CorrelationId", query.CorrelationId);
        AddCriterion(where, parameters, "TimestampUtc", ">=", "FromUtc", query.FromUtc);
        AddCriterion(where, parameters, "TimestampUtc", "<=", "ToUtc", query.ToUtc);
        AddText(where, parameters, "IpAddress", query.IpAddress);

        return (where.ToString(), parameters);
    }

    private static void AddText(StringBuilder where, DynamicParameters parameters, string column, string? value) =>
        AddCriterion(where, parameters, column, "=", column, string.IsNullOrWhiteSpace(value) ? null : value);

    private static void AddCriterion(
        StringBuilder where,
        DynamicParameters parameters,
        string column,
        string comparison,
        string parameterName,
        object? value)
    {
        if (value is null)
        {
            return;
        }

        where.Append(" AND `").Append(column).Append("` ").Append(comparison).Append(" @").Append(parameterName);
        parameters.Add(parameterName, value);
    }

    // Duration is computed, not stored, so it is filtered in memory.
    private static List<OperationAuditEntry> ApplyDurationFilter(IEnumerable<OperationAuditEntry> entries, OperationAuditQuery query) =>
        entries
            .Where(e => !query.MinDuration.HasValue || e.Duration >= query.MinDuration.Value)
            .Where(e => !query.MaxDuration.HasValue || e.Duration <= query.MaxDuration.Value)
            .ToList();

    private static OperationAuditEntry MapToEntry(OperationAuditEntryRow row) => new()
    {
        Id = row.Id,
        CorrelationId = row.CorrelationId,
        UserId = row.UserId,
        TenantId = row.TenantId,
        Action = row.Action,
        EntityType = row.EntityType,
        EntityId = row.EntityId,
        Outcome = (AuditOutcome)row.Outcome,
        ErrorMessage = row.ErrorMessage,
        TimestampUtc = row.TimestampUtc,
        StartedAtUtc = row.StartedAtUtc,
        CompletedAtUtc = row.CompletedAtUtc,
        IpAddress = row.IpAddress,
        UserAgent = row.UserAgent,
        RequestPayloadHash = row.RequestPayloadHash,
        RequestPayload = row.RequestPayload,
        ResponsePayload = row.ResponsePayload,
        Metadata = DeserializeMetadata(row.Metadata)
    };

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

    /// <summary>
    /// Internal row type for Dapper mapping.
    /// </summary>
    [SuppressMessage("SonarAnalyzer.CSharp", "S1144", Justification = "Properties set by Dapper via reflection")]
    [SuppressMessage("SonarAnalyzer.CSharp", "S3459", Justification = "Properties set by Dapper via reflection")]
    private sealed class OperationAuditEntryRow
    {
        public Guid Id { get; init; }
        public required string CorrelationId { get; init; }
        public string? UserId { get; init; }
        public string? TenantId { get; init; }
        public required string Action { get; init; }
        public required string EntityType { get; init; }
        public string? EntityId { get; init; }
        public int Outcome { get; init; }
        public string? ErrorMessage { get; init; }
        public DateTime TimestampUtc { get; init; }
        public DateTimeOffset StartedAtUtc { get; init; }
        public DateTimeOffset CompletedAtUtc { get; init; }
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
        public string? RequestPayloadHash { get; init; }
        public string? RequestPayload { get; init; }
        public string? ResponsePayload { get; init; }
        public string? Metadata { get; init; }
    }
}
