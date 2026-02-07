using System.Linq.Expressions;
using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.DomainModeling.Concurrency;
using Encina.MongoDB.Repository;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.UnitOfWork;

/// <summary>
/// MongoDB repository implementation that participates in Unit of Work transactions.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository automatically uses the parent Unit of Work's client session
/// for all operations when a transaction is active. Operations are executed
/// within the session context to ensure transactional consistency.
/// </para>
/// <para>
/// This repository passes the current session to all MongoDB operations,
/// ensuring changes are only committed when <see cref="IUnitOfWork.CommitAsync"/>
/// is called on the parent Unit of Work.
/// </para>
/// </remarks>
internal sealed class UnitOfWorkRepositoryMongoDB<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly Expression<Func<TEntity, TId>> _idSelector;
    private readonly Func<TEntity, TId> _compiledIdSelector;
    private readonly UnitOfWorkMongoDB _unitOfWork;
    private readonly SpecificationFilterBuilder<TEntity> _filterBuilder;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkRepositoryMongoDB{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="unitOfWork">The parent Unit of Work.</param>
    /// <param name="requestContext">Optional request context for audit information.</param>
    /// <param name="timeProvider">Optional time provider for timestamps.</param>
    public UnitOfWorkRepositoryMongoDB(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TId>> idSelector,
        UnitOfWorkMongoDB unitOfWork,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _collection = collection;
        _idSelector = idSelector;
        _compiledIdSelector = idSelector.Compile();
        _unitOfWork = unitOfWork;
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
            var session = _unitOfWork.CurrentSession;

            var entity = session is not null
                ? await _collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;
            var emptyFilter = Builders<TEntity>.Filter.Empty;

            var entities = session is not null
                ? await _collection.Find(session, emptyFilter).ToListAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(emptyFilter).ToListAsync(cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;

            var entities = session is not null
                ? await _collection.Find(session, filter).ToListAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;

            var entity = session is not null
                ? await _collection.Find(session, filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;
            var emptyFilter = Builders<TEntity>.Filter.Empty;

            var count = session is not null
                ? await _collection.CountDocumentsAsync(session, emptyFilter, cancellationToken: cancellationToken).ConfigureAwait(false)
                : await _collection.CountDocumentsAsync(emptyFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;

            var count = session is not null
                ? await _collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false)
                : await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;
            var countOptions = new CountOptions { Limit = 1 };

            var count = session is not null
                ? await _collection.CountDocumentsAsync(session, filter, countOptions, cancellationToken).ConfigureAwait(false)
                : await _collection.CountDocumentsAsync(filter, countOptions, cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;
            var emptyFilter = Builders<TEntity>.Filter.Empty;
            var countOptions = new CountOptions { Limit = 1 };

            var count = session is not null
                ? await _collection.CountDocumentsAsync(session, emptyFilter, countOptions, cancellationToken).ConfigureAwait(false)
                : await _collection.CountDocumentsAsync(emptyFilter, countOptions, cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;
            var emptyFilter = Builders<TEntity>.Filter.Empty;

            // Get total count
            var totalCount = session is not null
                ? (int)await _collection.CountDocumentsAsync(session, emptyFilter, cancellationToken: cancellationToken).ConfigureAwait(false)
                : (int)await _collection.CountDocumentsAsync(emptyFilter, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data
            var entities = session is not null
                ? await _collection.Find(session, emptyFilter)
                    .Skip(pagination.Skip)
                    .Limit(pagination.PageSize)
                    .ToListAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(emptyFilter)
                    .Skip(pagination.Skip)
                    .Limit(pagination.PageSize)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

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
            var session = _unitOfWork.CurrentSession;

            // Get total count with specification filter
            var totalCount = session is not null
                ? (int)await _collection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false)
                : (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (totalCount == 0)
            {
                return Right<EncinaError, PagedResult<TEntity>>(
                    PagedResult<TEntity>.Empty(pagination.PageNumber, pagination.PageSize));
            }

            // Get paged data with specification filter
            var entities = session is not null
                ? await _collection.Find(session, filter)
                    .Skip(pagination.Skip)
                    .Limit(pagination.PageSize)
                    .ToListAsync(cancellationToken).ConfigureAwait(false)
                : await _collection.Find(filter)
                    .Skip(pagination.Skip)
                    .Limit(pagination.PageSize)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

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
        var baseSpecification = specification switch
        {
            Specification<TEntity> spec => spec,
            _ => throw new NotSupportedException(
                $"IPagedSpecification must also inherit from Specification<{typeof(TEntity).Name}>")
        };

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

            var session = _unitOfWork.CurrentSession;

            if (session is not null)
            {
                await _collection.InsertOneAsync(session, entity, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

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
            var replaceOptions = new ReplaceOptions { IsUpsert = false };
            var session = _unitOfWork.CurrentSession;

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

            ReplaceOneResult result;
            if (session is not null)
            {
                result = await _collection.ReplaceOneAsync(session, filter, entity, replaceOptions, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await _collection.ReplaceOneAsync(filter, entity, replaceOptions, cancellationToken).ConfigureAwait(false);
            }

            if (result.MatchedCount == 0)
            {
                // Determine if it's a "not found" or "concurrency conflict"
                if (originalVersion.HasValue)
                {
                    // Check if entity exists with a different version
                    var countOptions = new CountOptions { Limit = 1 };
                    var exists = session is not null
                        ? await _collection.CountDocumentsAsync(session, idFilter, countOptions, cancellationToken).ConfigureAwait(false)
                        : await _collection.CountDocumentsAsync(idFilter, countOptions, cancellationToken).ConfigureAwait(false);

                    if (exists > 0)
                    {
                        // Entity exists but version doesn't match - concurrency conflict
                        var databaseEntity = session is not null
                            ? await _collection.Find(session, idFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                            : await _collection.Find(idFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

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
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = BuildIdFilter(id);
            var session = _unitOfWork.CurrentSession;

            DeleteResult result;
            if (session is not null)
            {
                result = await _collection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await _collection.DeleteOneAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

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

            var session = _unitOfWork.CurrentSession;

            if (session is not null)
            {
                await _collection.InsertManyAsync(session, entityList, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _collection.InsertManyAsync(entityList, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

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
                var session = _unitOfWork.CurrentSession;

                BulkWriteResult<TEntity> result;
                if (session is not null)
                {
                    result = await _collection.BulkWriteAsync(session, bulkOps, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    result = await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                // Check for concurrency conflicts (matched count less than entity count)
                if (result.MatchedCount < entityList.Count && originalVersions.Count > 0)
                {
                    // Find the first entity that wasn't matched (potential concurrency conflict)
                    foreach (var entity in entityList)
                    {
                        var id = _compiledIdSelector(entity);
                        var idFilter = BuildIdFilter(id);
                        var countOptions = new CountOptions { Limit = 1 };

                        var exists = session is not null
                            ? await _collection.CountDocumentsAsync(session, idFilter, countOptions, cancellationToken).ConfigureAwait(false)
                            : await _collection.CountDocumentsAsync(idFilter, countOptions, cancellationToken).ConfigureAwait(false);

                        if (exists > 0 && originalVersions.ContainsKey(entity))
                        {
                            // Entity exists but version didn't match - concurrency conflict
                            var databaseEntity = session is not null
                                ? await _collection.Find(session, idFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                                : await _collection.Find(idFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

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

            var session = _unitOfWork.CurrentSession;

            DeleteResult result;
            if (session is not null)
            {
                result = await _collection.DeleteManyAsync(session, filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                result = await _collection.DeleteManyAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

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
