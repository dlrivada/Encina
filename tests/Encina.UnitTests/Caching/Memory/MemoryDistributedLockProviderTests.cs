using Bogus;
using Encina.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Caching.Memory;

/// <summary>
/// Unit tests for <see cref="MemoryDistributedLockProvider"/>.
/// </summary>
public sealed class MemoryDistributedLockProviderTests
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly MemoryDistributedLockProvider _sut;
    private readonly Faker _faker;

    public MemoryDistributedLockProviderTests()
    {
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        _sut = new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance, _timeProvider);
        _faker = new Faker();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new MemoryDistributedLockProvider(null!));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("timeProvider", () =>
            new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance, null!));
    }

    [Fact]
    public void Constructor_WithLoggerOnly_UsesSystemTimeProvider()
    {
        // Act
        var provider = new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);

        // Assert
        provider.ShouldNotBeNull();
    }

    #endregion

    #region TryAcquireAsync Tests

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("resource", () =>
            _sut.TryAcquireAsync(null!, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.TryAcquireAsync(resource, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceNotLocked_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act
        var lockHandle = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();

        // Cleanup
        if (lockHandle is not null)
        {
            await lockHandle.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceAlreadyLocked_ReturnsNullAfterTimeout()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var task = _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Advance time past wait period
        _timeProvider.Advance(TimeSpan.FromMilliseconds(300));

        var secondLock = await task;

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldBeNull();

        // Cleanup
        if (firstLock is not null)
        {
            await firstLock.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockExpires_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Advance time past expiry
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // Act
        var secondLock = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldNotBeNull();

        // Cleanup
        if (secondLock is not null)
        {
            await secondLock.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockReleased_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Release the first lock
        if (firstLock is not null)
        {
            await firstLock.DisposeAsync();
        }

        // Act
        var secondLock = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();

        // Cleanup
        if (secondLock is not null)
        {
            await secondLock.DisposeAsync();
        }
    }

    #endregion

    #region AcquireAsync Tests

    [Fact]
    public async Task AcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("resource", () =>
            _sut.AcquireAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.AcquireAsync(resource, TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceNotLocked_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenLockReleased_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);
        await firstLock.DisposeAsync();

        // Act
        var secondLock = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();

        // Cleanup
        await secondLock.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenLockExpires_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.AcquireAsync(resource, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Advance time past expiry
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // Act
        var secondLock = await _sut.AcquireAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();

        // Cleanup
        await secondLock.DisposeAsync();
    }

    #endregion

    #region IsLockedAsync Tests

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("resource", () =>
            _sut.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.IsLockedAsync(resource, cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceNotLocked_ReturnsFalse()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ReturnsTrue()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WhenLockExpired_ReturnsFalse()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Advance time past expiry
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // Act
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenLockReleased_ReturnsFalse()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);
        await lockHandle.DisposeAsync();

        // Act
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    #endregion

    #region ExtendAsync Tests

    [Fact]
    public async Task ExtendAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("resource", () =>
            _sut.ExtendAsync(null!, TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExtendAsync(resource, TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_WhenResourceNotLocked_ReturnsFalse()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act
        var extended = await _sut.ExtendAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WhenResourceLocked_ReturnsTrue()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var extended = await _sut.ExtendAsync(resource, TimeSpan.FromMinutes(10), CancellationToken.None);

        // Assert
        extended.ShouldBeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockExpired_ReturnsFalse()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Advance time past expiry
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // Act
        var extended = await _sut.ExtendAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_ExtendsLockDuration()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Extend the lock
        await _sut.ExtendAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Advance time past original expiry but before extension
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));

        // Act
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    #endregion

    #region LockHandle Tests

    [Fact]
    public async Task LockHandle_WhenDisposed_ReleasesLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await lockHandle.DisposeAsync();

        // Assert
        var isLocked = await _sut.IsLockedAsync(resource, CancellationToken.None);
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task LockHandle_WhenDisposedMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var lockHandle = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await lockHandle.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () => await lockHandle.DisposeAsync());

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region Concurrent Access Tests

    [Fact]
    public async Task TryAcquireAsync_ConcurrentRequests_OnlyOneAcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act - ten parallel zero-wait attempts; every winner keeps its handle until all have finished
        var handles = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(1),
            CancellationToken.None))));

        // Assert - mutual exclusion: exactly one attempt holds the lock
        handles.Count(h => h is not null).ShouldBe(1);

        foreach (var handle in handles.Where(h => h is not null))
        {
            await handle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenResourceFree_AcquiresLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();

        // Act
        var lockHandle = await _sut.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockReleasedDuringWait_AcquiresOnNextRetry()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        var secondAttempt = _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act
        var pendingWhileHeld = secondAttempt.IsCompleted;
        await firstLock!.DisposeAsync();
        _timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var secondLock = await secondAttempt;

        // Assert
        pendingWhileHeld.ShouldBeFalse();
        secondLock.ShouldNotBeNull();
        await secondLock.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroRetry_WaitsOnTheClockInsteadOfSpinning()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        await using var firstLock = await _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act - a zero retry is floored to 1 ms on the fake clock, so the call stays pending until the clock moves
        var secondAttempt = _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(1), TimeSpan.Zero, CancellationToken.None);
        var pendingOnFrozenClock = secondAttempt.IsCompleted;
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        var secondLock = await secondAttempt;

        // Assert
        pendingOnFrozenClock.ShouldBeFalse();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task LockHandle_WhenExpiredHandleIsDisposedAfterTakeover_KeepsTheNewOwnersLock()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var expiredLock = await _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(1), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await using var newLock = await _sut.TryAcquireAsync(
            resource, TimeSpan.FromMinutes(5), TimeSpan.Zero, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act
        await expiredLock!.DisposeAsync();

        // Assert
        newLock.ShouldNotBeNull();
        (await _sut.IsLockedAsync(resource, CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceLocked_WaitsUntilReleased()
    {
        // Arrange
        var resource = _faker.Lorem.Word();
        var firstLock = await _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);
        var secondAttempt = _sut.AcquireAsync(resource, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        _timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var pendingWhileHeld = secondAttempt.IsCompleted;
        await firstLock.DisposeAsync();
        _timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        var secondLock = await secondAttempt;

        // Assert
        pendingWhileHeld.ShouldBeFalse();
        secondLock.ShouldNotBeNull();
        await secondLock.DisposeAsync();
    }

    #endregion
}
