using System.Data;
using Encina.Dapper.SqlServer.Inbox;
using Encina.Dapper.SqlServer.Outbox;
using Encina.Dapper.SqlServer.Sagas;
using Encina.Dapper.SqlServer.Scheduling;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapper.SqlServer;

/// <summary>
/// Extension methods for configuring Encina with Dapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence.
    /// All patterns are opt-in via configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDapper(
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
            services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(Messaging.Outbox.OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.AddScoped<IInboxStore, InboxStoreDapper>();
            services.AddScoped<IInboxMessageFactory, InboxMessageFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Messaging.Inbox.InboxPipelineBehavior<,>));
        }

        if (config.UseSagas)
        {
            services.AddSingleton(config.SagaOptions);
            services.AddScoped<ISagaStore, SagaStoreDapper>();
            services.AddScoped<ISagaStateFactory, SagaStateFactory>();
            services.AddScoped<SagaOrchestrator>();
        }

        if (config.UseScheduling)
        {
            services.AddSingleton(config.SchedulingOptions);
            services.AddScoped<IScheduledMessageStore, ScheduledMessageStoreDapper>();
            services.AddScoped<IScheduledMessageFactory, ScheduledMessageFactory>();
            services.AddScoped<SchedulerOrchestrator>();
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
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        // Register connection factory
        services.AddScoped(connectionFactory);

        // Add messaging patterns
        return services.AddEncinaDapper(configure);
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection string.
    /// Creates SQL Server connections by default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        return services.AddEncinaDapper(
            sp => new Microsoft.Data.SqlClient.SqlConnection(connectionString),
            configure);
    }
}
