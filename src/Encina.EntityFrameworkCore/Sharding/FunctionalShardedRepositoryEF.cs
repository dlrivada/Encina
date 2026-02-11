using System.Diagnostics;
using System.Linq.Expressions;
using System.Numerics;
using Encina.EntityFrameworkCore.Repository;
using Encina.Sharding;
using Encina.Sharding.Aggregation;
using Encina.Sharding.Data;

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
      IShardedAggregationSupport<TEntity, TId>
    where TContext : DbContext
    where TEntity : class
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedDbContextFactory<TContext> _contextFactory;
    private readonly IShardedQueryExecutor _queryExecutor;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryEF{TContext, TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="contextFactory">The sharded DbContext factory.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="logger">The logger for aggregation diagnostics.</param>
    public FunctionalShardedRepositoryEF(
        IShardRouter<TEntity> router,
        IShardedDbContextFactory<TContext> contextFactory,
        IShardedQueryExecutor queryExecutor,
        ILogger<FunctionalShardedRepositoryEF<TContext, TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(logger);

        _router = router;
        _contextFactory = contextFactory;
        _queryExecutor = queryExecutor;
        _logger = logger;
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

    private static FunctionalRepositoryEF<TEntity, TId> CreateRepository(DbContext context)
        => new(context);
}
