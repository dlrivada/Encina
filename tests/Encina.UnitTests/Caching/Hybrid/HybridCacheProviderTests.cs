using Bogus;
using Encina.Caching;
using Encina.Caching.Hybrid;
using Encina.Testing.Bogus;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Hybrid;

/// <summary>
/// Unit tests for <see cref="HybridCacheProvider"/>.
/// </summary>
public sealed class HybridCacheProviderTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly HybridCacheProvider _sut;
    private readonly Faker _faker;
    private readonly CacheKeyFaker _keyFaker;
    private bool _disposed;

    public HybridCacheProviderTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        _serviceProvider = services.BuildServiceProvider();

        var hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        var options = Options.Create(new HybridCacheProviderOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        _sut = new HybridCacheProvider(hybridCache, options, NullLogger<HybridCacheProvider>.Instance);
        _faker = new Faker();
        _keyFaker = new CacheKeyFaker();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _serviceProvider.Dispose();
            _disposed = true;
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new HybridCacheProviderOptions());
        var logger = NullLogger<HybridCacheProvider>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("cache", () =>
            new HybridCacheProvider(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var logger = NullLogger<HybridCacheProvider>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("options", () =>
            new HybridCacheProvider(cache, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var options = Options.Create(new HybridCacheProviderOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new HybridCacheProvider(cache, options, null!));
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.GetAsync<string>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetAsync<string>(key, cts.Token));
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var expectedValue = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, expectedValue, expiration, CancellationToken.None);

        // Act
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedValue);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var key = _keyFaker.Generate();

        // Act
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WithComplexType_ReturnsDeserializedValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var expectedValue = new TestData(_faker.Random.Guid(), _faker.Name.FullName(), _faker.Random.Int(1, 100));
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, expectedValue, expiration, CancellationToken.None);

        // Act
        var result = await _sut.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        result.ShouldBe(expectedValue);
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.SetAsync(null!, "value", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SetAsync(key, value, expiration, cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithValue_StoresValueInCache()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();

        // Act
        await _sut.SetAsync(key, value, expiration, CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.ShouldBe(value);
    }

    [Fact]
    public async Task SetAsync_WithNullExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();

        // Act
        await _sut.SetAsync(key, value, null, CancellationToken.None);

        // Assert - verify value was stored by retrieving it
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.ShouldBe(value);
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var originalValue = _faker.Lorem.Sentence();
        var updatedValue = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, originalValue, expiration, CancellationToken.None);

        // Act
        await _sut.SetAsync(key, updatedValue, expiration, CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.ShouldBe(updatedValue);
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.RemoveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RemoveAsync(key, cts.Token));
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_RemovesFromCache()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, value, expiration, CancellationToken.None);

        // Act
        await _sut.RemoveAsync(key, CancellationToken.None);

        // Assert
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingKey_DoesNotThrow()
    {
        // Arrange
        var key = _keyFaker.Generate();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _sut.RemoveAsync(key, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region RemoveByPatternAsync Tests

    [Fact]
    public async Task RemoveByPatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("pattern", () =>
            _sut.RemoveByPatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var pattern = _keyFaker.AsPattern().Generate();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RemoveByPatternAsync(pattern, cts.Token));
    }

    #endregion

    #region RemoveByTagAsync Tests

    [Fact]
    public async Task RemoveByTagAsync_WithNullTag_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("tag", () =>
            _sut.RemoveByTagAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByTagAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var tag = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RemoveByTagAsync(tag, cts.Token));
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.ExistsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExistsAsync(key, cts.Token));
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var key = _keyFaker.Generate();

        // Act
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.ShouldBeFalse();
    }

    #endregion

    #region GetOrSetAsync Tests

    [Fact]
    public async Task GetOrSetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.GetOrSetAsync(null!, _ => Task.FromResult(value), expiration, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var expiration = _faker.CacheExpiration();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("factory", () =>
            _sut.GetOrSetAsync<string>(key, null!, expiration, CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetOrSetAsync(key, _ => Task.FromResult(value), expiration, cts.Token));
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ReturnsExistingValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var existingValue = _faker.Lorem.Sentence();
        var newValue = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, existingValue, expiration, CancellationToken.None);

        // Act
        var result = await _sut.GetOrSetAsync(
            key,
            _ => Task.FromResult(newValue),
            expiration,
            CancellationToken.None);

        // Assert
        result.ShouldBe(existingValue);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_CallsFactoryAndStoresValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var newValue = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(newValue);
            },
            expiration,
            CancellationToken.None);

        // Assert
        result.ShouldBe(newValue);
        factoryCalled.ShouldBeTrue();

        // Verify it was stored
        var stored = await _sut.GetAsync<string>(key, CancellationToken.None);
        stored.ShouldBe(newValue);
    }

    [Fact]
    public async Task GetOrSetAsync_WithTags_StoresWithTags()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var tag1 = _faker.Lorem.Word();
        var tag2 = _faker.Lorem.Word();
        string[] tags = [tag1, tag2];
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();

        // Act
        var result = await _sut.GetOrSetAsync(
            key,
            _ => Task.FromResult(value),
            expiration,
            tags,
            CancellationToken.None);

        // Assert
        result.ShouldBe(value);
    }

    #endregion

    #region SetWithSlidingExpirationAsync Tests

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.SetWithSlidingExpirationAsync(null!, "value", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var slidingExpiration = _faker.CacheSlidingExpiration();
        var absoluteExpiration = _faker.CacheAbsoluteExpiration();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SetWithSlidingExpirationAsync(key, value, slidingExpiration, absoluteExpiration, cts.Token));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_StoresValue()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var slidingExpiration = _faker.CacheSlidingExpiration();
        var absoluteExpiration = _faker.CacheAbsoluteExpiration();

        // Act
        await _sut.SetWithSlidingExpirationAsync(key, value, slidingExpiration, absoluteExpiration, CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.ShouldBe(value);
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithNullAbsoluteExpiration_Works()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var slidingExpiration = _faker.CacheSlidingExpiration();

        // Act
        await _sut.SetWithSlidingExpirationAsync(key, value, slidingExpiration, null, CancellationToken.None);

        // Assert - verify value was stored by retrieving it
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.ShouldBe(value);
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.RefreshAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RefreshAsync(key, cts.Token));
    }

    [Fact]
    public async Task RefreshAsync_ReturnsFalse_HybridCacheDoesNotSupportRefresh()
    {
        // Arrange
        var key = _keyFaker.Generate();
        var value = _faker.Lorem.Sentence();
        var expiration = _faker.CacheExpiration();
        await _sut.SetAsync(key, value, expiration, CancellationToken.None);

        // Act
        var result = await _sut.RefreshAsync(key, CancellationToken.None);

        // Assert - HybridCache does not support refresh
        result.ShouldBeFalse();
    }

    #endregion

    private sealed record TestData(Guid Id, string Name, int Value);
}
