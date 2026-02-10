using Encina.EntityFrameworkCore.Repository;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
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
public sealed class FunctionalShardedRepositoryEF<TContext, TEntity, TId> : IFunctionalShardedRepository<TEntity, TId>
    where TContext : DbContext
    where TEntity : class
    where TId : notnull
{
    private readonly IShardRouter<TEntity> _router;
    private readonly IShardedDbContextFactory<TContext> _contextFactory;
    private readonly IShardedQueryExecutor _queryExecutor;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalShardedRepositoryEF{TContext, TEntity, TId}"/> class.
    /// </summary>
    /// <param name="router">The entity-aware shard router.</param>
    /// <param name="contextFactory">The sharded DbContext factory.</param>
    /// <param name="queryExecutor">The scatter-gather query executor.</param>
    public FunctionalShardedRepositoryEF(
        IShardRouter<TEntity> router,
        IShardedDbContextFactory<TContext> contextFactory,
        IShardedQueryExecutor queryExecutor)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(queryExecutor);

        _router = router;
        _contextFactory = contextFactory;
        _queryExecutor = queryExecutor;
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

    private static FunctionalRepositoryEF<TEntity, TId> CreateRepository(DbContext context)
        => new(context);
}
