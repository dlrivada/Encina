using Confluent.Kafka;
using Encina.Kafka;

namespace Encina.UnitTests.Kafka.Publishing;

/// <summary>
/// Tests the producer factory registered by <c>AddEncinaKafka</c>; building a producer does not connect
/// to a broker, so every <c>Acks</c> mapping can be exercised without one.
/// </summary>
public sealed class KafkaRegistrationFactoryTests
{
    [Theory]
    [InlineData("all")]
    [InlineData("none")]
    [InlineData("leader")]
    [InlineData("unknown-value")]
    public void AddEncinaKafka_ProducerFactory_BuildsAProducerForEveryAcksSetting(string acks)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaKafka(o =>
        {
            o.BootstrapServers = "localhost:1";
            o.Acks = acks;
            o.EnableIdempotence = acks == "all";
        });

        using var provider = services.BuildServiceProvider();
        var producer = provider.GetRequiredService<IProducer<string, byte[]>>();

        producer.ShouldNotBeNull();
        producer.Dispose();
    }
}
