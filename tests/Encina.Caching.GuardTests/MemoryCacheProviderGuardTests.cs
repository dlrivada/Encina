using Microsoft.Extensions.Caching.Memory;
using MemoryProviderOptions = Encina.Caching.Memory.MemoryCacheOptions;
using MsMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace Encina.Caching.GuardTests;

/// <summary>
/// Guard tests for <see cref="MemoryCacheProvider"/> to verify null parameter handling.
/// </summary>
public class MemoryCacheProviderGuardTests : IDisposable
{
    private readonly MemoryCache _cache;
    private bool _disposed;

    public MemoryCacheProviderGuardTests()
    {
        _cache = new MemoryCache(Options.Create(new MsMemoryCacheOptions()));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MemoryProviderOptions());
        var logger = NullLogger<MemoryCacheProvider>.Instance;

        // Act & Assert
        var act = () => new MemoryCacheProvider(null!, options, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("cache");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<MemoryCacheProvider>.Instance;

        // Act & Assert
        var act = () => new MemoryCacheProvider(_cache, null!, logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MemoryProviderOptions());

        // Act & Assert
        var act = () => new MemoryCacheProvider(_cache, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that GetAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.GetAsync<string>(key, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that SetAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.SetAsync(key, "value", TimeSpan.FromMinutes(5), CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that RemoveAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.RemoveAsync(key, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that ExistsAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task ExistsAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.ExistsAsync(key, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that RefreshAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.RefreshAsync(key, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that GetOrSetAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task GetOrSetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.GetOrSetAsync(
            key,
            _ => Task.FromResult("value"),
            TimeSpan.FromMinutes(5),
            CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    /// <summary>
    /// Verifies that GetOrSetAsync throws ArgumentNullException when factory is null.
    /// </summary>
    [Fact]
    public async Task GetOrSetAsync_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        Func<CancellationToken, Task<string>> factory = null!;

        // Act & Assert
        var act = () => provider.GetOrSetAsync(
            "key",
            factory,
            TimeSpan.FromMinutes(5),
            CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("factory");
    }

    /// <summary>
    /// Verifies that RemoveByPatternAsync throws ArgumentNullException when pattern is null.
    /// </summary>
    [Fact]
    public async Task RemoveByPatternAsync_NullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string pattern = null!;

        // Act & Assert
        var act = () => provider.RemoveByPatternAsync(pattern, CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("pattern");
    }

    /// <summary>
    /// Verifies that SetWithSlidingExpirationAsync throws ArgumentNullException when key is null.
    /// </summary>
    [Fact]
    public async Task SetWithSlidingExpirationAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        string key = null!;

        // Act & Assert
        var act = () => provider.SetWithSlidingExpirationAsync(
            key,
            "value",
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            CancellationToken.None).AsTask();
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("key");
    }

    private MemoryCacheProvider CreateProvider()
    {
        var options = Options.Create(new MemoryProviderOptions
        {
            DefaultExpiration = TimeSpan.FromMinutes(5)
        });
        return new MemoryCacheProvider(_cache, options, NullLogger<MemoryCacheProvider>.Instance);
    }
}
