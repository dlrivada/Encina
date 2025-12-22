using System.Data;
using Encina.ADO.PostgreSQL.Inbox;
using Encina.ADO.PostgreSQL.Outbox;
using Encina.Messaging;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Encina.ADO.PostgreSQL;

/// <summary>
/// Extension methods for configuring Encina with ADO.NET PostgreSQL provider.
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
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaADO(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));

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
