using Encina.Messaging.Health;
using Encina.SignalR.Health;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.SignalR.Tests.Health;

/// <summary>
/// Unit tests for <see cref="SignalRHealthCheck"/>.
/// </summary>
public sealed class SignalRHealthCheckTests
{
    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        SignalRHealthCheck.DefaultName.ShouldBe("encina-signalr");
    }

    [Fact]
    public void Constructor_SetsDefaultName()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var healthCheck = new SignalRHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(SignalRHealthCheck.DefaultName);
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ProviderHealthCheckOptions { Name = "custom-signalr" };

        // Act
        var healthCheck = new SignalRHealthCheck(serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-signalr");
    }

    [Fact]
    public void Tags_ContainsExpectedDefaultTags()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var healthCheck = new SignalRHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("signalr");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBroadcasterNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISignalRNotificationBroadcaster)).Returns((object?)null);

        var healthCheck = new SignalRHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        var description = result.Description;
        description.ShouldNotBeNull();
        description.ShouldContain("not configured");
        description.ShouldContain("AddEncinaSignalR");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHubContextNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        serviceProvider.GetService(typeof(ISignalRNotificationBroadcaster)).Returns(broadcaster);

        var hubContextType = typeof(IHubContext<>).MakeGenericType(typeof(Hub));
        serviceProvider.GetService(hubContextType).Returns((object?)null);

        var healthCheck = new SignalRHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        var description = result.Description;
        description.ShouldNotBeNull();
        description.ShouldContain("hub context is not available");
        description.ShouldContain("AddSignalR");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFullyConfigured_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var broadcaster = Substitute.For<ISignalRNotificationBroadcaster>();
        var hubContext = Substitute.For<IHubContext<Hub>>();

        serviceProvider.GetService(typeof(ISignalRNotificationBroadcaster)).Returns(broadcaster);

        var hubContextType = typeof(IHubContext<>).MakeGenericType(typeof(Hub));
        serviceProvider.GetService(hubContextType).Returns(hubContext);

        var healthCheck = new SignalRHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        var description = result.Description;
        description.ShouldNotBeNull();
        description.ShouldContain("configured and ready");
    }

    [Fact]
    public void Constructor_WithCustomTags_UsesCustomTagsPlusEncina()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ProviderHealthCheckOptions
        {
            Tags = ["custom", "tags"]
        };

        // Act
        var healthCheck = new SignalRHealthCheck(serviceProvider, options);

        // Assert
        healthCheck.Tags.ShouldContain("custom");
        healthCheck.Tags.ShouldContain("tags");
        // "encina" tag is always included per base class design (EncinaHealthCheck.EnsureEncinaTag)
        healthCheck.Tags.ShouldContain("encina");
        // But the other default tags should NOT be present when custom tags are provided
        healthCheck.Tags.ShouldNotContain("messaging");
        healthCheck.Tags.ShouldNotContain("signalr");
    }
}
