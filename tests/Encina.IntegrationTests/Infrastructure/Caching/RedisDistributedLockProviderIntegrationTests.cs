using Encina.Caching.Redis;
using Encina.IntegrationTests.Infrastructure.Caching.Fixtures;

namespace Encina.IntegrationTests.Infrastructure.Caching;

/// <summary>
/// Integration tests for RedisDistributedLockProvider using a real Redis container.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public class RedisDistributedLockProviderIntegrationTests : IAsyncLifetime
{
    private readonly RedisFixture _fixture;
    private RedisDistributedLockProvider? _provider;

    public RedisDistributedLockProviderIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        var options = Options.Create(new RedisLockOptions
        {
            KeyPrefix = $"lock:test:{Guid.NewGuid():N}"
        });

        _provider = new RedisDistributedLockProvider(
            _fixture.Connection!,
            options,
            NullLogger<RedisDistributedLockProvider>.Instance);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AcquireAsync_SuccessfullyAcquiresLock()
    {

        // Arrange
        var resource = $"test-resource-{Guid.NewGuid():N}";

        // Act
        await using var handle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var isLocked = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        handle.ShouldNotBeNull();
        isLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_ReleaseLock_ResourceBecomesUnlocked()
    {

        // Arrange
        var resource = $"release-test-{Guid.NewGuid():N}";

        // Act
        var handle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var lockedWhileHeld = await _provider.IsLockedAsync(resource, CancellationToken.None);

        await handle.DisposeAsync();

        var lockedAfterRelease = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        lockedWhileHeld.ShouldBeTrue();
        lockedAfterRelease.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenResourceLocked_ReturnsNull()
    {

        // Arrange
        var resource = $"contention-test-{Guid.NewGuid():N}";

        // Act - First acquire succeeds
        await using var firstHandle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Second acquire should fail quickly
        var secondHandle = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        firstHandle.ShouldNotBeNull();
        secondHandle.ShouldBeNull();
    }

    [Fact]
    public async Task TryAcquireAsync_AfterLockReleased_Succeeds()
    {

        // Arrange
        var resource = $"reacquire-test-{Guid.NewGuid():N}";

        // Act - First acquire and release
        var firstHandle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);
        await firstHandle.DisposeAsync();

        // Second acquire should succeed
        await using var secondHandle = await _provider.TryAcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        secondHandle.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockHeld_ExtendsLock()
    {

        // Arrange
        var resource = $"extend-test-{Guid.NewGuid():N}";

        // Act
        await using var handle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromSeconds(5),
            CancellationToken.None);

        var extended = await _provider.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        extended.ShouldBeTrue();
    }

    [Fact]
    public async Task ExtendAsync_WhenLockNotHeld_ReturnsFalse()
    {

        // Arrange
        var resource = $"extend-notheld-{Guid.NewGuid():N}";

        // Act
        var extended = await _provider!.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        extended.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLockedAsync_NonExistentResource_ReturnsFalse()
    {

        // Arrange
        var resource = $"nonexistent-{Guid.NewGuid():N}";

        // Act
        var isLocked = await _provider!.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task LockExpires_AfterTimeout()
    {

        // Arrange
        var resource = $"expire-test-{Guid.NewGuid():N}";
        var shortExpiry = TimeSpan.FromSeconds(2);

        // Act - Acquire lock with short expiry (don't release)
        var handle = await _provider!.AcquireAsync(
            resource,
            shortExpiry,
            CancellationToken.None);

        var lockedImmediately = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Don't dispose - let it expire
        await Task.Delay(TimeSpan.FromSeconds(3));

        var lockedAfterExpiry = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        lockedImmediately.ShouldBeTrue();
        lockedAfterExpiry.ShouldBeFalse();

        // Clean up the handle anyway
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task DifferentResources_CanBeLocked_Simultaneously()
    {

        // Arrange
        var resource1 = $"resource-1-{Guid.NewGuid():N}";
        var resource2 = $"resource-2-{Guid.NewGuid():N}";

        // Act
        await using var handle1 = await _provider!.AcquireAsync(
            resource1,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        await using var handle2 = await _provider.AcquireAsync(
            resource2,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var resource1Locked = await _provider.IsLockedAsync(resource1, CancellationToken.None);
        var resource2Locked = await _provider.IsLockedAsync(resource2, CancellationToken.None);

        // Assert
        handle1.ShouldNotBeNull();
        handle2.ShouldNotBeNull();
        resource1Locked.ShouldBeTrue();
        resource2Locked.ShouldBeTrue();
    }
}
