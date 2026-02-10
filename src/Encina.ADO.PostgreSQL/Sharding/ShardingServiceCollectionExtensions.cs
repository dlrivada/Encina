using Encina.ADO.PostgreSQL.Repository;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Encina.ADO.PostgreSQL.Sharding;

/// <summary>
/// Extension methods for configuring Encina ADO.NET PostgreSQL sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharded repository for a specific entity type using ADO.NET with PostgreSQL.
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
    /// for <see cref="NpgsqlConnection"/></item>
    /// <item><see cref="IShardedQueryExecutor"/> for scatter-gather queries</item>
    /// <item><see cref="IFunctionalShardedRepository{TEntity, TId}"/> with PostgreSQL ADO.NET implementation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First, register core sharding
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Host=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Host=shard1;Database=Orders;...");
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
        var mapping = builder.Build();

        services.TryAddSingleton<IEntityMapping<TEntity, TId>>(mapping);
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<ShardedConnectionFactory>();
        services.TryAddScoped<IShardedConnectionFactory>(sp => sp.GetRequiredService<ShardedConnectionFactory>());
        services.TryAddScoped<IShardedConnectionFactory<NpgsqlConnection>>(sp => sp.GetRequiredService<ShardedConnectionFactory>());

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
            var connectionFactory = sp.GetRequiredService<IShardedConnectionFactory<NpgsqlConnection>>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var queryExecutor = sp.GetRequiredService<IShardedQueryExecutor>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalShardedRepositoryADO<TEntity, TId>(
                router,
                connectionFactory,
                entityMapping,
                queryExecutor,
                requestContext,
                timeProvider);
        });

        return services;
    }
}
