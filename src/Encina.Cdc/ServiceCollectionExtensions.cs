using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
using Encina.Cdc.Sharding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc;

/// <summary>
/// Extension methods for configuring Encina CDC services.
/// </summary>
/// <example>
/// <code>
/// services.AddEncinaCdc(config =>
/// {
///     config.UseCdc()
///           .AddHandler&lt;Order, OrderChangeHandler&gt;()
///           .WithTableMapping&lt;Order&gt;("dbo.Orders")
///           .WithMessagingBridge(opts =>
///           {
///               opts.TopicPattern = "cdc.{tableName}.{operation}";
///           });
/// });
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina CDC services with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for CDC features.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><description><see cref="ICdcDispatcher"/> as a singleton</description></item>
    ///   <item><description><see cref="ICdcPositionStore"/> with an in-memory default (providers can override)</description></item>
    ///   <item><description><see cref="CdcProcessor"/> as a hosted service when <see cref="CdcOptions.Enabled"/> is true and sharded capture is disabled</description></item>
    ///   <item><description><see cref="ShardedCdcProcessor"/> as a hosted service when sharded capture is enabled</description></item>
    ///   <item><description>Handler registrations as scoped services</description></item>
    ///   <item><description><see cref="ICdcEventInterceptor"/> when messaging bridge is enabled</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When sharded capture is enabled via <see cref="CdcConfiguration.WithShardedCapture"/>,
    /// the standard <see cref="CdcProcessor"/> is NOT registered. Instead, the
    /// <see cref="ShardedCdcProcessor"/> is registered along with the
    /// <see cref="IShardedCdcConnector"/> and <see cref="IShardedCdcPositionStore"/>.
    /// </para>
    /// <para>
    /// Provider-specific packages (e.g., Encina.Cdc.SqlServer) should register their
    /// <see cref="ICdcConnector"/> and optionally override the <see cref="ICdcPositionStore"/>
    /// or <see cref="IShardedCdcPositionStore"/>.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaCdc(
        this IServiceCollection services,
        Action<CdcConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new CdcConfiguration();
        configure(configuration);

        // Register the configuration as singleton for use by dispatcher and processor
        services.AddSingleton(configuration);
        services.AddSingleton(configuration.Options);

        // Register dispatcher as singleton
        services.AddSingleton<ICdcDispatcher, CdcDispatcher>();

        // Register default in-memory position store (providers can override)
        services.TryAddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>();

        // Register handlers
        foreach (var registration in configuration.HandlerRegistrations)
        {
            var handlerInterfaceType = typeof(IChangeEventHandler<>).MakeGenericType(registration.EntityType);
            services.AddScoped(handlerInterfaceType, registration.HandlerType);
        }

        // Register messaging bridge interceptor when enabled
        if (configuration.Options.UseMessagingBridge)
        {
            var messagingOptions = configuration.MessagingOptions ?? new CdcMessagingOptions();
            services.AddSingleton(messagingOptions);
            services.AddScoped<ICdcEventInterceptor, CdcMessagingBridge>();
        }

        // Register outbox CDC handler when enabled
        if (configuration.Options.UseOutboxCdc)
        {
            services.AddScoped<OutboxCdcHandler>();
        }

        // Register sharded capture services when enabled
        if (configuration.Options.UseShardedCapture)
        {
            RegisterShardedCaptureServices(services, configuration);
        }

        // Register processor as hosted service when enabled.
        // Skip standard CdcProcessor when sharded capture is active to avoid conflicts.
        if (configuration.Options.Enabled && !configuration.Options.UseShardedCapture)
        {
            services.AddHostedService<CdcProcessor>();
        }

        return services;
    }

    private static void RegisterShardedCaptureServices(
        IServiceCollection services,
        CdcConfiguration configuration)
    {
        var shardedOptions = configuration.ShardedCaptureOptions ?? new ShardedCaptureOptions();
        services.AddSingleton(shardedOptions);

        // Register default in-memory sharded position store (providers can override).
        // If a custom PositionStoreType is specified, register that instead.
        if (shardedOptions.PositionStoreType is not null)
        {
            services.TryAddSingleton(typeof(IShardedCdcPositionStore), shardedOptions.PositionStoreType);
        }
        else
        {
            services.TryAddSingleton<IShardedCdcPositionStore, InMemoryShardedCdcPositionStore>();
        }

        // Register ShardedCdcConnector as IShardedCdcConnector singleton.
        // The connector factory and topology provider must be registered by the
        // provider-specific package (e.g., Encina.Cdc.SqlServer).
        services.TryAddSingleton<IShardedCdcConnector>(sp =>
        {
            var connectorFactory = sp.GetRequiredService<Func<global::Encina.Sharding.ShardInfo, ICdcConnector>>();
            var topologyProvider = sp.GetRequiredService<global::Encina.Sharding.IShardTopologyProvider>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ShardedCdcConnector>>();
            return new ShardedCdcConnector(shardedOptions.ConnectorId, connectorFactory, topologyProvider, logger);
        });

        // Register sharded processor as hosted service when CDC is enabled
        if (configuration.Options.Enabled)
        {
            services.AddHostedService<ShardedCdcProcessor>();
        }
    }
}
