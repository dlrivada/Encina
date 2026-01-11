using System.Net;
using Encina.Caching.Redis.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Tests.Health;

public sealed class RedisHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IServer _server;

    public RedisHealthCheckTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _server = Substitute.For<IServer>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IConnectionMultiplexer)).Returns(_connectionMultiplexer);

        var endpoint = new DnsEndPoint("localhost", 6379);
        _connectionMultiplexer.GetEndPoints().Returns([endpoint]);
        _connectionMultiplexer.GetServer(endpoint).Returns(_server);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        RedisHealthCheck.DefaultName.ShouldBe("encina-redis");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-redis" };

        // Act
        var healthCheck = new RedisHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-redis");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new RedisHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(RedisHealthCheck.DefaultName);
    }

    [Fact]
    public void Tags_ContainsExpectedValues()
    {
        // Arrange
        var healthCheck = new RedisHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("cache");
        healthCheck.Tags.ShouldContain("redis");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnected_ReturnsHealthy()
    {
        // Arrange
        _connectionMultiplexer.IsConnected.Returns(true);
        _server.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(5));
        var healthCheck = new RedisHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotConnected_ReturnsUnhealthy()
    {
        // Arrange
        _connectionMultiplexer.IsConnected.Returns(false);
        var healthCheck = new RedisHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingSlow_ReturnsDegraded()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Timeout = TimeSpan.FromSeconds(10) };
        _connectionMultiplexer.IsConnected.Returns(true);
        _server.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromSeconds(6)); // More than half the timeout
        var healthCheck = new RedisHealthCheck(_serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("slow");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _connectionMultiplexer.IsConnected.Returns(true);
        _server.PingAsync(Arg.Any<CommandFlags>()).Returns<TimeSpan>(_ => throw new RedisException("Connection failed"));
        var healthCheck = new RedisHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("failed");
    }
}
