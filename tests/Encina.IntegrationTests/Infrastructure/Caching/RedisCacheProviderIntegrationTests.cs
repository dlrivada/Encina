using Encina.Caching.Redis;
using Encina.IntegrationTests.Infrastructure.Caching.Fixtures;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Caching;

/// <summary>
/// Integration tests for RedisCacheProvider using a real Redis container.
/// </summary>
[Collection(RedisCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Redis")]
public class RedisCacheProviderIntegrationTests : IAsyncLifetime
{
    private readonly RedisFixture _fixture;
    private RedisCacheProvider? _provider;

    public RedisCacheProviderIntegrationTests(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return Task.CompletedTask;
        }

        var options = Options.Create(new RedisCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5),
            KeyPrefix = $"test:{Guid.NewGuid():N}"
        });

        _provider = new RedisCacheProvider(
            _fixture.Connection!,
            options,
            NullLogger<RedisCacheProvider>.Instance);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task SetAsync_And_GetAsync_WorkCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"test-key-{Guid.NewGuid():N}";
        var value = new TestData(Guid.NewGuid(), "Test Name", 42);

        // Act
        await _provider!.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);
        var retrieved = await _provider.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.Id.ShouldBe(value.Id);
        retrieved.Name.ShouldBe(value.Name);
        retrieved.Value.ShouldBe(value.Value);
    }

    [SkippableFact]
    public async Task GetAsync_NonExistentKey_ReturnsNull()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"nonexistent-{Guid.NewGuid():N}";

        // Act
        var result = await _provider!.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task ExistsAsync_AfterSet_ReturnsTrue()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"exists-test-{Guid.NewGuid():N}";
        var value = "test-value";

        // Act
        await _provider!.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);
        var exists = await _provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task ExistsAsync_NonExistentKey_ReturnsFalse()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"nonexistent-exists-{Guid.NewGuid():N}";

        // Act
        var exists = await _provider!.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task RemoveAsync_RemovesKey()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"remove-test-{Guid.NewGuid():N}";
        var value = "test-value";

        // Act
        await _provider!.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);
        await _provider.RemoveAsync(key, CancellationToken.None);
        var exists = await _provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task GetOrSetAsync_CacheMiss_CallsFactoryAndCaches()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"getorset-miss-{Guid.NewGuid():N}";
        var expectedValue = "factory-value";
        var factoryCalled = false;

        // Act
        var result = await _provider!.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(expectedValue);
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        var cached = await _provider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        factoryCalled.ShouldBeTrue();
        result.ShouldBe(expectedValue);
        cached.ShouldBe(expectedValue);
    }

    [SkippableFact]
    public async Task GetOrSetAsync_CacheHit_DoesNotCallFactory()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"getorset-hit-{Guid.NewGuid():N}";
        var cachedValue = "cached-value";
        var factoryCalled = false;

        await _provider!.SetAsync(key, cachedValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await _provider.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("factory-value");
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        factoryCalled.ShouldBeFalse();
        result.ShouldBe(cachedValue);
    }

    [SkippableFact]
    public async Task SetAsync_WithExpiration_KeyExpiresAfterTimeout()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"expiration-test-{Guid.NewGuid():N}";
        var value = "test-value";
        var shortExpiration = TimeSpan.FromSeconds(2);

        // Act
        await _provider!.SetAsync(key, value, shortExpiration, CancellationToken.None);
        var existsImmediately = await _provider.ExistsAsync(key, CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var existsAfterExpiration = await _provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        existsImmediately.ShouldBeTrue();
        existsAfterExpiration.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task RemoveByPatternAsync_RemovesMatchingKeys()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var prefix = $"pattern-{Guid.NewGuid():N}";
        var key1 = $"{prefix}:key1";
        var key2 = $"{prefix}:key2";
        var key3 = $"{prefix}:key3";
        var otherKey = $"other-{Guid.NewGuid():N}:key";

        await _provider!.SetAsync(key1, "value1", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _provider.SetAsync(key2, "value2", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _provider.SetAsync(key3, "value3", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _provider.SetAsync(otherKey, "other", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await _provider.RemoveByPatternAsync($"{prefix}:*", CancellationToken.None);

        // Assert
        (await _provider.ExistsAsync(key1, CancellationToken.None)).ShouldBeFalse();
        (await _provider.ExistsAsync(key2, CancellationToken.None)).ShouldBeFalse();
        (await _provider.ExistsAsync(key3, CancellationToken.None)).ShouldBeFalse();
        (await _provider.ExistsAsync(otherKey, CancellationToken.None)).ShouldBeTrue();
    }

    [SkippableFact]
    public async Task SetWithSlidingExpirationAsync_RefreshesExpiration()
    {
        Skip.IfNot(_fixture.IsAvailable, "Redis container not available");

        // Arrange
        var key = $"sliding-test-{Guid.NewGuid():N}";
        var value = "test-value";
        var slidingExpiration = TimeSpan.FromSeconds(3);
        var maxExpiration = TimeSpan.FromSeconds(10);

        // Act
        await _provider!.SetWithSlidingExpirationAsync(
            key,
            value,
            slidingExpiration,
            maxExpiration,
            CancellationToken.None);

        // Wait and refresh
        await Task.Delay(TimeSpan.FromSeconds(2));
        await _provider.RefreshAsync(key, CancellationToken.None);

        // Wait again (should still exist because we refreshed)
        await Task.Delay(TimeSpan.FromSeconds(2));
        var existsAfterRefresh = await _provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        existsAfterRefresh.ShouldBeTrue();
    }

    private sealed record TestData(Guid Id, string Name, int Value);
}
