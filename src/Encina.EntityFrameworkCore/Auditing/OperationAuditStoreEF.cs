using System.Data.Common;
using System.Linq.Expressions;
using System.Text.Json;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOperationAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core to persist operation audit entries to the database.
/// It provides:
/// <list type="bullet">
/// <item><description>Immediate persistence via SaveChangesAsync for durability</description></item>
/// <item><description>Optimized queries with proper indexing</description></item>
/// <item><description>Provider-agnostic support for SQL Server, PostgreSQL, and MySQL</description></item>
/// <item><description>Full support for <see cref="OperationAuditQuery"/> with pagination</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Decision</b>: Each RecordAsync call immediately persists the audit entry
/// to the database. This ensures audit entries are never lost, even if subsequent
/// operations fail. This aligns with the existing pattern used by OutboxStoreEF
/// and InboxStoreEF.
/// </para>
/// </remarks>
public sealed class OperationAuditStoreEF : IOperationAuditStore
{
    /// <summary>
    /// Error code of the failures returned by <see cref="OperationAuditStoreEF"/> when a database write fails.
    /// </summary>
    internal const string StoreErrorCode = "audit.store_error";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public OperationAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        OperationAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = MapToEntity(entry);
            await _dbContext.Set<OperationAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Right(unit);
        }
        catch (DbUpdateException ex)
        {
            return Left(StoreError("Record", "Failed to record audit entry", ex));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaError.New("Operation was cancelled"));
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
            var query = _dbContext.Set<OperationAuditEntryEntity>()
                .Where(e => e.EntityType == entityType);

            if (entityId is not null)
            {
                query = query.Where(e => e.EntityId == entityId);
            }

            var entities = await query
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            var query = _dbContext.Set<OperationAuditEntryEntity>()
                .Where(e => e.UserId == userId);

            var from = AsUtc(fromUtc);
            var to = AsUtc(toUtc);

            if (from.HasValue)
            {
                query = query.Where(e => e.TimestampUtc >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.TimestampUtc <= to.Value);
            }

            var entities = await query
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            var entities = await _dbContext.Set<OperationAuditEntryEntity>()
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<OperationAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<OperationAuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            // Validate pagination
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, OperationAuditQuery.MaxPageSize);

            // Build query with filters
            var dbQuery = ApplyFilters(_dbContext.Set<OperationAuditEntryEntity>().AsQueryable(), query);

            // Duration filtering - must be done in memory since Duration is computed
            var needsDurationFilter = query.MinDuration.HasValue || query.MaxDuration.HasValue;

            return needsDurationFilter
                ? await QueryWithDurationFilterAsync(dbQuery, query, pageNumber, pageSize, cancellationToken)
                : await QueryPageAsync(dbQuery, pageNumber, pageSize, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, PagedResult<OperationAuditEntry>>(EncinaError.New("Operation was cancelled"));
        }
    }

    private static IQueryable<OperationAuditEntryEntity> ApplyFilters(
        IQueryable<OperationAuditEntryEntity> dbQuery,
        OperationAuditQuery query)
    {
        var from = AsUtc(query.FromUtc);
        var to = AsUtc(query.ToUtc);
        var filtered = dbQuery;
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.UserId), e => e.UserId == query.UserId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.TenantId), e => e.TenantId == query.TenantId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.EntityType), e => e.EntityType == query.EntityType);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.EntityId), e => e.EntityId == query.EntityId);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.Action), e => e.Action == query.Action);
        filtered = WhereIf(filtered, query.Outcome.HasValue, e => e.Outcome == query.Outcome!.Value);
        filtered = WhereIf(filtered, !string.IsNullOrWhiteSpace(query.CorrelationId), e => e.CorrelationId == query.CorrelationId);
        filtered = WhereIf(filtered, from.HasValue, e => e.TimestampUtc >= from!.Value);
        filtered = WhereIf(filtered, to.HasValue, e => e.TimestampUtc <= to!.Value);
        return WhereIf(filtered, !string.IsNullOrWhiteSpace(query.IpAddress), e => e.IpAddress == query.IpAddress);
    }

    private static IQueryable<OperationAuditEntryEntity> WhereIf(
        IQueryable<OperationAuditEntryEntity> source,
        bool condition,
        Expression<Func<OperationAuditEntryEntity, bool>> predicate) =>
        condition ? source.Where(predicate) : source;

    private static async ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryWithDurationFilterAsync(
        IQueryable<OperationAuditEntryEntity> dbQuery,
        OperationAuditQuery query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Fetch all matching entities and filter by duration in memory
        var allEntities = await dbQuery
            .OrderByDescending(e => e.TimestampUtc)
            .ToListAsync(cancellationToken);

        var filteredList = allEntities
            .Select(MapToRecord)
            .Where(e => !query.MinDuration.HasValue || e.Duration >= query.MinDuration.Value)
            .Where(e => !query.MaxDuration.HasValue || e.Duration <= query.MaxDuration.Value)
            .ToList();

        var items = filteredList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Right(PagedResult<OperationAuditEntry>.Create(items, filteredList.Count, pageNumber, pageSize));
    }

    private static async ValueTask<Either<EncinaError, PagedResult<OperationAuditEntry>>> QueryPageAsync(
        IQueryable<OperationAuditEntryEntity> dbQuery,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var entities = await dbQuery
            .OrderByDescending(e => e.TimestampUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(MapToRecord).ToList();
        return Right(PagedResult<OperationAuditEntry>.Create(items, totalCount, pageNumber, pageSize));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ExecuteDeleteAsync for efficient bulk delete (EF Core 7+)
            var cutoffUtc = AsUtc(olderThanUtc);
            var purgedCount = await _dbContext.Set<OperationAuditEntryEntity>()
                .Where(e => e.TimestampUtc < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            return Right(purgedCount);
        }
        catch (DbUpdateException ex)
        {
            return Left<EncinaError, int>(StoreError("PurgeEntries", "Failed to purge audit entries", ex));
        }
        catch (DbException ex)
        {
            // ExecuteDeleteAsync issues the DELETE directly, without EF Core's DbUpdateException wrapper,
            // so the provider's own DbException (e.g. a missing table or a constraint violation) surfaces here (#1128).
            return Left<EncinaError, int>(StoreError("PurgeEntries", "Failed to purge audit entries", ex));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, int>(EncinaError.New("Operation was cancelled"));
        }
    }

    /// <summary>
    /// Builds the error returned when a database write fails, keeping the exception and the message of its root cause.
    /// </summary>
    /// <param name="operation">The store operation that failed.</param>
    /// <param name="message">The failure summary.</param>
    /// <param name="exception">The exception thrown by EF Core.</param>
    /// <returns>An error with code <see cref="StoreErrorCode"/> that carries <paramref name="exception"/>.</returns>
    internal static EncinaError StoreError(string operation, string message, Exception exception) =>
        EncinaErrors.FromException(
            StoreErrorCode,
            exception,
            $"{message}: {StoreExceptionMessages.Describe(exception)}",
            new Dictionary<string, object?> { ["operation"] = operation });

    /// <summary>
    /// Maps an <see cref="OperationAuditEntry"/> record to an <see cref="OperationAuditEntryEntity"/>.
    /// </summary>
    internal static OperationAuditEntryEntity MapToEntity(OperationAuditEntry entry) => new()
    {
        Id = entry.Id,
        CorrelationId = entry.CorrelationId,
        UserId = entry.UserId,
        TenantId = entry.TenantId,
        Action = entry.Action,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        Outcome = entry.Outcome,
        ErrorMessage = entry.ErrorMessage,
        TimestampUtc = AsUtc(entry.TimestampUtc),
        StartedAtUtc = entry.StartedAtUtc.ToUniversalTime(),
        CompletedAtUtc = entry.CompletedAtUtc.ToUniversalTime(),
        IpAddress = entry.IpAddress,
        UserAgent = entry.UserAgent,
        RequestPayloadHash = entry.RequestPayloadHash,
        RequestPayload = entry.RequestPayload,
        ResponsePayload = entry.ResponsePayload,
        Metadata = SerializeMetadata(entry.Metadata)
    };

    /// <summary>
    /// Maps an <see cref="OperationAuditEntryEntity"/> to an <see cref="OperationAuditEntry"/> record.
    /// </summary>
    internal static OperationAuditEntry MapToRecord(OperationAuditEntryEntity entity) => new()
    {
        Id = entity.Id,
        CorrelationId = entity.CorrelationId,
        UserId = entity.UserId,
        TenantId = entity.TenantId,
        Action = entity.Action,
        EntityType = entity.EntityType,
        EntityId = entity.EntityId,
        Outcome = entity.Outcome,
        ErrorMessage = entity.ErrorMessage,
        TimestampUtc = AsUtc(entity.TimestampUtc),
        StartedAtUtc = entity.StartedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        RequestPayloadHash = entity.RequestPayloadHash,
        RequestPayload = entity.RequestPayload,
        ResponsePayload = entity.ResponsePayload,
        Metadata = DeserializeMetadata(entity.Metadata)
    };

    // A Local value is converted and an Unspecified value (what SQL Server and MySQL return) is taken as UTC:
    // the columns and properties are UTC by contract. Npgsql rejects a non-UTC DateTime for timestamptz.
    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;

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
}
