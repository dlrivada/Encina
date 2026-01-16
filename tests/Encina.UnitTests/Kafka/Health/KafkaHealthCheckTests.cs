using Confluent.Kafka;
using Encina.Kafka.Health;
using Encina.Messaging.Health;

namespace Encina.UnitTests.Kafka.Health;

public sealed class KafkaHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProducer<string, byte[]> _producer;

    public KafkaHealthCheckTests()
    {
        _producer = Substitute.For<IProducer<string, byte[]>>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IProducer<string, byte[]>)).Returns(_producer);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        KafkaHealthCheck.DefaultName.ShouldBe("encina-kafka");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-kafka" };

        // Act
        var healthCheck = new KafkaHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-kafka");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new KafkaHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(KafkaHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new KafkaHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("kafka");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHandleValid_ReturnsHealthy()
    {
        // Arrange
        var handle = Substitute.For<Handle>();
        _producer.Handle.Returns(handle);
        var healthCheck = new KafkaHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenKafkaExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IProducer<string, byte[]>))
            .Returns(_ => throw new KafkaException(new Error(ErrorCode.BrokerNotAvailable, "Broker not available")));
        var healthCheck = new KafkaHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Broker not available");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGenericExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IProducer<string, byte[]>))
            .Returns(_ => throw new InvalidOperationException("Service not available"));
        var healthCheck = new KafkaHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}
