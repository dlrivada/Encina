using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Extension methods for configuring Debezium HTTP Consumer CDC services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Debezium HTTP Consumer CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Debezium CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><description><see cref="Channel{T}"/> for passing events from the HTTP listener to the connector</description></item>
    ///   <item><description><see cref="DebeziumHttpListener"/> as a hosted service for receiving events</description></item>
    ///   <item><description><see cref="ICdcConnector"/> as the Debezium connector</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaCdcDebezium(
        this IServiceCollection services,
        Action<DebeziumCdcOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new DebeziumCdcOptions();
        configure(options);

        services.AddSingleton(options);

        // Register the channel for passing events from HTTP listener to connector
        var channel = Channel.CreateUnbounded<JsonElement>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        services.AddSingleton(channel);

        // Register the HTTP listener as a hosted service
        services.AddHostedService<DebeziumHttpListener>();

        // Register the connector
        services.TryAddSingleton<ICdcConnector, DebeziumCdcConnector>();
        services.TryAddSingleton<DebeziumCdcHealthCheck>();

        return services;
    }
}
