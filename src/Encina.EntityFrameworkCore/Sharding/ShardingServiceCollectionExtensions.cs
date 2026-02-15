using Encina.EntityFrameworkCore.Sharding.ReferenceTables;
using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Data;
using Encina.Sharding.Execution;
using Encina.Sharding.ReferenceTables;
using Encina.Sharding.ReplicaSelection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Sharding;

/// <summary>
/// Extension methods for configuring Encina EF Core sharding services.
/// </summary>
/// <remarks>
/// <para>
/// Each method configures the <see cref="ShardedDbContextFactory{TContext}"/> with the
/// appropriate provider-specific <see cref="DbContextOptionsBuilder{TContext}"/> delegate,
/// and registers a provider-agnostic <see cref="FunctionalShardedRepositoryEF{TContext, TEntity, TId}"/>.
/// </para>
/// <para>
/// All methods require that <c>AddEncinaSharding&lt;TEntity&gt;</c> from <c>Encina.Sharding</c>
/// has been called first to register the core sharding services (topology, router, options).
/// </para>
/// </remarks>
public static class ShardingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina EF Core sharded repository using SQL Server as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services
    /// (topology, router, options).
    /// </para>
    /// <para>
    /// Configures the factory to use <c>UseSqlServer(connectionString)</c> for each shard.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...");
    /// });
    ///
    /// services.AddEncinaEFCoreShardingSqlServer&lt;AppDbContext, Order, Guid&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEFCoreShardingSqlServer<TContext, TEntity, TId>(
        this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class
        where TId : notnull
    {
        return AddEncinaEFCoreShardingCore<TContext, TEntity, TId>(
            services,
            static (builder, connectionString) => builder.UseSqlServer(connectionString));
    }

    /// <summary>
    /// Adds Encina EF Core sharded repository using PostgreSQL as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first.
    /// </para>
    /// <para>
    /// Configures the factory to use <c>UseNpgsql(connectionString)</c> for each shard.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Host=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Host=shard1;Database=Orders;...");
    /// });
    ///
    /// services.AddEncinaEFCoreShardingPostgreSql&lt;AppDbContext, Order, Guid&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEFCoreShardingPostgreSql<TContext, TEntity, TId>(
        this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class
        where TId : notnull
    {
        return AddEncinaEFCoreShardingCore<TContext, TEntity, TId>(
            services,
            static (builder, connectionString) => builder.UseNpgsql(connectionString));
    }

    /// <summary>
    /// Adds Encina EF Core sharded repository using MySQL as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <param name="configureProvider">
    /// Delegate that configures the <see cref="DbContextOptionsBuilder{TContext}"/> with a MySQL provider
    /// and the given connection string. This is required because the Pomelo MySQL provider
    /// needs a <c>ServerVersion</c> parameter that must be supplied by the caller.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first.
    /// </para>
    /// <para>
    /// Unlike the other provider-specific methods, this method requires a custom delegate
    /// because the Pomelo MySQL EF Core provider requires a <c>ServerVersion</c> parameter
    /// that cannot be auto-detected without a live connection. The caller must supply the
    /// appropriate <c>UseMySql</c> configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Server=shard0;Database=Orders;...")
    ///         .AddShard("shard-1", "Server=shard1;Database=Orders;...");
    /// });
    ///
    /// var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
    /// services.AddEncinaEFCoreShardingMySql&lt;AppDbContext, Order, Guid&gt;(
    ///     (builder, cs) =&gt; builder.UseMySql(cs, serverVersion));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEFCoreShardingMySql<TContext, TEntity, TId>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider)
        where TContext : DbContext
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(configureProvider);

        return AddEncinaEFCoreShardingCore<TContext, TEntity, TId>(
            services,
            configureProvider);
    }

    /// <summary>
    /// Adds Encina EF Core sharded repository using SQLite as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type to configure sharding for.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first.
    /// </para>
    /// <para>
    /// Configures the factory to use <c>UseSqlite(connectionString)</c> for each shard.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaSharding&lt;Order&gt;(options =&gt;
    /// {
    ///     options.UseHashRouting()
    ///         .AddShard("shard-0", "Data Source=shard0.db")
    ///         .AddShard("shard-1", "Data Source=shard1.db");
    /// });
    ///
    /// services.AddEncinaEFCoreShardingSqlite&lt;AppDbContext, Order, Guid&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEFCoreShardingSqlite<TContext, TEntity, TId>(
        this IServiceCollection services)
        where TContext : DbContext
        where TEntity : class
        where TId : notnull
    {
        return AddEncinaEFCoreShardingCore<TContext, TEntity, TId>(
            services,
            static (builder, connectionString) => builder.UseSqlite(connectionString));
    }

    /// <summary>
    /// Core registration method that configures the sharded DbContext factory and repository
    /// with a provider-specific options builder delegate.
    /// </summary>
    private static IServiceCollection AddEncinaEFCoreShardingCore<TContext, TEntity, TId>(
        IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider)
        where TContext : DbContext
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureProvider);

        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<ShardedDbContextFactory<TContext>>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var router = sp.GetRequiredService<IShardRouter>();
            return new ShardedDbContextFactory<TContext>(topology, router, sp, configureProvider);
        });

        services.TryAddScoped<IShardedDbContextFactory<TContext>>(sp =>
            sp.GetRequiredService<ShardedDbContextFactory<TContext>>());

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
            var contextFactory = sp.GetRequiredService<IShardedDbContextFactory<TContext>>();
            var queryExecutor = sp.GetRequiredService<IShardedQueryExecutor>();
            var repoLogger = sp.GetRequiredService<ILogger<FunctionalShardedRepositoryEF<TContext, TEntity, TId>>>();

            return new FunctionalShardedRepositoryEF<TContext, TEntity, TId>(
                router,
                contextFactory,
                queryExecutor,
                repoLogger);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina EF Core sharded read/write DbContext factory using SQL Server.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharded read/write options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services.
    /// </para>
    /// <para>
    /// Registers <see cref="IShardedReadWriteDbContextFactory{TContext}"/> that creates
    /// DbContext instances with read/write separation per shard using SQL Server.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreShardedReadWriteSqlServer<TContext>(
        this IServiceCollection services,
        Action<ShardedReadWriteOptions>? configure = null)
        where TContext : DbContext
    {
        return AddEncinaEFCoreShardedReadWriteCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseSqlServer(connectionString),
            configure);
    }

    /// <summary>
    /// Adds Encina EF Core sharded read/write DbContext factory using PostgreSQL.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharded read/write options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services.
    /// </para>
    /// <para>
    /// Registers <see cref="IShardedReadWriteDbContextFactory{TContext}"/> that creates
    /// DbContext instances with read/write separation per shard using PostgreSQL.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreShardedReadWritePostgreSql<TContext>(
        this IServiceCollection services,
        Action<ShardedReadWriteOptions>? configure = null)
        where TContext : DbContext
    {
        return AddEncinaEFCoreShardedReadWriteCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseNpgsql(connectionString),
            configure);
    }

    /// <summary>
    /// Adds Encina EF Core sharded read/write DbContext factory using MySQL.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureProvider">
    /// Delegate that configures the <see cref="DbContextOptionsBuilder{TContext}"/> with a MySQL provider
    /// and the given connection string. Required because the Pomelo MySQL provider needs a
    /// <c>ServerVersion</c> parameter.
    /// </param>
    /// <param name="configure">Optional configuration action for sharded read/write options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services.
    /// </para>
    /// <para>
    /// Registers <see cref="IShardedReadWriteDbContextFactory{TContext}"/> that creates
    /// DbContext instances with read/write separation per shard using MySQL.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreShardedReadWriteMySql<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider,
        Action<ShardedReadWriteOptions>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureProvider);

        return AddEncinaEFCoreShardedReadWriteCore<TContext>(
            services,
            configureProvider,
            configure);
    }

    /// <summary>
    /// Adds Encina EF Core sharded read/write DbContext factory using SQLite.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for sharded read/write options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method requires that <c>AddEncinaSharding&lt;TEntity&gt;</c>
    /// from <c>Encina.Sharding</c> has been called first to register the core sharding services.
    /// </para>
    /// <para>
    /// Registers <see cref="IShardedReadWriteDbContextFactory{TContext}"/> that creates
    /// DbContext instances with read/write separation per shard using SQLite.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreShardedReadWriteSqlite<TContext>(
        this IServiceCollection services,
        Action<ShardedReadWriteOptions>? configure = null)
        where TContext : DbContext
    {
        return AddEncinaEFCoreShardedReadWriteCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseSqlite(connectionString),
            configure);
    }

    /// <summary>
    /// Core registration method that configures the sharded read/write DbContext factory
    /// with a provider-specific options builder delegate.
    /// </summary>
    private static IServiceCollection AddEncinaEFCoreShardedReadWriteCore<TContext>(
        IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider,
        Action<ShardedReadWriteOptions>? configureOptions)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureProvider);

        var options = new ShardedReadWriteOptions();
        configureOptions?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IReplicaHealthTracker>(sp =>
            new ReplicaHealthTracker(options.UnhealthyReplicaRecoveryDelay, sp.GetService<TimeProvider>() ?? TimeProvider.System));

        services.TryAddScoped<ShardedReadWriteDbContextFactory<TContext>>(sp =>
        {
            var topology = sp.GetRequiredService<ShardTopology>();
            var rwOptions = sp.GetRequiredService<ShardedReadWriteOptions>();
            var healthTracker = sp.GetRequiredService<IReplicaHealthTracker>();
            return new ShardedReadWriteDbContextFactory<TContext>(
                topology, rwOptions, healthTracker, sp, configureProvider);
        });

        services.TryAddScoped<IShardedReadWriteDbContextFactory<TContext>>(sp =>
            sp.GetRequiredService<ShardedReadWriteDbContextFactory<TContext>>());

        return services;
    }

    /// <summary>
    /// Registers the EF Core reference table store factory using SQL Server as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IReferenceTableStoreFactory"/> backed by
    /// <see cref="ReferenceTableStoreFactoryEF{TContext}"/> configured with
    /// <c>UseSqlServer(connectionString)</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreReferenceTableStoreSqlServer<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        return AddEncinaEFCoreReferenceTableStoreCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseSqlServer(connectionString));
    }

    /// <summary>
    /// Registers the EF Core reference table store factory using PostgreSQL as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IReferenceTableStoreFactory"/> backed by
    /// <see cref="ReferenceTableStoreFactoryEF{TContext}"/> configured with
    /// <c>UseNpgsql(connectionString)</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreReferenceTableStorePostgreSql<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        return AddEncinaEFCoreReferenceTableStoreCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseNpgsql(connectionString));
    }

    /// <summary>
    /// Registers the EF Core reference table store factory using MySQL as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureProvider">
    /// Delegate that configures the <see cref="DbContextOptionsBuilder{TContext}"/> with a MySQL provider
    /// and the given connection string. Required because the Pomelo MySQL provider needs a
    /// <c>ServerVersion</c> parameter.
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IReferenceTableStoreFactory"/> backed by
    /// <see cref="ReferenceTableStoreFactoryEF{TContext}"/> configured with the supplied
    /// MySQL provider delegate.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreReferenceTableStoreMySql<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureProvider);

        return AddEncinaEFCoreReferenceTableStoreCore<TContext>(
            services,
            configureProvider);
    }

    /// <summary>
    /// Registers the EF Core reference table store factory using SQLite as the database provider.
    /// </summary>
    /// <typeparam name="TContext">The <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IReferenceTableStoreFactory"/> backed by
    /// <see cref="ReferenceTableStoreFactoryEF{TContext}"/> configured with
    /// <c>UseSqlite(connectionString)</c>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaEFCoreReferenceTableStoreSqlite<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        return AddEncinaEFCoreReferenceTableStoreCore<TContext>(
            services,
            static (builder, connectionString) => builder.UseSqlite(connectionString));
    }

    /// <summary>
    /// Core registration method that configures the reference table store factory
    /// with a provider-specific options builder delegate.
    /// </summary>
    private static IServiceCollection AddEncinaEFCoreReferenceTableStoreCore<TContext>(
        IServiceCollection services,
        Action<DbContextOptionsBuilder<TContext>, string> configureProvider)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureProvider);

        services.TryAddSingleton<IReferenceTableStoreFactory>(sp =>
            new ReferenceTableStoreFactoryEF<TContext>(sp, configureProvider));

        return services;
    }
}
