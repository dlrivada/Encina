using Encina.DistributedLock.Redis.Health;
using Encina.Messaging.Health;
using StackExchange.Redis;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Unit tests for <see cref="RedisDistributedLockHealthCheck"/>.
/// </summary>
public sealed class RedisDistributedLockHealthCheckTests
{
    [Fact]
    public void Constructor_NullConnection_ThrowsArgumentNullException()
    {
        var options = new ProviderHealthCheckOptions();
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(null!, options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockHealthCheck(connection, null!));
    }

    [Fact]
    public void Name_WithDefaultOptions_ReturnsDefaultName()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        healthCheck.Name.ShouldBe("redis-distributed-lock");
    }

    [Fact]
    public void Name_WithCustomName_ReturnsCustomName()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions { Name = "custom-redis-lock" };
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        healthCheck.Name.ShouldBe("custom-redis-lock");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ReturnsDefaultTags()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        healthCheck.Tags.ShouldContain("redis");
        healthCheck.Tags.ShouldContain("distributed-lock");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void Tags_WithCustomTags_ReturnsCustomTags()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions
        {
            Tags = ["custom", "lock"]
        };
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        healthCheck.Tags.ShouldContain("custom");
        healthCheck.Tags.ShouldContain("lock");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSetNxSucceeds_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);
        database.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSetNxFails_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(database);

        database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionThrows_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>())
            .Returns<IDatabase>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection failed"));

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Redis connection failed");
    }
}
