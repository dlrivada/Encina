using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.DataResidency;

/// <summary>
/// Entity Framework Core implementation of <see cref="IDataLocationStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the physical storage locations of data entities for GDPR compliance.
/// Per Article 30(1)(e), controllers must maintain records of processing activities
/// including transfers of personal data to a third country. Per Articles 44-49
/// (Chapter V), any transfer of personal data to a third country shall take place
/// only if the conditions laid down in this Chapter are complied with.
/// </para>
/// <para>
/// Uses EF Core LINQ queries for provider-agnostic data location management across
/// SQLite, SQL Server, PostgreSQL, and MySQL. All operations follow Railway Oriented
/// Programming with <c>Either&lt;EncinaError, T&gt;</c> return types.
/// </para>
/// <para>
/// Each write operation immediately persists via <see cref="DbContext.SaveChangesAsync"/>
/// to ensure location records are never lost. The store uses
/// <see cref="DataLocationMapper"/> to convert between domain and persistence models.
/// </para>
/// </remarks>
public sealed class DataLocationStoreEF : IDataLocationStore
{
    private readonly DbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLocationStoreEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="timeProvider">The time provider for UTC timestamps (default: <see cref="TimeProvider.System"/>).</param>
    public DataLocationStoreEF(DbContext dbContext, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> RecordAsync(
        DataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        try
        {
            var entity = DataLocationMapper.ToEntity(location);
            await _dbContext.Set<DataLocationEntity>().AddAsync(entity, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record data location: {ex.Message}",
                details: new Dictionary<string, object?> { ["locationId"] = location.Id, ["entityId"] = location.EntityId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to record data location: {ex.Message}",
                details: new Dictionary<string, object?> { ["locationId"] = location.Id }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            var entities = await _dbContext.Set<DataLocationEntity>()
                .Where(e => e.EntityId == entityId)
                .ToListAsync(cancellationToken);

            var locations = entities
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve data locations by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByRegionAsync(
        Region region,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(region);

        try
        {
            var entities = await _dbContext.Set<DataLocationEntity>()
                .Where(e => e.RegionCode == region.Code)
                .ToListAsync(cancellationToken);

            var locations = entities
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve data locations by region: {ex.Message}",
                details: new Dictionary<string, object?> { ["regionCode"] = region.Code }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<DataLocation>>> GetByCategoryAsync(
        string dataCategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataCategory);

        try
        {
            var entities = await _dbContext.Set<DataLocationEntity>()
                .Where(e => e.DataCategory == dataCategory)
                .ToListAsync(cancellationToken);

            var locations = entities
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to retrieve data locations by category: {ex.Message}",
                details: new Dictionary<string, object?> { ["dataCategory"] = dataCategory }));
        }
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeleteByEntityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        try
        {
            await _dbContext.Set<DataLocationEntity>()
                .Where(e => e.EntityId == entityId)
                .ExecuteDeleteAsync(cancellationToken);

            return Right(Unit.Default);
        }
        catch (DbUpdateException ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete data locations by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left(EncinaErrors.Create(
                code: "residency.store_error",
                message: $"Failed to delete data locations by entity: {ex.Message}",
                details: new Dictionary<string, object?> { ["entityId"] = entityId }));
        }
    }
}
