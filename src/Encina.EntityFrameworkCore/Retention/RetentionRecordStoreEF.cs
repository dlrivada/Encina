using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Retention;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRetentionRecordStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic retention record management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure retention records are never lost. The store uses
/// <see cref="RetentionRecordMapper"/> to convert between domain and persistence models.
/// </para>
/// <para>
/// Time-based queries (<see cref="GetExpiredRecordsAsync"/> and
/// <see cref="GetExpiringWithinAsync"/>) use <see cref="TimeProvider"/> for consistent,
/// testable time handling.
/// </para>
/// </remarks>
public sealed class RetentionRecordStoreEF : IRetentionRecordStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionRecordStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public RetentionRecordStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        RetentionRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            var entity = RetentionRecordMapper.ToEntity(record);
            await _dbContext.Set<RetentionRecordEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create retention record: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = record.Id, ["entityId"] = record.EntityId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to create retention record: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = record.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<RetentionRecord>>> GetByIdAsync(
        string recordId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var entity = await _dbContext.Set<RetentionRecordEntity>()
                .FirstOrDefaultAsync(e => e.Id == recordId, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<RetentionRecord>>(None);

            var domain = RetentionRecordMapper.ToDomain(entity);
            return Right<EncinaError, Option<RetentionRecord>>(domain is null ? None : Some(domain));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Option<RetentionRecord>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention record: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetByEntityIdAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var entities = await _dbContext.Set<RetentionRecordEntity>()
                .Where(e => e.EntityId == entityId)
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(RetentionRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention records by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiredRecordsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var activeStatus = (int)RetentionStatus.Active;

            var entities = await _dbContext.Set<RetentionRecordEntity>()
                .Where(e => e.ExpiresAtUtc < nowUtc && e.StatusValue == activeStatus)
                .OrderBy(e => e.ExpiresAtUtc)
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(RetentionRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve expired retention records: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetExpiringWithinAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var windowEnd = nowUtc + within;
            var activeStatus = (int)RetentionStatus.Active;

            var entities = await _dbContext.Set<RetentionRecordEntity>()
                .Where(e => e.ExpiresAtUtc >= nowUtc && e.ExpiresAtUtc <= windowEnd && e.StatusValue == activeStatus)
                .OrderBy(e => e.ExpiresAtUtc)
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(RetentionRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve expiring retention records: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string recordId,
        RetentionStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);

        try
        {
            var existing = await _dbContext.Set<RetentionRecordEntity>()
                .FirstOrDefaultAsync(e => e.Id == recordId, cancellationToken);

            if (existing is null)
            {
                return Left(EncinaErrors.Create(
                    code: "retention.not_found",
                    message: $"Retention record '{recordId}' not found",
                    details: new Dictionary<string, object?> { ["recordId"] = recordId }));
            }

            existing.StatusValue = (int)newStatus;

            if (newStatus == RetentionStatus.Deleted)
            {
                existing.DeletedAtUtc = _timeProvider.GetUtcNow();
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention record status: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId, ["newStatus"] = newStatus.ToString() }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to update retention record status: {ex.Message}",
                details: new Dictionary<string, object?> { ["recordId"] = recordId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<RetentionRecordEntity>()
                .ToListAsync(cancellationToken);

            var records = entities
                .Select(RetentionRecordMapper.ToDomain)
                .Where(r => r is not null)
                .Cast<RetentionRecord>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<RetentionRecord>>(records);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<RetentionRecord>>(EncinaErrors.Create(
                code: "retention.store_error",
                message: $"Failed to retrieve retention records: {ex.Message}"));
        }
    }
}
