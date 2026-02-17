using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.EntityFrameworkCore.Repository;
using Encina.Sharding;
using Encina.Sharding.Aggregation;
using Encina.Sharding.Data;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Sharding;

/// <summary>
/// EF Core implementation of <see cref="IFunctionalShardedRepository{TEntity, TId}"/>
/// that routes CRUD operations to the appropriate shard.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This implementation is provider-agnostic. The database provider (SQL Server, PostgreSQL,
/// MySQL, SQLite) is determined by the <see cref="IShardedDbContextFactory{TContext}"/>
/// configuration, not by this repository.
/// </para>
/// <para>
/// Single-entity operations (GetById, Add, Update, Delete) are routed to the correct
/// shard based on the entity's shard key. The shard key is extracted using
/// <see cref="IShardable"/> or <see cref="ShardKeyAttribute"/>.
/// </para>
/// <para>
/// Scatter-gather queries (QueryAllShards, QueryShards) use <see cref="ShardedQueryExecutor"/>
/// for parallel execution with timeout and partial failure handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI (SQL Server example)
/// services.AddEncinaEFCoreShardingSqlServer&lt;AppDbContext, Order, Guid&gt;();
///
/// // Use sharded repository
/// var result = await shardedRepo.AddAsync(order, ct);
/// </code>
/// </example>
public sealed class FunctionalShardedRepositoryEF<TContext, TEntity, TId>
    : IFunctionalShardedRepository<TEntity, TId>,
      IShardedAggregationSupport<TEntity, TId>,
      IShardedSpecificationSupport<TEntity, TId>
    where TContext : DbContext
    where TEntity : class
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedDbContextFactory<TContext> _contextFactory;
    private readonly IShardedQueryExecutor _queryExecutor;
    private readonly ILogger _logger;
    private readonly ShardRoutingMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryEF{TContext, TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="contextFactory">The sharded DbContext factory.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="logger">The logger for aggregation diagnostics.</param>
    /// <param name="metrics">Optional shard routing metrics for specification query observability.</param>
    public FunctionalShardedRepositoryEF(
        IShardRouter<TEntity> router,
        IShardedDbContextFactory<TContext> contextFactory,
        IShardedQueryExecutor queryExecutor,
        ILogger<FunctionalShardedRepositoryEF<TContext, TEntity, TId>> logger,
        ShardRoutingMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(logger);

        _router = router;
        _contextFactory = contextFactory;
        _queryExecutor = queryExecutor;
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

        var shardIdResult = _router.GetShardId(shardKey);

        return await shardIdResult
            .MapAsync(async shardId =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);

                return await ctxResult
                    .MapAsync(async context =>
                    {
                        await using var _ = context.ConfigureAwait(false);
                        var repo = CreateRepository(context);
                        return await repo.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
                    })
                    .BindAsync(x => x)
                    .ConfigureAwait(false);
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

        return await _router.GetShardId(entity)
            .MapAsync(async shardId =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);

                return await ctxResult
                    .MapAsync(async context =>
                    {
                        await using var _ = context.ConfigureAwait(false);
                        var repo = CreateRepository(context);
                        return await repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
                    })
                    .BindAsync(x => x)
                    .ConfigureAwait(false);
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

        return await _router.GetShardId(entity)
            .MapAsync(async shardId =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);

                return await ctxResult
                    .MapAsync(async context =>
                    {
                        await using var _ = context.ConfigureAwait(false);
                        var repo = CreateRepository(context);
                        return await repo.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
                    })
                    .BindAsync(x => x)
                    .ConfigureAwait(false);
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

        var shardIdResult = _router.GetShardId(shardKey);

        return await shardIdResult
            .MapAsync(async shardId =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);

                return await ctxResult
                    .MapAsync(async context =>
                    {
                        await using var _ = context.ConfigureAwait(false);
                        var repo = CreateRepository(context);
                        return await repo.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
                    })
                    .BindAsync(x => x)
                    .ConfigureAwait(false);
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
        return _queryExecutor.ExecuteAllAsync(queryFactory, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryShardsAsync(
        IEnumerable<string> shardIds,
        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<TEntity>>>> queryFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardIds);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return _queryExecutor.ExecuteAsync(shardIds, queryFactory, cancellationToken);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> GetShardIdForEntity(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _router.GetShardId(entity);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, AggregationResult<long>>> CountAcrossShardsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        _logger.LogDebug("Starting distributed Count aggregation for {EntityType}", typeof(TEntity).Name);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var count = (long)await context.Set<TEntity>()
                        .AsNoTracking()
                        .CountAsync(predicate, ct)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<ShardAggregatePartial<long>>)
                        [new ShardAggregatePartial<long>(shardId, count, count, null, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var totalCount = AggregationCombiner.CombineCount(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed Count aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed Count aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    typeof(TEntity).Name, totalCount, totalShards, elapsed.TotalMilliseconds);
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

        _logger.LogDebug("Starting distributed Sum aggregation for {EntityType}", typeof(TEntity).Name);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = context.Set<TEntity>().AsNoTracking();

                    if (predicate is not null)
                    {
                        query = query.Where(predicate);
                    }

                    // EF Core SumAsync only has type-specific overloads (int, long, double, etc.)
                    // so we materialize the selected values and sum in memory via INumber<TValue>.
                    var values = await query.Select(selector).ToListAsync(ct).ConfigureAwait(false);
                    var sum = TValue.Zero;

                    for (var i = 0; i < values.Count; i++)
                    {
                        sum += values[i];
                    }

                    return (IReadOnlyList<ShardAggregatePartial<TValue>>)
                        [new ShardAggregatePartial<TValue>(shardId, sum, 0, null, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var totalSum = AggregationCombiner.CombineSum(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed Sum aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed Sum aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    typeof(TEntity).Name, totalSum, totalShards, elapsed.TotalMilliseconds);
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

        _logger.LogDebug("Starting distributed Avg aggregation for {EntityType}", typeof(TEntity).Name);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = context.Set<TEntity>().AsNoTracking();

                    if (predicate is not null)
                    {
                        query = query.Where(predicate);
                    }

                    // Two-phase aggregation: collect sum and count per shard.
                    // EF Core AverageAsync only has type-specific overloads, so we
                    // materialize values and compute sum + count for correct global average.
                    var values = await query.Select(selector).ToListAsync(ct).ConfigureAwait(false);
                    var sum = TValue.Zero;

                    for (var i = 0; i < values.Count; i++)
                    {
                        sum += values[i];
                    }

                    return (IReadOnlyList<ShardAggregatePartial<TValue>>)
                        [new ShardAggregatePartial<TValue>(shardId, sum, values.Count, null, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var avg = AggregationCombiner.CombineAvg(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed Avg aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed Avg aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    typeof(TEntity).Name, avg, totalShards, elapsed.TotalMilliseconds);
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

        _logger.LogDebug("Starting distributed Min aggregation for {EntityType}", typeof(TEntity).Name);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = context.Set<TEntity>().AsNoTracking();

                    if (predicate is not null)
                    {
                        query = query.Where(predicate);
                    }

                    // MinAsync has a fully generic overload: MinAsync<TSource, TResult>(selector)
                    var min = await query.MinAsync(selector, ct).ConfigureAwait(false);

                    return (IReadOnlyList<ShardAggregatePartial<TValue>>)
                        [new ShardAggregatePartial<TValue>(shardId, default, 0, min, null)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var globalMin = AggregationCombiner.CombineMin(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed Min aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed Min aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    typeof(TEntity).Name, globalMin, totalShards, elapsed.TotalMilliseconds);
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

        _logger.LogDebug("Starting distributed Max aggregation for {EntityType}", typeof(TEntity).Name);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = context.Set<TEntity>().AsNoTracking();

                    if (predicate is not null)
                    {
                        query = query.Where(predicate);
                    }

                    // MaxAsync has a fully generic overload: MaxAsync<TSource, TResult>(selector)
                    var max = await query.MaxAsync(selector, ct).ConfigureAwait(false);

                    return (IReadOnlyList<ShardAggregatePartial<TValue>>)
                        [new ShardAggregatePartial<TValue>(shardId, default, 0, null, max)];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
        {
            var globalMax = AggregationCombiner.CombineMax(queryResult.Results);
            var elapsed = Stopwatch.GetElapsedTime(sw);
            var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;

            if (queryResult.FailedShards.Count > 0)
            {
                _logger.LogWarning(
                    "Distributed Max aggregation for {EntityType} completed with partial results: {FailedShards}/{TotalShards} shards failed in {DurationMs:F1}ms",
                    typeof(TEntity).Name, queryResult.FailedShards.Count, totalShards, elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Distributed Max aggregation for {EntityType} completed: Result={Result}, Shards={TotalShards}, Duration={DurationMs:F1}ms",
                    typeof(TEntity).Name, globalMax, totalShards, elapsed.TotalMilliseconds);
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

        var scatterResult = await _queryExecutor.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = SpecificationEvaluator.GetQuery(
                        context.Set<TEntity>().AsNoTracking().AsQueryable(),
                        specification);

                    IReadOnlyList<TEntity> items = await query.ToListAsync(ct).ConfigureAwait(false);
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

        // For pagination, we need both total count and the page data.
        // Fetch up to (page * pageSize) items from each shard to ensure correct ordering.
        var scatterResult = await _queryExecutor.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = SpecificationEvaluator.GetQuery(
                        context.Set<TEntity>().AsNoTracking().AsQueryable(),
                        specification);

                    IReadOnlyList<TEntity> items = await query
                        .Take(pagination.Page * pagination.PageSize)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);
                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[items];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        // Also scatter count queries
        var countResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var count = (long)await context.Set<TEntity>()
                        .AsNoTracking()
                        .CountAsync(specification.ToExpression(), ct)
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

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var count = (long)await context.Set<TEntity>()
                        .AsNoTracking()
                        .CountAsync(specification.ToExpression(), ct)
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

        var scatterResult = await _queryExecutor.ExecuteAsync(
            shardIds,
            async (shardId, ct) =>
            {
                var ctxResult = _contextFactory.CreateContextForShard(shardId);
                return await ctxResult.MapAsync(async context =>
                {
                    await using var _ = context.ConfigureAwait(false);
                    var query = SpecificationEvaluator.GetQuery(
                        context.Set<TEntity>().AsNoTracking().AsQueryable(),
                        specification);

                    IReadOnlyList<TEntity> items = await query.ToListAsync(ct).ConfigureAwait(false);
                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[items];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
            BuildSpecificationResult(queryResult, specification, sw, "query"));
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

    private static FunctionalRepositoryEF<TEntity, TId> CreateRepository(DbContext context)
        => new(context);
}
