using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium.Health;
using Encina.Cdc.Debezium.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Cdc.Debezium;

/// <summary>
/// Extension methods for configuring Debezium CDC services (HTTP and Kafka modes).
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

        services.TryAddSingleton(TimeProvider.System);

        var options = new DebeziumCdcOptions();
        configure(options);

        services.AddSingleton(options);

        // Register a bounded channel for passing events from HTTP listener to connector.
        // When the channel is full, the HTTP listener returns 503 to apply backpressure.
        var channel = Channel.CreateBounded<JsonElement>(new BoundedChannelOptions(options.ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        services.AddSingleton(channel);

        // Register the HTTP listener as a hosted service
        services.AddHostedService<DebeziumHttpListener>();

        // Register the connector
        services.TryAddSingleton<ICdcConnector, DebeziumCdcConnector>();
        services.TryAddSingleton<DebeziumCdcHealthCheck>();

        return services;
    }

    /// <summary>
    /// Adds Debezium Kafka Consumer CDC connector services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Debezium Kafka CDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method is mutually exclusive with <see cref="AddEncinaCdcDebezium"/>. Both register
    /// <see cref="ICdcConnector"/> via <c>TryAddSingleton</c>, so the first one registered wins.
    /// Use <see cref="AddEncinaCdcDebezium"/> for Debezium Server HTTP mode, or this method for
    /// Debezium Connect with Kafka.
    /// </para>
    /// <para>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><description><see cref="DebeziumKafkaOptions"/> as configuration</description></item>
    ///   <item><description><see cref="ICdcConnector"/> as the Debezium Kafka connector</description></item>
    ///   <item><description><see cref="DebeziumKafkaHealthCheck"/> for Kubernetes health probes</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaCdcDebeziumKafka(
        this IServiceCollection services,
        Action<DebeziumKafkaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.TryAddSingleton(TimeProvider.System);

        var options = new DebeziumKafkaOptions();
        configure(options);

        services.AddSingleton(options);

        // Register the Kafka connector (mutually exclusive with HTTP via TryAddSingleton)
        services.TryAddSingleton<ICdcConnector, DebeziumKafkaConnector>();
        services.TryAddSingleton<DebeziumKafkaHealthCheck>();

        return services;
    }
}
