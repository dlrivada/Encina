using System.Data;
using System.Text;
using Dapper;
using Encina.Messaging;
using Encina.Security.Audit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Auditing;

/// <summary>
/// Dapper implementation of <see cref="IReadAuditStore"/> for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses SQL Server-specific syntax:
/// <list type="bullet">
/// <item><description>Square bracket identifier quoting (e.g., [EntityType])</description></item>
/// <item><description>Native UNIQUEIDENTIFIER for GUID storage</description></item>
/// <item><description>DATETIMEOFFSET for timestamps with timezone information</description></item>
/// <item><description>OFFSET/FETCH for pagination</description></item>
/// <item><description>@@ROWCOUNT for purge affected row count</description></item>
/// <item><description>LIKE with '+' concatenation for purpose filtering</description></item>
/// </list>
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

        // Build and cache SQL statements using SQL Server syntax (square brackets for identifiers)
        _insertSql = $@"
            INSERT INTO [{_tableName}]
            ([Id], [EntityType], [EntityId], [UserId], [TenantId], [AccessedAtUtc],
             [CorrelationId], [Purpose], [AccessMethod], [EntityCount], [Metadata])
            VALUES
            (@Id, @EntityType, @EntityId, @UserId, @TenantId, @AccessedAtUtc,
             @CorrelationId, @Purpose, @AccessMethod, @EntityCount, @Metadata)";

        _selectByEntitySql = $@"
            SELECT [Id], [EntityType], [EntityId], [UserId], [TenantId], [AccessedAtUtc],
                   [CorrelationId], [Purpose], [AccessMethod], [EntityCount], [Metadata]
            FROM [{_tableName}]
            WHERE [EntityType] = @EntityType AND [EntityId] = @EntityId
            ORDER BY [AccessedAtUtc] DESC";

        _selectByUserSql = $@"
            SELECT [Id], [EntityType], [EntityId], [UserId], [TenantId], [AccessedAtUtc],
                   [CorrelationId], [Purpose], [AccessMethod], [EntityCount], [Metadata]
            FROM [{_tableName}]
            WHERE [UserId] = @UserId
              AND [AccessedAtUtc] >= @FromUtc
              AND [AccessedAtUtc] <= @ToUtc
            ORDER BY [AccessedAtUtc] DESC";

        _purgeSql = $@"
            DELETE FROM [{_tableName}]
            WHERE [AccessedAtUtc] < @OlderThanUtc;
            SELECT @@ROWCOUNT;";
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

            var parameters = new
            {
                entity.Id,
                entity.EntityType,
                entity.EntityId,
                entity.UserId,
                entity.TenantId,
                entity.AccessedAtUtc,
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
            var entities = await _connection.QueryAsync<ReadAuditEntryEntity>(
                _selectByEntitySql,
                new { EntityType = entityType, EntityId = entityId });

            var entries = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();
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
            var entities = await _connection.QueryAsync<ReadAuditEntryEntity>(
                _selectByUserSql,
                new { UserId = userId, FromUtc = fromUtc, ToUtc = toUtc });

            var entries = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();
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
                whereClause.Append(" AND [UserId] = @UserId");
                parameters.Add("UserId", query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.TenantId))
            {
                whereClause.Append(" AND [TenantId] = @TenantId");
                parameters.Add("TenantId", query.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                whereClause.Append(" AND [EntityType] = @EntityType");
                parameters.Add("EntityType", query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                whereClause.Append(" AND [EntityId] = @EntityId");
                parameters.Add("EntityId", query.EntityId);
            }

            if (query.AccessMethod.HasValue)
            {
                whereClause.Append(" AND [AccessMethod] = @AccessMethod");
                parameters.Add("AccessMethod", (int)query.AccessMethod.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Purpose))
            {
                whereClause.Append(" AND [Purpose] LIKE '%' + @Purpose + '%'");
                parameters.Add("Purpose", query.Purpose);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                whereClause.Append(" AND [CorrelationId] = @CorrelationId");
                parameters.Add("CorrelationId", query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                whereClause.Append(" AND [AccessedAtUtc] >= @FromUtc");
                parameters.Add("FromUtc", query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                whereClause.Append(" AND [AccessedAtUtc] <= @ToUtc");
                parameters.Add("ToUtc", query.ToUtc.Value);
            }

            var whereClauseStr = whereClause.ToString();

            // Get total count
            var countSql = $"SELECT COUNT(*) FROM [{_tableName}] {whereClauseStr}";
            var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

            // Get paginated results using SQL Server OFFSET/FETCH
            var selectSql = $@"
                SELECT [Id], [EntityType], [EntityId], [UserId], [TenantId], [AccessedAtUtc],
                       [CorrelationId], [Purpose], [AccessMethod], [EntityCount], [Metadata]
                FROM [{_tableName}]
                {whereClauseStr}
                ORDER BY [AccessedAtUtc] DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            var entities = await _connection.QueryAsync<ReadAuditEntryEntity>(selectSql, parameters);
            var entries = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();

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
            var purgedCount = await _connection.ExecuteScalarAsync<int>(
                _purgeSql,
                new { OlderThanUtc = olderThanUtc });

            return Right(purgedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.PurgeFailed(ex.Message, ex));
        }
    }
}
