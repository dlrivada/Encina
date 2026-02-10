using System.Data;
using Encina.ADO.SqlServer.Repository;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Data.SqlClient;
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
public sealed class FunctionalShardedRepositoryADO<TEntity, TId> : IFunctionalShardedRepository<TEntity, TId>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedConnectionFactory<SqlConnection> _connectionFactory;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly IShardedQueryExecutor _queryExecutor;
    private readonly IRequestContext? _requestContext;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryADO{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="connectionFactory">The sharded connection factory.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    /// <param name="requestContext">Optional request context for audit fields.</param>
    /// <param name="timeProvider">Optional time provider for audit timestamps.</param>
    public FunctionalShardedRepositoryADO(
        IShardRouter<TEntity> router,
        IShardedConnectionFactory<SqlConnection> connectionFactory,
        IEntityMapping<TEntity, TId> mapping,
        IShardedQueryExecutor queryExecutor,
        IRequestContext? requestContext = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(queryExecutor);

        _router = router;
        _connectionFactory = connectionFactory;
        _mapping = mapping;
        _queryExecutor = queryExecutor;
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

    private FunctionalRepositoryADO<TEntity, TId> CreateRepository(IDbConnection connection)
        => new(connection, _mapping, _requestContext, _timeProvider);
}
