using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.Redis.Tests;

public class RedisDistributedLockProviderTests
{
    private readonly IOptions<RedisLockOptions> _options;
    private readonly ILogger<RedisDistributedLockProvider> _logger;

    public RedisDistributedLockProviderTests()
    {
        _options = Options.Create(new RedisLockOptions());
        _logger = NullLogger<RedisDistributedLockProvider>.Instance;
    }

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange
        IConnectionMultiplexer? connection = null;

        // Act
        var act = () => new RedisDistributedLockProvider(connection!, _options, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        IOptions<RedisLockOptions>? options = null;

        // Act
        var act = () => new RedisDistributedLockProvider(connection, options!, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        ILogger<RedisDistributedLockProvider>? logger = null;

        // Act
        var act = () => new RedisDistributedLockProvider(connection, _options, logger!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var act = async () => await provider.TryAcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    [Fact]
    public async Task AcquireAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var act = async () => await provider.AcquireAsync(
            null!,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var act = async () => await provider.IsLockedAsync(null!, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    [Fact]
    public async Task ExtendAsync_WithNullResource_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var act = async () => await provider.ExtendAsync(
            null!,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("resource");
    }

    [Fact]
    public async Task TryAcquireAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var provider = new RedisDistributedLockProvider(connection, _options, _logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }
}
