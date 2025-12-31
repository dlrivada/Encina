using Encina.DistributedLock;

namespace Encina.Caching.ContractTests;

/// <summary>
/// Contract tests that verify all IDistributedLockProvider implementations follow the same behavioral contract.
/// </summary>
public abstract class IDistributedLockProviderContractTests : IAsyncLifetime
{
    protected IDistributedLockProvider Provider { get; private set; } = null!;

    protected abstract IDistributedLockProvider CreateProvider();

    public Task InitializeAsync()
    {
        Provider = CreateProvider();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    #region TryAcquireAsync Contract

    [Fact]
    public async Task TryAcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.TryAcquireAsync(null!, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), CancellationToken.None));
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.TryAcquireAsync("resource", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100), cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_WithUnlockedResource_ReturnsLockHandle()
    {
        // Arrange
        var resource = $"contract-tryacq-{Guid.NewGuid():N}";

        // Act
        var lockHandle = await Provider.TryAcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();

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
        var resource = $"contract-tryacq-locked-{Guid.NewGuid():N}";
        var firstLock = await Provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var secondLock = await Provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(500), // Short wait
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldBeNull();

        // Cleanup
        if (firstLock != null)
        {
            await firstLock.DisposeAsync();
        }
    }

    [Fact]
    public async Task TryAcquireAsync_DisposingLockHandle_ReleasesLock()
    {
        // Arrange
        var resource = $"contract-tryacq-release-{Guid.NewGuid():N}";
        var firstLock = await Provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Release the lock
        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await Provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();

        // Cleanup
        if (secondLock != null)
        {
            await secondLock.DisposeAsync();
        }
    }

    #endregion

    #region AcquireAsync Contract

    [Fact]
    public async Task AcquireAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.AcquireAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task AcquireAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.AcquireAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task AcquireAsync_WithUnlockedResource_ReturnsLockHandle()
    {
        // Arrange
        var resource = $"contract-acq-{Guid.NewGuid():N}";

        // Act
        var lockHandle = await Provider.AcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WithLockedResource_WaitsUntilReleased()
    {
        // Arrange
        var resource = $"contract-acq-wait-{Guid.NewGuid():N}";
        var firstLock = await Provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var acquireStarted = new TaskCompletionSource();
        var acquireTask = Task.Run(async () =>
        {
            acquireStarted.SetResult();
            return await Provider.AcquireAsync(
                resource,
                TimeSpan.FromMinutes(5),
                CancellationToken.None);
        });

        // Wait for second acquire to start attempting acquisition
        await acquireStarted.Task;

        // Verify second acquire is blocked (use WaitAsync with timeout)
        var completedTask = await Task.WhenAny(acquireTask, Task.Delay(TimeSpan.FromMilliseconds(100)));
        var completedInTime = completedTask == acquireTask;
        completedInTime.ShouldBeFalse();

        // Act - Release the first lock
        await firstLock.DisposeAsync();

        // Assert - Second acquire should complete
        var secondLock = await acquireTask;
        secondLock.ShouldNotBeNull();

        // Cleanup
        await secondLock.DisposeAsync();
    }

    #endregion

    #region IsLockedAsync Contract

    [Fact]
    public async Task IsLockedAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.IsLockedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task IsLockedAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.IsLockedAsync("resource", cts.Token));
    }

    [Fact]
    public async Task IsLockedAsync_WithUnlockedResource_ReturnsFalse()
    {
        // Arrange
        var resource = $"contract-islocked-false-{Guid.NewGuid():N}";

        // Act
        var isLocked = await Provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WithLockedResource_ReturnsTrue()
    {
        // Arrange
        var resource = $"contract-islocked-true-{Guid.NewGuid():N}";
        var lockHandle = await Provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Act
        var isLocked = await Provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WithReleasedLock_ReturnsFalse()
    {
        // Arrange
        var resource = $"contract-islocked-released-{Guid.NewGuid():N}";
        var lockHandle = await Provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        await lockHandle.DisposeAsync();

        // Act
        var isLocked = await Provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    #endregion

    #region ExtendAsync Contract

    [Fact]
    public async Task ExtendAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.ExtendAsync(null!, TimeSpan.FromSeconds(30), CancellationToken.None));
    }

    [Fact]
    public async Task ExtendAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.ExtendAsync("resource", TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact]
    public async Task ExtendAsync_WithUnlockedResource_ReturnsFalse()
    {
        // Arrange
        var resource = $"contract-extend-unlocked-{Guid.NewGuid():N}";

        // Act
        var extended = await Provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WithLockedResource_ReturnsTrue()
    {
        // Arrange
        var resource = $"contract-extend-locked-{Guid.NewGuid():N}";
        var lockHandle = await Provider.AcquireAsync(
            resource,
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Act
        var extended = await Provider.ExtendAsync(resource, TimeSpan.FromSeconds(30), CancellationToken.None);

        // Assert
        extended.ShouldBeTrue();

        // Cleanup
        await lockHandle.DisposeAsync();
    }

    #endregion

    #region Concurrent Acquisition Contract

    [Fact]
    public async Task ConcurrentAcquisitions_OnlyOneSucceeds()
    {
        // Arrange
        var resource = $"contract-concurrent-{Guid.NewGuid():N}";
        const int concurrentAttempts = 10;
        var acquiredCount = 0;
        var handles = new List<IAsyncDisposable?>();

        // Act
        var tasks = Enumerable.Range(0, concurrentAttempts)
            .Select(_ => Task.Run(async () =>
            {
                var handle = await Provider.TryAcquireAsync(
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
        acquiredCount.ShouldBe(1);

        // Cleanup
        foreach (var handle in handles.Where(h => h != null))
        {
            await handle!.DisposeAsync();
        }
    }

    #endregion
}

/// <summary>
/// Contract tests for MemoryDistributedLockProvider.
/// </summary>
public sealed class MemoryDistributedLockProviderContractTests : IDistributedLockProviderContractTests
{
    protected override IDistributedLockProvider CreateProvider()
    {
        return new MemoryDistributedLockProvider(NullLogger<MemoryDistributedLockProvider>.Instance);
    }
}
