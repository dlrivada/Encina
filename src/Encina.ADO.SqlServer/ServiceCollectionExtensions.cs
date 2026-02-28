using System.Data;
using Encina.ADO.SqlServer.Auditing;
using Encina.ADO.SqlServer.BulkOperations;
using Encina.ADO.SqlServer.Health;
using Encina.ADO.SqlServer.Inbox;
using Encina.ADO.SqlServer.Modules;
using Encina.ADO.SqlServer.Outbox;
using Encina.ADO.SqlServer.ReadWriteSeparation;
using Encina.ADO.SqlServer.Repository;
using Encina.ADO.SqlServer.Sagas;
using Encina.ADO.SqlServer.Scheduling;
using Encina.ADO.SqlServer.UnitOfWork;
using Encina.Compliance.Consent;
using Encina.Compliance.Anonymization;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.GDPR;
using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Modules.Isolation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.ADO.SqlServer;

/// <summary>
/// Extension methods for configuring Encina with ADO.NET provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a registered IDbConnection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var config = new MessagingConfiguration();
        configure(config);

        return services.AddEncinaADO(config);
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a pre-built configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The pre-built messaging configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    private static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        MessagingConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        services.AddMessagingServices<
            OutboxStoreADO,
            OutboxMessageFactory,
            InboxStoreADO,
            InboxMessageFactory,
            SagaStoreADO,
            SagaStateFactory,
            ScheduledMessageStoreADO,
            ScheduledMessageFactory,
            OutboxProcessor>(config);

        // Register audit log store if enabled
        if (config.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreADO>();
        }

        // Register consent stores if enabled
        if (config.UseConsent)
        {
            services.TryAddScoped<IConsentStore, Consent.ConsentStoreADO>();
            services.TryAddScoped<IConsentAuditStore, Consent.ConsentAuditStoreADO>();
            services.TryAddScoped<IConsentVersionManager, Consent.ConsentVersionManagerADO>();
        }

        // Register DSR (Data Subject Rights) stores if enabled
        if (config.UseDataSubjectRights)
        {
            services.TryAddScoped<IDSRRequestStore, DataSubjectRights.DSRRequestStoreADO>();
            services.TryAddScoped<IDSRAuditStore, DataSubjectRights.DSRAuditStoreADO>();
        }

        // Register Anonymization token mapping store if enabled
        if (config.UseAnonymization)
        {
            services.TryAddScoped<ITokenMappingStore, Anonymization.TokenMappingStoreADO>();
        }

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, SqlServerHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new SqlServerDatabaseHealthMonitor(sp));

        // Register module isolation services if enabled
        if (config.UseModuleIsolation)
        {
            // Register module isolation options
            services.AddSingleton(config.ModuleIsolationOptions);

            // Register core module isolation services (shared with EF Core and Dapper)
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
    /// Adds Encina with ADO.NET messaging patterns support using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When module isolation is enabled via <see cref="MessagingConfiguration.UseModuleIsolation"/>,
    /// connections are wrapped with schema validation to enforce module boundaries.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        return services.AddEncinaADO(
            _ => new SqlConnection(connectionString),
            configure);
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a custom connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create IDbConnection instances.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When module isolation is enabled via <see cref="MessagingConfiguration.UseModuleIsolation"/>,
    /// the connection factory is wrapped with <see cref="ModuleAwareConnectionFactory"/> to validate
    /// SQL statements against module schema boundaries.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
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
            services.TryAddScoped(connectionFactory);
        }

        return services.AddEncinaADO(config);
    }

    /// <summary>
    /// Registers a functional repository for an entity type using ADO.NET.
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
    /// Requires <see cref="IDbConnection"/> to be registered, typically via
    /// <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaADO(connectionString, config => { });
    ///
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("Orders")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerId, "CustomerId")
    ///         .MapProperty(o =&gt; o.Total, "Total")
    ///         .MapProperty(o =&gt; o.CreatedAtUtc, "CreatedAtUtc");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build mapping
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the repository with scoped lifetime, resolving audit dependencies
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryADO<TEntity, TId>(
                connection, mapping, requestContext, timeProvider);
        });

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using ADO.NET.
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
    /// Requires <see cref="IDbConnection"/> to be registered, typically via
    /// <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaReadRepository&lt;OrderSummary, Guid&gt;(mapping =&gt;
    /// {
    ///     mapping.ToTable("OrderSummaries")
    ///         .HasId(o =&gt; o.Id)
    ///         .MapProperty(o =&gt; o.CustomerName, "CustomerName")
    ///         .MapProperty(o =&gt; o.TotalAmount, "TotalAmount");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<EntityMappingBuilder<TEntity, TId>> configure)
        where TEntity : class, new()
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build mapping
        var builder = new EntityMappingBuilder<TEntity, TId>();
        configure(builder);
        var mapping = builder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var connection = sp.GetRequiredService<IDbConnection>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();

            return new FunctionalRepositoryADO<TEntity, TId>(
                connection, mapping, requestContext, timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with ADO.NET for SQL Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IUnitOfWork"/> implemented by <see cref="UnitOfWorkADO"/>
    /// with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires an <see cref="IDbConnection"/> to be registered in the service collection,
    /// typically via <see cref="AddEncinaADO(IServiceCollection, string, Action{MessagingConfiguration})"/>.
    /// </para>
    /// <para>
    /// Entity mappings must be registered separately using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{EntityMappingBuilder{TEntity, TId}})"/>
    /// for each entity type used with the Unit of Work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaADO(connectionString, config => { });
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
        services.TryAddScoped<IUnitOfWork, UnitOfWorkADO>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using ADO.NET.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsADO{TEntity, TId}"/> with scoped lifetime.
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
    /// services.AddEncinaADO(connectionString, config => { });
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

        services.TryAddScoped<IBulkOperations<TEntity>, BulkOperationsADO<TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// Adds persistent GDPR lawful basis stores using ADO.NET for SQL Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="ILawfulBasisRegistry"/> and <see cref="ILIAStore"/> with singleton
    /// lifetime, creating a new <see cref="SqlConnection"/> per operation for thread safety.
    /// </para>
    /// <para>
    /// These registrations override the in-memory defaults from <c>AddEncinaLawfulBasis()</c>.
    /// Call this method <b>before</b> <c>AddEncinaLawfulBasis()</c> so that <c>TryAddSingleton</c>
    /// in the core registration finds the persistent implementations already registered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaLawfulBasisADOSqlServer(connectionString);
    /// services.AddEncinaLawfulBasis(options =&gt;
    /// {
    ///     options.AutoRegisterFromAttributes = true;
    ///     options.ScanAssemblyContaining&lt;Program&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaLawfulBasisADOSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<ILawfulBasisRegistry>(
            new LawfulBasis.LawfulBasisRegistryADO(connectionString));
        services.TryAddSingleton<ILIAStore>(
            new LawfulBasis.LIAStoreADO(connectionString));

        return services;
    }

    /// <summary>
    /// Adds persistent GDPR processing activity registry using ADO.NET for SQL Server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IProcessingActivityRegistry"/> as a singleton backed by a SQL Server
    /// <c>[ProcessingActivities]</c> table. Each operation creates a new connection, ensuring thread safety.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaProcessingActivityADOSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IProcessingActivityRegistry>(
            new ProcessingActivity.ProcessingActivityRegistryADO(connectionString));

        return services;
    }
}
