using Encina.NBomber.Scenarios.Brokers.Providers;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Registry for broker provider factories.
/// Supports multiple broker types for load testing.
/// </summary>
public static class BrokerProviderRegistry
{
    /// <summary>
    /// Provider name constants.
    /// </summary>
    public static class ProviderNames
    {
        /// <summary>RabbitMQ AMQP broker.</summary>
        public const string RabbitMQ = "rabbitmq";

        /// <summary>Apache Kafka streaming platform.</summary>
        public const string Kafka = "kafka";

        /// <summary>NATS messaging system.</summary>
        public const string NATS = "nats";

        /// <summary>MQTT lightweight messaging protocol.</summary>
        public const string MQTT = "mqtt";
    }

    /// <summary>
    /// Container images for broker providers.
    /// </summary>
    public static class ContainerImages
    {
        /// <summary>RabbitMQ with management plugin.</summary>
        public const string RabbitMQ = "rabbitmq:3-management-alpine";

        /// <summary>Confluent Kafka.</summary>
        public const string Kafka = "confluentinc/cp-kafka:7.6.0";

        /// <summary>NATS server.</summary>
        public const string NATS = "nats:2-alpine";

        /// <summary>Eclipse Mosquitto MQTT broker.</summary>
        public const string MQTT = "eclipse-mosquitto:2";
    }

    /// <summary>
    /// Gets all supported provider names.
    /// </summary>
    public static IReadOnlyList<string> SupportedProviders =>
    [
        ProviderNames.RabbitMQ,
        ProviderNames.Kafka,
        ProviderNames.NATS,
        ProviderNames.MQTT
    ];

    /// <summary>
    /// Creates a broker provider factory for the specified provider name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    /// <returns>A configured broker provider factory.</returns>
    /// <exception cref="ArgumentException">Thrown if the provider name is not supported.</exception>
    public static IBrokerProviderFactory CreateFactory(
        string providerName,
        Action<BrokerProviderOptions>? configureOptions = null)
    {
        return providerName.ToLowerInvariant() switch
        {
            ProviderNames.RabbitMQ => new RabbitMQProviderFactory(configureOptions),
            ProviderNames.Kafka => new KafkaProviderFactory(configureOptions),
            ProviderNames.NATS => new NATSProviderFactory(configureOptions),
            ProviderNames.MQTT => new MQTTProviderFactory(configureOptions),
            _ => throw new ArgumentException($"Unsupported broker provider: {providerName}. " +
                $"Supported providers: {string.Join(", ", SupportedProviders)}", nameof(providerName))
        };
    }

    /// <summary>
    /// Determines if a provider name is known.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>True if the provider is supported.</returns>
    public static bool IsKnownProvider(string providerName)
    {
        return SupportedProviders.Contains(providerName.ToLowerInvariant());
    }
}
