using Confluent.Kafka;
using Testcontainers.Kafka;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// Kafka fixture using Testcontainers.
/// Provides a throwaway Kafka instance for integration tests.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private KafkaContainer? _container;

    /// <summary>
    /// Gets the bootstrap servers for Kafka.
    /// </summary>
    public string BootstrapServers => _container?.GetBootstrapAddress() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the Kafka container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && !string.IsNullOrEmpty(BootstrapServers);

    /// <summary>
    /// Creates a producer configuration for the test Kafka instance.
    /// </summary>
    public ProducerConfig CreateProducerConfig() => new()
    {
        BootstrapServers = BootstrapServers
    };

    /// <summary>
    /// Creates a consumer configuration for the test Kafka instance.
    /// </summary>
    public ConsumerConfig CreateConsumerConfig(string groupId = "test-group") => new()
    {
        BootstrapServers = BootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest
    };

    /// <summary>
    /// Creates an admin client configuration for the test Kafka instance.
    /// </summary>
    public AdminClientConfig CreateAdminClientConfig() => new()
    {
        BootstrapServers = BootstrapServers
    };

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _container = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for Kafka integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class KafkaCollection : ICollectionFixture<KafkaFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Kafka";
}
