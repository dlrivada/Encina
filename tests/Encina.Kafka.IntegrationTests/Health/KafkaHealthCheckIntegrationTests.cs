using Confluent.Kafka;
using Encina.Kafka.Health;
using Encina.Messaging.Health;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Kafka.IntegrationTests.Health;

/// <summary>
/// Integration tests for KafkaHealthCheck using a real Kafka container.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Messaging", "Kafka")]
public sealed class KafkaHealthCheckIntegrationTests : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _fixture;

    public KafkaHealthCheckIntegrationTests(KafkaFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WhenKafkaIsRunning_ReturnsHealthy()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-kafka" };
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-kafka");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka container not available");

        // Arrange
        var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("messaging");
        healthCheck.Tags.Should().Contain("kafka");
        healthCheck.Tags.Should().Contain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var producer = new ProducerBuilder<string, byte[]>(_fixture.CreateProducerConfig()).Build();
        services.AddSingleton<IProducer<string, byte[]>>(producer);
        return services.BuildServiceProvider();
    }
}
