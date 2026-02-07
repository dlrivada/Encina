using Encina.DistributedLock;
using Encina.DistributedLock.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.DistributedLock;

public class InMemoryDistributedLockProviderTests
{
    private readonly IOptions<InMemoryLockOptions> _options;
    private readonly ILogger<InMemoryDistributedLockProvider> _logger;

    public InMemoryDistributedLockProviderTests()
    {
        _options = Options.Create(new InMemoryLockOptions());
        _logger = NullLogger<InMemoryDistributedLockProvider>.Instance;
    }

    private InMemoryDistributedLockProvider CreateProvider(TimeProvider? timeProvider = null)
    {
        return new InMemoryDistributedLockProvider(_options, _logger, timeProvider);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceNotLocked_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        // Act
        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceAlreadyLocked_ShouldReturnNull()
    {
        // Arrange - use real TimeProvider so the wait deadline actually elapses
        // (FakeTimeProvider would cause an infinite loop since GetUtcNow() never advances
        // but Task.Delay uses real time)
        var provider = CreateProvider();
        var resource = "test-resource";

        await using var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Act - second attempt will fail because resource is already locked
        var secondLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        firstLock.ShouldNotBeNull();
        secondLock.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenLockReleased_ShouldAllowNewAcquisition()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        await firstLock!.DisposeAsync();

        // Act
        var secondLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        secondLock.ShouldNotBeNull();
        await secondLock!.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_WhenResourceNotLocked_ShouldAcquireLock()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        // Act
        var lockHandle = await provider.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceLocked_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        await using var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceNotLocked_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_WhenResourceReleased_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        await lockHandle!.DisposeAsync();

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockHeld_ShouldReturnTrue()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        await using var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var extended = await provider.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        extended.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockNotHeld_ShouldReturnFalse()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        // Act
        var extended = await provider.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WithKeyPrefix_ShouldUsePrefix()
    {
        // Arrange
        var optionsWithPrefix = Options.Create(new InMemoryLockOptions { KeyPrefix = "myapp" });
        var provider = new InMemoryDistributedLockProvider(optionsWithPrefix, _logger);
        var resource = "test-resource";

        // Act
        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldNotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WithCancellation_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";
        using var cts = new CancellationTokenSource();

        await using var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        await cts.CancelAsync();
        var act = () => provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMilliseconds(100),
            cts.Token);

        // Assert
        await Should.ThrowAsync<OperationCanceledException>(async () => await act());
    }

    [Fact]
    public async Task MultipleLocks_OnDifferentResources_ShouldAllSucceed()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var lock1 = await provider.TryAcquireAsync(
            "resource-1",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        var lock2 = await provider.TryAcquireAsync(
            "resource-2",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        var lock3 = await provider.TryAcquireAsync(
            "resource-3",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lock1.ShouldNotBeNull();
        lock2.ShouldNotBeNull();
        lock3.ShouldNotBeNull();

        await lock1!.DisposeAsync();
        await lock2!.DisposeAsync();
        await lock3!.DisposeAsync();
    }

    [Fact]
    public async Task LockHandle_WhenDisposedTwice_ShouldNotThrow()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        await lockHandle!.DisposeAsync();
        var act = async () => await lockHandle.DisposeAsync();

        // Assert
        await Should.NotThrowAsync(async () => await act());
    }

    [Fact]
    public async Task LockHandle_ShouldImplementILockHandle()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        // Act
        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Assert
        lockHandle.ShouldBeAssignableTo<ILockHandle>();
        var typedHandle = lockHandle as ILockHandle;
        typedHandle!.Resource.ShouldBe(resource);
        typedHandle.LockId.ShouldNotBeNullOrEmpty();
        typedHandle.IsReleased.ShouldBeFalse();

        await lockHandle!.DisposeAsync();
        typedHandle.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task LockHandle_ExtendAsync_ShouldExtendExpiry()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        var lockHandle = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None) as ILockHandle;

        var originalExpiry = lockHandle!.ExpiresAtUtc;

        // Act
        var extended = await lockHandle.ExtendAsync(TimeSpan.FromMinutes(10));

        // Assert
        extended.ShouldBeTrue();
        lockHandle.ExpiresAtUtc.ShouldBeGreaterThan(originalExpiry);

        await lockHandle.DisposeAsync();
    }
}
