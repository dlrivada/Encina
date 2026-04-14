using Confluent.Kafka;

using Encina.Kafka;
using Encina.Kafka.Health;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Kafka;

/// <summary>
/// Guard tests for Encina.Kafka covering constructor and method null guards.
/// </summary>
[Trait("Category", "Guard")]
public sealed class KafkaGuardTests
{
    private static readonly IProducer<string, byte[]> Producer = Substitute.For<IProducer<string, byte[]>>();

    // ─── KafkaMessagePublisher constructor guards ───

    [Fact]
    public void Constructor_NullProducer_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new KafkaMessagePublisher(null!,
                NullLogger<KafkaMessagePublisher>.Instance,
                Options.Create(new EncinaKafkaOptions())));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new KafkaMessagePublisher(Producer, null!,
                Options.Create(new EncinaKafkaOptions())));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new KafkaMessagePublisher(Producer,
                NullLogger<KafkaMessagePublisher>.Instance, null!));
    }

    [Fact]
    public void Constructor_ValidArgs_Constructs()
    {
        var sut = new KafkaMessagePublisher(Producer,
            NullLogger<KafkaMessagePublisher>.Instance,
            Options.Create(new EncinaKafkaOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── KafkaMessagePublisher method guards ───

    [Fact]
    public async Task ProduceAsync_NullMessage_Throws()
    {
        var sut = new KafkaMessagePublisher(Producer,
            NullLogger<KafkaMessagePublisher>.Instance,
            Options.Create(new EncinaKafkaOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ProduceAsync<object>(null!));
    }

    [Fact]
    public async Task ProduceBatchAsync_NullMessages_Throws()
    {
        var sut = new KafkaMessagePublisher(Producer,
            NullLogger<KafkaMessagePublisher>.Instance,
            Options.Create(new EncinaKafkaOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ProduceBatchAsync<object>(null!));
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_NullMessage_Throws()
    {
        var sut = new KafkaMessagePublisher(Producer,
            NullLogger<KafkaMessagePublisher>.Instance,
            Options.Create(new EncinaKafkaOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ProduceWithHeadersAsync<object>(null!, new Dictionary<string, byte[]>()));
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_NullHeaders_Throws()
    {
        var sut = new KafkaMessagePublisher(Producer,
            NullLogger<KafkaMessagePublisher>.Instance,
            Options.Create(new EncinaKafkaOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.ProduceWithHeadersAsync(new { Id = 1 }, (IDictionary<string, byte[]>)null!));
    }

    // ─── KafkaHealthCheck ───

    [Fact]
    public void KafkaHealthCheck_Constructs()
    {
        using var sp = new ServiceCollection().BuildServiceProvider();
        var sut = new KafkaHealthCheck(sp, null);
        sut.ShouldNotBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaKafka_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaKafka(_ => { }));
    }

    [Fact]
    public void AddEncinaKafka_ValidServices_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddEncinaKafka(o =>
            o.BootstrapServers = "localhost:9092");

        result.ShouldNotBeNull();

        ServiceDescriptor? publisherRegistration = null;
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(IKafkaMessagePublisher))
            {
                publisherRegistration = service;
                break;
            }
        }

        publisherRegistration.ShouldNotBeNull();
        publisherRegistration!.ImplementationType.ShouldBe(typeof(KafkaMessagePublisher));
        publisherRegistration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    // ─── EncinaKafkaOptions ───

    [Fact]
    public void EncinaKafkaOptions_Defaults()
    {
        var options = new EncinaKafkaOptions();

        options.ShouldNotBeNull();
        options.BootstrapServers.ShouldBe("localhost:9092");
        options.GroupId.ShouldBe("encina-consumer");
        options.DefaultCommandTopic.ShouldBe("encina-commands");
        options.DefaultEventTopic.ShouldBe("encina-events");
        options.AutoOffsetReset.ShouldBe("earliest");
        options.EnableAutoCommit.ShouldBeFalse();
        options.Acks.ShouldBe("all");
        options.EnableIdempotence.ShouldBeTrue();
        options.MessageTimeoutMs.ShouldBe(30000);
    }

    // ─── KafkaDeliveryResult record ───

    [Fact]
    public void KafkaDeliveryResult_PropertiesAssignable()
    {
        var result = new KafkaDeliveryResult(
            Topic: "test-topic",
            Partition: 0,
            Offset: 42,
            Timestamp: DateTimeOffset.UtcNow);

        result.Topic.ShouldBe("test-topic");
        result.Partition.ShouldBe(0);
        result.Offset.ShouldBe(42);
    }
}
