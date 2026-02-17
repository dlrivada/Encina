using Encina.MongoDB.ReadWriteSeparation;
using Encina.MongoDB.Sharding.ReferenceTables;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Execution;
using Encina.Sharding.Health;
using Encina.Sharding.ReferenceTables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding;

/// <summary>
/// Extension methods for configuring Encina MongoDB sharding services.
/// </summary>
/// <remarks>
/// <para>
/// Provides registration for both sharding modes:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Native <c>mongos</c> sharding (default)</b>: Uses the existing <see cref="IMongoClient"/>
///       registered in DI. All routing is handled by MongoDB's <c>mongos</c> router.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Application-level sharding</b>: Requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
///       from <c>Encina.Sharding</c> has been called first to register topology, router, and options.
///       Uses <see cref="IShardRouter{TEntity}"/> for routing decisions.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina MongoDB sharded repository for a specific entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MongoDB sharding options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When <see cref="MongoDbShardingOptions{TEntity, TId}.UseNativeSharding"/> is <c>true</c> (default):
    /// <list type="bullet">
    ///   <item>Requires <see cref="IMongoClient"/> registered (e.g., via <c>AddEncinaMongoDB</c>)</item>
    ///   <item>All routing is handled by MongoDB's <c>mongos</c></item>
    ///   <item>No <c>AddEncinaSharding&lt;TEntity&gt;</c> call needed</item>
    /// </list>
    /// </para>
    /// <para>
    /// When <c>UseNativeSharding</c> is <c>false</c>:
    /// <list type="bullet">
    ///   <item>Requires <c>AddEncinaSharding&lt;TEntity&gt;</c> to register topology and router</item>
    ///   <item>Connection strings in the topology should point to individual mongod/replica sets</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Native mongos sharding (recommended)
    /// services.AddEncinaMongoDB(options =&gt;
    /// {
    ///     options.ConnectionString = "mongodb://mongos:27017";
    ///     options.DatabaseName = "MyApp";
    /// });
    /// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
    /// {
    ///     options.UseNativeSharding = true;
    ///     options.ShardKeyField = "customerId";
    ///     options.CollectionName = "orders";
    ///     options.IdProperty = o =&gt; o.Id;
    /// });
    ///
    /// // Application-level sharding
    /// services.AddEncinaSharding&lt;Order&gt;(shardOptions =&gt;
    /// {
    ///     shardOptions.UseHashRouting()
    ///         .AddShard("shard-0", "mongodb://shard0:27017/MyApp")
    ///         .AddShard("shard-1", "mongodb://shard1:27017/MyApp");
    /// });
    /// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
    /// {
    ///     options.UseNativeSharding = false;
    ///     options.CollectionName = "orders";
    ///     options.IdProperty = o =&gt; o.Id;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaMongoDBSharding<TEntity, TId>(
        this IServiceCollection services,
        Action<MongoDbShardingOptions<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MongoDbShardingOptions<TEntity, TId>();
        configure(options);
        options.Validate();

        services.TryAddSingleton(TimeProvider.System);

        var collectionName = options.GetEffectiveCollectionName();
        var idSelector = options.IdProperty!;

        if (options.UseNativeSharding)
        {
            RegisterNativeShardingServices<TEntity, TId>(services, options, collectionName, idSelector);
        }
        else
        {
            RegisterAppLevelShardingServices<TEntity, TId>(services, options, collectionName, idSelector);
        }

        return services;
    }

    private static void RegisterNativeShardingServices<TEntity, TId>(
        IServiceCollection services,
        MongoDbShardingOptions<TEntity, TId> options,
        string collectionName,
        System.Linq.Expressions.Expression<Func<TEntity, TId>> idSelector)
        where TEntity : class
        where TId : notnull
    {
        var databaseName = options.DatabaseName;

        // Register collection factory for native mode
        services.TryAddSingleton<IShardedMongoCollectionFactory>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var effectiveDbName = databaseName
                ?? sp.GetService<IOptions<EncinaMongoDbOptions>>()?.Value.DatabaseName
                ?? "Encina";
            return new ShardedMongoCollectionFactory(mongoClient, effectiveDbName);
        });

        // Register sharded repository (native mode)
        services.TryAddScoped<IFunctionalShardedRepository<TEntity, TId>>(sp =>
        {
            var collectionFactory = sp.GetRequiredService<IShardedMongoCollectionFactory>();
            var logger = sp.GetRequiredService<ILogger<FunctionalShardedRepositoryMongoDB<TEntity, TId>>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalShardedRepositoryMongoDB<TEntity, TId>(
                collectionFactory,
                idSelector,
                collectionName,
                logger,
                requestContext,
                timeProvider);
        });

        // Register health monitor (native mode)
        services.TryAddSingleton<IShardedDatabaseHealthMonitor>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var collectionFactory = sp.GetRequiredService<IShardedMongoCollectionFactory>();

            // Create a minimal topology for native mode
            var topology = new ShardTopology(
            [
                new ShardInfo("mongos", "native", Weight: 1, IsActive: true)
            ]);

            return new ShardedMongoDbDatabaseHealthMonitor(
                mongoClient,
                topology,
                collectionFactory,
                useNativeSharding: true);
        });
    }

    private static void RegisterAppLevelShardingServices<TEntity, TId>(
        IServiceCollection services,
        MongoDbShardingOptions<TEntity, TId> options,
        string collectionName,
        System.Linq.Expressions.Expression<Func<TEntity, TId>> idSelector)
        where TEntity : class
        where TId : notnull
    {
        var databaseName = options.DatabaseName;

        // Register collection factory for application-level mode
        services.TryAddSingleton<IShardedMongoCollectionFactory>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var topology = sp.GetRequiredService<ShardTopology>();
            var effectiveDbName = databaseName
                ?? sp.GetService<IOptions<EncinaMongoDbOptions>>()?.Value.DatabaseName
                ?? "Encina";
            return new ShardedMongoCollectionFactory(mongoClient, effectiveDbName, topology);
        });

        // Register ShardedQueryExecutor
        services.TryAddScoped<IShardedQueryExecutor>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var scatterGatherOptions = sp.GetRequiredService<IOptions<ScatterGatherOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<ShardedQueryExecutor>>();
            return new ShardedQueryExecutor(topology, scatterGatherOptions, logger);
        });

        // Register sharded repository (app-level mode)
        services.TryAddScoped<IFunctionalShardedRepository<TEntity, TId>>(sp =>
        {
            var router = sp.GetRequiredService<IShardRouter<TEntity>>();
            var collectionFactory = sp.GetRequiredService<IShardedMongoCollectionFactory>();
            var queryExecutor = sp.GetRequiredService<IShardedQueryExecutor>();
            var logger = sp.GetRequiredService<ILogger<FunctionalShardedRepositoryMongoDB<TEntity, TId>>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalShardedRepositoryMongoDB<TEntity, TId>(
                router,
                collectionFactory,
                queryExecutor,
                idSelector,
                collectionName,
                logger,
                requestContext,
                timeProvider);
        });

        // Register health monitor (app-level mode)
        services.TryAddSingleton<IShardedDatabaseHealthMonitor>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var topology = sp.GetRequiredService<ShardTopology>();
            var collectionFactory = sp.GetRequiredService<IShardedMongoCollectionFactory>();

            return new ShardedMongoDbDatabaseHealthMonitor(
                mongoClient,
                topology,
                collectionFactory,
                useNativeSharding: false);
        });
    }

    /// <summary>
    /// Adds Encina MongoDB sharded read/write collection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for read/write separation options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaMongoDBSharding</c> has been called first
    /// to register the core sharding services and topology.
    /// </para>
    /// <para>
    /// When used with native <c>mongos</c> sharding, the factory applies MongoDB read preferences
    /// on the default client. When used with application-level sharding, the factory creates
    /// per-shard <see cref="IMongoClient"/> instances with appropriate read preferences.
    /// </para>
    /// <para>
    /// Registers <see cref="IShardedReadWriteMongoCollectionFactory"/> that provides
    /// read/write-separated collections per shard using MongoDB read preferences.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Native mongos sharding with read/write separation
    /// services.AddEncinaMongoDB(options =&gt;
    /// {
    ///     options.ConnectionString = "mongodb://mongos:27017";
    ///     options.DatabaseName = "MyApp";
    /// });
    /// services.AddEncinaMongoDBSharding&lt;Order, Guid&gt;(options =&gt;
    /// {
    ///     options.UseNativeSharding = true;
    ///     options.CollectionName = "orders";
    ///     options.IdProperty = o =&gt; o.Id;
    /// });
    /// services.AddEncinaMongoDBShardedReadWrite(rwOptions =&gt;
    /// {
    ///     rwOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
    ///     rwOptions.ReadConcern = MongoReadConcern.Majority;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaMongoDBShardedReadWrite(
        this IServiceCollection services,
        Action<MongoReadWriteSeparationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var rwOptions = new MongoReadWriteSeparationOptions();
        configure?.Invoke(rwOptions);

        services.TryAddSingleton(rwOptions);

        services.TryAddSingleton<IShardedReadWriteMongoCollectionFactory>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var options = sp.GetRequiredService<MongoReadWriteSeparationOptions>();
            var topology = sp.GetService<ShardTopology>();
            var databaseName = sp.GetService<IOptions<EncinaMongoDbOptions>>()?.Value.DatabaseName
                ?? "Encina";

            return topology is not null
                ? new ShardedReadWriteMongoCollectionFactory(mongoClient, databaseName, topology, options)
                : new ShardedReadWriteMongoCollectionFactory(mongoClient, databaseName, options);
        });

        return services;
    }

    /// <summary>
    /// Registers the MongoDB reference table store factory for shard-based replication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IReferenceTableStoreFactory"/> backed by
    /// <see cref="ReferenceTableStoreFactoryMongoDB"/>. The factory caches
    /// <see cref="IMongoClient"/> instances per connection string and extracts the
    /// database name from the MongoDB connection string.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaMongoDBReferenceTableStore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IReferenceTableStoreFactory, ReferenceTableStoreFactoryMongoDB>();

        return services;
    }
}
