using Encina.Dapper.SqlServer.Repository;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.Dapper.SqlServer.Sharding;

/// <summary>
/// Extension methods for configuring Encina Dapper SQL Server sharding services.
/// </summary>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina sharded repository for a specific entity type using Dapper with SQL Server.
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
    /// An <see cref="IShardedConnectionFactory"/> must also be registered in DI, typically
    /// via the corresponding ADO.NET provider's <c>AddEncinaADOSharding</c> method.
    /// Dapper operates on <see cref="System.Data.IDbConnection"/> and reuses the non-generic
    /// connection factory from the sharding infrastructure.
    /// </para>
    /// <para>
    /// Registers:
    /// <list type="bullet">
    /// <item><see cref="IShardedQueryExecutor"/> for scatter-gather queries</item>
    /// <item><see cref="IFunctionalShardedRepository{TEntity, TId}"/> with SQL Server Dapper implementation</item>
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
    /// // Then, register Dapper sharded repository
    /// services.AddEncinaDapperSharding&lt;Order, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("Orders")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerId, "CustomerId");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDapperSharding<TEntity, TId>(
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
            var connectionFactory = sp.GetRequiredService<IShardedConnectionFactory>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var queryExecutor = sp.GetRequiredService<IShardedQueryExecutor>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalShardedRepositoryDapper<TEntity, TId>(
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
