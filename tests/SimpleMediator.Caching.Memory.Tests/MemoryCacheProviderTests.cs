namespace SimpleMediator.Caching.Memory.Tests;

/// <summary>
/// Unit tests for <see cref="MemoryCacheProvider"/>.
/// </summary>
public sealed class MemoryCacheProviderTests : IDisposable
{
    private readonly MemoryCache _memoryCache;
    private readonly MemoryCacheProvider _sut;
    private bool _disposed;

    public MemoryCacheProviderTests()
    {
        var memoryCacheOptions = Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        _memoryCache = new MemoryCache(memoryCacheOptions);

        var providerOptions = Options.Create(new MemoryCacheOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        _sut = new MemoryCacheProvider(_memoryCache, providerOptions, NullLogger<MemoryCacheProvider>.Instance);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache.Dispose();
            _disposed = true;
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MemoryCacheOptions());
        var logger = NullLogger<MemoryCacheProvider>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("cache", () =>
            new MemoryCacheProvider(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = Substitute.For<IMemoryCache>();
        var logger = NullLogger<MemoryCacheProvider>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("options", () =>
            new MemoryCacheProvider(cache, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = Substitute.For<IMemoryCache>();
        var options = Options.Create(new MemoryCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new MemoryCacheProvider(cache, options, null!));
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetAsync<string>("key", cts.Token));
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ReturnsValue()
    {
        // Arrange
        const string key = "test-key";
        const string expectedValue = "test-value";
        await _sut.SetAsync(key, expectedValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ReturnsNull()
    {
        // Act
        var result = await _sut.GetAsync<string>("non-existing", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithComplexType_ReturnsDeserializedValue()
    {
        // Arrange
        const string key = "complex-key";
        var expectedValue = new TestData(Guid.NewGuid(), "Test Name", 42);
        await _sut.SetAsync(key, expectedValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await _sut.GetAsync<TestData>(key, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SetAsync("key", "value", TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithValue_StoresValueInCache()
    {
        // Arrange
        const string key = "set-key";
        const string value = "set-value";

        // Act
        await _sut.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithNullExpiration_UsesDefaultExpiration()
    {
        // Arrange
        const string key = "null-expiration-key";
        const string value = "value";

        // Act
        await _sut.SetAsync(key, value, null, CancellationToken.None);

        // Assert
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsync_OverwritesExistingValue()
    {
        // Arrange
        const string key = "overwrite-key";
        await _sut.SetAsync(key, "original", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await _sut.SetAsync(key, "updated", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.Should().Be("updated");
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RemoveAsync("key", cts.Token));
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_RemovesFromCache()
    {
        // Arrange
        const string key = "remove-key";
        await _sut.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await _sut.RemoveAsync(key, CancellationToken.None);

        // Assert
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WithNonExistingKey_DoesNotThrow()
    {
        // Act & Assert
        await _sut.Invoking(s => s.RemoveAsync("non-existing", CancellationToken.None))
            .Should().NotThrowAsync();
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RemoveByPatternAsync("pattern*", cts.Token));
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithWildcard_RemovesMatchingKeys()
    {
        // Arrange
        await _sut.SetAsync("user:1:name", "Alice", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _sut.SetAsync("user:1:email", "alice@test.com", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _sut.SetAsync("user:2:name", "Bob", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _sut.SetAsync("product:1:name", "Widget", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await _sut.RemoveByPatternAsync("user:1:*", CancellationToken.None);

        // Assert
        (await _sut.ExistsAsync("user:1:name", CancellationToken.None)).Should().BeFalse();
        (await _sut.ExistsAsync("user:1:email", CancellationToken.None)).Should().BeFalse();
        (await _sut.ExistsAsync("user:2:name", CancellationToken.None)).Should().BeTrue();
        (await _sut.ExistsAsync("product:1:name", CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithQuestionMarkWildcard_RemovesMatchingKeys()
    {
        // Arrange
        await _sut.SetAsync("cache:a", "value-a", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _sut.SetAsync("cache:b", "value-b", TimeSpan.FromMinutes(5), CancellationToken.None);
        await _sut.SetAsync("cache:ab", "value-ab", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await _sut.RemoveByPatternAsync("cache:?", CancellationToken.None);

        // Assert
        (await _sut.ExistsAsync("cache:a", CancellationToken.None)).Should().BeFalse();
        (await _sut.ExistsAsync("cache:b", CancellationToken.None)).Should().BeFalse();
        (await _sut.ExistsAsync("cache:ab", CancellationToken.None)).Should().BeTrue();
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExistsAsync("key", cts.Token));
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "exists-key";
        await _sut.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Act
        var exists = await _sut.ExistsAsync("non-existing", CancellationToken.None);

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region GetOrSetAsync Tests

    [Fact]
    public async Task GetOrSetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("key", () =>
            _sut.GetOrSetAsync<string>(null!, _ => Task.FromResult("value"), TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("factory", () =>
            _sut.GetOrSetAsync<string>("key", null!, TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetOrSetAsync<string>("key", _ => Task.FromResult("value"), TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHit_ReturnsExistingValue()
    {
        // Arrange
        const string key = "getorset-hit";
        const string existingValue = "existing";
        await _sut.SetAsync(key, existingValue, TimeSpan.FromMinutes(5), CancellationToken.None);
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult("new-value");
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        result.Should().Be(existingValue);
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheMiss_CallsFactoryAndStoresValue()
    {
        // Arrange
        const string key = "getorset-miss";
        const string newValue = "new-value";
        var factoryCalled = false;

        // Act
        var result = await _sut.GetOrSetAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(newValue);
            },
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        // Assert
        result.Should().Be(newValue);
        factoryCalled.Should().BeTrue();

        // Verify it was stored
        var stored = await _sut.GetAsync<string>(key, CancellationToken.None);
        stored.Should().Be(newValue);
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SetWithSlidingExpirationAsync("key", "value", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_StoresValueWithSlidingExpiration()
    {
        // Arrange
        const string key = "sliding-key";
        const string value = "sliding-value";

        // Act
        await _sut.SetWithSlidingExpirationAsync(key, value, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CancellationToken.None);

        // Assert
        var result = await _sut.GetAsync<string>(key, CancellationToken.None);
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithNullAbsoluteExpiration_Works()
    {
        // Arrange
        const string key = "sliding-only-key";
        const string value = "sliding-value";

        // Act
        await _sut.SetWithSlidingExpirationAsync(key, value, TimeSpan.FromMinutes(1), null, CancellationToken.None);

        // Assert
        var exists = await _sut.ExistsAsync(key, CancellationToken.None);
        exists.Should().BeTrue();
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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RefreshAsync("key", cts.Token));
    }

    [Fact]
    public async Task RefreshAsync_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        const string key = "refresh-key";
        await _sut.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await _sut.RefreshAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WithNonExistingKey_ReturnsFalse()
    {
        // Act
        var result = await _sut.RefreshAsync("non-existing", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    private sealed record TestData(Guid Id, string Name, int Value);
}
