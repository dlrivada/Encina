using System.Data;
using Encina.ADO.Oracle.Inbox;
using Encina.ADO.Oracle.Outbox;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oracle.ManagedDataAccess.Client;

namespace Encina.ADO.Oracle;

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

        RegisterMessagingServices(services, config);

        return services;
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Oracle database connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddScoped<IDbConnection>(_ => new OracleConnection(connectionString));

        return services.AddEncinaADO(configure);
    }

    /// <summary>
    /// Adds Encina with ADO.NET messaging patterns support using a custom connection factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionFactory">Factory function to create IDbConnection instances.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddScoped(connectionFactory);

        return services.AddEncinaADO(configure);
    }

    private static void RegisterMessagingServices(IServiceCollection services, MessagingConfiguration config)
    {
        // Outbox Pattern
        if (config.UseOutbox)
        {
            services.AddSingleton(config.OutboxOptions);
            services.TryAddScoped<IOutboxStore, OutboxStoreADO>();
            services.AddScoped<IOutboxMessageFactory, OutboxMessageFactory>();
            services.AddScoped(typeof(IRequestPostProcessor<,>), typeof(Messaging.Outbox.OutboxPostProcessor<,>));
            services.AddHostedService<OutboxProcessor>();
        }

        // Inbox Pattern
        if (config.UseInbox)
        {
            services.AddSingleton(config.InboxOptions);
            services.TryAddScoped<IInboxStore, InboxStoreADO>();
            services.AddScoped<IInboxMessageFactory, InboxMessageFactory>();
            services.AddScoped<InboxOrchestrator>();
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Messaging.Inbox.InboxPipelineBehavior<,>));
        }

        // Transaction Pattern
        if (config.UseTransactions)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        }
    }
}
