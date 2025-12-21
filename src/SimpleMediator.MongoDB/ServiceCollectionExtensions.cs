using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using SimpleMediator.Messaging.Inbox;
using SimpleMediator.Messaging.Outbox;
using SimpleMediator.Messaging.Sagas;
using SimpleMediator.Messaging.Scheduling;
using SimpleMediator.MongoDB.Inbox;
using SimpleMediator.MongoDB.Outbox;
using SimpleMediator.MongoDB.Sagas;
using SimpleMediator.MongoDB.Scheduling;

namespace SimpleMediator.MongoDB;

/// <summary>
/// Extension methods for configuring SimpleMediator MongoDB services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator MongoDB integration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for MongoDB options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddSimpleMediatorMongoDB(options =>
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
    public static IServiceCollection AddSimpleMediatorMongoDB(
        this IServiceCollection services,
        Action<SimpleMediatorMongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SimpleMediatorMongoDbOptions();
        configure(options);

        services.Configure(configure);

        // Register MongoDB client if not already registered
        services.TryAddSingleton<IMongoClient>(sp =>
            new MongoClient(options.ConnectionString));

        // Register stores based on configuration
        if (options.UseOutbox)
        {
            services.AddScoped<IOutboxStore, OutboxStoreMongoDB>();
        }

        if (options.UseInbox)
        {
            services.AddScoped<IInboxStore, InboxStoreMongoDB>();
        }

        if (options.UseSagas)
        {
            services.AddScoped<ISagaStore, SagaStoreMongoDB>();
        }

        if (options.UseScheduling)
        {
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreMongoDB>();
        }

        // Create indexes if configured
        if (options.CreateIndexes)
        {
            services.AddHostedService<MongoDbIndexCreator>();
        }

        return services;
    }

    /// <summary>
    /// Adds SimpleMediator MongoDB integration with an existing MongoDB client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mongoClient">The existing MongoDB client.</param>
    /// <param name="configure">Configuration action for MongoDB options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorMongoDB(
        this IServiceCollection services,
        IMongoClient mongoClient,
        Action<SimpleMediatorMongoDbOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SimpleMediatorMongoDbOptions();
        configure(options);

        services.Configure(configure);
        services.AddSingleton(mongoClient);

        // Register stores based on configuration
        if (options.UseOutbox)
        {
            services.AddScoped<IOutboxStore, OutboxStoreMongoDB>();
        }

        if (options.UseInbox)
        {
            services.AddScoped<IInboxStore, InboxStoreMongoDB>();
        }

        if (options.UseSagas)
        {
            services.AddScoped<ISagaStore, SagaStoreMongoDB>();
        }

        if (options.UseScheduling)
        {
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreMongoDB>();
        }

        // Create indexes if configured
        if (options.CreateIndexes)
        {
            services.AddHostedService<MongoDbIndexCreator>();
        }

        return services;
    }
}
