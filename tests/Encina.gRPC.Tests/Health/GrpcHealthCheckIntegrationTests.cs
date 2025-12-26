using Encina.gRPC.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.gRPC.Tests.Health;

/// <summary>
/// Integration tests for GrpcHealthCheck using real dependency injection.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Service", "gRPC")]
public sealed class GrpcHealthCheckIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public GrpcHealthCheckIntegrationTests()
    {
        var services = new ServiceCollection();

        // Configure gRPC services with Encina core
        services.AddEncina(typeof(GrpcHealthCheckIntegrationTests).Assembly);
        services.AddEncinaGrpc();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGrpcServicesRegistered_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new GrpcHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("configured and ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomName_UsesCustomName()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "my-custom-grpc" };
        var healthCheck = new GrpcHealthCheck(_serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        healthCheck.Name.Should().Be("my-custom-grpc");
        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutGrpcServices_ReturnsUnhealthy()
    {
        // Arrange
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var healthCheck = new GrpcHealthCheck(provider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not configured");
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new GrpcHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.Should().Contain("encina");
        healthCheck.Tags.Should().Contain("messaging");
        healthCheck.Tags.Should().Contain("grpc");
        healthCheck.Tags.Should().Contain("ready");
    }

    [Fact]
    public void DefaultName_ShouldBeEncinaGrpc()
    {
        GrpcHealthCheck.DefaultName.Should().Be("encina-grpc");
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }
}
