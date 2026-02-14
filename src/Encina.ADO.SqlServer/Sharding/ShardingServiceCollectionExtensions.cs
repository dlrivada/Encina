using Encina.ADO.SqlServer.Repository;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Encina.Sharding.ReplicaSelection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.ADO.SqlServer.Sharding;

/// <summary>
/// Extension methods for configuring Encina ADO.NET SQL Server sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharded repository for a specific entity type using ADO.NET with SQL Server.
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
    /// for <see cref="SqlConnection"/></item>
    /// <item><see cref="IShardedQueryExecutor"/> for scatter-gather queries</item>
    /// <item><see cref="IFunctionalShardedRepository{TEntity, TId}"/> with SQL Server ADO.NET implementation</item>
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

        // Build entity mapping
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configureMapping(builder);
        var mapping = builder.Build();

        // Register entity mapping
        services.TryAddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the sharded connection factory (both interfaces)
        services.TryAddScoped<ShardedConnectionFactory>();
        services.TryAddScoped<IShardedConnectionFactory>(sp => sp.GetRequiredService<ShardedConnectionFactory>());
        services.TryAddScoped<IShardedConnectionFactory<SqlConnection>>(sp => sp.GetRequiredService<ShardedConnectionFactory>());

        // Register the scatter-gather query executor
        services.TryAddScoped<IShardedQueryExecutor>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScatterGatherOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<ShardedQueryExecutor>>();
            return new ShardedQueryExecutor(topology, options, logger);
        });

        // Register the sharded repository
        services.TryAddScoped<IFunctionalShardedRepository<TEntity, TId>>(sp =>
        {
            var router = sp.GetRequiredService<IShardRouter<TEntity>>();
            var connectionFactory = sp.GetRequiredService<IShardedConnectionFactory<SqlConnection>>();
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
    /// Adds Encina sharded read/write connection factory using ADO.NET with SQL Server.
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
    /// <see cref="IShardedReadWriteConnectionFactory{TConnection}"/> for <see cref="SqlConnection"/></item>
    /// <item><see cref="ShardedReadWriteOptions"/> (singleton)</item>
    /// <item><see cref="IReplicaHealthTracker"/> (singleton)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...",
    ///             replicas: ["Server=shard0-r1;Database=Orders;..."])
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...",
    ///             replicas: ["Server=shard1-r1;Database=Orders;..."]);
    /// });
    ///
    /// services.AddEncinaADOShardedReadWrite(options =&gt;
    /// {
    ///     options.FallbackToPrimaryWhenNoReplicas = true;
    /// });
    /// </code>
    /// </example>
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
        services.TryAddScoped<IShardedReadWriteConnectionFactory<SqlConnection>>(sp =>
            sp.GetRequiredService<ShardedReadWriteConnectionFactory>());

        return services;
    }
}
