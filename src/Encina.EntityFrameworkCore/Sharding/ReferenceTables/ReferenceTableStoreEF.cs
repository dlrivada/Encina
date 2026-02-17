using Encina.Sharding.ReferenceTables;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Encina.EntityFrameworkCore.Sharding.ReferenceTables;

/// <summary>
/// EF Core implementation of <see cref="IReferenceTableStore"/> that uses
/// <see cref="DbContext"/> for entity persistence.
/// </summary>
/// <remarks>
/// <para>
/// The store uses the EF Core change tracker for upsert operations: for each entity,
/// it attempts to find an existing entity by primary key. If found, it updates the
/// tracked entity; otherwise, it adds a new entity. Changes are saved in a single
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> call.
/// </para>
/// <para>
/// Reads use <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>
/// for optimal performance since reference table data is read-only in the target shards.
/// </para>
/// </remarks>
public sealed class ReferenceTableStoreEF(DbContext dbContext) : IReferenceTableStore, IDisposable
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> UpsertAsync<TEntity>(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            var metadata = EntityMetadataCache.GetOrCreate<TEntity>();
            var entityList = entities as IList<TEntity> ?? entities.ToList();

            if (entityList.Count == 0)
            {
                return 0;
            }

            var set = _dbContext.Set<TEntity>();
            var count = 0;

            foreach (var entity in entityList)
            {
                var pkValue = metadata.PrimaryKey.Property.GetValue(entity);
                var existing = await set.FindAsync([pkValue], cancellationToken).ConfigureAwait(false);

                if (existing is not null)
                {
                    _dbContext.Entry(existing).CurrentValues.SetValues(entity);
                }
                else
                {
                    set.Add(entity);
                }

                count++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.UpsertFailed,
                $"Failed to upsert reference table '{typeof(TEntity).Name}': {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> GetAllAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            var results = await _dbContext.Set<TEntity>()
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return results.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.GetAllFailed,
                $"Failed to read reference table '{typeof(TEntity).Name}': {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, string>> GetHashAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var result = await GetAllAsync<TEntity>(cancellationToken).ConfigureAwait(false);

        return result.Map(entities => ReferenceTableHashComputer.ComputeHash(entities));
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _dbContext.Dispose();
}
