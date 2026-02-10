using Encina.Sharding;
using Encina.Sharding.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Sharding;

/// <summary>
/// EF Core implementation of <see cref="IShardedDbContextFactory{TContext}"/> that creates
/// DbContext instances connected to specific shards.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This factory is provider-agnostic. The actual database provider configuration
/// (UseSqlServer, UseNpgsql, UseMySql, UseSqlite) is supplied via the
/// <paramref name="configureOptions"/> delegate during construction.
/// </para>
/// <para>
/// Provider-specific registration extensions (e.g., <c>AddEncinaEFCoreShardingSqlServer</c>)
/// configure this factory with the appropriate provider delegate.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a context for a specific shard
/// var result = factory.CreateContextForShard("shard-0");
/// result.Match(
///     Right: context =&gt; { /* use context */ },
///     Left: error =&gt; logger.LogError("Failed: {Error}", error.Message));
/// </code>
/// </example>
public sealed class ShardedDbContextFactory<TContext> : IShardedDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ShardTopology _topology;
    private readonly IShardRouter _router;
    private readonly IServiceProvider _serviceProvider;
    private readonly Action<DbContextOptionsBuilder<TContext>, string> _configureOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="topology">The shard topology with connection strings.</param>
    /// <param name="router">The shard router.</param>
    /// <param name="serviceProvider">The service provider for context activation.</param>
    /// <param name="configureOptions">
    /// Delegate that configures the <see cref="DbContextOptionsBuilder{TContext}"/> for a given
    /// connection string. Provider-specific registration methods supply the appropriate delegate
    /// (e.g., <c>(builder, cs) =&gt; builder.UseSqlServer(cs)</c>).
    /// </param>
    public ShardedDbContextFactory(
        ShardTopology topology,
        IShardRouter router,
        IServiceProvider serviceProvider,
        Action<DbContextOptionsBuilder<TContext>, string> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configureOptions);

        _topology = topology;
        _router = router;
        _serviceProvider = serviceProvider;
        _configureOptions = configureOptions;
    }

    /// <inheritdoc />
    public Either<EncinaError, TContext> CreateContextForShard(string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        return _topology.GetConnectionString(shardId)
            .Map(connectionString => CreateContext(connectionString));
    }

    /// <inheritdoc />
    public Either<EncinaError, TContext> CreateContextForEntity<TEntity>(TEntity entity)
        where TEntity : notnull
    {
        ArgumentNullException.ThrowIfNull(entity);

        return ShardKeyExtractor.Extract(entity)
            .Bind(key => _router.GetShardId(key))
            .Bind(shardId => CreateContextForShard(shardId));
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllContexts()
    {
        var contexts = new Dictionary<string, TContext>();

        foreach (var shardId in _topology.ActiveShardIds)
        {
            var result = CreateContextForShard(shardId);

            var matched = result.Match<Either<EncinaError, TContext>>(
                Right: ctx =>
                {
                    contexts[shardId] = ctx;
                    return ctx;
                },
                Left: error =>
                {
                    // Cleanup already-created contexts on failure
                    foreach (var ctx in contexts.Values)
                    {
                        ctx.Dispose();
                    }

                    return error;
                });

            if (matched.IsLeft)
            {
                return matched.Match<Either<EncinaError, IReadOnlyDictionary<string, TContext>>>(
                    Right: _ => throw new InvalidOperationException("Unexpected Right after Left check"),
                    Left: error => error);
            }
        }

        return Either<EncinaError, IReadOnlyDictionary<string, TContext>>
            .Right(contexts);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TContext>> CreateContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        // DbContext creation is synchronous; wrap in ValueTask for interface compliance
        return new ValueTask<Either<EncinaError, TContext>>(CreateContextForShard(shardId));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TContext>> CreateContextForEntityAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
        where TEntity : notnull
    {
        return new ValueTask<Either<EncinaError, TContext>>(CreateContextForEntity(entity));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyDictionary<string, TContext>>> CreateAllContextsAsync(
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Either<EncinaError, IReadOnlyDictionary<string, TContext>>>(CreateAllContexts());
    }

    private TContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        _configureOptions(optionsBuilder, connectionString);

        return ActivatorUtilities.CreateInstance<TContext>(
            _serviceProvider,
            optionsBuilder.Options);
    }
}
