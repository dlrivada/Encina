using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DataSubjectRights;

/// <summary>
/// EF Core implementation of <see cref="IDSRRequestStore"/>.
/// Provider-agnostic — works with SQLite, SQL Server, PostgreSQL, and MySQL.
/// </summary>
public sealed class DSRRequestStoreEF : IDSRRequestStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DSRRequestStoreEF(
        DbContext dbContext,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<Either<EncinaError, Unit>> CreateAsync(
        DSRRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            var entity = DSRRequestMapper.ToEntity(request);
            entity.Id = request.Id;
            _dbContext.Set<DSRRequestEntity>().Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(DSRErrors.StoreError("Create", ex.Message));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Create", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, Option<DSRRequest>>> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        try
        {
            var entity = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (entity is null)
                return Right<EncinaError, Option<DSRRequest>>(None);

            var domain = DSRRequestMapper.ToDomain(entity);
            return domain is not null
                ? Right<EncinaError, Option<DSRRequest>>(Some(domain))
                : Right<EncinaError, Option<DSRRequest>>(None);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetById", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetBySubjectIdAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        try
        {
            var entities = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .Where(e => e.SubjectId == subjectId)
                .ToListAsync(cancellationToken);

            var results = MapEntities(entities);
            return Right<EncinaError, IReadOnlyList<DSRRequest>>(results);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetBySubjectId", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, Unit>> UpdateStatusAsync(
        string id,
        DSRRequestStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        try
        {
            var entity = await _dbContext.Set<DSRRequestEntity>()
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (entity is null)
                return Left(DSRErrors.RequestNotFound(id));

            var nowUtc = _timeProvider.GetUtcNow();

            switch (newStatus)
            {
                case DSRRequestStatus.Completed:
                    entity.StatusValue = (int)newStatus;
                    entity.CompletedAtUtc = nowUtc;
                    break;
                case DSRRequestStatus.Rejected:
                    entity.StatusValue = (int)newStatus;
                    entity.RejectionReason = reason;
                    entity.CompletedAtUtc = nowUtc;
                    break;
                case DSRRequestStatus.Extended:
                    entity.StatusValue = (int)newStatus;
                    entity.ExtensionReason = reason;
                    entity.ExtendedDeadlineAtUtc = entity.DeadlineAtUtc.AddMonths(2);
                    break;
                case DSRRequestStatus.IdentityVerified:
                    entity.StatusValue = (int)newStatus;
                    entity.VerifiedAtUtc = nowUtc;
                    break;
                default:
                    entity.StatusValue = (int)newStatus;
                    break;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(DSRErrors.StoreError("UpdateStatus", ex.Message));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("UpdateStatus", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            var entities = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .Where(e => pendingStatuses.Contains(e.StatusValue))
                .ToListAsync(cancellationToken);

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapEntities(entities));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetPendingRequests", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetOverdueRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _timeProvider.GetUtcNow();
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            var entities = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .Where(e => pendingStatuses.Contains(e.StatusValue)
                    && (e.ExtendedDeadlineAtUtc ?? e.DeadlineAtUtc) < nowUtc)
                .ToListAsync(cancellationToken);

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapEntities(entities));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetOverdueRequests", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, bool>> HasActiveRestrictionAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        try
        {
            var restrictionValue = (int)DataSubjectRight.Restriction;
            var pendingStatuses = new[]
            {
                (int)DSRRequestStatus.Received,
                (int)DSRRequestStatus.IdentityVerified,
                (int)DSRRequestStatus.InProgress,
                (int)DSRRequestStatus.Extended
            };

            var hasRestriction = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .AnyAsync(e => e.SubjectId == subjectId
                    && e.RightTypeValue == restrictionValue
                    && pendingStatuses.Contains(e.StatusValue),
                    cancellationToken);

            return Right<EncinaError, bool>(hasRestriction);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("HasActiveRestriction", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRRequest>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbContext.Set<DSRRequestEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Right<EncinaError, IReadOnlyList<DSRRequest>>(MapEntities(entities));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAll", ex.Message));
        }
    }

    private static List<DSRRequest> MapEntities(List<DSRRequestEntity> entities)
    {
        var results = new List<DSRRequest>();
        foreach (var entity in entities)
        {
            var domain = DSRRequestMapper.ToDomain(entity);
            if (domain is not null)
                results.Add(domain);
        }
        return results;
    }
}
