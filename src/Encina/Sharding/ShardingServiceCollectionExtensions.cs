using System.Reflection;
using Encina.Sharding.Colocation;
using Encina.Sharding.Configuration;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.TimeBased;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Encina.Sharding;

/// <summary>
/// Extension methods for configuring Encina database sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharding services for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for sharding options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><see cref="ShardingOptions{TEntity}"/> - Configuration via IOptions pattern</item>
    /// <item><see cref="ShardTopology"/> - The shard topology for this entity type</item>
    /// <item><see cref="IShardRouter"/> - The configured routing strategy</item>
    /// <item><see cref="IShardRouter{TEntity}"/> - Entity-aware router with shard key extraction</item>
    /// <item><see cref="ColocationGroupRegistry"/> - Co-location group registry (singleton, shared)</item>
    /// <item><see cref="IReferenceTableRegistry"/> - Reference table registry (singleton, if reference tables are configured)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Startup validation ensures all shard IDs have connection strings, a routing strategy is configured,
    /// and all co-located entities satisfy co-location constraints.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting(hash => hash.VirtualNodesPerShard = 200)
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...")
    ///         .AddShard("shard-2", "Server=shard2;Database=Orders;...")
    ///         .AddShard("shard-3", "Server=shard3;Database=Orders;...")
    ///         .AddColocatedEntity&lt;OrderItem&gt;()
    ///         .AddColocatedEntity&lt;OrderPayment&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSharding<TEntity>(
        this IServiceCollection services,
        Action<ShardingOptions<TEntity>> configure)
        where TEntity : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ShardingOptions<TEntity>();
        configure(options);

        ValidateOptions(options);

        var topology = options.BuildTopology();
        var router = options.BuildRouter(topology);

        // Register options
        services.Configure<ShardingOptions<TEntity>>(opt =>
        {
            foreach (var shard in options.Shards)
            {
                opt.AddShard(shard.Key, shard.Value.ConnectionString, shard.Value.Weight, shard.Value.IsActive);
            }

            opt.ScatterGather.MaxParallelism = options.ScatterGather.MaxParallelism;
            opt.ScatterGather.Timeout = options.ScatterGather.Timeout;
            opt.ScatterGather.AllowPartialResults = options.ScatterGather.AllowPartialResults;
        });

        // Register scatter-gather options
        services.Configure<ScatterGatherOptions>(opt =>
        {
            opt.MaxParallelism = options.ScatterGather.MaxParallelism;
            opt.Timeout = options.ScatterGather.Timeout;
            opt.AllowPartialResults = options.ScatterGather.AllowPartialResults;
        });

        // Register topology as singleton (keyed by entity type via wrapping)
        services.AddSingleton(topology);

        // Register topology provider (allows cached implementations to replace this)
        services.TryAddSingleton<IShardTopologyProvider>(new DefaultShardTopologyProvider(topology));

        // Register the base router
        services.TryAddSingleton<IShardRouter>(router);

        // Register the entity-aware router
        services.TryAddSingleton<IShardRouter<TEntity>>(new EntityShardRouter<TEntity>(router));

        // Register co-location group registry (singleton, populated from all registered groups)
        services.TryAddSingleton(sp =>
        {
            var registry = new ColocationGroupRegistry();
            var configurators = sp.GetServices<IConfigureColocationGroup>();

            foreach (var configurator in configurators)
            {
                configurator.Configure(registry);
            }

            return registry;
        });

        // Validate and register co-location groups
        ValidateAndRegisterColocationGroups<TEntity>(services, options);

        // Validate and register reference tables
        ValidateAndRegisterReferenceTables(services, options, topology);

        // Register time-based sharding services
        RegisterTimeBasedShardingServices(services, options, router);

        return services;
    }

    private static void ValidateOptions<TEntity>(ShardingOptions<TEntity> options)
        where TEntity : notnull
    {
        if (options.Shards.Count == 0)
        {
            throw new InvalidOperationException(
                $"No shards configured for entity type '{typeof(TEntity).Name}'. " +
                "Call AddShard() at least once.");
        }

        if (options.RouterFactory is null)
        {
            throw new InvalidOperationException(
                $"No routing strategy configured for entity type '{typeof(TEntity).Name}'. " +
                "Call UseHashRouting(), UseRangeRouting(), UseDirectoryRouting(), UseGeoRouting(), UseCompoundRouting(), UseTimeBasedRouting(), or UseCustomRouting().");
        }

        foreach (var shard in options.Shards.Values)
        {
            if (string.IsNullOrWhiteSpace(shard.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"Shard '{shard.ShardId}' for entity type '{typeof(TEntity).Name}' has no connection string.");
            }
        }
    }

    private static void ValidateAndRegisterReferenceTables<TEntity>(
        IServiceCollection services,
        ShardingOptions<TEntity> options,
        ShardTopology topology)
        where TEntity : notnull
    {
        var configurations = options.ReferenceTableConfigurations;

        if (configurations.Count == 0)
        {
            return;
        }

        // Validate each reference table configuration
        foreach (var config in configurations)
        {
            ValidateReferenceTableConfiguration(config, topology);
        }

        // Register the reference table registry as singleton
        // Accumulate configurations from all AddEncinaSharding<T> calls
        foreach (var config in configurations)
        {
            services.AddSingleton(config);
        }

        services.TryAddSingleton<IReferenceTableRegistry>(sp =>
        {
            var allConfigs = sp.GetServices<ReferenceTableConfiguration>();
            return new ReferenceTableRegistry(allConfigs);
        });

        // Register replication engine services
        services.TryAddSingleton<IReferenceTableStateStore, InMemoryReferenceTableStateStore>();
        services.TryAddSingleton<IReferenceTableReplicator, ReferenceTableReplicator>();
        services.TryAddSingleton<PollingRefreshDetector>();

        // Register background replication service
        services.AddSingleton<IHostedService, ReferenceTableReplicationService>();
    }

    private static void ValidateReferenceTableConfiguration(
        ReferenceTableConfiguration config,
        ShardTopology topology)
    {
        var entityType = config.EntityType;
        var opts = config.Options;

        // Validate: entity must have [ReferenceTable] attribute or be explicitly registered
        // (explicit registration via AddReferenceTable<T>() is always valid)
        // We just log a note; the attribute is informational, not mandatory when using fluent API.

        // Validate primary shard exists in topology
        if (opts.PrimaryShardId is not null && !topology.ContainsShard(opts.PrimaryShardId))
        {
            throw new InvalidOperationException(
                $"Reference table '{entityType.Name}' specifies primary shard '{opts.PrimaryShardId}' " +
                "which does not exist in the shard topology. " +
                $"Available shards: {string.Join(", ", topology.AllShardIds)}.");
        }

        // Validate batch size
        if (opts.BatchSize <= 0)
        {
            throw new InvalidOperationException(
                $"Reference table '{entityType.Name}' has an invalid batch size ({opts.BatchSize}). " +
                "BatchSize must be greater than zero.");
        }

        // Validate polling interval
        if (opts.RefreshStrategy == RefreshStrategy.Polling && opts.PollingInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"Reference table '{entityType.Name}' uses Polling refresh strategy " +
                $"but has an invalid polling interval ({opts.PollingInterval}). " +
                "PollingInterval must be a positive duration.");
        }
    }

    private static void ValidateAndRegisterColocationGroups<TEntity>(
        IServiceCollection services,
        ShardingOptions<TEntity> options)
        where TEntity : notnull
    {
        var rootEntityType = typeof(TEntity);

        // Collect co-located entity types from both fluent API and [ColocatedWith] attributes
        var colocatedTypes = new HashSet<Type>(options.ColocatedEntityTypes);

        // Scan for [ColocatedWith] attribute declarations pointing to this root entity
        ScanColocatedWithAttributes(rootEntityType, colocatedTypes);

        if (colocatedTypes.Count == 0)
        {
            return;
        }

        // Determine root entity's shard key type for compatibility checking
        var rootShardKeyType = GetShardKeyType(rootEntityType);

        foreach (var colocatedType in colocatedTypes)
        {
            ValidateColocatedEntity(rootEntityType, colocatedType, rootShardKeyType);
        }

        // Register the co-location group via a post-configuration callback
        // This uses a deferred action that runs when the registry is resolved
        var colocatedList = colocatedTypes.ToList();

        services.AddSingleton<IConfigureColocationGroup>(
            new ConfigureColocationGroup(rootEntityType, colocatedList));
    }

    private static void RegisterTimeBasedShardingServices<TEntity>(
        IServiceCollection services,
        ShardingOptions<TEntity> options,
        IShardRouter router)
        where TEntity : notnull
    {
        var tbOptions = options.TimeBasedOptions;

        if (tbOptions is null)
        {
            return;
        }

        ValidateTimeBasedOptions(tbOptions);

        // Register the time-based router as ITimeBasedShardRouter if the router implements it
        if (router is ITimeBasedShardRouter timeBasedRouter)
        {
            services.TryAddSingleton(timeBasedRouter);
        }

        // Register ITierStore (InMemoryTierStore seeded with initial shards)
        services.TryAddSingleton<ITierStore>(sp =>
        {
            var timeProvider = sp.GetService<TimeProvider>();
            var store = new InMemoryTierStore(timeProvider);

            // Seed with initial shards
            foreach (var tierInfo in tbOptions.InitialShards)
            {
                store.AddShardAsync(tierInfo).GetAwaiter().GetResult();
            }

            return store;
        });

        // Register IShardArchiver (default implementation)
        services.TryAddSingleton<IShardArchiver>(sp =>
        {
            var tierStore = sp.GetRequiredService<ITierStore>();
            var topologyProvider = sp.GetRequiredService<IShardTopologyProvider>();
            var enforcer = sp.GetService<IReadOnlyEnforcer>();
            return new ShardArchiver(tierStore, topologyProvider, enforcer);
        });

        // Register TierTransitionScheduler if tier transitions or auto-creation are configured
        var needsScheduler = tbOptions.TierTransitions.Count > 0 || tbOptions.AutoCreateShards;

        if (needsScheduler)
        {
            // Map TimeBasedShardRouterOptions to TimeBasedShardingOptions for the scheduler
            services.Configure<TimeBasedShardingOptions>(schedulerOpts =>
            {
                schedulerOpts.Enabled = true;
                schedulerOpts.CheckInterval = tbOptions.TransitionCheckInterval;
                schedulerOpts.Period = tbOptions.Period;
                schedulerOpts.WeekStart = tbOptions.WeekStart;
                schedulerOpts.Transitions = tbOptions.TierTransitions;
                schedulerOpts.EnableAutoShardCreation = tbOptions.AutoCreateShards;
                schedulerOpts.ShardCreationLeadTime = tbOptions.ShardCreationLeadTime;
                schedulerOpts.ShardIdPrefix = tbOptions.ShardIdPrefix;
                schedulerOpts.ConnectionStringTemplate = tbOptions.HotTierConnectionString;
            });

            services.AddSingleton<IHostedService, TierTransitionScheduler>();
        }
    }

    private static void ValidateTimeBasedOptions(TimeBasedShardRouterOptions options)
    {
        // Validate tier transitions don't skip tiers
        foreach (var transition in options.TierTransitions)
        {
            var validTransitions = new Dictionary<ShardTier, ShardTier>
            {
                [ShardTier.Hot] = ShardTier.Warm,
                [ShardTier.Warm] = ShardTier.Cold,
                [ShardTier.Cold] = ShardTier.Archived,
            };

            if (validTransitions.TryGetValue(transition.FromTier, out var expectedTarget) &&
                transition.ToTier != expectedTarget)
            {
                throw new InvalidOperationException(
                    $"Invalid tier transition from '{transition.FromTier}' to '{transition.ToTier}'. " +
                    $"Tiers must not be skipped. Expected transition to '{expectedTarget}'.");
            }

            if (!validTransitions.ContainsKey(transition.FromTier))
            {
                throw new InvalidOperationException(
                    $"Invalid tier transition source '{transition.FromTier}'. " +
                    "Only Hot, Warm, and Cold tiers can be transition sources.");
            }
        }

        // Validate connection strings are provided for tiers that have transitions targeting them
        var targetTiers = options.TierTransitions.Select(t => t.ToTier).ToHashSet();

        if (targetTiers.Contains(ShardTier.Warm) && string.IsNullOrWhiteSpace(options.WarmTierConnectionString))
        {
            throw new InvalidOperationException(
                "A tier transition targets the Warm tier but no WarmTierConnectionString is configured.");
        }

        if (targetTiers.Contains(ShardTier.Cold) && string.IsNullOrWhiteSpace(options.ColdTierConnectionString))
        {
            throw new InvalidOperationException(
                "A tier transition targets the Cold tier but no ColdTierConnectionString is configured.");
        }

        if (targetTiers.Contains(ShardTier.Archived) && string.IsNullOrWhiteSpace(options.ArchivedTierConnectionString))
        {
            throw new InvalidOperationException(
                "A tier transition targets the Archived tier but no ArchivedTierConnectionString is configured.");
        }

        // Validate auto-creation requires a connection string template
        if (options.AutoCreateShards && string.IsNullOrWhiteSpace(options.HotTierConnectionString))
        {
            throw new InvalidOperationException(
                "AutoCreateShards is enabled but no HotTierConnectionString is configured. " +
                "The Hot-tier connection string template is required for auto-shard creation.");
        }

        // Validate check interval is positive
        if (options.TransitionCheckInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"TransitionCheckInterval must be a positive duration, but was {options.TransitionCheckInterval}.");
        }

        // Validate retention period is positive if set
        if (options.RetentionPeriod.HasValue && options.RetentionPeriod.Value <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"RetentionPeriod must be a positive duration, but was {options.RetentionPeriod.Value}.");
        }

        // Validate shard creation lead time is positive
        if (options.AutoCreateShards && options.ShardCreationLeadTime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"ShardCreationLeadTime must be a positive duration, but was {options.ShardCreationLeadTime}.");
        }
    }

    private static void ScanColocatedWithAttributes(Type rootEntityType, HashSet<Type> colocatedTypes)
    {
        // Scan all types in the root entity's assembly for [ColocatedWith] pointing to this root
        var assembly = rootEntityType.Assembly;

        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<ColocatedWithAttribute>();

            if (attribute is not null && attribute.RootEntityType == rootEntityType)
            {
                colocatedTypes.Add(type);
            }
        }
    }

    private static void ValidateColocatedEntity(Type rootEntityType, Type colocatedType, ShardKeyTypeInfo rootShardKeyType)
    {
        // Prevent self-referencing co-location
        if (colocatedType == rootEntityType)
        {
            throw new ColocationViolationException(
                rootEntityType,
                colocatedType,
                "An entity cannot be co-located with itself.");
        }

        // Verify entity is shardable
        if (!IsShardable(colocatedType))
        {
            throw new ColocationViolationException(
                rootEntityType,
                colocatedType,
                $"Co-located entity '{colocatedType.Name}' is not shardable. " +
                "It must implement IShardable, ICompoundShardable, or have properties marked with [ShardKey].");
        }

        // Check shard key type compatibility
        var colocatedShardKeyType = GetShardKeyType(colocatedType);
        ValidateShardKeyTypeCompatibility(rootEntityType, colocatedType, rootShardKeyType, colocatedShardKeyType);

        // Check that the co-located entity doesn't declare a [ColocatedWith] pointing to a different root
        var attribute = colocatedType.GetCustomAttribute<ColocatedWithAttribute>();

        if (attribute is not null && attribute.RootEntityType != rootEntityType)
        {
            throw new ColocationViolationException(
                rootEntityType,
                colocatedType,
                $"Co-located entity '{colocatedType.Name}' is already declared as co-located with " +
                $"'{attribute.RootEntityType.Name}' via [ColocatedWith] attribute. " +
                "An entity can belong to only one co-location group.");
        }
    }

    private static bool IsShardable(Type entityType)
    {
        // Check interface implementations
        if (typeof(IShardable).IsAssignableFrom(entityType))
        {
            return true;
        }

        if (typeof(ICompoundShardable).IsAssignableFrom(entityType))
        {
            return true;
        }

        // Check for [ShardKey] attribute on properties
        return entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(p => p.GetCustomAttribute<ShardKeyAttribute>() is not null);
    }

    private static ShardKeyTypeInfo GetShardKeyType(Type entityType)
    {
        // ICompoundShardable takes highest priority
        if (typeof(ICompoundShardable).IsAssignableFrom(entityType))
        {
            return new ShardKeyTypeInfo(ShardKeyMechanism.CompoundShardable, typeof(CompoundShardKey));
        }

        // Check for multiple [ShardKey] attributes (compound key via attributes)
        var shardKeyProperties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ShardKeyAttribute>() is not null)
            .ToList();

        if (shardKeyProperties.Count > 1)
        {
            return new ShardKeyTypeInfo(ShardKeyMechanism.CompoundAttribute, typeof(CompoundShardKey));
        }

        // IShardable interface
        if (typeof(IShardable).IsAssignableFrom(entityType))
        {
            return new ShardKeyTypeInfo(ShardKeyMechanism.Shardable, typeof(string));
        }

        // Single [ShardKey] attribute
        if (shardKeyProperties.Count == 1)
        {
            return new ShardKeyTypeInfo(ShardKeyMechanism.SingleAttribute, shardKeyProperties[0].PropertyType);
        }

        return new ShardKeyTypeInfo(ShardKeyMechanism.None, null);
    }

    private static void ValidateShardKeyTypeCompatibility(
        Type rootEntityType,
        Type colocatedType,
        ShardKeyTypeInfo rootInfo,
        ShardKeyTypeInfo colocatedInfo)
    {
        // Both must resolve to compatible mechanisms
        var rootIsCompound = rootInfo.Mechanism is ShardKeyMechanism.CompoundShardable or ShardKeyMechanism.CompoundAttribute;
        var colocatedIsCompound = colocatedInfo.Mechanism is ShardKeyMechanism.CompoundShardable or ShardKeyMechanism.CompoundAttribute;

        // If one is compound and the other is not, that's a mismatch
        if (rootIsCompound != colocatedIsCompound)
        {
            throw new ColocationViolationException(
                rootEntityType,
                colocatedType,
                "Shard key type mismatch: root entity uses a compound shard key but co-located entity uses a simple shard key, or vice versa.",
                rootInfo.ResolvedType?.Name ?? "unknown",
                colocatedInfo.ResolvedType?.Name ?? "unknown");
        }

        // For single-attribute shard keys, check property type assignability
        if (rootInfo.Mechanism == ShardKeyMechanism.SingleAttribute &&
            colocatedInfo.Mechanism == ShardKeyMechanism.SingleAttribute &&
            rootInfo.ResolvedType is not null &&
            colocatedInfo.ResolvedType is not null)
        {
            if (!rootInfo.ResolvedType.IsAssignableFrom(colocatedInfo.ResolvedType) &&
                !colocatedInfo.ResolvedType.IsAssignableFrom(rootInfo.ResolvedType))
            {
                throw new ColocationViolationException(
                    rootEntityType,
                    colocatedType,
                    "Shard key property types are incompatible.",
                    rootInfo.ResolvedType.Name,
                    colocatedInfo.ResolvedType.Name);
            }
        }
    }

    private enum ShardKeyMechanism
    {
        None,
        Shardable,
        CompoundShardable,
        SingleAttribute,
        CompoundAttribute
    }

    private sealed record ShardKeyTypeInfo(ShardKeyMechanism Mechanism, Type? ResolvedType);

    /// <summary>
    /// Marker interface for deferred co-location group configuration.
    /// </summary>
    internal interface IConfigureColocationGroup
    {
        /// <summary>
        /// Configures the co-location group in the registry.
        /// </summary>
        void Configure(ColocationGroupRegistry registry);
    }

    /// <summary>
    /// Deferred co-location group registration that runs when the registry is resolved.
    /// </summary>
    private sealed class ConfigureColocationGroup(
        Type rootEntityType,
        List<Type> colocatedEntityTypes) : IConfigureColocationGroup
    {
        public void Configure(ColocationGroupRegistry registry)
        {
            var group = new ColocationGroup(
                rootEntityType,
                colocatedEntityTypes.AsReadOnly(),
                string.Empty);

            registry.RegisterGroup(group);
        }
    }
}
