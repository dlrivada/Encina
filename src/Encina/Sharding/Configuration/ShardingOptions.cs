using Encina.Sharding.ReferenceTables;
using Encina.Sharding.Resharding;
using Encina.Sharding.Routing;
using Encina.Sharding.Shadow;
using Encina.Sharding.TimeBased;

namespace Encina.Sharding.Configuration;

/// <summary>
/// Configuration options for database sharding of a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type being sharded.</typeparam>
/// <remarks>
/// <para>
/// Configure sharding when calling <c>AddEncinaSharding&lt;TEntity&gt;()</c>:
/// </para>
/// <code>
/// services.AddEncinaSharding&lt;Order&gt;(options =>
/// {
///     options.UseHashRouting()
///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
///         .AddShard("shard-1", "Server=shard1;Database=Orders;...");
/// });
/// </code>
/// </remarks>
public sealed class ShardingOptions<TEntity>
    where TEntity : notnull
{
    private readonly Dictionary<string, ShardInfo> _shards = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _colocatedEntityTypes = [];
    private readonly List<ReferenceTableConfiguration> _referenceTableConfigurations = [];
    private Func<ShardTopology, IShardRouter>? _routerFactory;
    private TimeBasedShardRouterOptions? _timeBasedOptions;
    private ShadowShardingOptions? _shadowShardingOptions;
    private ReshardingBuilder? _reshardingBuilder;

    /// <summary>
    /// Gets the scatter-gather options for cross-shard queries.
    /// </summary>
    public ScatterGatherOptions ScatterGather { get; } = new();

    /// <summary>
    /// Gets the configured shards.
    /// </summary>
    internal IReadOnlyDictionary<string, ShardInfo> Shards => _shards;

    /// <summary>
    /// Gets the router factory, or null if no routing strategy has been configured.
    /// </summary>
    internal Func<ShardTopology, IShardRouter>? RouterFactory => _routerFactory;

    /// <summary>
    /// Gets the co-located entity types registered for this entity.
    /// </summary>
    internal IReadOnlyList<Type> ColocatedEntityTypes => _colocatedEntityTypes;

    /// <summary>
    /// Gets the reference table configurations registered for this sharding topology.
    /// </summary>
    internal IReadOnlyList<ReferenceTableConfiguration> ReferenceTableConfigurations => _referenceTableConfigurations;

    /// <summary>
    /// Gets the time-based routing options, or null if time-based routing is not configured.
    /// </summary>
    internal TimeBasedShardRouterOptions? TimeBasedOptions => _timeBasedOptions;

    /// <summary>
    /// Gets a value indicating whether shadow sharding is enabled for this entity type.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="WithShadowSharding"/> has been called; <c>false</c> by default.
    /// </value>
    public bool UseShadowSharding { get; private set; }

    /// <summary>
    /// Gets the shadow sharding options, or <c>null</c> if shadow sharding is not configured.
    /// </summary>
    /// <value>
    /// The <see cref="ShadowShardingOptions"/> instance configured via <see cref="WithShadowSharding"/>,
    /// or <c>null</c> if shadow sharding has not been enabled.
    /// </value>
    public ShadowShardingOptions? ShadowSharding => _shadowShardingOptions;

    /// <summary>
    /// Gets a value indicating whether online resharding is enabled for this entity type.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="WithResharding"/> has been called; <c>false</c> by default.
    /// </value>
    public bool UseResharding { get; private set; }

    /// <summary>
    /// Gets the resharding builder, or <c>null</c> if resharding is not configured.
    /// </summary>
    internal ReshardingBuilder? ReshardingBuilder => _reshardingBuilder;

    /// <summary>
    /// Declares that <typeparamref name="TColocated"/> should be co-located with
    /// <typeparamref name="TEntity"/> on the same shard.
    /// </summary>
    /// <typeparam name="TColocated">The entity type to co-locate.</typeparam>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Co-located entities share the same shard key and are guaranteed to reside on the same
    /// physical shard. This enables efficient local JOINs and shard-local transactions.
    /// </para>
    /// <para>
    /// Validation is performed at startup to ensure:
    /// <list type="bullet">
    /// <item>The co-located entity is shardable (implements <see cref="IShardable"/>,
    /// <see cref="ICompoundShardable"/>, or has <see cref="ShardKeyAttribute"/>).</item>
    /// <item>Shard key types are compatible between root and co-located entity.</item>
    /// <item>The entity is not already part of a different co-location group.</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;...")
    ///         .AddShard("shard-1", "Server=shard1;...")
    ///         .AddColocatedEntity&lt;OrderItem&gt;()
    ///         .AddColocatedEntity&lt;OrderPayment&gt;();
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> AddColocatedEntity<TColocated>()
        where TColocated : notnull
    {
        var entityType = typeof(TColocated);

        if (!_colocatedEntityTypes.Contains(entityType))
        {
            _colocatedEntityTypes.Add(entityType);
        }

        return this;
    }

    /// <summary>
    /// Adds a shard to the configuration.
    /// </summary>
    /// <param name="shardId">The unique shard identifier.</param>
    /// <param name="connectionString">The database connection string for this shard.</param>
    /// <param name="weight">Relative weight for load distribution (default 1).</param>
    /// <param name="isActive">Whether the shard is active (default true).</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> AddShard(
        string shardId,
        string connectionString,
        int weight = 1,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(connectionString);

        _shards[shardId] = new ShardInfo(shardId, connectionString, weight, isActive);
        return this;
    }

    /// <summary>
    /// Configures hash-based routing using consistent hashing with xxHash64.
    /// </summary>
    /// <param name="configureOptions">Optional configuration for the hash router.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseHashRouting(Action<HashShardRouterOptions>? configureOptions = null)
    {
        var options = new HashShardRouterOptions();
        configureOptions?.Invoke(options);

        _routerFactory = topology => new HashShardRouter(topology, options);
        return this;
    }

    /// <summary>
    /// Configures range-based routing.
    /// </summary>
    /// <param name="ranges">The key ranges that define shard boundaries.</param>
    /// <param name="comparer">Optional string comparer for key comparisons.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseRangeRouting(
        IEnumerable<ShardRange> ranges,
        StringComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var rangeList = ranges.ToList();
        _routerFactory = topology => new RangeShardRouter(topology, rangeList, comparer);
        return this;
    }

    /// <summary>
    /// Configures directory-based routing.
    /// </summary>
    /// <param name="store">The directory store for key-to-shard mappings.</param>
    /// <param name="defaultShardId">Optional default shard ID for unmapped keys.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseDirectoryRouting(
        IShardDirectoryStore store,
        string? defaultShardId = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _routerFactory = topology => new DirectoryShardRouter(topology, store, defaultShardId);
        return this;
    }

    /// <summary>
    /// Configures geo-based routing.
    /// </summary>
    /// <param name="regions">The geo region definitions.</param>
    /// <param name="regionResolver">A function that extracts a region code from a shard key.</param>
    /// <param name="configureOptions">Optional configuration for the geo router.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseGeoRouting(
        IEnumerable<GeoRegion> regions,
        Func<string, string> regionResolver,
        Action<GeoShardRouterOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(regionResolver);

        var regionList = regions.ToList();
        var options = new GeoShardRouterOptions();
        configureOptions?.Invoke(options);

        _routerFactory = topology => new GeoShardRouter(topology, regionList, regionResolver, options);
        return this;
    }

    /// <summary>
    /// Configures compound routing where each key component is routed by a dedicated strategy.
    /// </summary>
    /// <param name="configure">A builder configuration action for per-component routers.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use compound routing when entities have multi-field shard keys (e.g., region + customer ID)
    /// and each field requires a different routing strategy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.UseCompoundRouting(compound =>
    /// {
    ///     compound
    ///         .RangeComponent(0, regionRanges)    // Region via range routing
    ///         .HashComponent(1);                   // Customer via hash routing
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> UseCompoundRouting(Action<CompoundRoutingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new CompoundRoutingBuilder();
        configure(builder);

        _routerFactory = topology => builder.Build(topology);
        return this;
    }

    /// <summary>
    /// Configures a custom router.
    /// </summary>
    /// <param name="routerFactory">A factory that creates the router from the topology.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public ShardingOptions<TEntity> UseCustomRouting(Func<ShardTopology, IShardRouter> routerFactory)
    {
        ArgumentNullException.ThrowIfNull(routerFactory);
        _routerFactory = routerFactory;
        return this;
    }

    /// <summary>
    /// Configures time-based routing for temporal data partitioning with tier lifecycle management.
    /// </summary>
    /// <param name="configure">Configuration action for time-based routing options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Time-based routing partitions data by time periods (daily, weekly, monthly, quarterly, yearly)
    /// and manages shard lifecycle through tiers: Hot → Warm → Cold → Archived.
    /// </para>
    /// <para>
    /// When <see cref="TimeBasedShardRouterOptions.AutoCreateShards"/> or
    /// <see cref="TimeBasedShardRouterOptions.TierTransitions"/> are configured,
    /// a <see cref="TierTransitionScheduler"/> background service is automatically registered.
    /// </para>
    /// <para>
    /// Initial shards are provided via <see cref="TimeBasedShardRouterOptions.InitialShards"/>.
    /// These seed both the <see cref="ITierStore"/> and the <see cref="TimeBasedShardRouter"/>.
    /// Shards are also automatically added to the topology (no need to call <see cref="AddShard"/>
    /// separately for initial shards).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseTimeBasedRouting(tb =>
    ///     {
    ///         tb.Period = ShardPeriod.Monthly;
    ///         tb.ShardIdPrefix = "orders";
    ///         tb.HotTierConnectionString = "Server=hot;Database=orders_{0}";
    ///         tb.AutoCreateShards = true;
    ///         tb.TierTransitions =
    ///         [
    ///             new TierTransition(ShardTier.Hot, ShardTier.Warm, TimeSpan.FromDays(30)),
    ///             new TierTransition(ShardTier.Warm, ShardTier.Cold, TimeSpan.FromDays(90)),
    ///         ];
    ///         tb.InitialShards =
    ///         [
    ///             new ShardTierInfo("orders-2026-02", ShardTier.Hot,
    ///                 new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1),
    ///                 false, "Server=hot;Database=orders_2026_02", DateTime.UtcNow),
    ///             new ShardTierInfo("orders-2026-01", ShardTier.Warm,
    ///                 new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
    ///                 true, "Server=warm;Database=orders_2026_01", DateTime.UtcNow),
    ///         ];
    ///     });
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> UseTimeBasedRouting(Action<TimeBasedShardRouterOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var tbOptions = new TimeBasedShardRouterOptions();
        configure(tbOptions);

        _timeBasedOptions = tbOptions;

        // Auto-register initial shards into the topology
        foreach (var tierInfo in tbOptions.InitialShards)
        {
            _shards[tierInfo.ShardId] = new ShardInfo(
                tierInfo.ShardId,
                tierInfo.ConnectionString,
                Weight: 1,
                IsActive: tierInfo.CurrentTier != ShardTier.Archived);
        }

        // Set the router factory to create a TimeBasedShardRouter
        _routerFactory = topology => new TimeBasedShardRouter(
            topology,
            tbOptions.InitialShards,
            tbOptions.Period,
            tbOptions.WeekStart);

        return this;
    }

    /// <summary>
    /// Enables shadow sharding for testing a new shard topology under real production traffic.
    /// </summary>
    /// <param name="configure">
    /// Optional configuration action for the shadow sharding options. If <c>null</c>, the options
    /// must have been pre-configured or the <see cref="ShadowShardingOptions.ShadowTopology"/>
    /// property must be set before registration completes.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Shadow sharding allows you to test a new shard topology alongside the production topology.
    /// All production operations continue to use the original router; shadow operations run in
    /// parallel (fire-and-forget for writes, percentage-based sampling for reads).
    /// </para>
    /// <para>
    /// Shadow failures are logged but never affect the production path.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;...")
    ///         .AddShard("shard-1", "Server=shard1;...")
    ///         .WithShadowSharding(shadow =>
    ///         {
    ///             shadow.ShadowTopology = newTopology;
    ///             shadow.DualWriteEnabled = true;
    ///             shadow.ShadowReadPercentage = 10;
    ///             shadow.CompareResults = true;
    ///         });
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> WithShadowSharding(Action<ShadowShardingOptions>? configure = null)
    {
        _shadowShardingOptions ??= new ShadowShardingOptions();
        configure?.Invoke(_shadowShardingOptions);
        UseShadowSharding = true;
        return this;
    }

    /// <summary>
    /// Enables online resharding for automated data migration between shards with minimal downtime.
    /// </summary>
    /// <param name="configure">
    /// Optional configuration action for the resharding builder. If <c>null</c>, default options are used.
    /// </param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Online resharding coordinates the full 6-phase workflow: Plan → Copy → Replicate → Verify
    /// → Cutover → Cleanup. It integrates with <see cref="Routing.IShardRebalancer"/> for plan
    /// generation, bulk operations for data copy, and CDC for incremental replication.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;...")
    ///         .AddShard("shard-1", "Server=shard1;...")
    ///         .WithResharding(resharding =>
    ///         {
    ///             resharding.CopyBatchSize = 10_000;
    ///             resharding.CdcLagThreshold = TimeSpan.FromSeconds(5);
    ///             resharding.VerificationMode = VerificationMode.CountAndChecksum;
    ///             resharding.CutoverTimeout = TimeSpan.FromSeconds(30);
    ///             resharding.CleanupRetentionPeriod = TimeSpan.FromHours(24);
    ///         });
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> WithResharding(Action<ReshardingBuilder>? configure = null)
    {
        _reshardingBuilder ??= new ReshardingBuilder();
        configure?.Invoke(_reshardingBuilder);
        UseResharding = true;
        return this;
    }

    /// <summary>
    /// Registers a reference table (broadcast table) that will be replicated to all shards
    /// in the topology for local JOINs.
    /// </summary>
    /// <typeparam name="TRefTable">The entity type of the reference table.</typeparam>
    /// <param name="configure">Optional configuration action for the reference table options.</param>
    /// <returns>This instance for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// Reference tables are small, read-heavy lookup tables (e.g., countries, currencies)
    /// that are automatically replicated from a primary shard to all other shards. This
    /// enables efficient local JOINs without cross-shard traffic.
    /// </para>
    /// <para>
    /// The entity must either be decorated with <see cref="ReferenceTableAttribute"/> or
    /// be explicitly registered via this method. Validation occurs at startup.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =>
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;...")
    ///         .AddShard("shard-1", "Server=shard1;...")
    ///         .AddReferenceTable&lt;Country&gt;(rt =>
    ///         {
    ///             rt.RefreshStrategy = RefreshStrategy.Polling;
    ///             rt.PrimaryShardId = "shard-0";
    ///         })
    ///         .AddReferenceTable&lt;Currency&gt;();
    /// });
    /// </code>
    /// </example>
    public ShardingOptions<TEntity> AddReferenceTable<TRefTable>(
        Action<ReferenceTableOptions>? configure = null)
        where TRefTable : class
    {
        var refTableType = typeof(TRefTable);

        // Prevent duplicate registrations
        if (_referenceTableConfigurations.Any(c => c.EntityType == refTableType))
        {
            throw new InvalidOperationException(
                $"Reference table '{refTableType.Name}' is already registered. " +
                "Each reference table can only be registered once.");
        }

        var options = new ReferenceTableOptions();
        configure?.Invoke(options);

        _referenceTableConfigurations.Add(new ReferenceTableConfiguration(refTableType, options));
        return this;
    }

    /// <summary>
    /// Builds the <see cref="ShardTopology"/> from the configured shards.
    /// </summary>
    internal ShardTopology BuildTopology() => new(_shards.Values);

    /// <summary>
    /// Builds the <see cref="IShardRouter"/> from the configured router factory and topology.
    /// </summary>
    internal IShardRouter BuildRouter(ShardTopology topology)
    {
        if (_routerFactory is null)
        {
            throw new InvalidOperationException(
                "No routing strategy has been configured. " +
                "Call UseHashRouting(), UseRangeRouting(), UseDirectoryRouting(), UseGeoRouting(), UseCompoundRouting(), UseTimeBasedRouting(), or UseCustomRouting() before building.");
        }

        return _routerFactory(topology);
    }
}
