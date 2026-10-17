using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Encina.Testing.Time;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using StackExchange.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Unit tests for <see cref="RedisDistributedLockProvider"/>.
/// </summary>
public class RedisDistributedLockProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

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

    #region TryAcquireAsync — Success/Failure Paths

    [Fact]
    public async Task TryAcquireAsync_WhenLockAvailable_ReturnsHandle()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var handle = await provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockUnavailable_ReturnsNull()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false);

        var time = new FakeTimeProvider(StartTime);
        var provider = new RedisDistributedLockProvider(connection, _options, _logger, time);

        // Act — the wait runs on the fake clock, so advancing it ends the attempt (#531)
        var attempt = provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);
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
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false, true);

        var time = new FakeTimeProvider(StartTime);
        var provider = new RedisDistributedLockProvider(connection, _options, _logger, time);

        // Act
        var attempt = provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);
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
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger, new FakeTimeProvider(StartTime));

        // Act
        var handle = await provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenLockUnavailable_ReturnsNullAfterOneAttempt()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger, new FakeTimeProvider(StartTime));

        // Act
        var handle = await provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Assert
        handle.ShouldBeNull();
        await db.Received(1).StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
            Arg.Any<When>());
    }

    [Fact]
    public async Task AcquireAsync_WhenLockFreedDuringWait_ReturnsHandleOnNextRetry()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false, true);

        var time = new FakeTimeProvider(StartTime);
        var provider = new RedisDistributedLockProvider(connection, _options, _logger, time);

        // Act
        var attempt = provider.AcquireAsync("res", TimeSpan.FromSeconds(30), CancellationToken.None);
        var pendingBeforeRetry = attempt.IsCompleted;
        time.Advance(TimeSpan.FromMilliseconds(100));
        var handle = await attempt;

        // Assert
        pendingBeforeRetry.ShouldBeFalse();
        handle.ShouldNotBeNull();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelledWhileWaiting_ThrowsOperationCanceledException()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(false);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger, new FakeTimeProvider(StartTime));
        using var cts = new CancellationTokenSource();
        var attempt = provider.AcquireAsync("res", TimeSpan.FromSeconds(30), cts.Token);

        // Act
        await cts.CancelAsync();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => attempt);
    }

    [Fact]
    public async Task TryAcquireAsync_WithKeyPrefix_FormatIsCorrect()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisKey capturedKey = default;
        db.StringSetAsync(Arg.Do<RedisKey>(k => capturedKey = k), Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        var opts = Options.Create(new RedisLockOptions { KeyPrefix = "myapp" });
        var provider = new RedisDistributedLockProvider(connection, opts, _logger);

        // Act
        var handle = await provider.TryAcquireAsync("resource-1", TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        ((string?)capturedKey).ShouldBe("myapp:lock:resource-1");
        await handle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithoutPrefix_UsesDefaultFormat()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);

        RedisKey capturedKey = default;
        db.StringSetAsync(Arg.Do<RedisKey>(k => capturedKey = k), Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var handle = await provider.TryAcquireAsync("resource-1", TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        ((string?)capturedKey).ShouldBe("lock:resource-1");
        await handle!.DisposeAsync();
    }

    #endregion

    #region IsLockedAsync — Success Paths

    [Fact]
    public async Task IsLockedAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var result = await provider.IsLockedAsync("res", CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenKeyNotExists_ReturnsFalse()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(false);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var result = await provider.IsLockedAsync("res", CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region ExtendAsync — Success Paths

    [Fact]
    public async Task ExtendAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var result = await provider.ExtendAsync("res", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenKeyNotExists_ReturnsFalse()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.KeyExpireAsync(Arg.Any<RedisKey>(), Arg.Any<TimeSpan?>(), Arg.Any<ExpireWhen>(), Arg.Any<CommandFlags>())
            .Returns(false);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var result = await provider.ExtendAsync("res", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region LockHandle — via TryAcquireAsync

    [Fact]
    public async Task LockHandle_HasExpectedProperties()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);

        // Act
        var handle = await provider.TryAcquireAsync("my-res", TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50), CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        var lockHandle = handle as ILockHandle;
        lockHandle.ShouldNotBeNull();
        lockHandle!.Resource.ShouldBe("my-res");
        lockHandle.LockId.ShouldNotBeNullOrWhiteSpace();
        lockHandle.IsReleased.ShouldBeFalse();
        lockHandle.AcquiredAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        lockHandle.ExpiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow);

        await handle!.DisposeAsync();
        lockHandle.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task LockHandle_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var db = Substitute.For<IDatabase>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Returns(true);

        var provider = new RedisDistributedLockProvider(connection, _options, _logger);
        var handle = await provider.TryAcquireAsync("res", TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(50), CancellationToken.None);

        // Act & Assert — double dispose should not throw
        await handle!.DisposeAsync();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_WhenScriptReturnsOne_ReturnsTrueAndMovesExpiry()
    {
        // Arrange
        var db = CreateDatabaseThatGrantsLocks();
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create((RedisValue)1));
        var time = new FakeTimeProvider(StartTime);
        var handle = await AcquireHandleAsync(db, time);
        time.Advance(TimeSpan.FromSeconds(5));

        // Act
        var extended = await handle.ExtendAsync(TimeSpan.FromMinutes(2));

        // Assert
        extended.ShouldBeTrue();
        handle.ExpiresAtUtc.ShouldBe(StartTime.AddSeconds(5).AddMinutes(2).UtcDateTime);
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_WhenScriptReturnsZero_ReturnsFalseAndKeepsExpiry()
    {
        // Arrange
        var db = CreateDatabaseThatGrantsLocks();
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(RedisResult.Create((RedisValue)0));
        var handle = await AcquireHandleAsync(db, new FakeTimeProvider(StartTime));
        var originalExpiry = handle.ExpiresAtUtc;

        // Act
        var extended = await handle.ExtendAsync(TimeSpan.FromMinutes(2));

        // Assert
        extended.ShouldBeFalse();
        handle.ExpiresAtUtc.ShouldBe(originalExpiry);
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_AfterDispose_ReturnsFalseWithoutCallingRedis()
    {
        // Arrange
        var db = CreateDatabaseThatGrantsLocks();
        var handle = await AcquireHandleAsync(db, new FakeTimeProvider(StartTime));
        await handle.DisposeAsync();
        db.ClearReceivedCalls();

        // Act
        var extended = await handle.ExtendAsync(TimeSpan.FromMinutes(2));

        // Assert
        extended.ShouldBeFalse();
        await db.DidNotReceive().ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task LockHandle_DisposeAsync_WhenReleaseScriptThrows_MarksReleasedWithoutThrowing()
    {
        // Arrange
        var db = CreateDatabaseThatGrantsLocks();
        db.ScriptEvaluateAsync(Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns<Task<RedisResult>>(_ => throw new RedisConnectionException(ConnectionFailureType.SocketFailure, "connection lost"));
        var logger = new FakeLogger<RedisDistributedLockProvider>();
        var handle = await AcquireHandleAsync(db, new FakeTimeProvider(StartTime), logger);

        // Act
        await Should.NotThrowAsync(async () => await handle.DisposeAsync());

        // Assert - released, and the failure is logged by exception type only, never by its message
        handle.IsReleased.ShouldBeTrue();
        var entry = logger.Collector.GetSnapshot().Single(r => r.Level == LogLevel.Warning);
        entry.Id.Id.ShouldBe(3754);
        entry.Message.ShouldContain(nameof(RedisConnectionException));
        entry.Message.ShouldNotContain("connection lost");
        (entry.Exception?.Message ?? string.Empty).ShouldNotContain("connection lost");
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroRetry_WaitsOnTheClockInsteadOfSpinning()
    {
        // Arrange
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(false);
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var time = new FakeTimeProvider(StartTime);
        var provider = new RedisDistributedLockProvider(connection, _options, _logger, time);

        // Act - a zero retry is floored to 1 ms on the fake clock, so the call waits for the clock
        var attempt = provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(1), TimeSpan.Zero, CancellationToken.None);
        var pendingOnFrozenClock = attempt.IsCompleted;
        time.Advance(TimeSpan.FromMilliseconds(1));
        var attemptsAfterOneStep = db.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        time.Advance(TimeSpan.FromSeconds(1));
        var handle = await attempt;

        // Assert
        pendingOnFrozenClock.ShouldBeFalse();
        attemptsAfterOneStep.ShouldBe(2);
        handle.ShouldBeNull();
    }

    private static IDatabase CreateDatabaseThatGrantsLocks()
    {
        var db = Substitute.For<IDatabase>();
        db.StringSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);
        return db;
    }

    private async Task<ILockHandle> AcquireHandleAsync(IDatabase db, TimeProvider time, ILogger<RedisDistributedLockProvider>? logger = null)
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(db);
        var provider = new RedisDistributedLockProvider(connection, _options, logger ?? _logger, time);

        var handle = await provider.TryAcquireAsync("res", TimeSpan.FromSeconds(30),
            TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        return handle.ShouldBeAssignableTo<ILockHandle>()!;
    }

    #endregion

    #region Constructor — Optional TimeProvider

    [Fact]
    public void Constructor_WithCustomTimeProvider_AcceptsIt()
    {
        // Arrange
        var connection = Substitute.For<IConnectionMultiplexer>();
        var tp = Substitute.For<TimeProvider>();
        tp.GetUtcNow().Returns(DateTimeOffset.UtcNow);

        // Act
        var provider = new RedisDistributedLockProvider(connection, _options, _logger, tp);

        // Assert
        provider.ShouldNotBeNull();
    }

    #endregion
}
