using System.Text.Json;
using Encina.Security.Audit;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core implementation of <see cref="IAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core to persist security audit entries to the database.
/// It provides:
/// <list type="bullet">
/// <item><description>Immediate persistence via SaveChangesAsync for durability</description></item>
/// <item><description>Optimized queries with proper indexing</description></item>
/// <item><description>Provider-agnostic support for SQLite, SQL Server, PostgreSQL, and MySQL</description></item>
/// <item><description>Full support for <see cref="AuditQuery"/> with pagination</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Decision</b>: Each RecordAsync call immediately persists the audit entry
/// to the database. This ensures audit entries are never lost, even if subsequent
/// operations fail. This aligns with the existing pattern used by OutboxStoreEF
/// and InboxStoreEF.
/// </para>
/// </remarks>
public sealed class AuditStoreEF : IAuditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public AuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var entity = MapToEntity(entry);
            await _dbContext.Set<AuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Right(unit);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaError.New($"Failed to record audit entry: {ex.Message}"));
        }
        catch (OperationCanceledException)
        {
            return Left(EncinaError.New("Operation was cancelled"));
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
            var query = _dbContext.Set<AuditEntryEntity>()
                .Where(e => e.EntityType == entityType);

            if (entityId is not null)
            {
                query = query.Where(e => e.EntityId == entityId);
            }

            var entities = await query
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            var query = _dbContext.Set<AuditEntryEntity>()
                .Where(e => e.UserId == userId);

            if (fromUtc.HasValue)
            {
                query = query.Where(e => e.TimestampUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(e => e.TimestampUtc <= toUtc.Value);
            }

            var entities = await query
                .OrderByDescending(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            var entities = await _dbContext.Set<AuditEntryEntity>()
                .Where(e => e.CorrelationId == correlationId)
                .OrderBy(e => e.TimestampUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<AuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<AuditEntry>>(EncinaError.New("Operation was cancelled"));
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
            // Validate pagination
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, AuditQuery.MaxPageSize);

            // Build query with filters
            var dbQuery = _dbContext.Set<AuditEntryEntity>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                dbQuery = dbQuery.Where(e => e.UserId == query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.TenantId))
            {
                dbQuery = dbQuery.Where(e => e.TenantId == query.TenantId);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                dbQuery = dbQuery.Where(e => e.EntityType == query.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                dbQuery = dbQuery.Where(e => e.EntityId == query.EntityId);
            }

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                dbQuery = dbQuery.Where(e => e.Action == query.Action);
            }

            if (query.Outcome.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.Outcome == query.Outcome.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                dbQuery = dbQuery.Where(e => e.CorrelationId == query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.TimestampUtc >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.TimestampUtc <= query.ToUtc.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.IpAddress))
            {
                dbQuery = dbQuery.Where(e => e.IpAddress == query.IpAddress);
            }

            // Duration filtering - must be done in memory since Duration is computed
            // Get entities first, then filter by duration if needed
            var needsDurationFilter = query.MinDuration.HasValue || query.MaxDuration.HasValue;

            if (needsDurationFilter)
            {
                // Fetch all matching entities and filter by duration in memory
                var allEntities = await dbQuery
                    .OrderByDescending(e => e.TimestampUtc)
                    .ToListAsync(cancellationToken);

                var filteredEntries = allEntities
                    .Select(MapToRecord)
                    .AsEnumerable();

                if (query.MinDuration.HasValue)
                {
                    filteredEntries = filteredEntries.Where(e => e.Duration >= query.MinDuration.Value);
                }

                if (query.MaxDuration.HasValue)
                {
                    filteredEntries = filteredEntries.Where(e => e.Duration <= query.MaxDuration.Value);
                }

                var filteredList = filteredEntries.ToList();
                var totalCount = filteredList.Count;

                var items = filteredList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Right(PagedResult<AuditEntry>.Create(items, totalCount, pageNumber, pageSize));
            }
            else
            {
                // Get total count
                var totalCount = await dbQuery.CountAsync(cancellationToken);

                // Apply pagination
                var entities = await dbQuery
                    .OrderByDescending(e => e.TimestampUtc)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var items = entities.Select(MapToRecord).ToList();
                return Right(PagedResult<AuditEntry>.Create(items, totalCount, pageNumber, pageSize));
            }
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, PagedResult<AuditEntry>>(EncinaError.New("Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ExecuteDeleteAsync for efficient bulk delete (EF Core 7+)
            var purgedCount = await _dbContext.Set<AuditEntryEntity>()
                .Where(e => e.TimestampUtc < olderThanUtc)
                .ExecuteDeleteAsync(cancellationToken);

            return Right(purgedCount);
        }
        catch (DbUpdateException ex)
        {
            return Left<EncinaError, int>(EncinaError.New($"Failed to purge audit entries: {ex.Message}"));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, int>(EncinaError.New("Operation was cancelled"));
        }
    }

    /// <summary>
    /// Maps an <see cref="AuditEntry"/> record to an <see cref="AuditEntryEntity"/>.
    /// </summary>
    internal static AuditEntryEntity MapToEntity(AuditEntry entry) => new()
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
        TimestampUtc = entry.TimestampUtc,
        StartedAtUtc = entry.StartedAtUtc,
        CompletedAtUtc = entry.CompletedAtUtc,
        IpAddress = entry.IpAddress,
        UserAgent = entry.UserAgent,
        RequestPayloadHash = entry.RequestPayloadHash,
        RequestPayload = entry.RequestPayload,
        ResponsePayload = entry.ResponsePayload,
        Metadata = SerializeMetadata(entry.Metadata)
    };

    /// <summary>
    /// Maps an <see cref="AuditEntryEntity"/> to an <see cref="AuditEntry"/> record.
    /// </summary>
    internal static AuditEntry MapToRecord(AuditEntryEntity entity) => new()
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
        TimestampUtc = entity.TimestampUtc,
        StartedAtUtc = entity.StartedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent,
        RequestPayloadHash = entity.RequestPayloadHash,
        RequestPayload = entity.RequestPayload,
        ResponsePayload = entity.ResponsePayload,
        Metadata = DeserializeMetadata(entity.Metadata)
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
}
