using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.MySQL.Auditing;

/// <summary>
/// Dapper implementation of <see cref="IAuditStore"/> for MySQL/MariaDB.
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
public sealed class AuditStoreDapper : IAuditStore
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
    /// Initializes a new instance of the <see cref="AuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The audit entries table name (default: SecurityAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public AuditStoreDapper(IDbConnection connection, string tableName = "SecurityAuditEntries")
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
        AuditEntry entry,
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
    public async ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        try
        {
            var rows = await _connection.QueryAsync<AuditEntryRow>(
                _selectByEntitySql,
                new { EntityType = entityType, EntityId = entityId });

            var entries = rows.Select(MapToEntry).ToList();
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
            var rows = await _connection.QueryAsync<AuditEntryRow>(
                _selectByUserSql,
                new { UserId = userId, FromUtc = fromUtc, ToUtc = toUtc });

            var entries = rows.Select(MapToEntry).ToList();
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
            var rows = await _connection.QueryAsync<AuditEntryRow>(
                _selectByCorrelationIdSql,
                new { CorrelationId = correlationId });

            var entries = rows.Select(MapToEntry).ToList();
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
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                whereClause.Append(" AND `UserId` = @UserId");
                parameters.Add("UserId", query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.TenantId))
            {
                whereClause.Append(" AND `TenantId` = @TenantId");
                parameters.Add("TenantId", query.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                whereClause.Append(" AND `EntityType` = @EntityType");
                parameters.Add("EntityType", query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                whereClause.Append(" AND `EntityId` = @EntityId");
                parameters.Add("EntityId", query.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                whereClause.Append(" AND `Action` = @Action");
                parameters.Add("Action", query.Action);
            }

            if (query.Outcome.HasValue)
            {
                whereClause.Append(" AND `Outcome` = @Outcome");
                parameters.Add("Outcome", (int)query.Outcome.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                whereClause.Append(" AND `CorrelationId` = @CorrelationId");
                parameters.Add("CorrelationId", query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                whereClause.Append(" AND `TimestampUtc` >= @FromUtc");
                parameters.Add("FromUtc", query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                whereClause.Append(" AND `TimestampUtc` <= @ToUtc");
                parameters.Add("ToUtc", query.ToUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                whereClause.Append(" AND `IpAddress` = @IpAddress");
                parameters.Add("IpAddress", query.IpAddress);
            }

            var whereClauseStr = whereClause.ToString();

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

            var rows = await _connection.QueryAsync<AuditEntryRow>(selectSql, parameters);
            var entries = rows.Select(MapToEntry).ToList();

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

    private static AuditEntry MapToEntry(AuditEntryRow row) => new()
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
    private sealed class AuditEntryRow
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
