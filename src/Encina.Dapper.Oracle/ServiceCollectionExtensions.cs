using System.Data;
using Dapper;
using Encina.Dapper.Oracle.Health;
using Encina.Dapper.Oracle.Inbox;
using Encina.Dapper.Oracle.Outbox;
using Encina.Dapper.Oracle.Sagas;
using Encina.Dapper.Oracle.Scheduling;
using Encina.Messaging;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;

namespace Encina.Dapper.Oracle;

/// <summary>
/// Extension methods for configuring Encina with Dapper for Oracle Database.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static bool _typeHandlersRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures Oracle-specific Dapper type handlers are registered.
    /// This is called automatically by AddEncinaDapper methods.
    /// </summary>
    private static void EnsureTypeHandlersRegistered()
    {
        if (_typeHandlersRegistered)
            return;

        lock (_lock)
        {
            if (_typeHandlersRegistered)
                return;

            // Register Oracle GUID type handlers for RAW(16) storage
            SqlMapper.AddTypeHandler(new OracleGuidTypeHandler());
            SqlMapper.AddTypeHandler(new OracleNullableGuidTypeHandler());

            _typeHandlersRegistered = true;
        }
    }

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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Register Oracle-specific type handlers for Dapper
        EnsureTypeHandlersRegistered();

        var config = new MessagingConfiguration();
        configure(config);

        services.AddMessagingServices<
            OutboxStoreDapper,
            OutboxMessageFactory,
            InboxStoreDapper,
            InboxMessageFactory,
            SagaStoreDapper,
            SagaStateFactory,
            ScheduledMessageStoreDapper,
            ScheduledMessageFactory,
            OutboxProcessor>(config);

        // Register provider health check if enabled
        if (config.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(config.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, OracleHealthCheck>();
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
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddScoped(connectionFactory);
        return services.AddEncinaDapper(configure);
    }

    /// <summary>
    /// Adds Encina messaging patterns with Dapper persistence using a connection string.
    /// Creates Oracle Database connections by default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Oracle database connection string.</param>
    /// <param name="configure">Configuration action for messaging patterns.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDapper(
        this IServiceCollection services,
        string connectionString,
        Action<MessagingConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDapper(
            _ => new OracleConnection(connectionString),
            configure);
    }
}
