using Encina.Caching.Redis;
using Encina.Testing.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Encina.UnitTests.Caching.Redis;

/// <summary>
/// Unit tests for <see cref="RedisDistributedLockProvider"/>.
/// </summary>
public sealed class RedisDistributedLockProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly RedisLockOptions _options;
    private readonly ILogger<RedisDistributedLockProvider> _logger;

    public RedisDistributedLockProviderTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _logger = NullLogger<RedisDistributedLockProvider>.Instance;
        _options = new RedisLockOptions
        {
            KeyPrefix = "test",
            Database = 0
        };

        _connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Returns(_database);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(null!, Options.Create(_options), _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connectionMultiplexer, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisDistributedLockProvider(_connectionMultiplexer, Options.Create(_options), null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var provider = CreateProvider();
        provider.ShouldNotBeNull();
    }

    #endregion

    #region TryAcquireAsync Tests

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.TryAcquireAsync(null!, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockUnavailable_ReturnsNullOnceWaitElapses()
    {
        // Arrange
        var time = new FakeTimeProvider(StartTime);
        var provider = CreateProvider(time);
        SetupStringSet(false);

        // Act
        var attempt = provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);
        var pendingBeforeDeadline = attempt.IsCompleted;
        time.Advance(TimeSpan.FromSeconds(5));
        var handle = await attempt;

        // Assert
        pendingBeforeDeadline.ShouldBeFalse();
        handle.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockFreedDuringWait_ReturnsHandleOnNextRetry()
    {
        // Arrange
        var time = new FakeTimeProvider(StartTime);
        var provider = CreateProvider(time);
        SetupStringSet(false, true);

        // Act
        var attempt = provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);
        time.Advance(TimeSpan.FromMilliseconds(100));
        var handle = await attempt;

        // Assert
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenLockAvailable_ReturnsHandle()
    {
        // Arrange
        var provider = CreateProvider(new FakeTimeProvider(StartTime));
        SetupStringSet(true);

        // Act
        var handle = await provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenLockUnavailable_ReturnsNullAfterOneAttempt()
    {
        // Arrange
        var provider = CreateProvider(new FakeTimeProvider(StartTime));
        SetupStringSet(false);

        // Act
        var handle = await provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        handle.ShouldBeNull();
        await _database.Received(1).StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>());
    }

    #endregion

    #region AcquireAsync Tests

    [Fact]
    public async Task AcquireAsync_WhenLockFreedDuringWait_ReturnsHandleOnNextRetry()
    {
        // Arrange
        var time = new FakeTimeProvider(StartTime);
        var provider = CreateProvider(time);
        SetupStringSet(false, true);

        // Act
        var attempt = provider.AcquireAsync("resource", TimeSpan.FromSeconds(30), CancellationToken.None);
        var pendingBeforeRetry = attempt.IsCompleted;
        time.Advance(TimeSpan.FromMilliseconds(100));
        var handle = await attempt;

        // Assert
        pendingBeforeRetry.ShouldBeFalse();
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_WhenReleaseScriptThrows_DoesNotThrow()
    {
        // Arrange
        var logger = new FakeLogger<RedisDistributedLockProvider>();
        var provider = CreateProvider(new FakeTimeProvider(StartTime), logger);
        SetupStringSet(true);
        _database.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisResult>>(_ => throw new RedisConnectionException(ConnectionFailureType.SocketFailure, "connection lost"));
        var handle = await provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act
        handle.ShouldNotBeNull();
        await Should.NotThrowAsync(async () => await handle.DisposeAsync());

        // Assert - the failure is logged by exception type only, never by its message
        var entry = logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Id.Id.ShouldBe(3615);
        entry.Message.ShouldContain("resource: resource ");
        entry.Message.ShouldContain(nameof(RedisConnectionException));
        entry.Message.ShouldNotContain("connection lost");
        (entry.Exception?.Message ?? string.Empty).ShouldNotContain("connection lost");
    }

    [Fact]
    public async Task AcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.AcquireAsync(null!, TimeSpan.FromSeconds(10), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.AcquireAsync("resource", TimeSpan.FromSeconds(10), cts.Token));
    }

    #endregion

    #region IsLockedAsync Tests

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.IsLockedAsync("resource", cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceIsLocked_ReturnsTrue()
    {
        var provider = CreateProvider();

        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceIsNotLocked_ReturnsFalse()
    {
        var provider = CreateProvider();

        _database.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var result = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        result.ShouldBeFalse();
    }

    #endregion

    #region ExtendAsync Tests

    [Fact]
    public async Task ExtendAsync_WithNullResource_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.ExtendAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.ExtendAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_WhenKeyExists_ExtendsPrefixedKeyAndReturnsTrue()
    {
        var provider = CreateProvider();
        _database.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var result = await provider.ExtendAsync("resource", TimeSpan.FromSeconds(30), CancellationToken.None);

        result.ShouldBeTrue();
        await _database.Received(1).KeyExpireAsync((RedisKey)"test:lock:resource", TimeSpan.FromSeconds(30), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ExtendAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var provider = CreateProvider();
        _database.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var result = await provider.ExtendAsync("resource", TimeSpan.FromSeconds(30), CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_CalledTwice_ReleasesOnce()
    {
        var provider = CreateProvider(new FakeTimeProvider(StartTime));
        SetupStringSet(true);
        var handle = await provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        handle.ShouldNotBeNull();

        await handle.DisposeAsync();
        await handle.DisposeAsync();

        await _database.Received(1).ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }

    #endregion

    #region RedisLockOptions Tests

    [Fact]
    public void RedisLockOptions_DefaultValues_AreCorrect()
    {
        var options = new RedisLockOptions();

        options.Database.ShouldBe(0);
        options.KeyPrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void RedisLockOptions_CanSetAllProperties()
    {
        var options = new RedisLockOptions
        {
            Database = 5,
            KeyPrefix = "myapp"
        };

        options.Database.ShouldBe(5);
        options.KeyPrefix.ShouldBe("myapp");
    }

    #endregion

    #region Helper Methods

    private RedisDistributedLockProvider CreateProvider(TimeProvider? timeProvider = null, ILogger<RedisDistributedLockProvider>? logger = null)
    {
        return new RedisDistributedLockProvider(
            _connectionMultiplexer,
            Options.Create(_options),
            logger ?? _logger,
            timeProvider);
    }

    private void SetupStringSet(bool first, params bool[] next)
    {
        _database.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(first, next);
    }

    #endregion
}
