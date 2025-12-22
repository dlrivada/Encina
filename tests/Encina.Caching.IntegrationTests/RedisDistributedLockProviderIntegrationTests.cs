using Encina.Caching.IntegrationTests.Fixtures;

namespace Encina.Caching.IntegrationTests;

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

    [SkippableFact]
    public async Task AcquireAsync_SuccessfullyAcquiresLock()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var resource = $"test-resource-{Guid.NewGuid():N}";

        // Act
        await using var handle = await _provider!.AcquireAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var isLocked = await _provider.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        handle.Should().NotBeNull();
        isLocked.Should().BeTrue();
    }

    [SkippableFact]
    public async Task AcquireAsync_ReleaseLock_ResourceBecomesUnlocked()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        lockedWhileHeld.Should().BeTrue();
        lockedAfterRelease.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryAcquireAsync_WhenResourceLocked_ReturnsNull()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        firstHandle.Should().NotBeNull();
        secondHandle.Should().BeNull();
    }

    [SkippableFact]
    public async Task TryAcquireAsync_AfterLockReleased_Succeeds()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        secondHandle.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task ExtendAsync_WhenLockHeld_ExtendsLock()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        extended.Should().BeTrue();
    }

    [SkippableFact]
    public async Task ExtendAsync_WhenLockNotHeld_ReturnsFalse()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var resource = $"extend-notheld-{Guid.NewGuid():N}";

        // Act
        var extended = await _provider!.ExtendAsync(
            resource,
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        extended.Should().BeFalse();
    }

    [SkippableFact]
    public async Task IsLockedAsync_NonExistentResource_ReturnsFalse()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var resource = $"nonexistent-{Guid.NewGuid():N}";

        // Act
        var isLocked = await _provider!.IsLockedAsync(resource, CancellationToken.None);

        // Assert
        isLocked.Should().BeFalse();
    }

    [SkippableFact]
    public async Task LockExpires_AfterTimeout()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        lockedImmediately.Should().BeTrue();
        lockedAfterExpiry.Should().BeFalse();

        // Clean up the handle anyway
        await handle.DisposeAsync();
    }

    [SkippableFact]
    public async Task DifferentResources_CanBeLocked_Simultaneously()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

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
        handle1.Should().NotBeNull();
        handle2.Should().NotBeNull();
        resource1Locked.Should().BeTrue();
        resource2Locked.Should().BeTrue();
    }
}
