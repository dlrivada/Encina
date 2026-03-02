using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MongoDB.Driver;

using static LanguageExt.Prelude;

namespace Encina.MongoDB.DataResidency;

/// <summary>
/// MongoDB implementation of <see cref="IDataLocationStore"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides persistence for <see cref="DataLocation"/> records in a MongoDB collection,
/// tracking where data entities are physically stored and processed. This enables the system
/// to verify compliance with residency policies and to provide evidence of data location
/// for regulatory audits.
/// </para>
/// <para>
/// Per GDPR Article 30(1)(e), the controller must maintain records of processing activities
/// including "where applicable, transfers of personal data to a third country". Per GDPR
/// Articles 44-49, any transfer of personal data to a third country shall take place only
/// if the conditions of Chapter V are complied with.
/// </para>
/// <para>
/// Uses <see cref="DataLocationEntity"/> as the MongoDB document type and
/// <see cref="DataLocationMapper"/> for domain-entity conversion. Documents are stored
/// using default MongoDB serialization conventions.
/// </para>
/// </remarks>
public sealed class DataLocationStoreMongoDB : IDataLocationStore
{
    private readonly IMongoCollection<DataLocationEntity> _collection;
    private readonly ILogger<DataLocationStoreMongoDB> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLocationStoreMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options.</param>
    /// <param name="logger">The logger.</param>
    public DataLocationStoreMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        ILogger<DataLocationStoreMongoDB> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var config = options.Value;
        var database = mongoClient.GetDatabase(config.DatabaseName);
        _collection = database.GetCollection<DataLocationEntity>(config.Collections.DataLocations);
        _logger = logger;
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
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Recorded data location '{LocationId}' for entity '{EntityId}' in region '{RegionCode}'",
                location.Id, location.EntityId, location.Region.Code);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("Record", ex.Message, ex));
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
            var filter = Builders<DataLocationEntity>.Filter.Eq(d => d.EntityId, entityId);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = documents
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(
                DataResidencyErrors.StoreError("GetByEntity", ex.Message, ex));
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
            var filter = Builders<DataLocationEntity>.Filter.Eq(d => d.RegionCode, region.Code);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = documents
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(
                DataResidencyErrors.StoreError("GetByRegion", ex.Message, ex));
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
            var filter = Builders<DataLocationEntity>.Filter.Eq(d => d.DataCategory, dataCategory);
            var documents = await _collection
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var locations = documents
                .Select(DataLocationMapper.ToDomain)
                .Where(l => l is not null)
                .Cast<DataLocation>()
                .ToList();

            return Right<EncinaError, IReadOnlyList<DataLocation>>(locations);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<DataLocation>>(
                DataResidencyErrors.StoreError("GetByCategory", ex.Message, ex));
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
            var filter = Builders<DataLocationEntity>.Filter.Eq(d => d.EntityId, entityId);
            await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Deleted all data location records for entity '{EntityId}'", entityId);
            return Right(unit);
        }
        catch (Exception ex)
        {
            return Left(DataResidencyErrors.StoreError("DeleteByEntity", ex.Message, ex));
        }
    }
}
