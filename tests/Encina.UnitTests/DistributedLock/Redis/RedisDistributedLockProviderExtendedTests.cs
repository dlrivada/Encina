using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Extended unit tests for <see cref="RedisDistributedLockProvider"/> covering
/// TryAcquire success, IsLocked, Extend, AcquireAsync cancellation, and key prefix logic.
/// </summary>
public sealed class RedisDistributedLockProviderExtendedTests
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly IOptions<RedisLockOptions> _options;
    private readonly ILogger<RedisDistributedLockProvider> _logger;

    public RedisDistributedLockProviderExtendedTests()
    {
        _connection = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_database);
        _options = Options.Create(new RedisLockOptions());
        _logger = NullLogger<RedisDistributedLockProvider>.Instance;
    }

    private RedisDistributedLockProvider CreateProvider(RedisLockOptions? options = null) =>
        new(_connection, Options.Create(options ?? new RedisLockOptions()), _logger);

    #region TryAcquireAsync Tests

    [Fact]
    public async Task TryAcquireAsync_WhenLockAcquired_ReturnsLockHandle()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider();

        // Act
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockAcquired_HandleHasCorrectResource()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider();

        // Act
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None) as ILockHandle;

        // Assert
        handle.ShouldNotBeNull();
        handle.Resource.ShouldBe("test-resource");
        handle.LockId.ShouldNotBeNullOrEmpty();
        handle.IsReleased.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockNotAcquiredAndWaitExpires_ReturnsNull()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var provider = CreateProvider();

        // Act - very short wait so it expires quickly
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(1),
            CancellationToken.None);

        // Assert
        handle.ShouldBeNull();
    }

    #endregion

    #region AcquireAsync Tests

    [Fact]
    public async Task AcquireAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.AcquireAsync("test-resource", TimeSpan.FromMinutes(1), cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_WhenLockAcquired_ReturnsHandle()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider();

        // Act
        var handle = await provider.AcquireAsync("test-resource", TimeSpan.FromMinutes(1), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
    }

    #endregion

    #region IsLockedAsync Tests

    [Fact]
    public async Task IsLockedAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);
        var provider = CreateProvider();

        // Act
        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);
        var provider = CreateProvider();

        // Act
        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.IsLockedAsync("test-resource", cts.Token));
    }

    #endregion

    #region ExtendAsync Tests

    [Fact]
    public async Task ExtendAsync_WhenKeyExpireSucceeds_ReturnsTrue()
    {
        // Arrange
        _database.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(true);
        var provider = CreateProvider();

        // Act
        var result = await provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenKeyExpireFails_ReturnsFalse()
    {
        // Arrange
        _database.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(false);
        var provider = CreateProvider();

        // Act
        var result = await provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), cts.Token));
    }

    #endregion

    #region Key Prefix Tests

    [Fact]
    public async Task TryAcquireAsync_WithKeyPrefix_UsesCorrectKey()
    {
        // Arrange
        RedisKey capturedKey = default;
        _database.StringSetAsync(
                Arg.Do<RedisKey>(k => capturedKey = k), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider(new RedisLockOptions { KeyPrefix = "myapp" });

        // Act
        await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        ((string)capturedKey!).ShouldBe("myapp:lock:test-resource");
    }

    [Fact]
    public async Task TryAcquireAsync_WithoutKeyPrefix_UsesDefaultPrefix()
    {
        // Arrange
        RedisKey capturedKey = default;
        _database.StringSetAsync(
                Arg.Do<RedisKey>(k => capturedKey = k), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<bool>(), Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider();

        // Act
        await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        ((string)capturedKey!).ShouldBe("lock:test-resource");
    }

    #endregion

    #region LockHandle Tests

    [Fact]
    public async Task LockHandle_DisposeAsync_ReleasesLock()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(1));

        var provider = CreateProvider();
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        await handle!.DisposeAsync();

        // Assert
        await _database.Received(1).ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task LockHandle_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create(1));

        var provider = CreateProvider();
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act - should not throw on double dispose
        await handle!.DisposeAsync();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_ExpiresAtUtc_IsSetCorrectly()
    {
        // Arrange
        _database.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<bool>(),
                Arg.Any<When>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = CreateProvider();
        var expiry = TimeSpan.FromMinutes(10);
        var before = DateTime.UtcNow;

        // Act
        var handle = await provider.TryAcquireAsync(
            "test-resource",
            expiry,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None) as ILockHandle;

        // Assert
        var after = DateTime.UtcNow.Add(expiry);
        handle.ShouldNotBeNull();
        handle.ExpiresAtUtc.ShouldBeInRange(before.Add(expiry).AddSeconds(-1), after.AddSeconds(1));
        handle.AcquiredAtUtc.ShouldBeInRange(before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    #endregion

    #region RedisLockOptions Tests

    [Fact]
    public void RedisLockOptions_DefaultDatabase_IsZero()
    {
        var options = new RedisLockOptions();
        options.Database.ShouldBe(0);
    }

    [Fact]
    public void RedisLockOptions_Database_CanBeSet()
    {
        var options = new RedisLockOptions { Database = 5 };
        options.Database.ShouldBe(5);
    }

    [Fact]
    public void RedisLockOptions_InheritsFromDistributedLockOptions()
    {
        var options = new RedisLockOptions();
        options.ShouldBeAssignableTo<DistributedLockOptions>();
    }

    #endregion
}
