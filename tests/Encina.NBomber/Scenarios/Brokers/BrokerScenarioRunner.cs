using NBomber.Contracts;

namespace Encina.NBomber.Scenarios.Brokers;

/// <summary>
/// Runner for message broker load test scenarios.
/// Creates and executes RabbitMQ, Kafka, NATS, and MQTT broker scenarios.
/// </summary>
public sealed class BrokerScenarioRunner : IAsyncDisposable
{
    private readonly BrokerFeature _feature;
    private readonly string _providerName;
    private IBrokerProviderFactory? _providerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerScenarioRunner"/> class.
    /// </summary>
    /// <param name="feature">The broker feature to test.</param>
    /// <param name="providerName">Optional provider name.</param>
    public BrokerScenarioRunner(BrokerFeature feature, string? providerName = null)
    {
        _feature = feature;
        _providerName = providerName ?? GetDefaultProviderName(feature);
    }

    /// <summary>
    /// Creates scenarios for the specified broker feature.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public async Task<ScenarioProps[]> CreateScenariosAsync()
    {
        var scenarios = new List<ScenarioProps>();

        _providerFactory = BrokerProviderRegistry.CreateFactory(_providerName, options =>
        {
            options.MessageSizeBytes = 1024;
            options.TopicPrefix = "nbomber";
            options.ConsumerGroupId = "nbomber-consumer-group";
        });

        await _providerFactory.InitializeAsync().ConfigureAwait(false);

        if (!_providerFactory.IsAvailable)
        {
            Console.WriteLine($"Broker provider '{_providerName}' is not available (Docker may be required).");
            return [];
        }

        var context = new BrokerScenarioContext(_providerFactory, _providerName);

        if (_feature is BrokerFeature.RabbitMQ or BrokerFeature.All)
        {
            if (_providerFactory.Category == BrokerProviderCategory.RabbitMQ)
            {
                var rabbitFactory = new RabbitMQScenarioFactory(context);
                scenarios.AddRange(rabbitFactory.CreateScenarios());
            }
        }

        if (_feature is BrokerFeature.Kafka or BrokerFeature.All)
        {
            if (_providerFactory.Category == BrokerProviderCategory.Kafka)
            {
                var kafkaFactory = new KafkaScenarioFactory(context);
                scenarios.AddRange(kafkaFactory.CreateScenarios());
            }
        }

        if (_feature is BrokerFeature.NATS or BrokerFeature.All)
        {
            if (_providerFactory.Category == BrokerProviderCategory.NATS)
            {
                var natsFactory = new NATSScenarioFactory(context);
                scenarios.AddRange(natsFactory.CreateScenarios());
            }
        }

        if (_feature is BrokerFeature.MQTT or BrokerFeature.All)
        {
            if (_providerFactory.Category == BrokerProviderCategory.MQTT)
            {
                var mqttFactory = new MQTTScenarioFactory(context);
                scenarios.AddRange(mqttFactory.CreateScenarios());
            }
        }

        return scenarios.ToArray();
    }

    /// <summary>
    /// Creates scenarios for all available providers.
    /// </summary>
    /// <returns>A collection of scenario props to run.</returns>
    public static async Task<ScenarioProps[]> CreateAllProvidersAsync()
    {
        var allScenarios = new List<ScenarioProps>();

        // RabbitMQ scenarios
        var rabbitRunner = new BrokerScenarioRunner(BrokerFeature.RabbitMQ, BrokerProviderRegistry.ProviderNames.RabbitMQ);
        allScenarios.AddRange(await rabbitRunner.CreateScenariosAsync().ConfigureAwait(false));
        await rabbitRunner.DisposeAsync().ConfigureAwait(false);

        // Kafka scenarios
        var kafkaRunner = new BrokerScenarioRunner(BrokerFeature.Kafka, BrokerProviderRegistry.ProviderNames.Kafka);
        allScenarios.AddRange(await kafkaRunner.CreateScenariosAsync().ConfigureAwait(false));
        await kafkaRunner.DisposeAsync().ConfigureAwait(false);

        // NATS scenarios
        var natsRunner = new BrokerScenarioRunner(BrokerFeature.NATS, BrokerProviderRegistry.ProviderNames.NATS);
        allScenarios.AddRange(await natsRunner.CreateScenariosAsync().ConfigureAwait(false));
        await natsRunner.DisposeAsync().ConfigureAwait(false);

        // MQTT scenarios
        var mqttRunner = new BrokerScenarioRunner(BrokerFeature.MQTT, BrokerProviderRegistry.ProviderNames.MQTT);
        allScenarios.AddRange(await mqttRunner.CreateScenariosAsync().ConfigureAwait(false));
        await mqttRunner.DisposeAsync().ConfigureAwait(false);

        return allScenarios.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_providerFactory is not null)
        {
            await _providerFactory.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string GetDefaultProviderName(BrokerFeature feature)
    {
        return feature switch
        {
            BrokerFeature.RabbitMQ => BrokerProviderRegistry.ProviderNames.RabbitMQ,
            BrokerFeature.Kafka => BrokerProviderRegistry.ProviderNames.Kafka,
            BrokerFeature.NATS => BrokerProviderRegistry.ProviderNames.NATS,
            BrokerFeature.MQTT => BrokerProviderRegistry.ProviderNames.MQTT,
            _ => BrokerProviderRegistry.ProviderNames.RabbitMQ
        };
    }
}

/// <summary>
/// Feature categories for message broker load testing.
/// </summary>
public enum BrokerFeature
{
    /// <summary>RabbitMQ AMQP message broker.</summary>
    RabbitMQ,

    /// <summary>Apache Kafka streaming platform.</summary>
    Kafka,

    /// <summary>NATS messaging system.</summary>
    NATS,

    /// <summary>MQTT lightweight messaging protocol.</summary>
    MQTT,

    /// <summary>All broker features.</summary>
    All
}
