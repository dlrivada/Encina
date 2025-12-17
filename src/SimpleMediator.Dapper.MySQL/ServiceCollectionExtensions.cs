using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySqlConnector;
using SimpleMediator.Dapper.MySQL.Inbox;
using SimpleMediator.Dapper.MySQL.Outbox;
using SimpleMediator.Dapper.MySQL.Sagas;
using SimpleMediator.Dapper.MySQL.Scheduling;
using SimpleMediator.Messaging;
using SimpleMediator.Messaging.Inbox;
using SimpleMediator.Messaging.Outbox;
using SimpleMediator.Messaging.Sagas;
using SimpleMediator.Messaging.Scheduling;

namespace SimpleMediator.Dapper.MySQL;

/// <summary>
/// Extension methods for configuring SimpleMediator with Dapper for MySQL/MariaDB.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator messaging patterns with Dapper persistence.
    /// All patterns are opt-in via configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorDapper(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        var config = new MessagingConfiguration();
        configure(config);

        // IDbConnection should be registered by the application
        // (e.g., scoped SqlConnection with connection string from configuration)

        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }

        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.AddScoped<IOutboxStore, OutboxStoreDapper>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, InboxStoreDapper>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        if (config.UseSagas)
        {
            services.AddScoped<ISagaStore, SagaStoreDapper>();
        }

        if (config.UseScheduling)
        {
            services.AddSingleton(config.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreDapper>();
        }

        return services;
    }

    /// <summary>
    /// Adds SimpleMediator messaging patterns with Dapper persistence using a connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create database connections.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorDapper(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        // Register connection factory
        services.AddScoped(connectionFactory);

        // Add messaging patterns
        return services.AddSimpleMediatorDapper(configure);
    }

    /// <summary>
    /// Adds SimpleMediator messaging patterns with Dapper persistence using a connection string.
    /// Creates MySQL/MariaDB connections using MySqlConnector.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The MySQL/MariaDB connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        return services.AddSimpleMediatorDapper(
            sp => new MySqlConnection(connectionString),
            configure);
    }
}
