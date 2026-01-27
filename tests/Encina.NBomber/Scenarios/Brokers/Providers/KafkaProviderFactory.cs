using Confluent.Kafka;
using Testcontainers.Kafka;

namespace Encina.NBomber.Scenarios.Brokers.Providers;

/// <summary>
/// Factory for creating Kafka broker providers for load testing.
/// </summary>
public sealed class KafkaProviderFactory : BrokerProviderFactoryBase
{
    private KafkaContainer? _container;
    private string? _bootstrapServers;
    private bool _containerInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaProviderFactory"/> class.
    /// </summary>
    /// <param name="configureOptions">Optional delegate to configure options.</param>
    public KafkaProviderFactory(Action<BrokerProviderOptions>? configureOptions = null)
        : base(configureOptions)
    {
    }

    /// <inheritdoc/>
    public override string ProviderName => "kafka";

    /// <inheritdoc/>
    public override BrokerProviderCategory Category => BrokerProviderCategory.Kafka;

    /// <inheritdoc/>
    public override bool IsAvailable => base.IsAvailable && _containerInitialized;

    /// <summary>
    /// Gets the Kafka bootstrap servers.
    /// </summary>
    public string? BootstrapServers => _bootstrapServers;

    /// <summary>
    /// Creates a producer configuration.
    /// </summary>
    /// <returns>A producer configuration.</returns>
    public ProducerConfig CreateProducerConfig()
    {
        EnsureInitialized();
        return new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            Acks = Acks.Leader,
            EnableIdempotence = false,
            LingerMs = 5,
            BatchSize = 16384
        };
    }

    /// <summary>
    /// Creates a consumer configuration.
    /// </summary>
    /// <param name="groupId">The consumer group ID.</param>
    /// <returns>A consumer configuration.</returns>
    public ConsumerConfig CreateConsumerConfig(string? groupId = null)
    {
        EnsureInitialized();
        return new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = groupId ?? Options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnablePartitionEof = true
        };
    }

    /// <summary>
    /// Creates an admin client configuration.
    /// </summary>
    /// <returns>An admin client configuration.</returns>
    public AdminClientConfig CreateAdminClientConfig()
    {
        EnsureInitialized();
        return new AdminClientConfig
        {
            BootstrapServers = _bootstrapServers
        };
    }

    /// <summary>
    /// Creates a new producer.
    /// </summary>
    /// <returns>A new Kafka producer.</returns>
    public IProducer<string, byte[]> CreateProducer()
    {
        return new ProducerBuilder<string, byte[]>(CreateProducerConfig()).Build();
    }

    /// <summary>
    /// Creates a new consumer.
    /// </summary>
    /// <param name="groupId">Optional consumer group ID.</param>
    /// <returns>A new Kafka consumer.</returns>
    public IConsumer<string, byte[]> CreateConsumer(string? groupId = null)
    {
        return new ConsumerBuilder<string, byte[]>(CreateConsumerConfig(groupId)).Build();
    }

    /// <inheritdoc/>
    protected override async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(Options.KafkaBootstrapServers))
            {
                _bootstrapServers = Options.KafkaBootstrapServers;
            }
            else
            {
                _container = new KafkaBuilder(Options.KafkaImage)
                    .WithCleanUp(true)
                    .Build();

                await _container.StartAsync(cancellationToken).ConfigureAwait(false);
                _bootstrapServers = _container.GetBootstrapAddress();
            }

            _containerInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize Kafka: {ex.Message}");
            _containerInitialized = false;
        }
    }

    /// <inheritdoc/>
    protected override async Task DisposeCoreAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
