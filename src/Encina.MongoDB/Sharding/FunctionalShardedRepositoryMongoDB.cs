using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.MongoDB.Aggregation;
using Encina.MongoDB.Repository;
using Encina.Sharding;
using Encina.Sharding.Aggregation;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
public sealed class FunctionalShardedRepositoryMongoDB<TEntity, TId> :
    IFunctionalShardedRepository<TEntity, TId>,
    IShardedAggregationSupport<TEntity, TId>,
    IShardedSpecificationSupport<TEntity, TId>
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
    private readonly ILogger _logger;
    private readonly ShardRoutingMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryMongoDB{TEntity, TId}"/> class
    /// for native <c>mongos</c> sharding mode.
    /// </summary>
    /// <param name="collectionFactory">The sharded collection factory.</param>
    /// <param name="idSelector">Expression to select the ID property from an entity.</param>
    /// <param name="collectionName">The MongoDB collection name.</param>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    /// <param name="metrics">Optional shard routing metrics for specification query observability.</param>
    public FunctionalShardedRepositoryMongoDB(
        IShardedMongoCollectionFactory collectionFactory,
        Expression<Func<TEntity, TId>> idSelector,
        string collectionName,
        ILogger<FunctionalShardedRepositoryMongoDB<TEntity, TId>> logger,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null,
        ShardRoutingMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(collectionFactory);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(logger);

        _collectionFactory = collectionFactory;
        _idSelector = idSelector;
        _collectionName = collectionName;
        _useNativeSharding = true;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        _metrics = metrics;
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
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="requestContext">Optional request context for audit field population.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    /// <param name="metrics">Optional shard routing metrics for specification query observability.</param>
    public FunctionalShardedRepositoryMongoDB(
        IShardRouter<TEntity> router,
        IShardedMongoCollectionFactory collectionFactory,
        IShardedQueryExecutor queryExecutor,
        Expression<Func<TEntity, TId>> idSelector,
        string collectionName,
        ILogger<FunctionalShardedRepositoryMongoDB<TEntity, TId>> logger,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null,
        ShardRoutingMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(collectionFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(idSelector);
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        ArgumentNullException.ThrowIfNull(logger);

        _router = router;
        _collectionFactory = collectionFactory;
        _queryExecutor = queryExecutor;
        _idSelector = idSelector;
        _collectionName = collectionName;
        _useNativeSharding = false;
        _requestContext = requestContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        _metrics = metrics;
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

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<long>>> CountAcrossShardsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Count", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();
        var pipelineBuilder = new AggregationPipelineBuilder<TEntity>();

        if (_useNativeSharding)
        {
            return await ExecuteNativeAggregationAsync(
                collection =>
                {
                    var pipeline = pipelineBuilder.BuildCountPipeline(collection, predicate);
                    return ExecuteCountPipelineAsync(pipeline, "mongos", cancellationToken);
                },
                queryResult =>
                {
                    var totalCount = AggregationCombiner.CombineCount(queryResult.Results);
                    var elapsed = Stopwatch.GetElapsedTime(sw);
                    var totalShards = queryResult.TotalShardsQueried;

                    if (queryResult.FailedShards.Count > 0)
                    {
                        _logger.LogWarning(
                            "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                            "Count", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                            "Count", typeof(TEntity).Name, totalCount, totalShards, elapsed.TotalMilliseconds);
                    }

                    return new AggregationResult<long>(
                        totalCount,
                        totalShards,
                        queryResult.FailedShards,
                        elapsed);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    return await ExecuteCountPipelineAsync(
                        pipelineBuilder.BuildCountPipeline(collection, predicate),
                        shardId,
                        ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var totalCount = AggregationCombiner.CombineCount(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.TotalShardsQueried;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    "Count", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    "Count", typeof(TEntity).Name, totalCount, totalShards, elapsed.TotalMilliseconds);
            }

            return new AggregationResult<long>(
                totalCount,
                totalShards,
                queryResult.FailedShards,
                elapsed);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<TValue>>> SumAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(selector);

        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Sum", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();
        var pipelineBuilder = new AggregationPipelineBuilder<TEntity>();

        if (_useNativeSharding)
        {
            return await ExecuteNativeAggregationAsync(
                collection =>
                {
                    var pipeline = pipelineBuilder.BuildSumPipeline(collection, selector, predicate);
                    return ExecuteSumPipelineAsync<TValue>(pipeline, "mongos", cancellationToken);
                },
                queryResult =>
                {
                    var totalSum = AggregationCombiner.CombineSum(queryResult.Results);
                    var elapsed = Stopwatch.GetElapsedTime(sw);
                    var totalShards = queryResult.TotalShardsQueried;

                    if (queryResult.FailedShards.Count > 0)
                    {
                        _logger.LogWarning(
                            "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                            "Sum", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                            "Sum", typeof(TEntity).Name, totalSum, totalShards, elapsed.TotalMilliseconds);
                    }

                    return new AggregationResult<TValue>(
                        totalSum,
                        totalShards,
                        queryResult.FailedShards,
                        elapsed);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    return await ExecuteSumPipelineAsync<TValue>(
                        pipelineBuilder.BuildSumPipeline(collection, selector, predicate),
                        shardId,
                        ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var totalSum = AggregationCombiner.CombineSum(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.TotalShardsQueried;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    "Sum", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    "Sum", typeof(TEntity).Name, totalSum, totalShards, elapsed.TotalMilliseconds);
            }

            return new AggregationResult<TValue>(
                totalSum,
                totalShards,
                queryResult.FailedShards,
                elapsed);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<TValue>>> AvgAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, INumber<TValue>
    {
        ArgumentNullException.ThrowIfNull(selector);

        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Avg", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();
        var pipelineBuilder = new AggregationPipelineBuilder<TEntity>();

        if (_useNativeSharding)
        {
            return await ExecuteNativeAggregationAsync(
                collection =>
                {
                    var pipeline = pipelineBuilder.BuildAvgPartialPipeline(collection, selector, predicate);
                    return ExecuteAvgPartialPipelineAsync<TValue>(pipeline, "mongos", cancellationToken);
                },
                queryResult =>
                {
                    var avg = AggregationCombiner.CombineAvg(queryResult.Results);
                    var elapsed = Stopwatch.GetElapsedTime(sw);
                    var totalShards = queryResult.TotalShardsQueried;

                    if (queryResult.FailedShards.Count > 0)
                    {
                        _logger.LogWarning(
                            "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                            "Avg", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                            "Avg", typeof(TEntity).Name, avg, totalShards, elapsed.TotalMilliseconds);
                    }

                    return new AggregationResult<TValue>(
                        avg,
                        totalShards,
                        queryResult.FailedShards,
                        elapsed);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    return await ExecuteAvgPartialPipelineAsync<TValue>(
                        pipelineBuilder.BuildAvgPartialPipeline(collection, selector, predicate),
                        shardId,
                        ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var avg = AggregationCombiner.CombineAvg(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.TotalShardsQueried;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    "Avg", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    "Avg", typeof(TEntity).Name, avg, totalShards, elapsed.TotalMilliseconds);
            }

            return new AggregationResult<TValue>(
                avg,
                totalShards,
                queryResult.FailedShards,
                elapsed);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<TValue?>>> MinAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(selector);

        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Min", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();
        var pipelineBuilder = new AggregationPipelineBuilder<TEntity>();

        if (_useNativeSharding)
        {
            return await ExecuteNativeAggregationAsync(
                collection =>
                {
                    var pipeline = pipelineBuilder.BuildMinPipeline(collection, selector, predicate);
                    return ExecuteMinMaxPipelineAsync<TValue>(pipeline, "mongos", cancellationToken);
                },
                queryResult =>
                {
                    var globalMin = AggregationCombiner.CombineMin(queryResult.Results);
                    var elapsed = Stopwatch.GetElapsedTime(sw);
                    var totalShards = queryResult.TotalShardsQueried;

                    if (queryResult.FailedShards.Count > 0)
                    {
                        _logger.LogWarning(
                            "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                            "Min", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                            "Min", typeof(TEntity).Name, globalMin, totalShards, elapsed.TotalMilliseconds);
                    }

                    return new AggregationResult<TValue?>(
                        globalMin,
                        totalShards,
                        queryResult.FailedShards,
                        elapsed);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    return await ExecuteMinMaxPipelineAsync<TValue>(
                        pipelineBuilder.BuildMinPipeline(collection, selector, predicate),
                        shardId,
                        ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var globalMin = AggregationCombiner.CombineMin(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.TotalShardsQueried;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    "Min", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    "Min", typeof(TEntity).Name, globalMin, totalShards, elapsed.TotalMilliseconds);
            }

            return new AggregationResult<TValue?>(
                globalMin,
                totalShards,
                queryResult.FailedShards,
                elapsed);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<TValue?>>> MaxAcrossShardsAsync<TValue>(
        Expression<Func<TEntity, TValue>> selector,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        where TValue : struct, IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(selector);

        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Max", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();
        var pipelineBuilder = new AggregationPipelineBuilder<TEntity>();

        if (_useNativeSharding)
        {
            return await ExecuteNativeAggregationAsync(
                collection =>
                {
                    var pipeline = pipelineBuilder.BuildMaxPipeline(collection, selector, predicate);
                    return ExecuteMinMaxPipelineAsync<TValue>(pipeline, "mongos", cancellationToken);
                },
                queryResult =>
                {
                    var globalMax = AggregationCombiner.CombineMax(queryResult.Results);
                    var elapsed = Stopwatch.GetElapsedTime(sw);
                    var totalShards = queryResult.TotalShardsQueried;

                    if (queryResult.FailedShards.Count > 0)
                    {
                        _logger.LogWarning(
                            "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                            "Max", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                            "Max", typeof(TEntity).Name, globalMax, totalShards, elapsed.TotalMilliseconds);
                    }

                    return new AggregationResult<TValue?>(
                        globalMax,
                        totalShards,
                        queryResult.FailedShards,
                        elapsed);
                },
                cancellationToken).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    return await ExecuteMinMaxPipelineAsync<TValue>(
                        pipelineBuilder.BuildMaxPipeline(collection, selector, predicate),
                        shardId,
                        ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var globalMax = AggregationCombiner.CombineMax(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.TotalShardsQueried;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed {Operation} aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    "Max", typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed {Operation} aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    "Max", typeof(TEntity).Name, globalMax, totalShards, elapsed.TotalMilliseconds);
            }

            return new AggregationResult<TValue?>(
                globalMax,
                totalShards,
                queryResult.FailedShards,
                elapsed);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryAllShardsAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var specTypeName = specification.GetType().Name;
        _logger.LogDebug(
            "Starting specification scatter-gather query for {EntityType} across all shards (Specification={SpecificationType})",
            typeof(TEntity).Name, specTypeName);
        var sw = Stopwatch.GetTimestamp();

        if (_useNativeSharding)
        {
            var collectionResult = _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
            return await collectionResult.MapAsync(async collection =>
            {
                var items = await EvaluateSpecificationAsync(collection, specification, cancellationToken)
                    .ConfigureAwait(false);

                var elapsed = Stopwatch.GetElapsedTime(sw);
                LogSpecificationResult(1, 0, elapsed, "query");

                return new ShardedSpecificationResult<TEntity>(
                    items,
                    new Dictionary<string, int> { ["mongos"] = items.Count },
                    elapsed,
                    new Dictionary<string, TimeSpan> { ["mongos"] = elapsed },
                    []);
            }).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    IReadOnlyList<TEntity> items = await EvaluateSpecificationAsync(collection, specification, ct)
                        .ConfigureAwait(false);
                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[items];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
            BuildSpecificationResult(queryResult, specification, sw, "query"));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardedPagedResult<TEntity>>> QueryAllShardsPagedAsync(
        Specification<TEntity> specification,
        ShardedPaginationOptions pagination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(pagination);

        var specTypeName = specification.GetType().Name;
        _logger.LogDebug(
            "Starting paged specification scatter-gather query for {EntityType} across all shards (Specification={SpecificationType}, Page={Page}, PageSize={PageSize})",
            typeof(TEntity).Name, specTypeName, pagination.Page, pagination.PageSize);
        var sw = Stopwatch.GetTimestamp();

        if (_useNativeSharding)
        {
            var collectionResult = _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
            return await collectionResult.MapAsync(async collection =>
            {
                var items = await EvaluateSpecificationAsync(collection, specification, cancellationToken)
                    .ConfigureAwait(false);

                var filterBuilder = new SpecificationFilterBuilder<TEntity>();
                var filter = BuildSpecificationFilter(filterBuilder, specification);
                var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var perShardItems = new Dictionary<string, IReadOnlyList<TEntity>> { ["mongos"] = items };
                var pagedItems = ScatterGatherResultMerger.MergeOrderAndPaginate(
                    perShardItems, specification, pagination.Page, pagination.PageSize);

                var elapsed = Stopwatch.GetElapsedTime(sw);
                LogSpecificationResult(1, 0, elapsed, "paged query");

                return new ShardedPagedResult<TEntity>(
                    pagedItems,
                    totalCount,
                    pagination.Page,
                    pagination.PageSize,
                    new Dictionary<string, long> { ["mongos"] = totalCount },
                    []);
            }).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    IReadOnlyList<TEntity> items = await EvaluateSpecificationAsync(collection, specification, ct)
                        .ConfigureAwait(false);
                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[items];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        var countResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    var filterBuilder = new SpecificationFilterBuilder<TEntity>();
                    var filter = BuildSpecificationFilter(filterBuilder, specification);
                    var count = await collection.CountDocumentsAsync(filter, cancellationToken: ct)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<ShardAggregatePartial<long>>)
                        [new ShardAggregatePartial<long>(shardId, count, count, null, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Bind(queryResult =>
            countResult.Map(countQueryResult =>
            {
                var elapsed = Stopwatch.GetElapsedTime(sw);

                // Merge and paginate items
                var mergeStart = Stopwatch.GetTimestamp();
                var perShardItems = BuildPerShardItemsDictionary(queryResult);
                var pagedItems = ScatterGatherResultMerger.MergeOrderAndPaginate(
                    perShardItems, specification, pagination.Page, pagination.PageSize);
                var mergeElapsed = Stopwatch.GetElapsedTime(mergeStart);

                // Aggregate counts per shard
                var countPerShard = new Dictionary<string, long>();
                foreach (var partial in countQueryResult.Results)
                {
                    countPerShard[partial.ShardId] = partial.Count;
                }

                var totalCount = countPerShard.Values.Sum();
                var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

                LogSpecificationResult(totalShards, queryResult.FailedShards.Count, elapsed, "paged query");
                RecordSpecificationMetrics(
                    specTypeName, "paged_query", totalShards,
                    queryResult.FailedShards.Count, pagedItems.Count,
                    mergeElapsed.TotalMilliseconds, perShardItems);

                _logger.LogDebug(
                    "Specification paged query pagination merge for {EntityType}: MergeDuration={MergeDurationMs:F1}ms, PageItems={PageItems}",
                    typeof(TEntity).Name, mergeElapsed.TotalMilliseconds, pagedItems.Count);

                return new ShardedPagedResult<TEntity>(
                    pagedItems,
                    totalCount,
                    pagination.Page,
                    pagination.PageSize,
                    countPerShard,
                    queryResult.FailedShards);
            }));
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardedCountResult>> CountAllShardsAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var specTypeName = specification.GetType().Name;
        _logger.LogDebug(
            "Starting specification count across all shards for {EntityType} (Specification={SpecificationType})",
            typeof(TEntity).Name, specTypeName);
        var sw = Stopwatch.GetTimestamp();

        if (_useNativeSharding)
        {
            var collectionResult = _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
            return await collectionResult.MapAsync(async collection =>
            {
                var filterBuilder = new SpecificationFilterBuilder<TEntity>();
                var filter = BuildSpecificationFilter(filterBuilder, specification);
                var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var elapsed = Stopwatch.GetElapsedTime(sw);
                LogSpecificationResult(1, 0, elapsed, "count");

                return new ShardedCountResult(
                    count,
                    new Dictionary<string, long> { ["mongos"] = count },
                    []);
            }).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    var filterBuilder = new SpecificationFilterBuilder<TEntity>();
                    var filter = BuildSpecificationFilter(filterBuilder, specification);
                    var count = await collection.CountDocumentsAsync(filter, cancellationToken: ct)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<ShardAggregatePartial<long>>)
                        [new ShardAggregatePartial<long>(shardId, count, count, null, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var countPerShard = new Dictionary<string, long>();

            foreach (var partial in queryResult.Results)
            {
                countPerShard[partial.ShardId] = partial.Count;
            }

            var totalCount = countPerShard.Values.Sum();
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            LogSpecificationResult(totalShards, queryResult.FailedShards.Count, elapsed, "count");
            _metrics?.RecordSpecificationQuery(specTypeName, "count", totalShards, (int)totalCount);

            return new ShardedCountResult(totalCount, countPerShard, queryResult.FailedShards);
        });
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardedSpecificationResult<TEntity>>> QueryShardsAsync(
        Specification<TEntity> specification,
        IReadOnlyList<string> shardIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(shardIds);

        var specTypeName = specification.GetType().Name;
        _logger.LogDebug(
            "Starting specification scatter-gather query for {EntityType} across {ShardCount} specific shards (Specification={SpecificationType})",
            typeof(TEntity).Name, shardIds.Count, specTypeName);
        var sw = Stopwatch.GetTimestamp();

        if (_useNativeSharding)
        {
            // In native mode, mongos handles routing — shard IDs are ignored.
            var collectionResult = _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);
            return await collectionResult.MapAsync(async collection =>
            {
                var items = await EvaluateSpecificationAsync(collection, specification, cancellationToken)
                    .ConfigureAwait(false);

                var elapsed = Stopwatch.GetElapsedTime(sw);
                LogSpecificationResult(1, 0, elapsed, "query");

                return new ShardedSpecificationResult<TEntity>(
                    items,
                    new Dictionary<string, int> { ["mongos"] = items.Count },
                    elapsed,
                    new Dictionary<string, TimeSpan> { ["mongos"] = elapsed },
                    []);
            }).ConfigureAwait(false);
        }

        var scatterResult = await _queryExecutor!.ExecuteAsync<IReadOnlyList<TEntity>>(
            shardIds,
            async (shardId, ct) =>
            {
                var collectionResult = _collectionFactory.GetCollectionForShard<TEntity>(shardId, _collectionName);
                return await collectionResult.MapAsync(async collection =>
                {
                    IReadOnlyList<TEntity> items = await EvaluateSpecificationAsync(collection, specification, ct)
                        .ConfigureAwait(false);
                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[items];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
            BuildSpecificationResult(queryResult, specification, sw, "query"));
    }

    private static async Task<IReadOnlyList<TEntity>> EvaluateSpecificationAsync(
        IMongoCollection<TEntity> collection,
        Specification<TEntity> specification,
        CancellationToken cancellationToken)
    {
        if (specification is IQuerySpecification<TEntity> querySpec)
        {
            var evaluator = new SpecificationEvaluatorMongoDB<TEntity>(collection);
            return await evaluator.ToListAsync(querySpec, cancellationToken).ConfigureAwait(false);
        }

        // Base Specification — filter only, no ordering/pagination
        var filterBuilder = new SpecificationFilterBuilder<TEntity>();
        var filter = filterBuilder.BuildFilter(specification);
        return await collection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FilterDefinition<TEntity> BuildSpecificationFilter(
        SpecificationFilterBuilder<TEntity> filterBuilder,
        Specification<TEntity> specification)
    {
        if (specification is QuerySpecification<TEntity> querySpec)
        {
            return filterBuilder.BuildFilter(querySpec);
        }

        return filterBuilder.BuildFilter(specification);
    }

    private ShardedSpecificationResult<TEntity> BuildSpecificationResult(
        ShardedQueryResult<IReadOnlyList<TEntity>> queryResult,
        Specification<TEntity> specification,
        long startTimestamp,
        string operationKind)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        // Build per-shard items dictionary and merge
        var mergeStart = Stopwatch.GetTimestamp();
        var perShardItems = BuildPerShardItemsDictionary(queryResult);
        var mergedItems = ScatterGatherResultMerger.MergeAndOrder(perShardItems, specification);
        var mergeElapsed = Stopwatch.GetElapsedTime(mergeStart);

        // Build per-shard counts
        var itemsPerShard = new Dictionary<string, int>();
        foreach (var (shardId, items) in perShardItems)
        {
            itemsPerShard[shardId] = items.Count;
        }

        var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;
        LogSpecificationResult(totalShards, queryResult.FailedShards.Count, elapsed, operationKind);

        // Record specification-level metrics
        RecordSpecificationMetrics(
            specification.GetType().Name, operationKind, totalShards,
            queryResult.FailedShards.Count, mergedItems.Count,
            mergeElapsed.TotalMilliseconds, perShardItems);

        // Duration per shard is not tracked individually at the executor level,
        // so we report the total duration for all shards
        var durationPerShard = new Dictionary<string, TimeSpan>();
        foreach (var shardId in queryResult.SuccessfulShards)
        {
            durationPerShard[shardId] = elapsed;
        }

        return new ShardedSpecificationResult<TEntity>(
            mergedItems,
            itemsPerShard,
            elapsed,
            durationPerShard,
            queryResult.FailedShards);
    }

    private static Dictionary<string, IReadOnlyList<TEntity>> BuildPerShardItemsDictionary(
        ShardedQueryResult<IReadOnlyList<TEntity>> queryResult)
    {
        var perShardItems = new Dictionary<string, IReadOnlyList<TEntity>>();

        // Each shard returns a single IReadOnlyList<TEntity> wrapped in a list.
        // The executor flattens these, so Results[i] is the items from the i-th successful shard.
        for (var i = 0; i < queryResult.SuccessfulShards.Count; i++)
        {
            perShardItems[queryResult.SuccessfulShards[i]] = queryResult.Results[i];
        }

        return perShardItems;
    }

    private void RecordSpecificationMetrics(
        string specTypeName,
        string operationKind,
        int totalShards,
        int failedShardCount,
        int totalItems,
        double mergeDurationMs,
        Dictionary<string, IReadOnlyList<TEntity>> perShardItems)
    {
        _metrics?.RecordSpecificationQuery(specTypeName, operationKind, totalShards, totalItems);
        _metrics?.RecordSpecificationMergeDuration(operationKind, totalItems, mergeDurationMs);

        foreach (var (shardId, items) in perShardItems)
        {
            _metrics?.RecordSpecificationItemsPerShard(shardId, items.Count);
        }

        if (failedShardCount > 0)
        {
            _metrics?.RecordPartialFailure(failedShardCount, totalShards);
        }
    }

    private void LogSpecificationResult(int totalShards, int failedShardCount, TimeSpan elapsed, string operation)
    {
        if (failedShardCount > 0)
        {
            _logger.LogWarning(
                "Specification {Operation} for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                operation, typeof(TEntity).Name, failedShardCount, totalShards, elapsed.TotalMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Specification {Operation} for {EntityType} completed: Shards={TotalShards}, Duration={DurationMs:F1}ms",
                operation, typeof(TEntity).Name, totalShards, elapsed.TotalMilliseconds);
        }
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

    private async Task<Either<EncinaError, AggregationResult<TResult>>> ExecuteNativeAggregationAsync<TPartial, TResult>(
        Func<IMongoCollection<TEntity>, Task<IReadOnlyList<ShardAggregatePartial<TPartial>>>> pipelineExecutor,
        Func<ShardedQueryResult<ShardAggregatePartial<TPartial>>, AggregationResult<TResult>> combiner,
        CancellationToken cancellationToken)
        where TPartial : struct
    {
        var collectionResult = _collectionFactory.GetDefaultCollection<TEntity>(_collectionName);

        return await collectionResult.MapAsync(async collection =>
        {
            var partials = await pipelineExecutor(collection).ConfigureAwait(false);
            var queryResult = new ShardedQueryResult<ShardAggregatePartial<TPartial>>(
                partials,
                ["mongos"],
                []);
            return combiner(queryResult);
        }).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ShardAggregatePartial<long>>> ExecuteCountPipelineAsync(
        IAggregateFluent<BsonDocument> pipeline,
        string shardId,
        CancellationToken cancellationToken)
    {
        var result = await pipeline.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var count = result?["count"].AsInt64 ?? 0L;

        return [new ShardAggregatePartial<long>(shardId, count, count, null, null)];
    }

    private static async Task<IReadOnlyList<ShardAggregatePartial<TValue>>> ExecuteSumPipelineAsync<TValue>(
        IAggregateFluent<BsonDocument> pipeline,
        string shardId,
        CancellationToken cancellationToken)
        where TValue : struct, INumber<TValue>
    {
        var result = await pipeline.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var sum = result is not null
            ? TValue.CreateChecked(Convert.ToDouble(result["sum"], CultureInfo.InvariantCulture))
            : TValue.Zero;

        return [new ShardAggregatePartial<TValue>(shardId, sum, 0, null, null)];
    }

    private static async Task<IReadOnlyList<ShardAggregatePartial<TValue>>> ExecuteAvgPartialPipelineAsync<TValue>(
        IAggregateFluent<BsonDocument> pipeline,
        string shardId,
        CancellationToken cancellationToken)
        where TValue : struct, INumber<TValue>
    {
        var result = await pipeline.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return [new ShardAggregatePartial<TValue>(shardId, TValue.Zero, 0, null, null)];
        }

        var sum = TValue.CreateChecked(Convert.ToDouble(result["sum"], CultureInfo.InvariantCulture));
        var count = result["count"].AsInt64;

        return [new ShardAggregatePartial<TValue>(shardId, sum, count, null, null)];
    }

    private static async Task<IReadOnlyList<ShardAggregatePartial<TValue>>> ExecuteMinMaxPipelineAsync<TValue>(
        IAggregateFluent<BsonDocument> pipeline,
        string shardId,
        CancellationToken cancellationToken)
        where TValue : struct, IComparable<TValue>
    {
        var result = await pipeline.SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (result is null || result["result"].IsBsonNull)
        {
            return [new ShardAggregatePartial<TValue>(shardId, default, 0, null, null)];
        }

        var value = (TValue)Convert.ChangeType(
            BsonTypeMapper.MapToDotNetValue(result["result"]),
            typeof(TValue),
            CultureInfo.InvariantCulture)!;

        return [new ShardAggregatePartial<TValue>(shardId, default, 0, value, value)];
    }
}
