using Encina.Messaging.Health;
using Encina.NATS.Health;
using NATS.Client.Core;

namespace Encina.UnitTests.NATS.Health;

public sealed class NATSHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INatsConnection _connection;

    public NATSHealthCheckTests()
    {
        _connection = Substitute.For<INatsConnection>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(INatsConnection)).Returns(_connection);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        NATSHealthCheck.DefaultName.ShouldBe("encina-nats");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-nats" };

        // Act
        var healthCheck = new NATSHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-nats");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(NATSHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("nats");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionOpen_ReturnsHealthy()
    {
        // Arrange
        _connection.ConnectionState.Returns(NatsConnectionState.Open);
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionClosed_ReturnsUnhealthy()
    {
        // Arrange
        _connection.ConnectionState.Returns(NatsConnectionState.Closed);
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Closed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionReconnecting_ReturnsUnhealthy()
    {
        // Arrange
        _connection.ConnectionState.Returns(NatsConnectionState.Reconnecting);
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Reconnecting");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNatsExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(INatsConnection))
            .Returns(_ => throw new NatsException("Connection failed"));
        var healthCheck = new NATSHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Connection failed");
    }
}
