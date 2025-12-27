using Encina.DistributedLock.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.DistributedLock.Tests;

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
        lockHandle.Should().NotBeNull();
        await lockHandle!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceAlreadyLocked_ShouldReturnNull()
    {
        // Arrange
        var provider = CreateProvider();
        var resource = "test-resource";

        await using var firstLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var secondLock = await provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMilliseconds(100), // Short wait
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        firstLock.Should().NotBeNull();
        secondLock.Should().BeNull();
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
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
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
        secondLock.Should().NotBeNull();
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
        lockHandle.Should().NotBeNull();
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
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(100),
            CancellationToken.None);

        // Act
        var isLocked = await provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.Should().BeTrue();
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
        isLocked.Should().BeFalse();
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
        isLocked.Should().BeFalse();
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
        extended.Should().BeTrue();
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
        extended.Should().BeFalse();
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
        lockHandle.Should().NotBeNull();
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
        await act.Should().ThrowAsync<OperationCanceledException>();
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
        lock1.Should().NotBeNull();
        lock2.Should().NotBeNull();
        lock3.Should().NotBeNull();

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
        await act.Should().NotThrowAsync();
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
        lockHandle.Should().BeAssignableTo<ILockHandle>();
        var typedHandle = lockHandle as ILockHandle;
        typedHandle!.Resource.Should().Be(resource);
        typedHandle.LockId.Should().NotBeNullOrEmpty();
        typedHandle.IsReleased.Should().BeFalse();

        await lockHandle!.DisposeAsync();
        typedHandle.IsReleased.Should().BeTrue();
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
        extended.Should().BeTrue();
        lockHandle.ExpiresAtUtc.Should().BeAfter(originalExpiry);

        await lockHandle.DisposeAsync();
    }
}
