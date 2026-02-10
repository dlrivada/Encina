using System.Linq.Expressions;
using Encina.MongoDB.Repository;
using Encina.Sharding;
using Encina.Sharding.Execution;
using LanguageExt;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// MongoDB implementation of <see cref="IFunctionalShardedRepository{TEntity, TId}"/>
/// that supports both native <c>mongos</c> routing and application-level shard routing.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// <b>Native sharding mode (default)</b>: All operations use a single
/// <see cref="IMongoCollection{TDocument}"/> from the <c>mongos</c> client. MongoDB
/// handles routing transparently based on the shard key configured in the cluster.
/// Scatter-gather queries are delegated to MongoDB's internal scatter-gather mechanism.
/// </para>
/// <para>
/// <b>Application-level sharding mode</b>: Operations use <see cref="IShardRouter{TEntity}"/>
/// and <see cref="IShardedMongoCollectionFactory"/> to route to shard-specific collections.
/// Scatter-gather uses <see cref="ShardedQueryExecutor"/> for parallel execution with
/// timeout and partial failure handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Native mongos routing (recommended)
/// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
/// {
///     options.UseNativeSharding = true;
///     options.CollectionName = "orders";
///     options.IdProperty = o =&gt; o.Id;
/// });
///
/// // Use sharded repository
/// var result = await shardedRepo.AddAsync(order, ct);
/// </code>
/// </example>
public sealed class FunctionalShardedRepositoryMongoDB<TEntity, TId> : IFunctionalShardedRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly IShardRouter<TEntity>? _router;
    private readonly IShardedMongoCollectionFactory _collectionFactory;
    private readonly IShardedQueryExecutor? _queryExecutor;
    private readonly Expression<Func<TEntity, TId>> _idSelector;
    private readonly string _collectionName;
    private readonly bool _useNativeSharding;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryMongoDB{TEntity, TId}"/> class
    /// for native <c>mongos</c> sharding mode.
    /// </summary>
    /// <param name="collectionFactory">The sharded collection factory.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="collectionName">The MongoDB collection name.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public FunctionalShardedRepositoryMongoDB(
        IShardedMongoCollectionFactory collectionFactory,
        Expression<Func<TEntity, TId>> idSelector,
        string collectionName,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(collectionFactory);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        _collectionFactory = collectionFactory;
        _idSelector = idSelector;
        _collectionName = collectionName;
        _useNativeSharding = true;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryMongoDB{TEntity, TId}"/> class
    /// for application-level sharding mode.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="collectionFactory">The sharded collection factory.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="collectionName">The MongoDB collection name.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public FunctionalShardedRepositoryMongoDB(
        IShardRouter<TEntity> router,
        IShardedMongoCollectionFactory collectionFactory,
        IShardedQueryExecutor queryExecutor,
        Expression<Func<TEntity, TId>> idSelector,
        string collectionName,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(collectionFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

        _router = router;
        _collectionFactory = collectionFactory;
        _queryExecutor = queryExecutor;
        _idSelector = idSelector;
        _collectionName = collectionName;
        _useNativeSharding = false;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        string shardKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(shardKey);

        var collectionResult = ResolveCollection(shardKey);

        return await collectionResult
            .MapAsync(async collection =>
            {
                var repo = CreateRepository(collection);
                return await repo.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var collectionResult = ResolveCollectionForEntity(entity);

        return await collectionResult
            .MapAsync(async collection =>
            {
                var repo = CreateRepository(collection);
                return await repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var collectionResult = ResolveCollectionForEntity(entity);

        return await collectionResult
            .MapAsync(async collection =>
            {
                var repo = CreateRepository(collection);
                return await repo.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        string shardKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(shardKey);

        var collectionResult = ResolveCollection(shardKey);

        return await collectionResult
            .MapAsync(async collection =>
            {
                var repo = CreateRepository(collection);
                return await repo.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            })
            .BindAsync(x => x)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryAllShardsAsync(
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryFactory);

        if (_useNativeSharding)
        {
            // In native mode, mongos handles scatter-gather internally.
            // Execute the query against the default "mongos" shard entry.
            return ExecuteNativeQueryAsync("mongos", queryFactory, cancellationToken);
        }

        return _queryExecutor!.ExecuteAllAsync(queryFactory, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryShardsAsync(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardIds);
        ArgumentNullException.ThrowIfNull(queryFactory);

        if (_useNativeSharding)
        {
            // In native mode, mongos handles routing, so we query through default.
            return ExecuteNativeQueryAsync("mongos", queryFactory, cancellationToken);
        }

        return _queryExecutor!.ExecuteAsync(shardIds, queryFactory, cancellationToken);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardIdForEntity(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_useNativeSharding)
        {
            // In native mode, mongos determines the shard — return a sentinel value
            return Either<EncinaError, string>.Right("mongos");
        }

        return _router!.GetShardId(entity);
    }

    private Either<EncinaError, IMongoCollection<TEntity>> ResolveCollection(string shardKey)
    {
        if (_useNativeSharding)
        {
            return _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
        }

        return _router!.GetShardId(shardKey)
            .Bind(shardId => _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName));
    }

    private Either<EncinaError, IMongoCollection<TEntity>> ResolveCollectionForEntity(TEntity entity)
    {
        if (_useNativeSharding)
        {
            return _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
        }

        return _router!.GetShardId(entity)
            .Bind(shardId => _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName));
    }

    private FunctionalRepositoryMongoDB<TEntity, TId> CreateRepository(IMongoCollection<TEntity> collection)
        => new(collection, _idSelector, _requestContext, _timeProvider);

    private static async Task<Either<EncinaError, ShardedQueryResult<TEntity>>> ExecuteNativeQueryAsync(
        string virtualShardId,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken)
    {
        var result = await queryFactory(virtualShardId, cancellationToken).ConfigureAwait(false);

        return result.Match<Either<EncinaError, ShardedQueryResult<TEntity>>>(
            Right: items => new ShardedQueryResult<TEntity>(
                items,
                [virtualShardId],
                []),
            Left: error => error);
    }
}
