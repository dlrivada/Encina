using MemoryProviderOptions = Encina.Caching.Memory.MemoryCacheOptions;

namespace Encina.Caching.ContractTests;

/// <summary>
/// Contract tests that verify all ICacheProvider implementations follow the same behavioral contract.
/// </summary>
public abstract class ICacheProviderContractTests : IAsyncLifetime
{
    protected ICacheProvider Provider { get; private set; } = null!;

    protected abstract ICacheProvider CreateProvider();

    public Task InitializeAsync()
    {
        Provider = CreateProvider();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    #region GetAsync Contract

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await Provider.GetAsync<string>("nonexistent-key", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsStoredValue()
    {
        // Arrange
        const string key = "contract-get-exists";
        const string expectedValue = "stored-value";
        await Provider.SetAsync(key, expectedValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await Provider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.GetAsync<string>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.GetAsync<string>("key", cts.Token));
    }

    [Fact]
    public async Task GetAsync_WithComplexType_ReturnsDeserializedValue()
    {
        // Arrange
        const string key = "contract-get-complex";
        var expectedValue = new TestEntity(Guid.NewGuid(), "Test Name", 42);
        await Provider.SetAsync(key, expectedValue, TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await Provider.GetAsync<TestEntity>(key, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    #endregion

    #region SetAsync Contract

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SetAsync<string>(null!, "value", TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.SetAsync("key", "value", TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithValue_MakesValueRetrievable()
    {
        // Arrange
        var key = $"contract-set-{Guid.NewGuid():N}";
        const string value = "test-value";

        // Act
        await Provider.SetAsync(key, value, TimeSpan.FromMinutes(5), CancellationToken.None);
        var retrieved = await Provider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithSameKey_OverwritesPreviousValue()
    {
        // Arrange
        const string key = "contract-set-overwrite";
        await Provider.SetAsync(key, "original", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await Provider.SetAsync(key, "updated", TimeSpan.FromMinutes(5), CancellationToken.None);
        var result = await Provider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().Be("updated");
    }

    [Fact]
    public async Task SetAsync_WithNullExpiration_UsesDefaultExpiration()
    {
        // Arrange
        var key = $"contract-set-null-exp-{Guid.NewGuid():N}";

        // Act
        await Provider.SetAsync(key, "value", null, CancellationToken.None);
        var exists = await Provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.Should().BeTrue();
    }

    #endregion

    #region RemoveAsync Contract

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.RemoveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.RemoveAsync("key", cts.Token));
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyExists_RemovesKey()
    {
        // Arrange
        const string key = "contract-remove-exists";
        await Provider.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await Provider.RemoveAsync(key, CancellationToken.None);
        var exists = await Provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WhenKeyDoesNotExist_DoesNotThrow()
    {
        // Act & Assert
        await Provider.Invoking(p => p.RemoveAsync("nonexistent", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    #endregion

    #region RemoveByPatternAsync Contract

    [Fact]
    public async Task RemoveByPatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.RemoveByPatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.RemoveByPatternAsync("pattern*", cts.Token));
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithWildcard_RemovesMatchingKeys()
    {
        // Arrange
        var prefix = $"pattern-{Guid.NewGuid():N}";
        await Provider.SetAsync($"{prefix}:a", "value-a", TimeSpan.FromMinutes(5), CancellationToken.None);
        await Provider.SetAsync($"{prefix}:b", "value-b", TimeSpan.FromMinutes(5), CancellationToken.None);
        await Provider.SetAsync("other:c", "value-c", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        await Provider.RemoveByPatternAsync($"{prefix}:*", CancellationToken.None);

        // Assert
        (await Provider.ExistsAsync($"{prefix}:a", CancellationToken.None)).Should().BeFalse();
        (await Provider.ExistsAsync($"{prefix}:b", CancellationToken.None)).Should().BeFalse();
        (await Provider.ExistsAsync("other:c", CancellationToken.None)).Should().BeTrue();
    }

    #endregion

    #region ExistsAsync Contract

    [Fact]
    public async Task ExistsAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.ExistsAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExistsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.ExistsAsync("key", cts.Token));
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        const string key = "contract-exists-true";
        await Provider.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var exists = await Provider.ExistsAsync(key, CancellationToken.None);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Act
        var exists = await Provider.ExistsAsync("nonexistent", CancellationToken.None);

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region GetOrSetAsync Contract

    [Fact]
    public async Task GetOrSetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.GetOrSetAsync<string>(null!, _ => Task.FromResult("value"), TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithNullFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.GetOrSetAsync<string>("key", null!, TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task GetOrSetAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.GetOrSetAsync<string>("key", _ => Task.FromResult("value"), TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task GetOrSetAsync_WhenKeyExists_ReturnsExistingValue()
    {
        // Arrange
        const string key = "contract-getorset-hit";
        const string existingValue = "existing";
        await Provider.SetAsync(key, existingValue, TimeSpan.FromMinutes(5), CancellationToken.None);
        var factoryCalled = false;

        // Act
        var result = await Provider.GetOrSetAsync(
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
    public async Task GetOrSetAsync_WhenKeyDoesNotExist_CallsFactoryAndStoresValue()
    {
        // Arrange
        var key = $"contract-getorset-miss-{Guid.NewGuid():N}";
        const string newValue = "new-value";
        var factoryCalled = false;

        // Act
        var result = await Provider.GetOrSetAsync(
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

        // Verify storage
        var stored = await Provider.GetAsync<string>(key, CancellationToken.None);
        stored.Should().Be(newValue);
    }

    #endregion

    #region SetWithSlidingExpirationAsync Contract

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SetWithSlidingExpirationAsync(null!, "value", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CancellationToken.None));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.SetWithSlidingExpirationAsync("key", "value", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), cts.Token));
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_MakesValueRetrievable()
    {
        // Arrange
        var key = $"contract-sliding-{Guid.NewGuid():N}";
        const string value = "sliding-value";

        // Act
        await Provider.SetWithSlidingExpirationAsync(key, value, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), CancellationToken.None);
        var result = await Provider.GetAsync<string>(key, CancellationToken.None);

        // Assert
        result.Should().Be(value);
    }

    #endregion

    #region RefreshAsync Contract

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.RefreshAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.RefreshAsync("key", cts.Token));
    }

    [Fact]
    public async Task RefreshAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        const string key = "contract-refresh-exists";
        await Provider.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Act
        var result = await Provider.RefreshAsync(key, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        // Act
        var result = await Provider.RefreshAsync("nonexistent", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    protected sealed record TestEntity(Guid Id, string Name, int Value);
}

/// <summary>
/// Contract tests for MemoryCacheProvider.
/// </summary>
public sealed class MemoryCacheProviderContractTests : ICacheProviderContractTests, IDisposable
{
    private MemoryCache? _memoryCache;
    private bool _disposed;

    protected override ICacheProvider CreateProvider()
    {
        var memoryCacheOptions = Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        _memoryCache = new MemoryCache(memoryCacheOptions);

        var providerOptions = Options.Create(new MemoryProviderOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });

        return new MemoryCacheProvider(_memoryCache, providerOptions, NullLogger<MemoryCacheProvider>.Instance);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCache?.Dispose();
            _disposed = true;
        }
    }
}
