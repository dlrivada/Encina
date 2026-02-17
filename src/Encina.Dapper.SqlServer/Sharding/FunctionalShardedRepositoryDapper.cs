using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using Dapper;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.DomainModeling.Sharding;
using Encina.Sharding;
using Encina.Sharding.Aggregation;
using Encina.Sharding.Data;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.Sharding;

/// <summary>
/// SQL Server Dapper implementation of <see cref="IFunctionalShardedRepository{TEntity, TId}"/>
/// that routes CRUD operations to the appropriate shard.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Single-entity operations (GetById, Add, Update, Delete) are routed to the correct
/// shard based on the entity's shard key. The shard key is extracted using
/// <see cref="IShardable"/> or <see cref="ShardKeyAttribute"/>.
/// </para>
/// <para>
/// Scatter-gather queries (QueryAllShards, QueryShards) use <see cref="ShardedQueryExecutor"/>
/// for parallel execution with timeout and partial failure handling.
/// </para>
/// <para>
/// Distributed aggregation operations (Count, Sum, Avg, Min, Max) use two-phase aggregation
/// via <see cref="AggregationCombiner"/> to ensure mathematically correct global results.
/// </para>
/// <para>
/// This implementation reuses the non-generic <see cref="IShardedConnectionFactory"/>
/// from the core sharding infrastructure, which returns <see cref="IDbConnection"/> instances
/// compatible with Dapper's extension methods.
/// </para>
/// </remarks>
public sealed class FunctionalShardedRepositoryDapper<TEntity, TId>
    : IFunctionalShardedRepository<TEntity, TId>,
      IShardedAggregationSupport<TEntity, TId>,
      IShardedSpecificationSupport<TEntity, TId>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedConnectionFactory _connectionFactory;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly IShardedQueryExecutor _queryExecutor;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;
    private readonly ShardRoutingMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="connectionFactory">The sharded connection factory.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="logger">The logger for distributed aggregation diagnostics.</param>
    /// <param name="requestContext">Optional request context for audit fields.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    /// <param name="metrics">Optional shard routing metrics for observability.</param>
    public FunctionalShardedRepositoryDapper(
        IShardRouter<TEntity> router,
        IShardedConnectionFactory connectionFactory,
        IEntityMapping<TEntity, TId> mapping,
        IShardedQueryExecutor queryExecutor,
        ILogger<FunctionalShardedRepositoryDapper<TEntity, TId>> logger,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null,
        ShardRoutingMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(queryExecutor);
        ArgumentNullException.ThrowIfNull(logger);

        _router = router;
        _connectionFactory = connectionFactory;
        _mapping = mapping;
        _queryExecutor = queryExecutor;
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

        var shardIdResult = _router.GetShardId(shardKey);

        return await shardIdResult
            .MapAsync(async shardId =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken)
                    .ConfigureAwait(false);

                return await connResult
                    .MapAsync(async connection =>
                    {
                        await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                        var repo = CreateRepository(connection);
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
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken)
                    .ConfigureAwait(false);

                return await connResult
                    .MapAsync(async connection =>
                    {
                        await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                        var repo = CreateRepository(connection);
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
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken)
                    .ConfigureAwait(false);

                return await connResult
                    .MapAsync(async connection =>
                    {
                        await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                        var repo = CreateRepository(connection);
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
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, cancellationToken)
                    .ConfigureAwait(false);

                return await connResult
                    .MapAsync(async connection =>
                    {
                        await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                        var repo = CreateRepository(connection);
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
        _logger.LogDebug("Starting distributed {Operation} aggregation for {EntityType}", "Count", typeof(TEntity).Name);

        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = sqlBuilder.BuildAggregationSql(_mapping.TableName, "COUNT(*)", predicate);

                    var count = await connection.QuerySingleAsync<long>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
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
        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var columnName = sqlBuilder.GetColumnNameFromSelector(selector);

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = builder.BuildAggregationSql(
                        _mapping.TableName, $"SUM([{columnName}])", predicate);

                    var result = await connection.QuerySingleAsync<double?>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);
                    var sum = result is null ? TValue.Zero : TValue.CreateChecked(result.Value);

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
        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var columnName = sqlBuilder.GetColumnNameFromSelector(selector);

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = builder.BuildAggregationSql(
                        _mapping.TableName,
                        $"SUM([{columnName}]) AS SumValue, COUNT(*) AS CountValue",
                        predicate);

                    var row = await connection.QuerySingleAsync<dynamic>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);

                    var sumRaw = (object?)row.SumValue;
                    var sum = sumRaw is null or DBNull ? TValue.Zero : TValue.CreateChecked(Convert.ToDouble(sumRaw, CultureInfo.InvariantCulture));
                    var count = (long)row.CountValue;

                    return (IReadOnlyList<ShardAggregatePartial<TValue>>)
                        [new ShardAggregatePartial<TValue>(shardId, sum, count, null, null)];
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
        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var columnName = sqlBuilder.GetColumnNameFromSelector(selector);

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = builder.BuildAggregationSql(
                        _mapping.TableName, $"MIN([{columnName}])", predicate);

                    var result = await connection.QuerySingleAsync<object?>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);
                    TValue? min = result is null or DBNull ? null : (TValue)Convert.ChangeType(result, typeof(TValue), CultureInfo.InvariantCulture)!;

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
        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var columnName = sqlBuilder.GetColumnNameFromSelector(selector);

        var scatterResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<TValue>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = builder.BuildAggregationSql(
                        _mapping.TableName, $"MAX([{columnName}])", predicate);

                    var result = await connection.QuerySingleAsync<object?>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);
                    TValue? max = result is null or DBNull ? null : (TValue)Convert.ChangeType(result, typeof(TValue), CultureInfo.InvariantCulture)!;

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
            "Starting specification scatter-gather query for {EntityType} (Specification={SpecificationType}) across all shards",
            typeof(TEntity).Name, specTypeName);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = BuildSpecificationSelectSql(sqlBuilder, specification);

                    var results = await connection.QueryAsync<TEntity>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);

                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[results.AsList()];
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
            "Starting paged specification scatter-gather query for {EntityType} (Specification={SpecificationType}) across all shards (Page={Page}, PageSize={PageSize})",
            typeof(TEntity).Name, specTypeName, pagination.Page, pagination.PageSize);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAllAsync<IReadOnlyList<TEntity>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = BuildSpecificationSelectSql(sqlBuilder, specification);

                    var results = await connection.QueryAsync<TEntity>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);

                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[results.AsList()];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        var countResult = await _queryExecutor.ExecuteAllAsync<ShardAggregatePartial<long>>(
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = sqlBuilder.BuildAggregationSql(
                        _mapping.TableName, "COUNT(*)", specification.ToExpression());

                    var count = await connection.QuerySingleAsync<long>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
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
                var mergeStart = Stopwatch.GetTimestamp();
                var perShardItems = BuildPerShardItemsDictionary(queryResult);
                var pagedItems = ScatterGatherResultMerger.MergeOrderAndPaginate(
                    perShardItems, specification, pagination.Page, pagination.PageSize);
                var mergeElapsed = Stopwatch.GetElapsedTime(mergeStart);

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
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = sqlBuilder.BuildAggregationSql(
                        _mapping.TableName, "COUNT(*)", specification.ToExpression());

                    var count = await connection.QuerySingleAsync<long>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
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
            "Starting specification scatter-gather query for {EntityType} (Specification={SpecificationType}) across {ShardCount} specific shards",
            typeof(TEntity).Name, specTypeName, shardIds.Count);
        var sw = Stopwatch.GetTimestamp();

        var scatterResult = await _queryExecutor.ExecuteAsync<IReadOnlyList<TEntity>>(
            shardIds,
            async (shardId, ct) =>
            {
                var connResult = await _connectionFactory.GetConnectionAsync(shardId, ct).ConfigureAwait(false);
                return await connResult.MapAsync(async connection =>
                {
                    await using var _ = (connection as IAsyncDisposable ?? throw new InvalidOperationException("Connection does not support async disposal")).ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, parameters) = BuildSpecificationSelectSql(sqlBuilder, specification);

                    var results = await connection.QueryAsync<TEntity>(
                        new CommandDefinition(sql, new DynamicParameters(parameters), cancellationToken: ct))
                        .ConfigureAwait(false);

                    return (IReadOnlyList<IReadOnlyList<TEntity>>)[results.AsList()];
                }).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return scatterResult.Map(queryResult =>
            BuildSpecificationResult(queryResult, specification, sw, "query"));
    }

    private (string Sql, IDictionary<string, object?> Parameters) BuildSpecificationSelectSql(
        SpecificationSqlBuilder<TEntity> sqlBuilder,
        Specification<TEntity> specification)
    {
        if (specification is QuerySpecification<TEntity> querySpec)
        {
            return sqlBuilder.BuildSelectStatement(_mapping.TableName, querySpec);
        }

        return sqlBuilder.BuildSelectStatement(_mapping.TableName, specification);
    }

    private ShardedSpecificationResult<TEntity> BuildSpecificationResult(
        ShardedQueryResult<IReadOnlyList<TEntity>> queryResult,
        Specification<TEntity> specification,
        long startTimestamp,
        string operationKind)
    {
        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        var mergeStart = Stopwatch.GetTimestamp();
        var perShardItems = BuildPerShardItemsDictionary(queryResult);
        var mergedItems = ScatterGatherResultMerger.MergeAndOrder(perShardItems, specification);
        var mergeElapsed = Stopwatch.GetElapsedTime(mergeStart);

        var itemsPerShard = new Dictionary<string, int>();
        foreach (var (shardId, items) in perShardItems)
        {
            itemsPerShard[shardId] = items.Count;
        }

        var totalShards = queryResult.SuccessfulShards.Count + queryResult.FailedShards.Count;
        LogSpecificationResult(totalShards, queryResult.FailedShards.Count, elapsed, operationKind);

        RecordSpecificationMetrics(
            specification.GetType().Name, operationKind, totalShards,
            queryResult.FailedShards.Count, mergedItems.Count,
            mergeElapsed.TotalMilliseconds, perShardItems);

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

    private FunctionalRepositoryDapper<TEntity, TId> CreateRepository(IDbConnection connection)
        => new(connection, _mapping, _requestContext, _timeProvider);
}
