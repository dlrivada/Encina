using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.EntityFrameworkCore.Auditing;
using Encina.EntityFrameworkCore.BulkOperations;
using Encina.EntityFrameworkCore.DomainEvents;
using Encina.EntityFrameworkCore.Health;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Modules;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.ReadWriteSeparation;
using Encina.EntityFrameworkCore.Repository;
using Encina.EntityFrameworkCore.Resilience;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.EntityFrameworkCore.SoftDelete;
using Encina.EntityFrameworkCore.Temporal;
using Encina.EntityFrameworkCore.Tenancy;
using Encina.EntityFrameworkCore.UnitOfWork;
using Encina.Messaging;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Modules.Isolation;
using Encina.Security.Audit;
using Encina.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Encina.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring Encina Entity Framework Core integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core messaging patterns support to Encina with opt-in configuration.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for enabling messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method provides a unified configuration for all messaging patterns.
    /// All patterns are opt-in (disabled by default). Enable only what you need:
    /// </para>
    /// <para>
    /// <b>Available Patterns</b>:
    /// <list type="bullet">
    /// <item><description><b>Transactions</b>: Automatic database transaction management</description></item>
    /// <item><description><b>Outbox</b>: Reliable event publishing (at-least-once delivery)</description></item>
    /// <item><description><b>Inbox</b>: Idempotent message processing (exactly-once semantics)</description></item>
    /// <item><description><b>Sagas</b>: Distributed transactions with compensation</description></item>
    /// <item><description><b>Scheduling</b>: Delayed/recurring command execution</description></item>
    /// <item><description><b>Tenancy</b>: Multi-tenant data isolation and automatic tenant assignment</description></item>
    /// <item><description><b>ReadWriteSeparation</b>: Route queries to read replicas and commands to primary</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><typeparamref name="TDbContext"/> must be registered in DI</description></item>
    /// <item><description>Encina must be configured first</description></item>
    /// <item><description>For each enabled pattern, the corresponding DbSet and configuration must be added to your DbContext</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple app - only transactions
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTransactions = true;
    /// });
    ///
    /// // Multi-tenant app with tenancy support
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTransactions = true;
    ///     config.UseTenancy = true;
    ///
    ///     // Configure tenancy-specific options
    ///     config.TenancyOptions.AutoAssignTenantId = true;
    ///     config.TenancyOptions.ValidateTenantOnSave = true;
    /// });
    ///
    /// // Complex distributed system - all patterns
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTransactions = true;
    ///     config.UseOutbox = true;
    ///     config.UseInbox = true;
    ///     config.UseSagas = true;
    ///     config.UseScheduling = true;
    ///     config.UseTenancy = true;
    ///
    ///     // Configure pattern-specific options
    ///     config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(30);
    ///     config.InboxOptions.MaxRetries = 5;
    /// });
    ///
    /// // Read/Write separation - route queries to replicas
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseReadWriteSeparation = true;
    ///     config.ReadWriteSeparationOptions.WriteConnectionString = "Server=primary;...";
    ///     config.ReadWriteSeparationOptions.ReadConnectionStrings.AddRange(new[]
    ///     {
    ///         "Server=replica1;...",
    ///         "Server=replica2;..."
    ///     });
    ///     config.ReadWriteSeparationOptions.ReplicaStrategy = ReplicaStrategy.RoundRobin;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
        where TDbContext : DbContext
    {
        var config = new MessagingConfiguration();
        configure(config);

        // Register the DbContext as DbContext (non-generic) for behaviors
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

        // Register enabled patterns
        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }

        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.AddScoped<IOutboxStore, OutboxStoreEF>();
            services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(Messaging.Outbox.OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, InboxStoreEF>();
            services.AddScoped<IInboxMessageFactory, InboxMessageFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Messaging.Inbox.InboxPipelineBehavior<,>));
        }

        if (config.UseSagas)
        {
            services.AddSingleton(config.SagaOptions);
            services.AddScoped<ISagaStore, SagaStoreEF>();
            services.AddScoped<ISagaStateFactory, SagaStateFactory>();
            services.AddScoped<SagaOrchestrator>();
        }

        if (config.UseScheduling)
        {
            services.AddSingleton(config.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreEF>();
            services.AddScoped<IScheduledMessageFactory, ScheduledMessageFactory>();
            services.AddScoped<SchedulerOrchestrator>();
        }

        if (config.UseTenancy)
        {
            // Register EF Core tenancy options based on messaging configuration
            var efCoreTenancyOptions = new EfCoreTenancyOptions
            {
                AutoAssignTenantId = config.TenancyOptions.AutoAssignTenantId,
                ValidateTenantOnSave = config.TenancyOptions.ValidateTenantOnSave,
                UseQueryFilters = config.TenancyOptions.UseQueryFilters,
                ThrowOnMissingTenantContext = config.TenancyOptions.ThrowOnMissingTenantContext
            };
            services.AddSingleton(Options.Create(efCoreTenancyOptions));

            // Register core tenancy options if not already registered
            // These are needed by DefaultTenantSchemaConfigurator
            services.TryAddSingleton(Options.Create(new TenancyOptions()));

            // Register default schema configurator if not already registered
            services.TryAddScoped<ITenantSchemaConfigurator, DefaultTenantSchemaConfigurator>();

            // Register TenantDbContextFactory for database-per-tenant scenarios
            services.TryAddScoped<TenantDbContextFactory<TDbContext>>();
        }

        if (config.UseModuleIsolation)
        {
            // Register module isolation options
            services.AddSingleton(config.ModuleIsolationOptions);

            // Register core module isolation services
            services.TryAddSingleton<IModuleSchemaRegistry, ModuleSchemaRegistry>();
            services.TryAddScoped<IModuleExecutionContext, ModuleExecutionContext>();

            // Register the pipeline behavior that sets module context
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ModuleExecutionContextBehavior<,>));

            // Register the interceptor for SQL validation
            services.AddScoped<ModuleSchemaValidationInterceptor>();

            // Register appropriate permission script generator based on configuration
            // (Users can override this with their own generator if needed)
            services.TryAddSingleton<IModulePermissionScriptGenerator, SqlServerPermissionScriptGenerator>();
        }

        if (config.UseReadWriteSeparation)
        {
            // Register read/write separation options
            services.AddSingleton(config.ReadWriteSeparationOptions);

            // Create and register the replica selector only if replicas are configured
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
                // Register connection selector without replica support (uses primary for all operations)
                services.AddSingleton<IReadWriteConnectionSelector>(
                    new ReadWriteConnectionSelector(config.ReadWriteSeparationOptions, replicaSelector: null));
            }

            // Register DbContext factory for read/write routing
            services.AddScoped<IReadWriteDbContextFactory<TDbContext>, ReadWriteDbContextFactory<TDbContext>>();

            // Register the pipeline behavior for automatic routing
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadWriteRoutingPipelineBehavior<,>));

            // Register health check for read/write separation
            services.AddSingleton<IEncinaHealthCheck, ReadWriteSeparationHealthCheck>();
        }

        if (config.UseDomainEvents)
        {
            // Register domain event dispatcher options
            var dispatcherOptions = new DomainEventDispatcherOptions
            {
                Enabled = config.DomainEventsOptions.Enabled,
                StopOnFirstError = config.DomainEventsOptions.StopOnFirstError,
                RequireINotification = config.DomainEventsOptions.RequireINotification,
                ClearEventsAfterDispatch = config.DomainEventsOptions.ClearEventsAfterDispatch
            };
            services.TryAddSingleton(dispatcherOptions);

            // Register the interceptor as singleton
            services.TryAddSingleton<DomainEventDispatcherInterceptor>();
        }

        if (config.UseAuditing)
        {
            // Register audit interceptor options
            var auditOptions = new AuditInterceptorOptions
            {
                Enabled = true,
                TrackCreatedAt = config.AuditingOptions.TrackCreatedAt,
                TrackCreatedBy = config.AuditingOptions.TrackCreatedBy,
                TrackModifiedAt = config.AuditingOptions.TrackModifiedAt,
                TrackModifiedBy = config.AuditingOptions.TrackModifiedBy,
                LogAuditChanges = config.AuditingOptions.LogAuditChanges,
                LogChangesToStore = config.AuditingOptions.LogChangesToStore
            };
            services.TryAddSingleton(auditOptions);

            // Register TimeProvider for consistent timestamps
            services.TryAddSingleton(TimeProvider.System);

            // Register the interceptor as singleton
            services.TryAddSingleton<AuditInterceptor>();
        }

        if (config.UseAuditLogStore)
        {
            // Register persistent audit log store
            services.AddScoped<IAuditLogStore, AuditLogStoreEF>();
        }

        if (config.UseSoftDelete)
        {
            // Register soft delete interceptor options
            var softDeleteOptions = new SoftDeleteInterceptorOptions
            {
                Enabled = true,
                TrackDeletedAt = config.SoftDeleteOptions.TrackDeletedAt,
                TrackDeletedBy = config.SoftDeleteOptions.TrackDeletedBy,
                LogSoftDeletes = config.SoftDeleteOptions.LogSoftDeletes
            };
            services.TryAddSingleton(softDeleteOptions);

            // Register TimeProvider for consistent timestamps (if not already registered by UseAuditing)
            services.TryAddSingleton(TimeProvider.System);

            // Register the interceptor as singleton
            services.TryAddSingleton<SoftDeleteInterceptor>();
        }

        if (config.UseSecurityAuditStore)
        {
            // Register security audit trail store (Encina.Security.Audit)
            services.AddScoped<IAuditStore, AuditStoreEF>();
        }

        if (config.UseTemporalTables)
        {
            // Register temporal table options for point-in-time queries
            services.TryAddSingleton(config.TemporalTableOptions);
        }

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, EntityFrameworkCoreHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        // EF Core shares the same underlying connection pool as the ADO.NET driver
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new EfCoreDatabaseHealthMonitor(sp));

        // Register connection pool monitoring interceptor
        services.TryAddSingleton<ConnectionPoolMonitoringInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core messaging patterns support with default configuration (no patterns enabled).
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload registers the DbContext mapping but doesn't enable any patterns.
    /// Use the overload with configuration action to enable patterns.
    /// </remarks>
    public static IServiceCollection AddEncinaEntityFrameworkCore<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Just register DbContext mapping, no patterns enabled
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        return services;
    }

    /// <summary>
    /// Adds a functional repository for an entity type with Railway Oriented Programming support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers both <see cref="IFunctionalRepository{TEntity, TId}"/> and
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// The repository uses <see cref="DbContext"/> which must be registered separately.
    /// Typically, you would call <see cref="AddEncinaEntityFrameworkCore{TDbContext}(IServiceCollection)"/>
    /// first to register the DbContext mapping.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register DbContext and repository
    /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;();
    /// services.AddEncinaRepository&lt;Order, OrderId&gt;();
    ///
    /// // Use in a service
    /// public class OrderService(IFunctionalRepository&lt;Order, OrderId&gt; repository)
    /// {
    ///     public Task&lt;Either&lt;EncinaError, Order&gt;&gt; GetOrderAsync(OrderId id, CancellationToken ct)
    ///         =&gt; repository.GetByIdAsync(id, ct);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class
        where TId : notnull
    {
        // Register the concrete implementation
        services.TryAddScoped<FunctionalRepositoryEF<TEntity, TId>>();

        // Register both interfaces pointing to the same implementation
        services.TryAddScoped<IFunctionalRepository<TEntity, TId>>(
            sp => sp.GetRequiredService<FunctionalRepositoryEF<TEntity, TId>>());

        services.TryAddScoped<IFunctionalReadRepository<TEntity, TId>>(
            sp => sp.GetRequiredService<FunctionalRepositoryEF<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Adds a read-only functional repository for an entity type with Railway Oriented Programming support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers only <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this when you only need read operations for CQRS query handlers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register read-only repository for query handlers
    /// services.AddEncinaReadRepository&lt;Order, OrderId&gt;();
    ///
    /// // Use in a query handler
    /// public class GetOrderHandler(IFunctionalReadRepository&lt;Order, OrderId&gt; repository)
    /// {
    ///     public Task&lt;Either&lt;EncinaError, OrderDto&gt;&gt; HandleAsync(GetOrderQuery query, CancellationToken ct)
    ///         =&gt; repository.GetByIdAsync(query.Id, ct).Map(o =&gt; new OrderDto(o));
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class
        where TId : notnull
    {
        services.TryAddScoped<IFunctionalReadRepository<TEntity, TId>, FunctionalRepositoryEF<TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// Adds Unit of Work pattern support for Entity Framework Core.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <b>This is completely optional.</b> Most applications work perfectly fine using
    /// <c>DbContext</c> directly. The <c>DbContext</c> is already a Unit of Work.
    /// </para>
    /// <para>
    /// Consider using this when you need:
    /// <list type="bullet">
    /// <item><description>Explicit transaction control with Railway Oriented Programming</description></item>
    /// <item><description>Coordination between different repository instances</description></item>
    /// <item><description>Clear separation of read and write operations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><typeparamref name="TDbContext"/> must be registered in DI</description></item>
    /// <item><description>Call <see cref="AddEncinaEntityFrameworkCore{TDbContext}(IServiceCollection)"/> first</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Registration
    /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;();
    /// services.AddEncinaUnitOfWork&lt;AppDbContext&gt;();
    ///
    /// // Usage in a handler
    /// public class TransferHandler(IUnitOfWork unitOfWork)
    /// {
    ///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
    ///     {
    ///         var accounts = unitOfWork.Repository&lt;Account, AccountId&gt;();
    ///
    ///         var beginResult = await unitOfWork.BeginTransactionAsync(ct);
    ///         if (beginResult.IsLeft) return beginResult;
    ///
    ///         // ... perform operations
    ///
    ///         var saveResult = await unitOfWork.SaveChangesAsync(ct);
    ///         if (saveResult.IsLeft)
    ///         {
    ///             await unitOfWork.RollbackAsync(ct);
    ///             return saveResult.Map(_ =&gt; Unit.Default);
    ///         }
    ///
    ///         return await unitOfWork.CommitAsync(ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaUnitOfWork<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Ensure DbContext is registered as DbContext (non-generic)
        services.TryAddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

        // Register UnitOfWork with scoped lifetime
        services.TryAddScoped<IUnitOfWork, UnitOfWorkEF>();

        return services;
    }

    /// <summary>
    /// Adds a soft delete repository for an entity type with soft delete support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type that implements <see cref="ISoftDeletable"/>.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="ISoftDeleteRepository{TEntity, TId}"/> with scoped lifetime.
    /// The repository provides operations for querying soft-deleted entities, restoring them,
    /// and performing hard deletes.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="DbContext"/> must be registered in DI</description></item>
    /// <item><description><typeparamref name="TEntity"/> must implement <see cref="ISoftDeletable"/></description></item>
    /// <item><description>For restore operations, entity should implement <see cref="ISoftDeletableEntity"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register DbContext and soft delete repository
    /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseSoftDelete = true;
    /// });
    /// services.AddEncinaSoftDeleteRepository&lt;Order, OrderId&gt;();
    ///
    /// // Use in a service
    /// public class OrderService(ISoftDeleteRepository&lt;Order, OrderId&gt; repository)
    /// {
    ///     public Task&lt;Either&lt;RepositoryError, Order&gt;&gt; RestoreOrderAsync(OrderId id, CancellationToken ct)
    ///         =&gt; repository.RestoreAsync(id, ct);
    ///
    ///     public Task&lt;Either&lt;RepositoryError, Unit&gt;&gt; PermanentDeleteAsync(OrderId id, CancellationToken ct)
    ///         =&gt; repository.HardDeleteAsync(id, ct);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSoftDeleteRepository<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class, IEntity<TId>, ISoftDeletable
        where TId : notnull
    {
        services.TryAddScoped<ISoftDeleteRepository<TEntity, TId>, SoftDeleteRepositoryEF<TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// Adds a temporal repository for an entity type with SQL Server temporal table support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="ITemporalRepository{TEntity, TId}"/> with scoped lifetime.
    /// The repository provides point-in-time query capabilities for entities stored in
    /// SQL Server temporal tables (system-versioned tables).
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="DbContext"/> must be registered in DI</description></item>
    /// <item><description>SQL Server 2016 or later</description></item>
    /// <item><description>Entity must be configured as temporal using <c>ConfigureTemporalTable</c></description></item>
    /// <item><description><see cref="MessagingConfiguration.UseTemporalTables"/> must be enabled</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Temporal Query Capabilities</b>:
    /// <list type="bullet">
    /// <item><description><c>GetAsOfAsync</c>: Query entity state at a specific point in time</description></item>
    /// <item><description><c>GetHistoryAsync</c>: Retrieve all historical versions of an entity</description></item>
    /// <item><description><c>GetChangedBetweenAsync</c>: Query changes within a time range</description></item>
    /// <item><description><c>ListAsOfAsync</c>: Combine point-in-time queries with specifications</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register DbContext and temporal repository
    /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
    /// {
    ///     config.UseTemporalTables = true;
    /// });
    /// services.AddEncinaTemporalRepository&lt;Order, OrderId&gt;();
    ///
    /// // Use in a service
    /// public class OrderAuditService(ITemporalRepository&lt;Order, OrderId&gt; repository)
    /// {
    ///     public Task&lt;Either&lt;RepositoryError, Order&gt;&gt; GetOrderLastWeekAsync(OrderId id, CancellationToken ct)
    ///         =&gt; repository.GetAsOfAsync(id, DateTime.UtcNow.AddDays(-7), ct);
    ///
    ///     public Task&lt;Either&lt;RepositoryError, IReadOnlyList&lt;Order&gt;&gt;&gt; GetOrderHistoryAsync(OrderId id, CancellationToken ct)
    ///         =&gt; repository.GetHistoryAsync(id, ct);
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaTemporalRepository<TEntity, TId>(
        this IServiceCollection services)
        where TEntity : class, IEntity<TId>
        where TId : notnull
    {
        services.TryAddScoped<ITemporalRepository<TEntity, TId>, TemporalRepositoryEF<TEntity, TId>>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using Entity Framework Core.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsEF{TEntity}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="DbContext"/> must be registered in DI</description></item>
    /// <item><description><typeparamref name="TEntity"/> must be configured in the DbContext model</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Unlike ADO.NET and Dapper providers, EF Core implementation uses
    /// <see cref="DbContext.Model"/> metadata for automatic column mapping resolution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register DbContext and bulk operations
    /// services.AddDbContext&lt;AppDbContext&gt;(options => ...);
    /// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;();
    /// services.AddEncinaBulkOperations&lt;Order&gt;();
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
    public static IServiceCollection AddEncinaBulkOperations<TEntity>(
        this IServiceCollection services)
        where TEntity : class, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IBulkOperations<TEntity>, BulkOperationsEF<TEntity>>();

        return services;
    }
}
