using Encina.DistributedLock.Redis.Health;
using Encina.Messaging.Health;
using StackExchange.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Unit tests for <see cref="RedisDistributedLockHealthCheck"/>.
/// </summary>
public class RedisDistributedLockHealthCheckTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IConnectionMultiplexer? connection = null;
        var options = new ProviderHealthCheckOptions();

        // Act
        var act = () => new RedisDistributedLockHealthCheck(connection!, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        ProviderHealthCheckOptions? options = null;

        // Act
        var act = () => new RedisDistributedLockHealthCheck(connection, options!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region Name and Tags

    [Fact]
    public void Name_WithDefaultOptions_ReturnsDefaultName()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act & Assert
        healthCheck.Name.ShouldBe("redis-distributed-lock");
    }

    [Fact]
    public void Name_WithCustomName_ReturnsCustomName()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions { Name = "custom-lock-check" };
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act & Assert
        healthCheck.Name.ShouldBe("custom-lock-check");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ReturnsDefaultTags()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act & Assert
        healthCheck.Tags.ShouldNotBeNull();
        // Default ProviderHealthCheckOptions has tags ["encina", "database", "ready"]
        healthCheck.Tags.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Tags_WithCustomTags_ReturnsCustomTags()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions
        {
            Tags = ["redis", "distributed-lock", "critical"]
        };
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act & Assert
        healthCheck.Tags.ShouldContain("redis");
        healthCheck.Tags.ShouldContain("distributed-lock");
        healthCheck.Tags.ShouldContain("critical");
        healthCheck.Tags.Count.ShouldBe(3);
    }

    [Fact]
    public void Tags_WithNullTags_ReturnsDefaultLockTags()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = new ProviderHealthCheckOptions { Tags = null! };
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act & Assert — should use fallback tags
        healthCheck.Tags.ShouldNotBeNull();
        healthCheck.Tags.ShouldContain("redis");
        healthCheck.Tags.ShouldContain("distributed-lock");
        healthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region CheckHealthAsync — Success

    [Fact]
    public async Task CheckHealthAsync_WhenSetSucceeds_ReturnsHealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSetSucceeds_DeletesTestKey()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert — verify cleanup happened
        await db.Received(1).KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    #endregion

    #region CheckHealthAsync — Failure Paths

    [Fact]
    public async Task CheckHealthAsync_WhenSetFails_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed to acquire test lock");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns<bool>(_ => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Redis connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeoutException_ReturnsUnhealthy()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns<bool>(_ => throw new TimeoutException("Redis operation timed out"));

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Redis connection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSetFails_DoesNotCallDelete()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert — should NOT try to delete when set failed
        await db.DidNotReceive().KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
    }

    #endregion

    #region CheckHealthAsync — Key Format

    [Fact]
    public async Task CheckHealthAsync_UsesHealthPrefixedKey()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisKey capturedKey = default;
        db.StringSetAsync(
                Arg.Do<RedisKey>(k => capturedKey = k),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        ((string?)capturedKey).ShouldStartWith("health:lock:");
    }

    [Fact]
    public async Task CheckHealthAsync_SetsWhenNotExists()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        When capturedWhen = When.Always;
        db.StringSetAsync(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Do<When>(w => capturedWhen = w))
            .Returns(true);
        db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var options = new ProviderHealthCheckOptions();
        var healthCheck = new RedisDistributedLockHealthCheck(connection, options);

        // Act
        await healthCheck.CheckHealthAsync();

        // Assert
        capturedWhen.ShouldBe(When.NotExists);
    }

    #endregion
}
