using Encina.Security.Audit;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Entity Framework Core implementation of <see cref="IReadAuditStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses EF Core to persist read audit entries to the database.
/// It provides:
/// <list type="bullet">
/// <item><description>Immediate persistence via SaveChangesAsync for durability</description></item>
/// <item><description>Optimized queries with proper indexing via <see cref="ReadAuditEntryEntityConfiguration"/></description></item>
/// <item><description>Provider-agnostic support for SQLite, SQL Server, PostgreSQL, and MySQL</description></item>
/// <item><description>Full support for <see cref="ReadAuditQuery"/> with pagination</description></item>
/// <item><description>Bulk delete via ExecuteDeleteAsync for efficient purge operations</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Design Decision</b>: Each LogReadAsync call immediately persists the audit entry
/// to the database. This ensures audit entries are never lost, even if subsequent
/// operations fail.
/// </para>
/// </remarks>
public sealed class ReadAuditStoreEF : IReadAuditStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public ReadAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
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
            await _dbContext.Set<ReadAuditEntryEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Right(unit);
        }
        catch (DbUpdateException ex)
        {
            return Left(ReadAuditErrors.StoreError("LogRead", ex.Message, ex));
        }
        catch (OperationCanceledException)
        {
            return Left(ReadAuditErrors.StoreError("LogRead", "Operation was cancelled"));
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
            var entities = await _dbContext.Set<ReadAuditEntryEntity>()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderByDescending(e => e.AccessedAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetAccessHistory", "Operation was cancelled"));
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
            var entities = await _dbContext.Set<ReadAuditEntryEntity>()
                .Where(e => e.UserId == userId)
                .Where(e => e.AccessedAtUtc >= fromUtc)
                .Where(e => e.AccessedAtUtc <= toUtc)
                .OrderByDescending(e => e.AccessedAtUtc)
                .ToListAsync(cancellationToken);

            var entries = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();
            return Right<EncinaError, IReadOnlyList<ReadAuditEntry>>(entries);
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, IReadOnlyList<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("GetUserAccessHistory", "Operation was cancelled"));
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

            // Build query with filters
            var dbQuery = _dbContext.Set<ReadAuditEntryEntity>().AsQueryable();

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

            if (query.AccessMethod.HasValue)
            {
                var accessMethodInt = (int)query.AccessMethod.Value;
                dbQuery = dbQuery.Where(e => e.AccessMethod == accessMethodInt);
            }

            if (!string.IsNullOrWhiteSpace(query.Purpose))
            {
                dbQuery = dbQuery.Where(e => e.Purpose != null && e.Purpose.Contains(query.Purpose));
            }

            if (!string.IsNullOrWhiteSpace(query.CorrelationId))
            {
                dbQuery = dbQuery.Where(e => e.CorrelationId == query.CorrelationId);
            }

            if (query.FromUtc.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.AccessedAtUtc >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                dbQuery = dbQuery.Where(e => e.AccessedAtUtc <= query.ToUtc.Value);
            }

            // Get total count
            var totalCount = await dbQuery.CountAsync(cancellationToken);

            // Apply pagination
            var entities = await dbQuery
                .OrderByDescending(e => e.AccessedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = entities.Select(ReadAuditEntryMapper.MapToRecord).ToList();
            return Right(PagedResult<ReadAuditEntry>.Create(items, totalCount, pageNumber, pageSize));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, PagedResult<ReadAuditEntry>>(
                ReadAuditErrors.StoreError("Query", "Operation was cancelled"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use ExecuteDeleteAsync for efficient bulk delete (EF Core 7+)
            var purgedCount = await _dbContext.Set<ReadAuditEntryEntity>()
                .Where(e => e.AccessedAtUtc < olderThanUtc)
                .ExecuteDeleteAsync(cancellationToken);

            return Right(purgedCount);
        }
        catch (DbUpdateException ex)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.PurgeFailed(ex.Message, ex));
        }
        catch (OperationCanceledException)
        {
            return Left<EncinaError, int>(
                ReadAuditErrors.StoreError("PurgeEntries", "Operation was cancelled"));
        }
    }
}
