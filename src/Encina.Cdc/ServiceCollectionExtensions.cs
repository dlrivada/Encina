using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
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
    ///   <item><description><see cref="CdcProcessor"/> as a hosted service when <see cref="CdcOptions.Enabled"/> is true</description></item>
    ///   <item><description>Handler registrations as scoped services</description></item>
    ///   <item><description><see cref="ICdcEventInterceptor"/> when messaging bridge is enabled</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Provider-specific packages (e.g., Encina.Cdc.SqlServer) should register their
    /// <see cref="ICdcConnector"/> and optionally override the <see cref="ICdcPositionStore"/>.
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

        // Register processor as hosted service when enabled
        if (configuration.Options.Enabled)
        {
            services.AddHostedService<CdcProcessor>();
        }

        return services;
    }
}
