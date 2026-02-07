using System.Linq.Expressions;
using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.DomainModeling.Concurrency;
using Encina.Messaging.SoftDelete;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.SoftDelete;

/// <summary>
/// MongoDB implementation of <see cref="IFunctionalRepository{TEntity, TId}"/> with automatic
/// soft delete filtering and soft delete operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository automatically applies soft delete filters to all read operations,
/// excluding soft-deleted entities from results. Write operations are modified to
/// perform soft delete instead of hard delete.
/// </para>
/// <para>
/// Key behaviors:
/// <list type="bullet">
///   <item><description>
///     <see cref="DeleteAsync(TId, CancellationToken)"/> performs soft delete by setting IsDeleted=true
///   </description></item>
///   <item><description>
///     All read operations automatically exclude soft-deleted entities
///   </description></item>
///   <item><description>
///     Use <see cref="ListWithDeletedAsync"/> and <see cref="GetByIdWithDeletedAsync"/> to include soft-deleted entities
///   </description></item>
///   <item><description>
///     Use <see cref="RestoreAsync"/> to restore soft-deleted entities
///   </description></item>
///   <item><description>
///     Use <see cref="HardDeleteAsync"/> to permanently delete entities
///   </description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Soft delete - marks entity as deleted but retains data
/// await repository.DeleteAsync(orderId, ct);
///
/// // Query excludes soft-deleted by default
/// var activeOrders = await repository.ListAsync(new ActiveOrdersSpec(), ct);
///
/// // Include soft-deleted entities when needed
/// var allOrders = await repository.ListWithDeletedAsync(new OrdersByDateSpec(), ct);
///
/// // Restore a soft-deleted entity
/// var restored = await repository.RestoreAsync(orderId, ct);
///
/// // Permanently delete (hard delete)
/// await repository.HardDeleteAsync(orderId, ct);
/// </code>
/// </example>
public sealed class SoftDeletableFunctionalRepositoryMongoDB<TEntity, TId>
    : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ISoftDeleteEntityMapping<TEntity, TId> _mapping;
    private readonly SoftDeleteOptions _options;
    private readonly SoftDeleteSpecificationFilterBuilder<TEntity> _filterBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeletableFunctionalRepositoryMongoDB{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <param name="mapping">The soft delete entity mapping.</param>
    /// <param name="options">The soft delete options.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public SoftDeletableFunctionalRepositoryMongoDB(
        IMongoCollection<TEntity> collection,
        ISoftDeleteEntityMapping<TEntity, TId> mapping,
        SoftDeleteOptions options,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);

        _collection = collection;
        _mapping = mapping;
        _options = options;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;

        // Create a generic object mapping adapter for the filter builder
        var genericMapping = new GenericSoftDeleteMappingAdapter<TEntity, TId>(mapping);
        _filterBuilder = new SoftDeleteSpecificationFilterBuilder<TEntity>(genericMapping, options);
    }

    #region Read Operations (with automatic soft delete filtering)

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = BuildIdFilterWithSoftDelete(id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            // Check if we should throw on deleted access
            if (_options.ThrowOnDeletedAccess && _mapping.GetIsDeleted(entity))
            {
                return Left<EncinaError, TEntity>(
                    RepositoryErrors.InvalidOperation<TEntity>("GetById", "Entity has been soft-deleted."));
            }

            return Right<EncinaError, TEntity>(entity);
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
            var filter = _filterBuilder.BuildSoftDeleteFilter();
            var entities = await _collection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(MapMongoException(ex, "List"));
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
            return Left<EncinaError, IReadOnlyList<TEntity>>(MapMongoException(ex, "List"));
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
            return Left<EncinaError, TEntity>(MapMongoException(ex, "FirstOrDefault"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = _filterBuilder.BuildSoftDeleteFilter();
            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
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
            var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
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
            var filter = _filterBuilder.BuildSoftDeleteFilter();
            var count = await _collection.CountDocumentsAsync(
                filter,
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

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagination);

        try
        {
            var filter = _filterBuilder.BuildSoftDeleteFilter();

            // Get total count with soft delete filter
            var totalCount = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data
            var entities = await _collection.Find(filter)
                .Skip(pagination.Skip)
                .Limit(pagination.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, PagedResult<TEntity>>(
                new PagedResult<TEntity>(entities, pagination.PageNumber, pagination.PageSize, totalCount));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(MapMongoException(ex, "GetPaged"));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        Specification<TEntity> specification,
        PaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(pagination);

        try
        {
            var filter = _filterBuilder.BuildFilter(specification);

            // Get total count with specification filter (includes soft delete)
            var totalCount = (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data with specification filter
            var entities = await _collection.Find(filter)
                .Skip(pagination.Skip)
                .Limit(pagination.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, PagedResult<TEntity>>(
                new PagedResult<TEntity>(entities, pagination.PageNumber, pagination.PageSize, totalCount));
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("GetPaged", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, PagedResult<TEntity>>(MapMongoException(ex, "GetPaged"));
        }
    }

    /// <inheritdoc/>
    public Task<Either<EncinaError, PagedResult<TEntity>>> GetPagedAsync(
        IPagedSpecification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        // Convert IPagedSpecification to Specification and PaginationOptions
        if (specification is not Specification<TEntity> baseSpecification)
        {
            return Task.FromResult<Either<EncinaError, PagedResult<TEntity>>>(
                RepositoryErrors.InvalidOperation<TEntity>(
                    "GetPaged",
                    $"IPagedSpecification must inherit from Specification<{typeof(TEntity).Name}> for MongoDB providers"));
        }

        var pagination = new PaginationOptions(
            specification.Pagination.PageNumber,
            specification.Pagination.PageSize);

        return GetPagedAsync(baseSpecification, pagination, cancellationToken);
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

            // Ensure IsDeleted is false for new entities
            _mapping.SetIsDeleted(entity, false);

            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoWriteException ex) when (IsDuplicateKeyException(ex))
        {
            var id = _mapping.GetId(entity);
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

            var id = _mapping.GetId(entity);
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
                    var exists = await _collection.CountDocumentsAsync(
                        idFilter,
                        new CountOptions { Limit = 1 },
                        cancellationToken).ConfigureAwait(false);

                    if (exists > 0)
                    {
                        // Entity exists but version doesn't match - concurrency conflict
                        var databaseEntity = await _collection.Find(idFilter)
                            .FirstOrDefaultAsync(cancellationToken)
                            .ConfigureAwait(false);

                        var conflictInfo = new ConcurrencyConflictInfo<TEntity>(
                            CurrentEntity: entity,
                            ProposedEntity: entity,
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
    /// <remarks>
    /// This method performs a soft delete by setting IsDeleted=true instead of
    /// removing the document from the collection.
    /// </remarks>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, get the entity (without soft delete filter)
            var filter = BuildIdFilter(id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            // Check if already deleted
            if (_mapping.GetIsDeleted(entity))
            {
                return Left<EncinaError, Unit>(
                    RepositoryErrors.InvalidOperation<TEntity>("Delete", "Entity is already soft-deleted."));
            }

            // Populate soft delete fields
            _mapping.SetIsDeleted(entity, true);
            _mapping.SetDeletedAt(entity, _timeProvider.GetUtcNow().UtcDateTime);
            _mapping.SetDeletedBy(entity, _requestContext?.UserId);

            // Also populate audit fields
            AuditFieldPopulator.PopulateForDelete(entity, _requestContext?.UserId, _timeProvider);

            // Replace the document with the soft-deleted version
            var result = await _collection.ReplaceOneAsync(
                filter,
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
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

        var id = _mapping.GetId(entity);
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
            // Populate audit fields and ensure IsDeleted is false
            foreach (var entity in entityList)
            {
                AuditFieldPopulator.PopulateForCreate(entity, _requestContext?.UserId, _timeProvider);
                _mapping.SetIsDeleted(entity, false);
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
            return Left<EncinaError, IReadOnlyList<TEntity>>(MapMongoException(ex, "AddRange"));
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
                var id = _mapping.GetId(entity);
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
                        var id = _mapping.GetId(entity);
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
            // Build filter with soft delete (only non-deleted entities)
            var filter = _filterBuilder.BuildFilter(specification);

            // Safety check: prevent accidental deletion of all documents
            if (filter == Builders<TEntity>.Filter.Empty)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.InvalidOperation<TEntity>("DeleteRange",
                        "DELETE requires a filter to prevent accidental data loss."));
            }

            // For soft delete, we need to update all matching documents
            var update = Builders<TEntity>.Update
                .Set(_mapping.IsDeletedFieldName, true)
                .Set(_mapping.DeletedAtFieldName ?? "DeletedAtUtc", _timeProvider.GetUtcNow().UtcDateTime)
                .Set(_mapping.DeletedByFieldName ?? "DeletedBy", _requestContext?.UserId);

            var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>((int)result.ModifiedCount);
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
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);

        return Task.FromResult<Either<EncinaError, Unit>>(
            RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutableAsync"));
    }

    #endregion

    #region Soft Delete Specific Operations

    /// <summary>
    /// Lists entities matching the specification, including soft-deleted entities.
    /// </summary>
    /// <param name="specification">The specification to filter entities.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the matching entities (including soft-deleted) or an error.</returns>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListWithDeletedAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            // Use filter builder without soft delete filter
            var filter = new SoftDeleteSpecificationFilterBuilder<TEntity>(
                new GenericSoftDeleteMappingAdapter<TEntity, TId>(_mapping),
                _options)
                .IncludeDeleted()
                .BuildFilter(specification);

            var entities = await _collection.Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (NotSupportedException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.InvalidOperation<TEntity>("ListWithDeleted", $"Specification not supported: {ex.Message}"));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(MapMongoException(ex, "ListWithDeleted"));
        }
    }

    /// <summary>
    /// Gets an entity by ID, including soft-deleted entities.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the entity (even if soft-deleted) or a NotFound error.</returns>
    public async Task<Either<EncinaError, TEntity>> GetByIdWithDeletedAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Query without soft delete filter
            var filter = BuildIdFilter(id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(MapMongoException<TId>(ex, id, "GetByIdWithDeleted"));
        }
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    /// <param name="id">The ID of the entity to restore.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either the restored entity or an error.</returns>
    public async Task<Either<EncinaError, TEntity>> RestoreAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = BuildIdFilter(id);
            var entity = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            if (!_mapping.GetIsDeleted(entity))
            {
                return Left<EncinaError, TEntity>(
                    RepositoryErrors.InvalidOperation<TEntity>("Restore", "Entity is not soft-deleted."));
            }

            // Restore the entity
            _mapping.SetIsDeleted(entity, false);
            _mapping.SetDeletedAt(entity, null);
            _mapping.SetDeletedBy(entity, null);

            // Also restore using audit field populator if available
            AuditFieldPopulator.RestoreFromDelete(entity);

            var result = await _collection.ReplaceOneAsync(
                filter,
                entity,
                new ReplaceOptions { IsUpsert = false },
                cancellationToken)
                .ConfigureAwait(false);

            if (result.MatchedCount == 0)
            {
                return Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            return Right<EncinaError, TEntity>(entity);
        }
        catch (MongoException ex)
        {
            return Left<EncinaError, TEntity>(MapMongoException<TId>(ex, id, "Restore"));
        }
    }

    /// <summary>
    /// Permanently deletes an entity (hard delete).
    /// </summary>
    /// <param name="id">The ID of the entity to permanently delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either Unit on success or an error.</returns>
    public async Task<Either<EncinaError, Unit>> HardDeleteAsync(
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
            return Left<EncinaError, Unit>(MapMongoException<TId>(ex, id, "HardDelete"));
        }
    }

    #endregion

    #region Private Helpers

    private FilterDefinition<TEntity> BuildIdFilter(TId id)
    {
        var idSelector = _mapping.IdSelector;
        var parameter = idSelector.Parameters[0];
        var body = Expression.Equal(idSelector.Body, Expression.Constant(id, typeof(TId)));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(body, parameter);

        return Builders<TEntity>.Filter.Where(lambda);
    }

    private FilterDefinition<TEntity> BuildIdFilterWithSoftDelete(TId id)
    {
        var idFilter = BuildIdFilter(id);
        var softDeleteFilter = _filterBuilder.BuildSoftDeleteFilter();

        if (softDeleteFilter == Builders<TEntity>.Filter.Empty)
        {
            return idFilter;
        }

        return Builders<TEntity>.Filter.And(idFilter, softDeleteFilter);
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

/// <summary>
/// Adapter to convert ISoftDeleteEntityMapping{TEntity, TId} to ISoftDeleteEntityMapping{TEntity, object}.
/// </summary>
internal sealed class GenericSoftDeleteMappingAdapter<TEntity, TId> : ISoftDeleteEntityMapping<TEntity, object>
    where TEntity : class
    where TId : notnull
{
    private readonly ISoftDeleteEntityMapping<TEntity, TId> _inner;

    public GenericSoftDeleteMappingAdapter(ISoftDeleteEntityMapping<TEntity, TId> inner)
    {
        _inner = inner;
    }

    public bool IsSoftDeletable => _inner.IsSoftDeletable;
    public string IsDeletedFieldName => _inner.IsDeletedFieldName;
    public string? DeletedAtFieldName => _inner.DeletedAtFieldName;
    public string? DeletedByFieldName => _inner.DeletedByFieldName;

    public Expression<Func<TEntity, object>> IdSelector =>
        Expression.Lambda<Func<TEntity, object>>(
            Expression.Convert(_inner.IdSelector.Body, typeof(object)),
            _inner.IdSelector.Parameters);

    public object GetId(TEntity entity) => _inner.GetId(entity)!;
    public bool GetIsDeleted(TEntity entity) => _inner.GetIsDeleted(entity);
    public void SetIsDeleted(TEntity entity, bool value) => _inner.SetIsDeleted(entity, value);
    public DateTime? GetDeletedAt(TEntity entity) => _inner.GetDeletedAt(entity);
    public void SetDeletedAt(TEntity entity, DateTime? value) => _inner.SetDeletedAt(entity, value);
    public string? GetDeletedBy(TEntity entity) => _inner.GetDeletedBy(entity);
    public void SetDeletedBy(TEntity entity, string? value) => _inner.SetDeletedBy(entity, value);
}
