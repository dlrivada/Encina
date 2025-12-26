using Encina.gRPC;
using Encina.gRPC.Health;
using Encina.Messaging.Health;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.gRPC.Tests.Health;

/// <summary>
/// Unit tests for <see cref="GrpcHealthCheck"/>.
/// </summary>
public sealed class GrpcHealthCheckTests
{
    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        GrpcHealthCheck.DefaultName.ShouldBe("encina-grpc");
    }

    [Fact]
    public void Constructor_SetsDefaultName()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var healthCheck = new GrpcHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(GrpcHealthCheck.DefaultName);
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ProviderHealthCheckOptions { Name = "custom-grpc" };

        // Act
        var healthCheck = new GrpcHealthCheck(serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-grpc");
    }

    [Fact]
    public void Tags_ContainsExpectedDefaultTags()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();

        // Act
        var healthCheck = new GrpcHealthCheck(serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("messaging");
        healthCheck.Tags.ShouldContain("grpc");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceNotRegistered_ReturnsUnhealthy()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IGrpcEncinaService)).Returns((object?)null);

        var healthCheck = new GrpcHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        var description = result.Description;
        description.ShouldNotBeNull();
        description.ShouldContain("not configured");
        description.ShouldContain("AddEncinaGrpc");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenServiceRegistered_ReturnsHealthy()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var grpcService = Substitute.For<IGrpcEncinaService>();
        serviceProvider.GetService(typeof(IGrpcEncinaService)).Returns(grpcService);

        var healthCheck = new GrpcHealthCheck(serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        var description = result.Description;
        description.ShouldNotBeNull();
        description.ShouldContain("configured and ready");
    }

    [Fact]
    public void Constructor_WithCustomTags_UsesCustomTags()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new ProviderHealthCheckOptions
        {
            Tags = ["custom", "tags"]
        };

        // Act
        var healthCheck = new GrpcHealthCheck(serviceProvider, options);

        // Assert
        healthCheck.Tags.ShouldContain("custom");
        healthCheck.Tags.ShouldContain("tags");
        healthCheck.Tags.ShouldNotContain("encina");
    }
}
