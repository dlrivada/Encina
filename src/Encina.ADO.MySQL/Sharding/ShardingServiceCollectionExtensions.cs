using Encina.ADO.MySQL.Repository;
using Encina.ADO.MySQL.Sharding.ReferenceTables;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Encina.ADO.MySQL.Sharding;

/// <summary>
/// Extension methods for configuring Encina ADO.NET MySQL sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharded repository for a specific entity type using ADO.NET with MySQL.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureMapping">Configuration action for entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services
    /// (topology, router, options).
    /// </para>
    /// <para>
    /// Registers:
    /// <list type="bullet">
    /// <item><see cref="IShardedConnectionFactory"/> and <see cref="IShardedConnectionFactory{TConnection}"/>
    /// for <see cref="MySqlConnection"/></item>
    /// <item><see cref="IShardedQueryExecutor"/> for scatter-gather queries</item>
    /// <item><see cref="IFunctionalShardedRepository{TEntity, TId}"/> with MySQL ADO.NET implementation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First, register core sharding
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...");
    /// });
    ///
    /// // Then, register ADO.NET sharded repository
    /// services.AddEncinaADOSharding&lt;Order, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("Orders")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerId, "CustomerId");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaADOSharding<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configureMapping)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureMapping);

        var builder = new EntityMappingBuilder<TEntity, TId>();
        configureMapping(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        services.TryAddSingleton<IEntityMapping<TEntity, TId>>(mapping);
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<ShardedConnectionFactory>();
        services.TryAddScoped<IShardedConnectionFactory>(sp => sp.GetRequiredService<ShardedConnectionFactory>());
        services.TryAddScoped<IShardedConnectionFactory<MySqlConnection>>(sp => sp.GetRequiredService<ShardedConnectionFactory>());

        services.TryAddScoped<IShardedQueryExecutor>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScatterGatherOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<ShardedQueryExecutor>>();
            return new ShardedQueryExecutor(topology, options, logger);
        });

        services.TryAddScoped<IFunctionalShardedRepository<TEntity, TId>>(sp =>
        {
            var router = sp.GetRequiredService<IShardRouter<TEntity>>();
            var connectionFactory = sp.GetRequiredService<IShardedConnectionFactory<MySqlConnection>>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var queryExecutor = sp.GetRequiredService<IShardedQueryExecutor>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();
            var logger = sp.GetRequiredService<ILogger<FunctionalShardedRepositoryADO<TEntity, TId>>>();

            return new FunctionalShardedRepositoryADO<TEntity, TId>(
                router,
                connectionFactory,
                entityMapping,
                queryExecutor,
                logger,
                requestContext,
                timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina sharded read/write connection factory using ADO.NET with MySQL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharded read/write options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services
    /// (topology, router, options).
    /// </para>
    /// <para>
    /// Registers:
    /// <list type="bullet">
    /// <item><see cref="IShardedReadWriteConnectionFactory"/> and
    /// <see cref="IShardedReadWriteConnectionFactory{TConnection}"/> for <see cref="MySqlConnection"/></item>
    /// <item><see cref="ShardedReadWriteOptions"/> (singleton)</item>
    /// <item><see cref="IReplicaHealthTracker"/> (singleton)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaADOShardedReadWrite(
        this IServiceCollection services,
        Action<ShardedReadWriteOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ShardedReadWriteOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IReplicaHealthTracker>(sp =>
            new ReplicaHealthTracker(options.UnhealthyReplicaRecoveryDelay, sp.GetService<TimeProvider>() ?? TimeProvider.System));

        services.TryAddScoped<ShardedReadWriteConnectionFactory>();
        services.TryAddScoped<IShardedReadWriteConnectionFactory>(sp =>
            sp.GetRequiredService<ShardedReadWriteConnectionFactory>());
        services.TryAddScoped<IShardedReadWriteConnectionFactory<MySqlConnection>>(sp =>
            sp.GetRequiredService<ShardedReadWriteConnectionFactory>());

        return services;
    }

    /// <summary>
    /// Registers the ADO.NET MySQL reference table store factory for shard-based replication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADOReferenceTableStore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IReferenceTableStoreFactory, ReferenceTableStoreFactoryADO>();

        return services;
    }
}
