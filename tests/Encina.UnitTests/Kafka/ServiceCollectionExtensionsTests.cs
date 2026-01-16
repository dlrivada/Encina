using Confluent.Kafka;
using Encina.Kafka;
using Encina.Messaging.Health;

namespace Encina.UnitTests.Kafka;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaKafka_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddEncinaKafka());
    }

    [Fact]
    public void AddEncinaKafka_WithoutConfiguration_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaKafkaOptions>>();

        options.Value.BootstrapServers.ShouldBe("localhost:9092");
        options.Value.GroupId.ShouldBe("encina-consumer");
        options.Value.DefaultCommandTopic.ShouldBe("encina-commands");
        options.Value.DefaultEventTopic.ShouldBe("encina-events");
        options.Value.AutoOffsetReset.ShouldBe("earliest");
        options.Value.EnableAutoCommit.ShouldBeFalse();
        options.Value.Acks.ShouldBe("all");
        options.Value.EnableIdempotence.ShouldBeTrue();
        options.Value.MessageTimeoutMs.ShouldBe(30000);
    }

    [Fact]
    public void AddEncinaKafka_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka(opt =>
        {
            opt.BootstrapServers = "kafka1:9092,kafka2:9092";
            opt.GroupId = "custom-group";
            opt.DefaultCommandTopic = "custom-commands";
            opt.DefaultEventTopic = "custom-events";
            opt.AutoOffsetReset = "latest";
            opt.EnableAutoCommit = true;
            opt.Acks = "leader";
            opt.EnableIdempotence = false;
            opt.MessageTimeoutMs = 60000;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<EncinaKafkaOptions>>();

        options.Value.BootstrapServers.ShouldBe("kafka1:9092,kafka2:9092");
        options.Value.GroupId.ShouldBe("custom-group");
        options.Value.DefaultCommandTopic.ShouldBe("custom-commands");
        options.Value.DefaultEventTopic.ShouldBe("custom-events");
        options.Value.AutoOffsetReset.ShouldBe("latest");
        options.Value.EnableAutoCommit.ShouldBeTrue();
        options.Value.Acks.ShouldBe("leader");
        options.Value.EnableIdempotence.ShouldBeFalse();
        options.Value.MessageTimeoutMs.ShouldBe(60000);
    }

    [Fact]
    public void AddEncinaKafka_RegistersPublisher()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka();

        // Assert - Verify publisher is registered (will fail at resolution without real connection)
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IKafkaMessagePublisher));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaKafka_RegistersProducerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IProducer<string, byte[]>));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaKafka_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaKafka();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaKafka_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka(opt =>
        {
            opt.ProviderHealthCheck.Enabled = true;
            opt.ProviderHealthCheck.Name = "custom-kafka";
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaKafka_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka(opt =>
        {
            opt.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaKafka_RegistersOptionsWithConfigure()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka(opt => opt.BootstrapServers = "test-host:9092");

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IConfigureOptions<EncinaKafkaOptions>));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaKafka_MultipleInvocations_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka();
        services.AddEncinaKafka();

        // Assert - Should only have one publisher registration due to TryAddScoped
        var publisherDescriptors = services.Where(d =>
            d.ServiceType == typeof(IKafkaMessagePublisher)).ToList();
        publisherDescriptors.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("all")]
    [InlineData("none")]
    [InlineData("leader")]
    [InlineData("unknown")]
    public void AddEncinaKafka_WithDifferentAcks_RegistersCorrectly(string acks)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaKafka(opt => opt.Acks = acks);

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IProducer<string, byte[]>));
        descriptor.ShouldNotBeNull();
    }
}
