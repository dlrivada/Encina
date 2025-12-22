namespace Encina.Caching.Memory.Tests;

/// <summary>
/// Unit tests for <see cref="MemoryDistributedLockProvider"/>.
/// </summary>
public sealed class MemoryDistributedLockProviderTests
{
    private readonly MemoryDistributedLockProvider _sut;

    public MemoryDistributedLockProviderTests()
    {
        _sut = new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new MemoryDistributedLockProvider(null!));
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_WithUnlockedResource_AcquiresLock()
    {
        // Act
        var lockHandle = await _sut.TryAcquireAsync(
            "unlocked-resource",
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();

        // Cleanup
        if (lockHandle != null)
        {
            await lockHandle.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WithLockedResource_ReturnsNullAfterWait()
    {
        // Arrange
        var firstLock = await _sut.TryAcquireAsync(
            "locked-resource",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var secondLock = await _sut.TryAcquireAsync(
            "locked-resource",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(500), // Short wait time
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        firstLock.Should().NotBeNull();
        secondLock.Should().BeNull();

        // Cleanup
        if (firstLock != null)
        {
            await firstLock.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_WithExpiredLock_AcquiresLock()
    {
        // Arrange
        var firstLock = await _sut.TryAcquireAsync(
            "expiring-resource",
            TimeSpan.FromMilliseconds(100), // Short expiry
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Wait for lock to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Act
        var secondLock = await _sut.TryAcquireAsync(
            "expiring-resource",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        secondLock.Should().NotBeNull();

        // Cleanup
        if (secondLock != null)
        {
            await secondLock.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_ReleasingLock_AllowsNewAcquisition()
    {
        // Arrange
        var firstLock = await _sut.TryAcquireAsync(
            "release-test-resource",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Release the lock
        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await _sut.TryAcquireAsync(
            "release-test-resource",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        secondLock.Should().NotBeNull();

        // Cleanup
        if (secondLock != null)
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.AcquireAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_WithUnlockedResource_AcquiresImmediately()
    {
        // Act
        var lockHandle = await _sut.AcquireAsync(
            "acquire-unlocked",
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Assert
        lockHandle.Should().NotBeNull();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WithLockedResource_WaitsUntilReleased()
    {
        // Arrange
        var firstLock = await _sut.AcquireAsync(
            "acquire-wait",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var acquireTask = Task.Run(async () =>
        {
            return await _sut.AcquireAsync(
                "acquire-wait",
                TimeSpan.FromMinutes(5),
                CancellationToken.None);
        });

        // Wait a bit to ensure second acquire is waiting
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        acquireTask.IsCompleted.Should().BeFalse();

        // Act - Release the first lock
        await firstLock.DisposeAsync();

        // Assert - Second acquire should complete
        var secondLock = await acquireTask;
        secondLock.Should().NotBeNull();

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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.IsLockedAsync("resource", cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WithUnlockedResource_ReturnsFalse()
    {
        // Act
        var isLocked = await _sut.IsLockedAsync("is-unlocked", CancellationToken.None);

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WithLockedResource_ReturnsTrue()
    {
        // Arrange
        var lockHandle = await _sut.AcquireAsync(
            "is-locked",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Act
        var isLocked = await _sut.IsLockedAsync("is-locked", CancellationToken.None);

        // Assert
        isLocked.Should().BeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WithExpiredLock_ReturnsFalse()
    {
        // Arrange
        await _sut.AcquireAsync(
            "is-expired",
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Wait for lock to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Act
        var isLocked = await _sut.IsLockedAsync("is-expired", CancellationToken.None);

        // Assert
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WithReleasedLock_ReturnsFalse()
    {
        // Arrange
        var lockHandle = await _sut.AcquireAsync(
            "is-released",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        await lockHandle.DisposeAsync();

        // Act
        var isLocked = await _sut.IsLockedAsync("is-released", CancellationToken.None);

        // Assert
        isLocked.Should().BeFalse();
    }

    #endregion

    #region ExtendAsync Tests

    [Fact]
    public async Task ExtendAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("resource", () =>
            _sut.ExtendAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExtendAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_WithUnlockedResource_ReturnsFalse()
    {
        // Act
        var extended = await _sut.ExtendAsync("extend-unlocked", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        extended.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WithLockedResource_ReturnsTrue()
    {
        // Arrange
        var lockHandle = await _sut.AcquireAsync(
            "extend-locked",
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Act
        var extended = await _sut.ExtendAsync("extend-locked", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        extended.Should().BeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task ExtendAsync_WithExpiredLock_ReturnsFalse()
    {
        // Arrange
        await _sut.AcquireAsync(
            "extend-expired",
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Wait for lock to expire
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Act
        var extended = await _sut.ExtendAsync("extend-expired", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        extended.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendAsync_ExtendsLockDuration()
    {
        // Arrange
        var lockHandle = await _sut.AcquireAsync(
            "extend-duration",
            TimeSpan.FromMilliseconds(200),
            CancellationToken.None);

        // Act - Extend before expiry
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        var extended = await _sut.ExtendAsync("extend-duration", TimeSpan.FromSeconds(30), CancellationToken.None);

        // Wait past original expiry
        await Task.Delay(TimeSpan.FromMilliseconds(150));

        // Assert - Lock should still be held
        var isLocked = await _sut.IsLockedAsync("extend-duration", CancellationToken.None);
        extended.Should().BeTrue();
        isLocked.Should().BeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ConcurrentAcquisitions_OnlyOneSucceeds()
    {
        // Arrange
        const string resource = "concurrent-resource";
        const int concurrentAttempts = 10;
        var acquiredCount = 0;
        var handles = new List<IAsyncDisposable?>();

        // Act
        var tasks = Enumerable.Range(0, concurrentAttempts)
            .Select(_ => Task.Run(async () =>
            {
                var handle = await _sut.TryAcquireAsync(
                    resource,
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(10),
                    CancellationToken.None);

                if (handle != null)
                {
                    Interlocked.Increment(ref acquiredCount);
                }

                return handle;
            }))
            .ToList();

        handles.AddRange(await Task.WhenAll(tasks));

        // Assert
        acquiredCount.Should().Be(1);

        // Cleanup
        foreach (var handle in handles.Where(h => h != null))
        {
            await handle!.DisposeAsync();
        }
    }

    [Fact]
    public async Task LockHandle_DoubleDispose_DoesNotThrow()
    {
        // Arrange
        var lockHandle = await _sut.AcquireAsync(
            "double-dispose",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Act & Assert
        await lockHandle.DisposeAsync();
        await lockHandle.Invoking(h => h.DisposeAsync().AsTask())
            .Should().NotThrowAsync();
    }

    #endregion
}
