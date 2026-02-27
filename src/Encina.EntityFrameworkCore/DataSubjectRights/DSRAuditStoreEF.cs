using Encina.Compliance.DataSubjectRights;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DataSubjectRights;

/// <summary>
/// EF Core implementation of <see cref="IDSRAuditStore"/>.
/// Provider-agnostic — works with SQLite, SQL Server, PostgreSQL, and MySQL.
/// </summary>
public sealed class DSRAuditStoreEF : IDSRAuditStore
{
    private readonly DbContext _dbContext;

    public DSRAuditStoreEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DSRAuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            var entity = DSRAuditEntryMapper.ToEntity(entry);
            _dbContext.Set<DSRAuditEntryEntity>().Add(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(DSRErrors.StoreError("Record", ex.Message));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("Record", ex.Message));
        }
    }

    public async ValueTask<Either<EncinaError, IReadOnlyList<DSRAuditEntry>>> GetAuditTrailAsync(
        string dsrRequestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dsrRequestId);
        try
        {
            var entities = await _dbContext.Set<DSRAuditEntryEntity>()
                .AsNoTracking()
                .Where(e => e.DSRRequestId == dsrRequestId)
                .OrderBy(e => e.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            var results = entities.Select(DSRAuditEntryMapper.ToDomain).ToList();
            return Right<EncinaError, IReadOnlyList<DSRAuditEntry>>(results);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return Left(DSRErrors.StoreError("GetAuditTrail", ex.Message));
        }
    }
}
