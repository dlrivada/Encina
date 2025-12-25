using Encina.Messaging.Health;
using Encina.RabbitMQ.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RabbitMQ.Client;
using Shouldly;

namespace Encina.RabbitMQ.Tests.Health;

public sealed class RabbitMQHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;

    public RabbitMQHealthCheckTests()
    {
        _connection = Substitute.For<IConnection>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IConnection)).Returns(_connection);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        RabbitMQHealthCheck.DefaultName.ShouldBe("encina-rabbitmq");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-rabbitmq" };

        // Act
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-rabbitmq");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(RabbitMQHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("rabbitmq");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionIsOpen_ReturnsHealthy()
    {
        // Arrange
        _connection.IsOpen.Returns(true);
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionIsClosed_ReturnsUnhealthy()
    {
        // Arrange
        _connection.IsOpen.Returns(false);
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("closed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBrokerUnreachable_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IConnection))
            .Returns(_ => throw new global::RabbitMQ.Client.Exceptions.BrokerUnreachableException(
                new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused)));
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("unreachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(IConnection))
            .Returns(_ => throw new InvalidOperationException("Service not available"));
        var healthCheck = new RabbitMQHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }
}
