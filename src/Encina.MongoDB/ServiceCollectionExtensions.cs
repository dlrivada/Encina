using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Modules.Isolation;
using Encina.MongoDB.Auditing;
using Encina.MongoDB.BulkOperations;
using Encina.MongoDB.Health;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Modules;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.ReadWriteSeparation;
using Encina.MongoDB.Repository;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Encina.MongoDB.UnitOfWork;
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

        // Register the collection as scoped
        services.AddScoped<IMongoCollection<TEntity>>(sp =>
        {
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var mongoOptions = sp.GetRequiredService<IOptions<EncinaMongoDbOptions>>().Value;
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            return database.GetCollection<TEntity>(collectionName);
        });

        // Register the repository with scoped lifetime
        services.AddScoped<IFunctionalRepository<TEntity, TId>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            return new FunctionalRepositoryMongoDB<TEntity, TId>(collection, idProperty);
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
            return new FunctionalRepositoryMongoDB<TEntity, TId>(collection, idProperty);
        });

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

        services.TryAddScoped<IBulkOperations<TEntity>>(sp =>
        {
            var collection = sp.GetRequiredService<IMongoCollection<TEntity>>();
            var options = sp.GetRequiredService<MongoDbRepositoryOptions<TEntity, TId>>();
            return new BulkOperationsMongoDB<TEntity, TId>(collection, options.IdProperty!);
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
}
