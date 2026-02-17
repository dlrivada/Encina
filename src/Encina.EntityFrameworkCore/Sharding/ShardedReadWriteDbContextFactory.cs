using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.ReplicaSelection;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.EntityFrameworkCore.Sharding;

/// <summary>
/// EF Core implementation of <see cref="IShardedReadWriteDbContextFactory{TContext}"/> that
/// creates DbContext instances combining shard routing with read/write separation.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
/// <remarks>
/// <para>
/// This factory is provider-agnostic. The actual database provider configuration
/// (UseSqlServer, UseNpgsql, UseMySql, UseSqlite) is supplied via the
/// <c>configureOptions</c> delegate during construction.
/// </para>
/// <para>
/// Replica selection is handled by per-shard <see cref="IShardReplicaSelector"/> instances,
/// and unhealthy replicas are automatically excluded via the <see cref="IReplicaHealthTracker"/>.
/// </para>
/// <para>
/// The context-aware <see cref="IShardedReadWriteDbContextFactory{TContext}.CreateContextForShard"/>
/// method reads the ambient <see cref="DatabaseRoutingContext"/> to determine whether to
/// connect to the primary or a replica.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Explicit read — uses a replica
/// var readResult = factory.CreateReadContextForShard("shard-0");
///
/// // Explicit write — uses the primary
/// var writeResult = factory.CreateWriteContextForShard("shard-0");
///
/// // Context-aware — uses DatabaseRoutingContext.CurrentIntent
/// DatabaseRoutingContext.CurrentIntent = DatabaseIntent.Read;
/// var autoResult = factory.CreateContextForShard("shard-0");
/// </code>
/// </example>
public sealed class ShardedReadWriteDbContextFactory<TContext> : IShardedReadWriteDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ShardTopology _topology;
    private readonly ShardedReadWriteOptions _options;
    private readonly IReplicaHealthTracker _healthTracker;
    private readonly IServiceProvider _serviceProvider;
    private readonly Action<DbContextOptionsBuilder<TContext>, string> _configureOptions;
    private readonly Dictionary<string, IShardReplicaSelector> _selectors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedReadWriteDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="topology">The shard topology with connection strings.</param>
    /// <param name="options">The sharded read/write options.</param>
    /// <param name="healthTracker">The replica health tracker.</param>
    /// <param name="serviceProvider">The service provider for context activation.</param>
    /// <param name="configureOptions">
    /// Delegate that configures the <see cref="DbContextOptionsBuilder{TContext}"/> for a given
    /// connection string.
    /// </param>
    public ShardedReadWriteDbContextFactory(
        ShardTopology topology,
        ShardedReadWriteOptions options,
        IReplicaHealthTracker healthTracker,
        IServiceProvider serviceProvider,
        Action<DbContextOptionsBuilder<TContext>, string> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(topology);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(healthTracker);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(configureOptions);

        _topology = topology;
        _options = options;
        _healthTracker = healthTracker;
        _serviceProvider = serviceProvider;
        _configureOptions = configureOptions;

        InitializeSelectors();
    }

    /// <inheritdoc />
    public Either<EncinaError, TContext> CreateReadContextForShard(string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        return _topology.GetShard(shardId)
            .Bind(shard =>
            {
                var replicaCs = SelectReplicaConnectionString(shard);
                return replicaCs.Map(cs =>
                {
                    _healthTracker.MarkHealthy(shardId, cs);
                    return CreateContext(cs);
                });
            });
    }

    /// <inheritdoc />
    public Either<EncinaError, TContext> CreateWriteContextForShard(string shardId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        return _topology.GetConnectionString(shardId)
            .Map(CreateContext);
    }

    /// <inheritdoc />
    public Either<EncinaError, TContext> CreateContextForShard(string shardId)
    {
        return DatabaseRoutingContext.IsReadIntent
            ? CreateReadContextForShard(shardId)
            : CreateWriteContextForShard(shardId);
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllReadContexts()
    {
        return CreateAllContextsInternal(CreateReadContextForShard);
    }

    /// <inheritdoc />
    public Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllWriteContexts()
    {
        return CreateAllContextsInternal(CreateWriteContextForShard);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TContext>> CreateReadContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Either<EncinaError, TContext>>(CreateReadContextForShard(shardId));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TContext>> CreateWriteContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Either<EncinaError, TContext>>(CreateWriteContextForShard(shardId));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, TContext>> CreateContextForShardAsync(
        string shardId,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<Either<EncinaError, TContext>>(CreateContextForShard(shardId));
    }

    private Either<EncinaError, string> SelectReplicaConnectionString(ShardInfo shard)
    {
        if (!shard.HasReplicas)
        {
            return _options.FallbackToPrimaryWhenNoReplicas
                ? Either<EncinaError, string>.Right(shard.ConnectionString)
                : Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.NoReplicasConfigured,
                        $"Shard '{shard.ShardId}' has no read replicas configured."));
        }

        var availableReplicas = _healthTracker.GetAvailableReplicas(
            shard.ShardId, shard.ReplicaConnectionStrings);

        if (availableReplicas.Count == 0)
        {
            return _options.FallbackToPrimaryWhenNoReplicas
                ? Either<EncinaError, string>.Right(shard.ConnectionString)
                : Either<EncinaError, string>.Left(
                    EncinaErrors.Create(
                        ShardingErrorCodes.AllReplicasUnhealthy,
                        $"All replicas for shard '{shard.ShardId}' are currently unhealthy."));
        }

        var selector = _selectors.GetValueOrDefault(shard.ShardId)
            ?? _selectors.GetValueOrDefault(string.Empty)!;

        var selected = selector.SelectReplica(availableReplicas);
        return Either<EncinaError, string>.Right(selected);
    }

    private Either<EncinaError, IReadOnlyDictionary<string, TContext>> CreateAllContextsInternal(
        Func<string, Either<EncinaError, TContext>> createForShard)
    {
        var contexts = new Dictionary<string, TContext>();

        foreach (var shardId in _topology.ActiveShardIds)
        {
            var result = createForShard(shardId);

            var matched = result.Match<Either<EncinaError, TContext>>(
                Right: ctx =>
                {
                    contexts[shardId] = ctx;
                    return ctx;
                },
                Left: error =>
                {
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

        return Either<EncinaError, IReadOnlyDictionary<string, TContext>>.Right(contexts);
    }

    private TContext CreateContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        _configureOptions(optionsBuilder, connectionString);

        return ActivatorUtilities.CreateInstance<TContext>(
            _serviceProvider,
            optionsBuilder.Options);
    }

    private void InitializeSelectors()
    {
        _selectors[string.Empty] = ShardReplicaSelectorFactory.Create(_options.DefaultReplicaStrategy);

        foreach (var shard in _topology.GetAllShards())
        {
            if (shard.ReplicaStrategy.HasValue)
            {
                _selectors[shard.ShardId] = ShardReplicaSelectorFactory.Create(shard.ReplicaStrategy.Value);
            }
        }
    }
}
