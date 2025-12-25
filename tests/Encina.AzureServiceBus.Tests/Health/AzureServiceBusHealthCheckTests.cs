using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.AzureServiceBus.Tests.Health;

public sealed class AzureServiceBusHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServiceBusClient _client;

    public AzureServiceBusHealthCheckTests()
    {
        _client = Substitute.For<ServiceBusClient>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(ServiceBusClient)).Returns(_client);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        AzureServiceBusHealthCheck.DefaultName.ShouldBe("encina-azure-servicebus");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-servicebus" };

        // Act
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-servicebus");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(AzureServiceBusHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("azure-servicebus");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientNotClosed_ReturnsHealthy()
    {
        // Arrange
        _client.IsClosed.Returns(false);
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenClientClosed_ReturnsUnhealthy()
    {
        // Arrange
        _client.IsClosed.Returns(true);
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("closed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _serviceProvider.GetService(typeof(ServiceBusClient))
            .Returns(_ => throw new ServiceBusException("Connection failed", ServiceBusFailureReason.ServiceCommunicationProblem));
        var healthCheck = new AzureServiceBusHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("ServiceCommunicationProblem");
    }
}
