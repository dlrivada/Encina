using System.Data;
using Encina.Dapper.SqlServer.Auditing;
using Encina.Dapper.SqlServer.BulkOperations;
using Encina.Dapper.SqlServer.Health;
using Encina.Dapper.SqlServer.Inbox;
using Encina.Dapper.SqlServer.Modules;
using Encina.Dapper.SqlServer.Outbox;
using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Dapper.SqlServer.Repository;
using Encina.Dapper.SqlServer.Sagas;
using Encina.Dapper.SqlServer.Scheduling;
using Encina.Dapper.SqlServer.UnitOfWork;
using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Modules.Isolation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Dapper.SqlServer;

/// <summary>
/// Extension methods for configuring Encina with Dapper for SQL Server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence for SQL Server.
    /// All patterns are opt-in via configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MessagingConfiguration();
        configure(config);

        return services.AddEncinaDapper(config);
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence for SQL Server using a pre-built configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The pre-built messaging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        MessagingConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddMessagingServices<
            OutboxStoreDapper,
            OutboxMessageFactory,
            InboxStoreDapper,
            InboxMessageFactory,
            SagaStoreDapper,
            SagaStateFactory,
            ScheduledMessageStoreDapper,
            ScheduledMessageFactory,
            OutboxProcessor>(config);

        // Register audit log store if enabled
        if (config.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreDapper>();
        }

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, SqlServerHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        // Dapper shares the same underlying connection pool as ADO.NET (Microsoft.Data.SqlClient)
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new DapperSqlServerDatabaseHealthMonitor(sp));

        // Register module isolation services if enabled
        if (config.UseModuleIsolation)
        {
            // Register module isolation options
            services.AddSingleton(config.ModuleIsolationOptions);

            // Register core module isolation services (shared with EF Core)
            services.TryAddSingleton<IModuleSchemaRegistry, ModuleSchemaRegistry>();
            services.TryAddScoped<IModuleExecutionContext, ModuleExecutionContext>();

            // Register appropriate permission script generator based on configuration
            // (Users can override this with their own generator if needed)
            services.TryAddSingleton<IModulePermissionScriptGenerator, SqlServerPermissionScriptGenerator>();
        }

        // Register read/write separation services if enabled
        if (config.UseReadWriteSeparation)
        {
            // Register the options
            services.AddSingleton(config.ReadWriteSeparationOptions);

            // Register replica selector based on strategy if replicas are configured
            if (config.ReadWriteSeparationOptions.ReadConnectionStrings.Count > 0)
            {
                var replicaSelector = ReplicaSelectorFactory.Create(config.ReadWriteSeparationOptions);
                services.AddSingleton<IReplicaSelector>(replicaSelector);

                // Register connection selector with replica support
                services.AddSingleton<IReadWriteConnectionSelector>(sp =>
                    new ReadWriteConnectionSelector(
                        config.ReadWriteSeparationOptions,
                        sp.GetRequiredService<IReplicaSelector>()));
            }
            else
            {
                // Register connection selector without replicas (falls back to primary)
                services.AddSingleton<IReadWriteConnectionSelector>(
                    new ReadWriteConnectionSelector(
                        config.ReadWriteSeparationOptions,
                        replicaSelector: null));
            }

            // Register the connection factory
            services.AddScoped<IReadWriteConnectionFactory, ReadWriteConnectionFactory>();

            // Register the pipeline behavior for automatic routing
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadWriteRoutingPipelineBehavior<,>));

            // Register the health check
            services.AddSingleton<IEncinaHealthCheck, ReadWriteSeparationHealthCheck>();
        }

        return services;
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When module isolation is enabled via <see cref="MessagingConfiguration.UseModuleIsolation"/>,
    /// the connection factory is wrapped with <see cref="ModuleAwareConnectionFactory"/> to validate
    /// SQL statements against module schema boundaries.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(configure);

        // Build config first to check if module isolation is enabled
        var config = new MessagingConfiguration();
        configure(config);

        if (config.UseModuleIsolation)
        {
            // Register the ModuleAwareConnectionFactory that wraps connections with validation
            services.AddScoped(sp =>
            {
                // Create a factory function that doesn't depend on IServiceProvider
                // to avoid circular dependencies
                Func<IDbConnection> innerFactory = () => connectionFactory(sp);

                return new ModuleAwareConnectionFactory(
                    innerFactory,
                    sp.GetRequiredService<IModuleExecutionContext>(),
                    sp.GetRequiredService<IModuleSchemaRegistry>(),
                    sp.GetRequiredService<ModuleIsolationOptions>());
            });

            // Register IDbConnection that uses the ModuleAwareConnectionFactory
            services.AddScoped<IDbConnection>(sp =>
                sp.GetRequiredService<ModuleAwareConnectionFactory>().CreateConnection());
        }
        else
        {
            // Standard registration without module isolation
            services.AddScoped(connectionFactory);
        }

        return services.AddEncinaDapper(config);
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection string.
    /// Creates SQL Server connections by default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When module isolation is enabled via <see cref="MessagingConfiguration.UseModuleIsolation"/>,
    /// connections are wrapped with schema validation to enforce module boundaries.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddEncinaDapper(
            _ => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            configure);
    }

    /// <summary>
    /// Registers a functional repository for an entity type using Dapper.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/> and
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection.
    /// </para>
    /// <para>
    /// When <see cref="IRequestContext"/> and/or <see cref="TimeProvider"/> are registered,
    /// the repository will automatically populate audit fields (<see cref="ICreatedAtUtc"/>,
    /// <see cref="ICreatedBy"/>, <see cref="IModifiedAtUtc"/>, <see cref="IModifiedBy"/>)
    /// on entities implementing these interfaces.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =>
    ///     mapping.ToTable("Orders")
    ///            .HasId(o => o.Id)
    ///            .MapProperty(o => o.CustomerId)
    ///            .MapProperty(o => o.Total)
    ///            .ExcludeFromInsert(o => o.Id));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build();

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Ensure TimeProvider is registered (default to System if not present)
        services.TryAddSingleton(TimeProvider.System);

        // Register the repository with scoped lifetime, injecting audit dependencies
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryDapper<TEntity, TId>(
                connection,
                entityMapping,
                requestContext,
                timeProvider);
        });
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using Dapper.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Only registers <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this for read-only scenarios where write operations are not needed.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaReadRepository&lt;OrderSummary, Guid&gt;(mapping =>
    ///     mapping.ToTable("vw_OrderSummaries")
    ///            .HasId(o => o.Id)
    ///            .MapProperty(o => o.CustomerName)
    ///            .MapProperty(o => o.TotalAmount));
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build the mapping once at registration time
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build();

        // Register the mapping as singleton (immutable)
        services.AddSingleton<IEntityMapping<TEntity, TId>>(mapping);

        // Ensure TimeProvider is registered (default to System if not present)
        services.TryAddSingleton(TimeProvider.System);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var entityMapping = sp.GetRequiredService<IEntityMapping<TEntity, TId>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryDapper<TEntity, TId>(
                connection,
                entityMapping,
                requestContext,
                timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with Dapper for SQL Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IUnitOfWork"/> implemented by <see cref="UnitOfWorkDapper"/>
    /// with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection,
    /// typically via <see cref="AddEncinaDapper(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// <para>
    /// Entity mappings must be registered separately using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// for each entity type used with the Unit of Work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDapper(connectionString, config => { });
    ///
    /// // Register entity mappings
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("Orders")
    ///            .HasId(o =&gt; o.Id)
    ///            .MapProperty(o =&gt; o.CustomerId));
    ///
    /// // Add Unit of Work
    /// services.AddEncinaUnitOfWork();
    ///
    /// // Usage in handler
    /// public class TransferHandler(IUnitOfWork unitOfWork)
    /// {
    ///     public async Task HandleAsync(TransferCommand cmd, CancellationToken ct)
    ///     {
    ///         await unitOfWork.BeginTransactionAsync(ct);
    ///         var accounts = unitOfWork.Repository&lt;Account, Guid&gt;();
    ///         // ... operations ...
    ///         await unitOfWork.CommitAsync(ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaUnitOfWork(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Only register if not already registered
        services.TryAddScoped<IUnitOfWork, UnitOfWorkDapper>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using Dapper.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsDapper{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="IDbConnection"/> must be registered in DI</description></item>
    /// <item><description><see cref="IEntityMapping{TEntity, TId}"/> must be registered for the entity</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Typically, you register the entity mapping using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// which automatically registers both the repository and the entity mapping.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register connection and repository
    /// services.AddEncinaDapper(connectionString, config => { });
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    ///     mapping.ToTable("Orders")
    ///            .HasId(o =&gt; o.Id)
    ///            .MapProperty(o =&gt; o.CustomerId)
    ///            .MapProperty(o =&gt; o.Total));
    ///
    /// // Register bulk operations
    /// services.AddEncinaBulkOperations&lt;Order, Guid&gt;();
    ///
    /// // Usage in service
    /// public class OrderBatchService(IBulkOperations&lt;Order&gt; bulkOps)
    /// {
    ///     public async Task&lt;Either&lt;EncinaError, int&gt;&gt; ImportOrdersAsync(
    ///         IEnumerable&lt;Order&gt; orders,
    ///         CancellationToken ct)
    ///     {
    ///         var config = BulkConfig.Default with { BatchSize = 5000 };
    ///         return await bulkOps.BulkInsertAsync(orders, config, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaBulkOperations<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IBulkOperations<TEntity>, BulkOperationsDapper<TEntity, TId>>();

        return services;
    }
}
