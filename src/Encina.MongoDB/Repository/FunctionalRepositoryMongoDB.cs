using System.Linq.Expressions;
using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.DomainModeling.Concurrency;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Repository;

/// <summary>
/// MongoDB implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with Railway Oriented Programming error handling.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository uses MongoDB's <see cref="IMongoCollection{TDocument}"/> for persistence.
/// Entities must have an ID property that can be mapped to MongoDB's <c>_id</c> field.
/// </para>
/// <para>
/// For specification-based queries, the <see cref="SpecificationFilterBuilder{TEntity}"/>
/// translates specifications to MongoDB filter definitions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure in DI
/// services.AddEncinaRepository&lt;Order, Guid&gt;(config =&gt;
/// {
///     config.CollectionName = "orders";
///     config.IdProperty = o =&gt; o.Id;
/// });
///
/// // Use repository
/// public class OrderService(IFunctionalRepository&lt;Order, Guid&gt; repository)
/// {
///     public Task&lt;Either&lt;EncinaError, Order&gt;&gt; GetOrderAsync(Guid id, CancellationToken ct)
///         =&gt; repository.GetByIdAsync(id, ct);
/// }
/// </code>
/// </example>
public sealed class FunctionalRepositoryMongoDB<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly Expression<Func<TEntity, TId>> _idSelector;
    private readonly Func<TEntity, TId> _compiledIdSelector;
    private readonly SpecificationFilterBuilder<TEntity> _filterBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalRepositoryMongoDB{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps. Defaults to <see cref="TimeProvider.System"/>.</param>
    public FunctionalRepositoryMongoDB(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TId>> idSelector,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(idSelector);

        _collection = collection;
        _idSelector = idSelector;
        _compiledIdSelector = idSelector.Compile();
        _filterBuilder = new SpecificationFilterBuilder<TEntity>();
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = BuildIdFilter(id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(MapMongoException<TId>(ex, id, "GetById"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _collection.Find(Builders<TEntity>.Filter.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                MapMongoException(ex, "List"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var entities = await _collection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("List", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                MapMongoException(ex, "List"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var entity = await _collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(
                    RepositoryErrors.NotFound<TEntity>($"specification: {specification.GetType().Name}"));
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.InvalidOperation<TEntity>("FirstOrDefault", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(
                MapMongoException(ex, "FirstOrDefault"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                Builders<TEntity>.Filter.Empty,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>((int)count);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(MapMongoException(ex, "Count"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var count = await _collection.CountDocumentsAsync(
                filter,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>((int)count);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("Count", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(MapMongoException(ex, "Count"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);
            var count = await _collection.CountDocumentsAsync(
                filter,
                new CountOptions { Limit = 1 },
                cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.InvalidOperation<TEntity>("Any", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, bool>(MapMongoException(ex, "Any"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _collection.CountDocumentsAsync(
                Builders<TEntity>.Filter.Empty,
                new CountOptions { Limit = 1 },
                cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(count > 0);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, bool>(MapMongoException(ex, "Any"));
        }
    }

    #endregion

    #region Write Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);

            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoWriteException ex) when (IsDuplicateKeyException(ex))
        {
            var id = _compiledIdSelector(entity);
            return Left<EncinaError, TEntity>(RepositoryErrors.AlreadyExists<TEntity, TId>(id));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(MapMongoException(ex, "Add"));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// For entities implementing <see cref="IVersionedEntity"/>, this method performs
    /// optimistic concurrency control by including the version in the filter predicate.
    /// If the version doesn't match, a concurrency conflict error is returned.
    /// </para>
    /// <para>
    /// The version is automatically incremented before the update operation.
    /// </para>
    /// </remarks>
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Populate audit fields before persistence
            AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

            var id = _compiledIdSelector(entity);
            var idFilter = BuildIdFilter(id);

            // Build versioned filter if entity implements IVersionedEntity
            long? originalVersion = null;
            FilterDefinition<TEntity> filter;

            if (entity is IVersionedEntity versionedEntity)
            {
                originalVersion = versionedEntity.Version;
                filter = BuildVersionedFilter(idFilter, originalVersion.Value);

                // Increment version before save
                versionedEntity.Version = (int)(originalVersion.Value + 1);
            }
            else if (entity is IVersioned versioned)
            {
                // For IVersioned (getter-only), we can still check but not increment
                originalVersion = versioned.Version;
                filter = BuildVersionedFilter(idFilter, originalVersion.Value);
            }
            else
            {
                filter = idFilter;
            }

            var result = await _collection.ReplaceOneAsync(
                filter,
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                // Determine if it's a "not found" or "concurrency conflict"
                if (originalVersion.HasValue)
                {
                    // Check if entity exists with a different version
                    var existsFilter = idFilter;
                    var exists = await _collection.CountDocumentsAsync(
                        existsFilter,
                        new CountOptions { Limit = 1 },
                        cancellationToken).ConfigureAwait(false);

                    if (exists > 0)
                    {
                        // Entity exists but version doesn't match - concurrency conflict
                        var databaseEntity = await _collection.Find(existsFilter)
                            .FirstOrDefaultAsync(cancellationToken)
                            .ConfigureAwait(false);

                        var conflictInfo = new ConcurrencyConflictInfo<TEntity>(
                            CurrentEntity: entity, // The entity before increment
                            ProposedEntity: entity, // The entity we tried to save
                            DatabaseEntity: databaseEntity);

                        return Left<EncinaError, TEntity>(
                            RepositoryErrors.ConcurrencyConflict(conflictInfo));
                    }
                }

                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(MapMongoException(ex, "Update"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = BuildIdFilter(id);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            if (result.DeletedCount == 0)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, Unit>(MapMongoException<TId>(ex, id, "Delete"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var id = _compiledIdSelector(entity);
        return await DeleteAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Populate audit fields before persistence
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);
            }

            await _collection.InsertManyAsync(entityList, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entityList);
        }
        catch (MongoBulkWriteException ex) when (HasDuplicateKeyError(ex))
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.AlreadyExists<TEntity>("One or more entities already exist"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                MapMongoException(ex, "AddRange"));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// For entities implementing <see cref="IVersionedEntity"/>, this method performs
    /// optimistic concurrency control by including the version in each filter predicate.
    /// If any version doesn't match, a concurrency conflict error is returned.
    /// </para>
    /// <para>
    /// The version is automatically incremented for each entity before the update operation.
    /// </para>
    /// </remarks>
    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        var originalVersions = new Dictionary<TEntity, long>();

        try
        {
            // Populate audit fields and handle versioning before persistence
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForUpdate(entity, _requestContext?.UserId, _timeProvider);

                // Store original version and increment for IVersionedEntity
                if (entity is IVersionedEntity versionedEntity)
                {
                    originalVersions[entity] = versionedEntity.Version;
                    versionedEntity.Version = (int)(versionedEntity.Version + 1);
                }
                else if (entity is IVersioned versioned)
                {
                    originalVersions[entity] = versioned.Version;
                }
            }

            var bulkOps = entityList.Select(entity =>
            {
                var id = _compiledIdSelector(entity);
                var idFilter = BuildIdFilter(id);

                // Use versioned filter if we have original version info
                var filter = originalVersions.TryGetValue(entity, out var originalVersion)
                    ? BuildVersionedFilter(idFilter, originalVersion)
                    : idFilter;

                return new ReplaceOneModel<TEntity>(filter, entity) { IsUpsert = false };
            }).ToList();

            if (bulkOps.Count > 0)
            {
                var result = await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Check for concurrency conflicts (matched count less than entity count)
                if (result.MatchedCount < entityList.Count && originalVersions.Count > 0)
                {
                    // Find the first entity that wasn't matched (potential concurrency conflict)
                    foreach (var entity in entityList)
                    {
                        var id = _compiledIdSelector(entity);
                        var idFilter = BuildIdFilter(id);

                        var exists = await _collection.CountDocumentsAsync(
                            idFilter,
                            new CountOptions { Limit = 1 },
                            cancellationToken).ConfigureAwait(false);

                        if (exists > 0 && originalVersions.ContainsKey(entity))
                        {
                            // Entity exists but version didn't match - concurrency conflict
                            var databaseEntity = await _collection.Find(idFilter)
                                .FirstOrDefaultAsync(cancellationToken)
                                .ConfigureAwait(false);

                            var conflictInfo = new ConcurrencyConflictInfo<TEntity>(
                                CurrentEntity: entity,
                                ProposedEntity: entity,
                                DatabaseEntity: databaseEntity);

                            return Left<EncinaError, Unit>(
                                RepositoryErrors.ConcurrencyConflict(conflictInfo));
                        }
                    }
                }
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, Unit>(MapMongoException(ex, "UpdateRange"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);

            // Safety check: prevent accidental deletion of all documents
            if (filter == Builders<TEntity>.Filter.Empty)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange",
                        "DELETE requires a filter to prevent accidental data loss."));
            }

            var result = await _collection.DeleteManyAsync(filter, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>((int)result.DeletedCount);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.InvalidOperation<TEntity>("DeleteRange", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, int>(MapMongoException(ex, "DeleteRange"));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported for MongoDB providers because they lack change tracking.
    /// Use <see cref="ImmutableAggregateHelper.PrepareForUpdate{TAggregate}"/> followed by the
    /// standard <c>UpdateAsync</c> method instead.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);

        return Task.FromResult<Either<EncinaError, Unit>>(
            RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutableAsync"));
    }

    #endregion

    #region Private Helpers

    private FilterDefinition<TEntity> BuildIdFilter(TId id)
    {
        // Build a filter using the ID selector expression
        var parameter = _idSelector.Parameters[0];
        var body = Expression.Equal(_idSelector.Body, Expression.Constant(id, typeof(TId)));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);

        return Builders<TEntity>.Filter.Where(lambda);
    }

    /// <summary>
    /// Builds a filter that combines the ID filter with a version equality check.
    /// </summary>
    /// <param name="idFilter">The base ID filter.</param>
    /// <param name="expectedVersion">The expected version for optimistic concurrency.</param>
    /// <returns>A combined filter that matches both ID and version.</returns>
    private static FilterDefinition<TEntity> BuildVersionedFilter(
        FilterDefinition<TEntity> idFilter,
        long expectedVersion)
    {
        // Use BSON field access for the Version property
        var versionFilter = Builders<TEntity>.Filter.Eq("Version", expectedVersion);
        return Builders<TEntity>.Filter.And(idFilter, versionFilter);
    }

    private static EncinaError MapMongoException(MongoException ex, string operation)
    {
        return RepositoryErrors.PersistenceError<TEntity>(operation, ex);
    }

    private static EncinaError MapMongoException<TIdType>(MongoException ex, TIdType id, string operation)
        where TIdType : notnull
    {
        return RepositoryErrors.PersistenceError<TEntity, TIdType>(id, operation, ex);
    }

    private static bool IsDuplicateKeyException(MongoWriteException ex)
    {
        return ex.WriteError?.Category == ServerErrorCategory.DuplicateKey;
    }

    private static bool HasDuplicateKeyError(MongoBulkWriteException ex)
    {
        return ex.WriteErrors?.Any(e => e.Category == ServerErrorCategory.DuplicateKey) == true;
    }

    #endregion
}
