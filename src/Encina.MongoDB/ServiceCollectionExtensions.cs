using Encina.Messaging.Health;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.MongoDB.Health;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        return services;
    }
}
