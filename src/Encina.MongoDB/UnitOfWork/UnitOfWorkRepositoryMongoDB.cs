using System.Linq.Expressions;
using Encina;
using Encina.DomainModeling;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkRepositoryMongoDB{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="collection">The MongoDB collection.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="unitOfWork">The parent Unit of Work.</param>
    public UnitOfWorkRepositoryMongoDB(
        IMongoCollection<TEntity> collection,
        Expression<Func<TEntity, TId>> idSelector,
        UnitOfWorkMongoDB unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _collection = collection;
        _idSelector = idSelector;
        _compiledIdSelector = idSelector.Compile();
        _unitOfWork = unitOfWork;
        _filterBuilder = new SpecificationFilterBuilder<TEntity>();
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
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            var id = _compiledIdSelector(entity);
            var filter = BuildIdFilter(id);
            var replaceOptions = new ReplaceOptions { IsUpsert = false };
            var session = _unitOfWork.CurrentSession;

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
    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            var bulkOps = entityList.Select(entity =>
            {
                var id = _compiledIdSelector(entity);
                var filter = BuildIdFilter(id);
                return new ReplaceOneModel<TEntity>(filter, entity) { IsUpsert = false };
            }).ToList();

            if (bulkOps.Count > 0)
            {
                var session = _unitOfWork.CurrentSession;

                if (session is not null)
                {
                    await _collection.BulkWriteAsync(session, bulkOps, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await _collection.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken).ConfigureAwait(false);
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
