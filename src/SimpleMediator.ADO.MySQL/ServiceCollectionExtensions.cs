using System.Data;
using MySqlConnector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleMediator.ADO.MySQL.Inbox;
using SimpleMediator.ADO.MySQL.Outbox;
using SimpleMediator.Messaging;
using SimpleMediator.Messaging.Inbox;
using SimpleMediator.Messaging.Outbox;

namespace SimpleMediator.ADO.MySQL;

/// <summary>
/// Extension methods for configuring SimpleMediator with ADO.NET provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator with ADO.NET messaging patterns support using a registered IDbConnection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorADO(
        this IServiceCollection services,
        Action<MessagingConfiguration> configure)
    {
        var config = new MessagingConfiguration();
        configure(config);

        RegisterMessagingServices(services, config);

        return services;
    }

    /// <summary>
    /// Adds SimpleMediator with ADO.NET messaging patterns support using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorADO(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        services.TryAddScoped<IDbConnection>(_ => new MySqlConnection(connectionString));

        return services.AddSimpleMediatorADO(configure);
    }

    /// <summary>
    /// Adds SimpleMediator with ADO.NET messaging patterns support using a custom connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create IDbConnection instances.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorADO(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        services.TryAddScoped(connectionFactory);

        return services.AddSimpleMediatorADO(configure);
    }

    private static void RegisterMessagingServices(IServiceCollection services, MessagingConfiguration config)
    {
        // Outbox Pattern
        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.TryAddScoped<IOutboxStore, OutboxStoreADO>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        // Inbox Pattern
        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.TryAddScoped<IInboxStore, InboxStoreADO>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InboxPipelineBehavior<,>));
        }

        // Transaction Pattern
        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }
    }
}
