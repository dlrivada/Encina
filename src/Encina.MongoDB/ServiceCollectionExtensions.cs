using Encina.Compliance.Consent;
using Encina.Compliance.GDPR;
using Encina.Database;
using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.SoftDelete;
using Encina.Modules.Isolation;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.BulkOperations;
using Encina.MongoDB.Consent;
using Encina.MongoDB.Health;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Modules;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.MongoDB.Repository;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Encina.MongoDB.SoftDelete;
using Encina.MongoDB.UnitOfWork;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Encina.MongoDB;

/// <summary>
/// Extension methods for configuring Encina MongoDB services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina MongoDB integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MongoDB options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaMongoDB(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "MyApp";
    ///     options.UseOutbox = true;
    ///     options.UseInbox = true;
    ///     options.UseSagas = true;
    ///     options.UseScheduling = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaMongoDB(
        this IServiceCollection services,
        Action<EncinaMongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncinaMongoDbOptions();
        configure(options);

        services.Configure(configure);

        // Register TimeProvider for consistent timestamps across all MongoDB components
        services.TryAddSingleton(TimeProvider.System);

        // Register MongoDB client if not already registered
        services.TryAddSingleton<IMongoClient>(sp =>
            new MongoClient(options.ConnectionString));

        // Register stores based on configuration
        if (options.UseOutbox)
        {
            services.AddScoped<IOutboxStore, OutboxStoreMongoDB>();
            services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(Messaging.Outbox.OutboxPostProcessor<,>));
        }

        if (options.UseInbox)
        {
            services.AddScoped<IInboxStore, InboxStoreMongoDB>();
            services.AddScoped<IInboxMessageFactory, InboxMessageFactory>();
            services.AddScoped<InboxOrchestrator>();
        }

        if (options.UseSagas)
        {
            services.AddSingleton(options.SagaOptions);
            services.AddScoped<ISagaStore, SagaStoreMongoDB>();
            services.AddScoped<ISagaStateFactory, SagaStateFactory>();
            services.AddScoped<SagaOrchestrator>();
        }

        if (options.UseScheduling)
        {
            services.AddSingleton(options.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreMongoDB>();
            services.AddScoped<IScheduledMessageFactory, ScheduledMessageFactory>();
            services.AddScoped<SchedulerOrchestrator>();
        }

        // Register audit log store if enabled
        if (options.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreMongoDB>();
        }

        // Register consent stores if enabled
        if (options.UseConsent)
        {
            services.AddScoped<IConsentStore, ConsentStoreMongoDB>();
            services.AddScoped<IConsentAuditStore, ConsentAuditStoreMongoDB>();
            services.AddScoped<IConsentVersionManager, ConsentVersionManagerMongoDB>();
        }

        // Create indexes if configured
        if (options.CreateIndexes)
        {
            services.AddHostedService<MongoDbIndexCreator>();
        }

        // Register provider health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, MongoDbHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new MongoDbDatabaseHealthMonitor(sp));

        // Register module isolation services if enabled
        if (options.UseModuleIsolation)
        {
            RegisterModuleIsolationServices(services, options);
        }

        // Register read/write separation services if enabled
        if (options.UseReadWriteSeparation)
        {
            RegisterReadWriteSeparationServices(services, options);
        }

        return services;
    }

    /// <summary>
    /// Adds Encina MongoDB integration with an existing MongoDB client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mongoClient">The existing MongoDB client.</param>
    /// <param name="configure">Configuration action for MongoDB options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMongoDB(
        this IServiceCollection services,
        IMongoClient mongoClient,
        Action<EncinaMongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EncinaMongoDbOptions();
        configure(options);

        services.Configure(configure);
        services.AddSingleton(mongoClient);

        // Register stores based on configuration
        if (options.UseOutbox)
        {
            services.AddScoped<IOutboxStore, OutboxStoreMongoDB>();
            services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(Messaging.Outbox.OutboxPostProcessor<,>));
        }

        if (options.UseInbox)
        {
            services.AddScoped<IInboxStore, InboxStoreMongoDB>();
            services.AddScoped<IInboxMessageFactory, InboxMessageFactory>();
            services.AddScoped<InboxOrchestrator>();
        }

        if (options.UseSagas)
        {
            services.AddSingleton(options.SagaOptions);
            services.AddScoped<ISagaStore, SagaStoreMongoDB>();
            services.AddScoped<ISagaStateFactory, SagaStateFactory>();
            services.AddScoped<SagaOrchestrator>();
        }

        if (options.UseScheduling)
        {
            services.AddSingleton(options.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreMongoDB>();
            services.AddScoped<IScheduledMessageFactory, ScheduledMessageFactory>();
            services.AddScoped<SchedulerOrchestrator>();
        }

        // Register audit log store if enabled
        if (options.UseAuditLogStore)
        {
            services.AddScoped<IAuditLogStore, AuditLogStoreMongoDB>();
        }

        // Register consent stores if enabled
        if (options.UseConsent)
        {
            services.AddScoped<IConsentStore, ConsentStoreMongoDB>();
            services.AddScoped<IConsentAuditStore, ConsentAuditStoreMongoDB>();
            services.AddScoped<IConsentVersionManager, ConsentVersionManagerMongoDB>();
        }

        // Create indexes if configured
        if (options.CreateIndexes)
        {
            services.AddHostedService<MongoDbIndexCreator>();
        }

        // Register provider health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, MongoDbHealthCheck>();
        }

        // Register database health monitor for resilience infrastructure
        services.TryAddSingleton<IDatabaseHealthMonitor>(sp =>
            new MongoDbDatabaseHealthMonitor(sp));

        // Register module isolation services if enabled
        if (options.UseModuleIsolation)
        {
            RegisterModuleIsolationServices(services, options);
        }

        // Register read/write separation services if enabled
        if (options.UseReadWriteSeparation)
        {
            RegisterReadWriteSeparationServices(services, options);
        }

        return services;
    }

    /// <summary>
    /// Registers a functional repository for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for repository options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/> and
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// Requires <see cref="IMongoClient"/> and <see cref="EncinaMongoDbOptions"/> to be registered,
    /// typically via <see cref="AddEncinaMongoDB(IServiceCollection, Action{EncinaMongoDbOptions})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaMongoDB(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "MyApp";
    /// });
    ///
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(config =>
    /// {
    ///     config.CollectionName = "orders";
    ///     config.IdProperty = o => o.Id;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<MongoDbRepositoryOptions<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build options
        var options = new MongoDbRepositoryOptions<TEntity, TId>();
        configure(options);
        options.Validate();

        var collectionName = options.GetEffectiveCollectionName();
        var idProperty = options.IdProperty!;

        // Register the options for UnitOfWork to access
        services.AddSingleton(options);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the collection as scoped
        services.AddScoped<IMongoCollection<TEntity>>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            return database.GetCollection<TEntity>(collectionName);
        });

        // Register the repository with scoped lifetime, resolving audit dependencies
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new FunctionalRepositoryMongoDB<TEntity, TId>(
                collection, idProperty, requestContext, timeProvider);
        });

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<IFunctionalRepository<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Registers a read-only functional repository for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for repository options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Only registers <see cref="IFunctionalReadRepository{TEntity, TId}"/> with scoped lifetime.
    /// Use this for read-only scenarios where write operations are not needed.
    /// </para>
    /// <para>
    /// Requires <see cref="IMongoClient"/> and <see cref="EncinaMongoDbOptions"/> to be registered,
    /// typically via <see cref="AddEncinaMongoDB(IServiceCollection, Action{EncinaMongoDbOptions})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaReadRepository&lt;OrderSummary, Guid&gt;(config =>
    /// {
    ///     config.CollectionName = "order_summaries";
    ///     config.IdProperty = o => o.Id;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaReadRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<MongoDbRepositoryOptions<TEntity, TId>> configure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Build options
        var options = new MongoDbRepositoryOptions<TEntity, TId>();
        configure(options);
        options.Validate();

        var collectionName = options.GetEffectiveCollectionName();
        var idProperty = options.IdProperty!;

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register the collection as scoped
        services.TryAddScoped<IMongoCollection<TEntity>>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            return database.GetCollection<TEntity>(collectionName);
        });

        // Register only the read repository with scoped lifetime
        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new FunctionalRepositoryMongoDB<TEntity, TId>(
                collection, idProperty, requestContext, timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Registers a functional repository with soft delete support for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="repositoryConfigure">Configuration action for repository options.</param>
    /// <param name="mappingConfigure">Configuration action for soft delete entity mapping.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IFunctionalRepository{TEntity, TId}"/>,
    /// <see cref="IFunctionalReadRepository{TEntity, TId}"/>, and
    /// <see cref="SoftDeletableFunctionalRepositoryMongoDB{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// The soft delete repository automatically filters out soft-deleted entities from queries
    /// and converts <c>DeleteAsync</c> operations into soft deletes (setting <c>IsDeleted = true</c>).
    /// </para>
    /// <para>
    /// Inject <see cref="SoftDeletableFunctionalRepositoryMongoDB{TEntity, TId}"/> directly to access
    /// soft delete specific operations:
    /// <list type="bullet">
    ///   <item><description><c>RestoreAsync</c>: Restores a soft-deleted entity</description></item>
    ///   <item><description><c>HardDeleteAsync</c>: Permanently deletes an entity</description></item>
    ///   <item><description><c>ListWithDeletedAsync</c>: Queries including soft-deleted entities</description></item>
    ///   <item><description><c>GetByIdWithDeletedAsync</c>: Gets entity by ID including soft-deleted</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The entity does not need to implement <c>ISoftDeletable</c> since the soft delete properties
    /// are mapped explicitly via the <paramref name="mappingConfigure"/> action.
    /// </para>
    /// <para>
    /// Requires <see cref="IMongoClient"/> and <see cref="EncinaMongoDbOptions"/> to be registered,
    /// typically via <see cref="AddEncinaMongoDB(IServiceCollection, Action{EncinaMongoDbOptions})"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaMongoDB(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "MyApp";
    /// });
    ///
    /// services.AddEncinaSoftDeleteRepository&lt;Order, Guid&gt;(
    ///     repositoryConfig =>
    ///     {
    ///         repositoryConfig.CollectionName = "orders";
    ///         repositoryConfig.IdProperty = o => o.Id;
    ///     },
    ///     mappingConfig =>
    ///     {
    ///         mappingConfig
    ///             .HasId(o => o.Id)
    ///             .HasSoftDelete(o => o.IsDeleted, "isDeleted")
    ///             .HasDeletedAt(o => o.DeletedAtUtc, "deletedAtUtc")
    ///             .HasDeletedBy(o => o.DeletedBy, "deletedBy");
    ///     });
    ///
    /// // Usage with IFunctionalRepository (soft delete transparent)
    /// public class OrderService(IFunctionalRepository&lt;Order, Guid&gt; repo)
    /// {
    ///     public async Task DeleteAsync(Guid id, CancellationToken ct)
    ///     {
    ///         // Soft delete (sets IsDeleted = true)
    ///         await repo.DeleteAsync(id, ct);
    ///     }
    /// }
    ///
    /// // Usage with explicit soft delete operations
    /// public class OrderAdminService(SoftDeletableFunctionalRepositoryMongoDB&lt;Order, Guid&gt; repo)
    /// {
    ///     public async Task RestoreAsync(Guid id, CancellationToken ct)
    ///     {
    ///         await repo.RestoreAsync(id, ct);
    ///     }
    ///
    ///     public async Task PermanentDeleteAsync(Guid id, CancellationToken ct)
    ///     {
    ///         await repo.HardDeleteAsync(id, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaSoftDeleteRepository<TEntity, TId>(
        this IServiceCollection services,
        Action<MongoDbRepositoryOptions<TEntity, TId>> repositoryConfigure,
        Action<SoftDeleteEntityMappingBuilder<TEntity, TId>> mappingConfigure)
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(repositoryConfigure);
        ArgumentNullException.ThrowIfNull(mappingConfigure);

        // Build repository options
        var repositoryOptions = new MongoDbRepositoryOptions<TEntity, TId>();
        repositoryConfigure(repositoryOptions);
        repositoryOptions.Validate();

        var collectionName = repositoryOptions.GetEffectiveCollectionName();
        var idProperty = repositoryOptions.IdProperty!;

        // Build soft delete mapping
        var mappingBuilder = new SoftDeleteEntityMappingBuilder<TEntity, TId>();
        mappingConfigure(mappingBuilder);
        var mapping = mappingBuilder.Build()
            .Match(Right: m => m, Left: error => throw new InvalidOperationException(error.Message));

        // Register the options for UnitOfWork to access
        services.AddSingleton(repositoryOptions);

        // Register the mapping as singleton (immutable after configuration)
        services.AddSingleton(mapping);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        // Register SoftDeleteOptions if not already registered
        services.TryAddSingleton<SoftDeleteOptions>();

        // Register the collection as scoped
        services.AddScoped<IMongoCollection<TEntity>>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            return database.GetCollection<TEntity>(collectionName);
        });

        // Register the soft delete repository with scoped lifetime
        services.AddScoped<SoftDeletableFunctionalRepositoryMongoDB<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var entityMapping = sp.GetRequiredService<ISoftDeleteEntityMapping<TEntity, TId>>();
            var softDeleteOptions = sp.GetRequiredService<SoftDeleteOptions>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new SoftDeletableFunctionalRepositoryMongoDB<TEntity, TId>(
                collection, entityMapping, softDeleteOptions, requestContext, timeProvider);
        });

        // Register interfaces pointing to the same implementation
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<SoftDeletableFunctionalRepositoryMongoDB<TEntity, TId>>());

        services.AddScoped<IFunctionalReadRepository<TEntity, TId>>(sp =>
            sp.GetRequiredService<SoftDeletableFunctionalRepositoryMongoDB<TEntity, TId>>());

        return services;
    }

    /// <summary>
    /// Adds Encina Unit of Work pattern with MongoDB.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IUnitOfWork"/> implemented by <see cref="UnitOfWorkMongoDB"/>
    /// with scoped lifetime.
    /// </para>
    /// <para>
    /// <strong>Important:</strong> MongoDB transactions require a replica set deployment.
    /// Standalone MongoDB servers do not support multi-document transactions.
    /// For development, consider using a single-node replica set.
    /// </para>
    /// <para>
    /// Requires <see cref="IMongoClient"/> and <see cref="EncinaMongoDbOptions"/> to be registered,
    /// typically via <see cref="AddEncinaMongoDB(IServiceCollection, Action{EncinaMongoDbOptions})"/>.
    /// </para>
    /// <para>
    /// Entity repository options must be registered separately using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{MongoDbRepositoryOptions{TEntity, TId}})"/>
    /// for each entity type used with the Unit of Work.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaMongoDB(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017/?replicaSet=rs0";
    ///     options.DatabaseName = "MyApp";
    /// });
    ///
    /// // Register entity mappings
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(config =>
    /// {
    ///     config.CollectionName = "orders";
    ///     config.IdProperty = o => o.Id;
    /// });
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
        services.TryAddScoped<IUnitOfWork, UnitOfWorkMongoDB>();

        return services;
    }

    /// <summary>
    /// Registers bulk operations for an entity type using MongoDB.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TId">The entity identifier type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers <see cref="IBulkOperations{TEntity}"/> implemented by
    /// <see cref="BulkOperationsMongoDB{TEntity, TId}"/> with scoped lifetime.
    /// </para>
    /// <para>
    /// <b>Requirements</b>:
    /// <list type="bullet">
    /// <item><description><see cref="IMongoCollection{TDocument}"/> for <typeparamref name="TEntity"/> must be registered in DI</description></item>
    /// <item><description><see cref="MongoDbRepositoryOptions{TEntity, TId}"/> must be registered for the entity</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Typically, you register both by using
    /// <see cref="AddEncinaRepository{TEntity, TId}(IServiceCollection, Action{MongoDbRepositoryOptions{TEntity, TId}})"/>
    /// which automatically registers the collection and repository options.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register MongoDB and repository
    /// services.AddEncinaMongoDB(options =>
    /// {
    ///     options.ConnectionString = "mongodb://localhost:27017";
    ///     options.DatabaseName = "MyApp";
    /// });
    ///
    /// services.AddEncinaRepository&lt;Order, Guid&gt;(config =>
    /// {
    ///     config.CollectionName = "orders";
    ///     config.IdProperty = o => o.Id;
    /// });
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
        where TEntity : class
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register TimeProvider.System as singleton if not already registered
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddScoped<IBulkOperations<TEntity>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var options = sp.GetRequiredService<MongoDbRepositoryOptions<TEntity, TId>>();
            var requestContext = sp.GetService<IRequestContext>();
            var timeProvider = sp.GetService<TimeProvider>();
            return new BulkOperationsMongoDB<TEntity, TId>(
                collection, options.IdProperty!, null, requestContext, timeProvider);
        });

        return services;
    }

    /// <summary>
    /// Registers module isolation services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The MongoDB options containing module isolation configuration.</param>
    private static void RegisterModuleIsolationServices(
        IServiceCollection services,
        EncinaMongoDbOptions options)
    {
        // Register module isolation options
        services.Configure<MongoDbModuleIsolationOptions>(opt =>
        {
            opt.EnableDatabasePerModule = options.ModuleIsolationOptions.EnableDatabasePerModule;
            opt.DatabaseNamePattern = options.ModuleIsolationOptions.DatabaseNamePattern;
            opt.ThrowOnMissingModuleContext = options.ModuleIsolationOptions.ThrowOnMissingModuleContext;
            opt.LogWarningOnFallback = options.ModuleIsolationOptions.LogWarningOnFallback;

            foreach (var mapping in options.ModuleIsolationOptions.ModuleDatabaseMappings)
            {
                opt.ModuleDatabaseMappings[mapping.Key] = mapping.Value;
            }
        });

        // Register core module isolation services (shared with EF Core, Dapper, ADO.NET)
        services.TryAddSingleton<IModuleSchemaRegistry, ModuleSchemaRegistry>();
        services.TryAddScoped<IModuleExecutionContext, ModuleExecutionContext>();

        // Register module-aware collection factory
        services.AddScoped<IModuleAwareMongoCollectionFactory, ModuleAwareMongoCollectionFactory>();
    }

    /// <summary>
    /// Registers read/write separation services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The MongoDB options containing read/write separation configuration.</param>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="MongoReadWriteSeparationOptions"/>: Configuration options</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="IReadWriteMongoCollectionFactory"/>: Factory for creating routed collections</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="ReadWriteRoutingPipelineBehavior{TRequest, TResponse}"/>: Pipeline behavior for automatic routing</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="ReadWriteMongoHealthCheck"/>: Health check for replica set topology</description>
    ///   </item>
    /// </list>
    /// <para>
    /// <b>Requirements:</b> Read/write separation requires a MongoDB replica set deployment.
    /// Standalone MongoDB servers do not support read preferences other than Primary.
    /// </para>
    /// </remarks>
    private static void RegisterReadWriteSeparationServices(
        IServiceCollection services,
        EncinaMongoDbOptions options)
    {
        // Register read/write separation options
        services.Configure<MongoReadWriteSeparationOptions>(opt =>
        {
            opt.ReadPreference = options.ReadWriteSeparationOptions.ReadPreference;
            opt.ReadConcern = options.ReadWriteSeparationOptions.ReadConcern;
            opt.ValidateOnStartup = options.ReadWriteSeparationOptions.ValidateOnStartup;
            opt.FallbackToPrimaryOnNoSecondaries = options.ReadWriteSeparationOptions.FallbackToPrimaryOnNoSecondaries;
            opt.MaxStaleness = options.ReadWriteSeparationOptions.MaxStaleness;
        });

        // Register collection factory for read/write routing
        services.AddScoped<IReadWriteMongoCollectionFactory, ReadWriteMongoCollectionFactory>();

        // Register pipeline behavior for automatic routing based on request type
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ReadWriteRoutingPipelineBehavior<,>));

        // Register health check for replica set topology monitoring
        services.AddSingleton<IEncinaHealthCheck, ReadWriteMongoHealthCheck>();
    }

    /// <summary>
    /// Adds GDPR Lawful Basis persistent stores using MongoDB.
    /// Registers <see cref="ILawfulBasisRegistry"/> and <see cref="ILIAStore"/> as singletons
    /// that create their own MongoClient.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The database name (default: Encina).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaLawfulBasisMongoDB(
        this IServiceCollection services,
        string connectionString,
        string databaseName = "Encina")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        services.TryAddSingleton<ILawfulBasisRegistry>(
            new LawfulBasis.LawfulBasisRegistryMongoDB(connectionString, databaseName));
        services.TryAddSingleton<ILIAStore>(
            new LawfulBasis.LIAStoreMongoDB(connectionString, databaseName));

        return services;
    }
}
