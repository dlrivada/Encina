using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Encina.DistributedLock.GuardTests;

/// <summary>
/// Guard clause tests for RedisDistributedLockProvider.
/// </summary>
public class RedisDistributedLockProviderGuardTests
{
    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IConnectionMultiplexer? connection = null;
        var options = Options.Create(new RedisLockOptions());
        var logger = NullLogger<RedisDistributedLockProvider>.Instance;

        // Act
        var act = () => new RedisDistributedLockProvider(connection!, options, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("connection");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        IOptions<RedisLockOptions>? options = null;
        var logger = NullLogger<RedisDistributedLockProvider>.Instance;

        // Act
        var act = () => new RedisDistributedLockProvider(connection, options!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var options = Options.Create(new RedisLockOptions());
        ILogger<RedisDistributedLockProvider>? logger = null;

        // Act
        var act = () => new RedisDistributedLockProvider(connection, options, logger!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }
}
