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
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldContain("connected");
    }

    [SkippableFact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka container not available");

        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-kafka" };
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.ShouldBe("my-custom-kafka");
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [SkippableFact]
    public void Tags_ContainsExpectedValues()
    {
        Skip.IfNot(_fixture.IsAvailable, "Kafka container not available");

        // Arrange
        using var serviceProvider = CreateServiceProvider();
        var healthCheck = new KafkaHealthCheck(serviceProvider, null);

        // Act
        var tags = healthCheck.Tags;

        // Assert
        tags.ShouldContain("encina");
        tags.ShouldContain("messaging");
        tags.ShouldContain("kafka");
        tags.ShouldContain("ready");
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        var producer = new ProducerBuilder<string, byte[]>(_fixture.CreateProducerConfig()).Build();
        services.AddSingleton<IProducer<string, byte[]>>(producer);
        return services.BuildServiceProvider();
    }
}
