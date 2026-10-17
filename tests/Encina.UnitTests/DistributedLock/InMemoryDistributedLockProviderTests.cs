using Encina.DistributedLock;
using Encina.DistributedLock.InMemory;
using Encina.Testing.Time;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.DistributedLock;

/// <summary>
/// Unit tests for <see cref="InMemoryDistributedLockProvider"/>.
/// </summary>
/// <remarks>
/// Every wait runs on a <see cref="FakeTimeProvider"/>: the provider's deadline and its retry delay
/// both read the injected clock, so the tests advance time instead of waiting on the wall clock (#531).
/// </remarks>
public class InMemoryDistributedLockProviderTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Retry = TimeSpan.FromMilliseconds(100);

    private readonly IOptions<InMemoryLockOptions> _options = Options.Create(new InMemoryLockOptions());
    private readonly ILogger<InMemoryDistributedLockProvider> _logger = NullLogger<InMemoryDistributedLockProvider>.Instance;
    private readonly FakeTimeProvider _time = new(StartTime);

    private InMemoryDistributedLockProvider CreateProvider() => new(_options, _logger, _time);

    [Fact]
    public async Task TryAcquireAsync_WhenResourceNotLocked_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceAlreadyLocked_ShouldReturnNullOnceWaitElapses()
    {
        // Arrange
        var provider = CreateProvider();
        await using var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        var pendingBeforeDeadline = secondAttempt.IsCompleted;
        _time.Advance(Wait);
        var secondLock = await secondAttempt;

        // Assert
        firstLock.ShouldNotBeNull();
        pendingBeforeDeadline.ShouldBeFalse();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockReleasedDuringWait_ShouldAcquireOnNextRetry()
    {
        // Arrange
        var provider = CreateProvider();
        var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        await firstLock!.DisposeAsync();
        _time.Advance(Retry);
        var secondLock = await secondAttempt;

        // Assert
        secondLock.ShouldNotBeNull();
        await secondLock.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenResourceNotLocked_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.Zero, Retry, CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroWait_WhenResourceLocked_ShouldReturnNullWithoutWaiting()
    {
        // Arrange
        var provider = CreateProvider();
        await using var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.Zero, Retry, CancellationToken.None);

        // Assert
        secondAttempt.IsCompleted.ShouldBeTrue();
        (await secondAttempt).ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenRetryIsLongerThanWait_ShouldGiveUpAtTheDeadline()
    {
        // Arrange
        var provider = CreateProvider();
        await using var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act - the retry interval is clamped to the remaining wait, so advancing by the wait ends the attempt
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(10), CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(1));
        var secondLock = await secondAttempt;

        // Assert
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WithZeroRetry_ShouldWaitOnTheClockInsteadOfSpinning()
    {
        // Arrange
        var provider = CreateProvider();
        await using var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act - a zero retry is floored to 1 ms on the fake clock, so the call stays pending until the clock moves
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.FromSeconds(1), TimeSpan.Zero, CancellationToken.None);
        var pendingOnFrozenClock = secondAttempt.IsCompleted;
        _time.Advance(TimeSpan.FromSeconds(1));
        var secondLock = await secondAttempt;

        // Assert
        pendingOnFrozenClock.ShouldBeFalse();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenHeldLockHasExpired_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();
        await using var expiredLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        _time.Advance(Expiry);

        // Act
        var newLock = await provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.Zero, Retry, CancellationToken.None);

        // Assert
        newLock.ShouldNotBeNull();
        await newLock.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockReleased_ShouldAllowNewAcquisition()
    {
        // Arrange
        var provider = CreateProvider();
        var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();
        await secondLock.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, cts.Token));
    }

    [Fact]
    public async Task TryAcquireAsync_WhenCancelledWhileWaiting_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        await using var firstLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        var secondAttempt = provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, cts.Token);

        // Act
        await cts.CancelAsync();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => secondAttempt);
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceNotLocked_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lockHandle = await provider.AcquireAsync("test-resource", Expiry, CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceLocked_ShouldWaitUntilReleased()
    {
        // Arrange
        var provider = CreateProvider();
        var firstLock = await provider.AcquireAsync("test-resource", Expiry, CancellationToken.None);
        var secondAttempt = provider.AcquireAsync("test-resource", Expiry, CancellationToken.None);

        // Act
        _time.Advance(TimeSpan.FromMilliseconds(100));
        var pendingWhileHeld = secondAttempt.IsCompleted;
        await firstLock.DisposeAsync();
        _time.Advance(TimeSpan.FromMilliseconds(100));
        var secondLock = await secondAttempt;

        // Assert
        pendingWhileHeld.ShouldBeFalse();
        secondLock.ShouldNotBeNull();
        await secondLock.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenCancelledWhileWaiting_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        await using var firstLock = await provider.AcquireAsync("test-resource", Expiry, CancellationToken.None);
        var secondAttempt = provider.AcquireAsync("test-resource", Expiry, cts.Token);

        // Act
        await cts.CancelAsync();

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(() => secondAttempt);
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();
        await using var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        var isLocked = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceNotLocked_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var isLocked = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceReleased_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        await lockHandle!.DisposeAsync();

        // Act
        var isLocked = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenLockExpired_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        await using var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        _time.Advance(Expiry);

        // Act
        var isLocked = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockHeld_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();
        await using var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        var extended = await provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        extended.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockHeld_ShouldKeepTheLockPastTheOriginalExpiry()
    {
        // Arrange
        var provider = CreateProvider();
        await using var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        await provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        _time.Advance(Expiry);
        var isLocked = await provider.IsLockedAsync("test-resource", CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockNotHeld_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var extended = await provider.ExtendAsync("test-resource", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WithKeyPrefix_ShouldUsePrefix()
    {
        // Arrange
        var optionsWithPrefix = Options.Create(new InMemoryLockOptions { KeyPrefix = "myapp" });
        var provider = new InMemoryDistributedLockProvider(optionsWithPrefix, _logger, _time);

        // Act
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        (await provider.IsLockedAsync("test-resource", CancellationToken.None)).ShouldBeTrue();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task MultipleLocks_OnDifferentResources_ShouldAllSucceed()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lock1 = await provider.TryAcquireAsync("resource-1", Expiry, Wait, Retry, CancellationToken.None);
        var lock2 = await provider.TryAcquireAsync("resource-2", Expiry, Wait, Retry, CancellationToken.None);
        var lock3 = await provider.TryAcquireAsync("resource-3", Expiry, Wait, Retry, CancellationToken.None);

        // Assert
        lock1.ShouldNotBeNull();
        lock2.ShouldNotBeNull();
        lock3.ShouldNotBeNull();

        await lock1.DisposeAsync();
        await lock2.DisposeAsync();
        await lock3.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_WhenDisposedTwice_ShouldNotThrow()
    {
        // Arrange
        var provider = CreateProvider();
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Act
        await lockHandle!.DisposeAsync();

        // Assert
        await Should.NotThrowAsync(async () => await lockHandle.DisposeAsync());
    }

    [Fact]
    public async Task LockHandle_WhenExpiredLockIsDisposed_ShouldNotReleaseTheNewOwnersLock()
    {
        // Arrange
        var provider = CreateProvider();
        var expiredLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);
        _time.Advance(Expiry);
        await using var newLock = await provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.Zero, Retry, CancellationToken.None);

        // Act
        await expiredLock!.DisposeAsync();

        // Assert
        newLock.ShouldNotBeNull();
        (await provider.IsLockedAsync("test-resource", CancellationToken.None)).ShouldBeTrue();
    }

    [Fact]
    public async Task LockHandle_ShouldExposeResourceLockIdAndUtcTimes()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None);

        // Assert
        var typedHandle = lockHandle.ShouldBeAssignableTo<ILockHandle>();
        typedHandle!.Resource.ShouldBe("test-resource");
        typedHandle.LockId.ShouldNotBeNullOrEmpty();
        typedHandle.IsReleased.ShouldBeFalse();
        typedHandle.AcquiredAtUtc.ShouldBe(StartTime.UtcDateTime);
        typedHandle.AcquiredAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        typedHandle.ExpiresAtUtc.ShouldBe(StartTime.Add(Expiry).UtcDateTime);

        await typedHandle.DisposeAsync();
        typedHandle.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_ShouldExtendExpiry()
    {
        // Arrange
        var provider = CreateProvider();
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None) as ILockHandle;
        var originalExpiry = lockHandle!.ExpiresAtUtc;

        // Act
        var extended = await lockHandle.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        extended.ShouldBeTrue();
        lockHandle.ExpiresAtUtc.ShouldBeGreaterThan(originalExpiry);

        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_AfterLockExpiredAndWasTakenOver_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var expiredLock = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None) as ILockHandle;
        _time.Advance(Expiry);
        await using var newLock = await provider.TryAcquireAsync("test-resource", Expiry, TimeSpan.Zero, Retry, CancellationToken.None);

        // Act
        var extended = await expiredLock!.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        newLock.ShouldNotBeNull();
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_AfterDispose_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var lockHandle = await provider.TryAcquireAsync("test-resource", Expiry, Wait, Retry, CancellationToken.None) as ILockHandle;
        await lockHandle!.DisposeAsync();

        // Act
        var extended = await lockHandle.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        extended.ShouldBeFalse();
    }
}
