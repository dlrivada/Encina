using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using Encina.ADO.SqlServer.Repository;
using Encina.Sharding;
using Encina.Sharding.Aggregation;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Sharding;

/// <summary>
/// SQL Server ADO.NET implementation of <see cref="IFunctionalShardedRepository{TEntity, TId}"/>
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
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddEncinaADOSharding&lt;Order, Guid&gt;(mapping =&gt;
/// {
///     mapping.ToTable("Orders").HasId(o =&gt; o.Id);
/// });
///
/// // Use sharded repository
/// var result = await shardedRepo.AddAsync(order, ct);
/// </code>
/// </example>
public sealed class FunctionalShardedRepositoryADO<TEntity, TId>
    : IFunctionalShardedRepository<TEntity, TId>,
      IShardedAggregationSupport<TEntity, TId>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedConnectionFactory<SqlConnection> _connectionFactory;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly IShardedQueryExecutor _queryExecutor;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryADO{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="connectionFactory">The sharded connection factory.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="requestContext">Optional request context for audit fields.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    /// <param name="logger">The logger for distributed aggregation diagnostics.</param>
    public FunctionalShardedRepositoryADO(
        IShardRouter<TEntity> router,
        IShardedConnectionFactory<SqlConnection> connectionFactory,
        IEntityMapping<TEntity, TId> mapping,
        IShardedQueryExecutor queryExecutor,
        ILogger<FunctionalShardedRepositoryADO<TEntity, TId>> logger,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
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
                        await using var _ = connection.ConfigureAwait(false);
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
                        await using var _ = connection.ConfigureAwait(false);
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
                        await using var _ = connection.ConfigureAwait(false);
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
                        await using var _ = connection.ConfigureAwait(false);
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
                    await using var _ = connection.ConfigureAwait(false);
                    var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, addParams) = sqlBuilder.BuildAggregationSql(_mapping.TableName, "COUNT(*)", predicate);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    addParams(cmd);

                    var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    var count = Convert.ToInt64(result, CultureInfo.InvariantCulture);

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
                    await using var _ = connection.ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, addParams) = builder.BuildAggregationSql(
                        _mapping.TableName, $"SUM([{columnName}])", predicate);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    addParams(cmd);

                    var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    var sum = result is DBNull or null ? TValue.Zero : TValue.CreateChecked(Convert.ToDouble(result, CultureInfo.InvariantCulture));

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
                    await using var _ = connection.ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, addParams) = builder.BuildAggregationSql(
                        _mapping.TableName,
                        $"SUM([{columnName}]) AS SumValue, COUNT(*) AS CountValue",
                        predicate);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    addParams(cmd);

                    await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    TValue sum = TValue.Zero;
                    var count = 0L;

                    if (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        var sumRaw = reader.GetValue(0);
                        sum = sumRaw is DBNull ? TValue.Zero : TValue.CreateChecked(Convert.ToDouble(sumRaw, CultureInfo.InvariantCulture));
                        count = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
                    }

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
                    await using var _ = connection.ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, addParams) = builder.BuildAggregationSql(
                        _mapping.TableName, $"MIN([{columnName}])", predicate);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    addParams(cmd);

                    var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    TValue? min = result is DBNull or null ? null : (TValue)Convert.ChangeType(result, typeof(TValue), CultureInfo.InvariantCulture)!;

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
                    await using var _ = connection.ConfigureAwait(false);
                    var builder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
                    var (sql, addParams) = builder.BuildAggregationSql(
                        _mapping.TableName, $"MAX([{columnName}])", predicate);

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    addParams(cmd);

                    var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    TValue? max = result is DBNull or null ? null : (TValue)Convert.ChangeType(result, typeof(TValue), CultureInfo.InvariantCulture)!;

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

    private FunctionalRepositoryADO<TEntity, TId> CreateRepository(IDbConnection connection)
        => new(connection, _mapping, _requestContext, _timeProvider);
}
