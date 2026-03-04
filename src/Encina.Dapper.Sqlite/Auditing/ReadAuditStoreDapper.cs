using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.Auditing;

/// <summary>
/// Dapper implementation of <see cref="IReadAuditStore"/> for SQLite.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses SQLite-specific syntax:
/// <list type="bullet">
/// <item><description>Double-quote identifier quoting (e.g., "EntityType")</description></item>
/// <item><description>TEXT for GUID storage (SQLite has no native GUID type)</description></item>
/// <item><description>TEXT for DateTimeOffset storage (ISO 8601 format)</description></item>
/// <item><description>LIMIT/OFFSET for pagination</description></item>
/// <item><description>LIKE with '||' concatenation for purpose filtering</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important</b>: DateTime comparisons use parameterized values from C# DateTimeOffset,
/// NOT SQLite's datetime('now') function, to ensure consistent ISO 8601 format matching.
/// </para>
/// <para>
/// Each call to <see cref="LogReadAsync"/> immediately persists the audit entry to the database.
/// </para>
/// </remarks>
public sealed class ReadAuditStoreDapper : IReadAuditStore
{
    private readonly IDbConnection _connection;
    private readonly string _tableName;
    private readonly string _insertSql;
    private readonly string _selectByEntitySql;
    private readonly string _selectByUserSql;
    private readonly string _purgeSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditStoreDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tableName">The read audit entries table name (default: ReadAuditEntries).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public ReadAuditStoreDapper(IDbConnection connection, string tableName = "ReadAuditEntries")
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _tableName = SqlIdentifierValidator.ValidateTableName(tableName);

        // Build and cache SQL statements using SQLite syntax (double-quotes for identifiers)
        _insertSql = $@"
            INSERT INTO ""{_tableName}""
            (""Id"", ""EntityType"", ""EntityId"", ""UserId"", ""TenantId"", ""AccessedAtUtc"",
             ""CorrelationId"", ""Purpose"", ""AccessMethod"", ""EntityCount"", ""Metadata"")
            VALUES
            (@Id, @EntityType, @EntityId, @UserId, @TenantId, @AccessedAtUtc,
             @CorrelationId, @Purpose, @AccessMethod, @EntityCount, @Metadata)";

        _selectByEntitySql = $@"
            SELECT ""Id"", ""EntityType"", ""EntityId"", ""UserId"", ""TenantId"", ""AccessedAtUtc"",
                   ""CorrelationId"", ""Purpose"", ""AccessMethod"", ""EntityCount"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""EntityType"" = @EntityType AND ""EntityId"" = @EntityId
            ORDER BY ""AccessedAtUtc"" DESC";

        _selectByUserSql = $@"
            SELECT ""Id"", ""EntityType"", ""EntityId"", ""UserId"", ""TenantId"", ""AccessedAtUtc"",
                   ""CorrelationId"", ""Purpose"", ""AccessMethod"", ""EntityCount"", ""Metadata""
            FROM ""{_tableName}""
            WHERE ""UserId"" = @UserId
              AND ""AccessedAtUtc"" >= @FromUtc
              AND ""AccessedAtUtc"" <= @ToUtc
            ORDER BY ""AccessedAtUtc"" DESC";

        // SQLite returns affected row count from ExecuteAsync
        _purgeSql = $@"
            DELETE FROM ""{_tableName}""
            WHERE ""AccessedAtUtc"" < @OlderThanUtc";
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

            // Convert GUID to string and DateTimeOffset to ISO 8601 for SQLite storage
            var parameters = new
            {
                Id = entity.Id.ToString(),
                entity.EntityType,
                entity.EntityId,
                entity.UserId,
                entity.TenantId,
                AccessedAtUtc = entity.AccessedAtUtc.ToString("o"),
                entity.CorrelationId,
                entity.Purpose,
                entity.AccessMethod,
                entity.EntityCount,
                entity.Metadata
            };

            await _connection.ExecuteAsync(_insertSql, parameters);
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
            var rows = await _connection.QueryAsync<ReadAuditEntryRow>(
                _selectByEntitySql,
                new { EntityType = entityType, EntityId = entityId });

            var entries = rows.Select(MapRowToRecord).ToList();
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
            var rows = await _connection.QueryAsync<ReadAuditEntryRow>(
                _selectByUserSql,
                new
                {
                    UserId = userId,
                    FromUtc = fromUtc.ToString("o"),
                    ToUtc = toUtc.ToString("o")
                });

            var entries = rows.Select(MapRowToRecord).ToList();
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
            var whereClause = new StringBuilder("WHERE 1=1");
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                whereClause.Append(" AND \"UserId\" = @UserId");
                parameters.Add("UserId", query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.TenantId))
            {
                whereClause.Append(" AND \"TenantId\" = @TenantId");
                parameters.Add("TenantId", query.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                whereClause.Append(" AND \"EntityType\" = @EntityType");
                parameters.Add("EntityType", query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                whereClause.Append(" AND \"EntityId\" = @EntityId");
                parameters.Add("EntityId", query.EntityId);
            }

            if (query.AccessMethod.HasValue)
            {
                whereClause.Append(" AND \"AccessMethod\" = @AccessMethod");
                parameters.Add("AccessMethod", (int)query.AccessMethod.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Purpose))
            {
                whereClause.Append(" AND \"Purpose\" LIKE '%' || @Purpose || '%'");
                parameters.Add("Purpose", query.Purpose);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                whereClause.Append(" AND \"CorrelationId\" = @CorrelationId");
                parameters.Add("CorrelationId", query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                whereClause.Append(" AND \"AccessedAtUtc\" >= @FromUtc");
                parameters.Add("FromUtc", query.FromUtc.Value.ToString("o"));
            }

            if (query.ToUtc.HasValue)
            {
                whereClause.Append(" AND \"AccessedAtUtc\" <= @ToUtc");
                parameters.Add("ToUtc", query.ToUtc.Value.ToString("o"));
            }

            var whereClauseStr = whereClause.ToString();

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM \"{_tableName}\" {whereClauseStr}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Get paginated results using SQLite LIMIT/OFFSET
            var selectSql = $@"
                SELECT ""Id"", ""EntityType"", ""EntityId"", ""UserId"", ""TenantId"", ""AccessedAtUtc"",
                       ""CorrelationId"", ""Purpose"", ""AccessMethod"", ""EntityCount"", ""Metadata""
                FROM ""{_tableName}""
                {whereClauseStr}
                ORDER BY ""AccessedAtUtc"" DESC
                LIMIT @PageSize OFFSET @Offset";

            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var rows = await _connection.QueryAsync<ReadAuditEntryRow>(selectSql, parameters);
            var entries = rows.Select(MapRowToRecord).ToList();

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
            // SQLite returns affected row count from ExecuteAsync
            var purgedCount = await _connection.ExecuteAsync(
                _purgeSql,
                new { OlderThanUtc = olderThanUtc.ToString("o") });

            return Right(purgedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.PurgeFailed(ex.Message, ex));
        }
    }

    private static ReadAuditEntry MapRowToRecord(ReadAuditEntryRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        EntityType = row.EntityType,
        EntityId = row.EntityId,
        UserId = row.UserId,
        TenantId = row.TenantId,
        AccessedAtUtc = DateTimeOffset.Parse(row.AccessedAtUtc, null, DateTimeStyles.RoundtripKind),
        CorrelationId = row.CorrelationId,
        Purpose = row.Purpose,
        AccessMethod = (ReadAccessMethod)row.AccessMethod,
        EntityCount = row.EntityCount,
        Metadata = ReadAuditEntryMapper.DeserializeMetadata(row.Metadata)
    };

    /// <summary>
    /// Internal row type for Dapper mapping.
    /// </summary>
    /// <remarks>
    /// SQLite stores GUIDs and DateTimeOffsets as TEXT, so we use string properties
    /// and convert during mapping.
    /// </remarks>
    private sealed class ReadAuditEntryRow
    {
        public required string Id { get; init; }
        public required string EntityType { get; init; }
        public string? EntityId { get; init; }
        public string? UserId { get; init; }
        public string? TenantId { get; init; }
        public required string AccessedAtUtc { get; init; }
        public string? CorrelationId { get; init; }
        public string? Purpose { get; init; }
        public int AccessMethod { get; init; }
        public int EntityCount { get; init; }
        public string? Metadata { get; init; }
    }
}
