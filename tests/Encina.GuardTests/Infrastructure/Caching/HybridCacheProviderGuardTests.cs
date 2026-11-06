using Encina.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;

namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for <see cref="HybridCacheProvider"/>: null arguments, pre-cancelled tokens and
/// the behavior of valid boundary arguments, using a real <see cref="HybridCache"/> built through DI.
/// </summary>
public sealed class HybridCacheProviderGuardTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly HybridCache _cache;

    // _sut sits on a substituted HybridCache that accepts anything, so a missing guard clause cannot be
    // masked by the underlying cache rejecting the argument itself; _real sits on a real HybridCache.
    private readonly HybridCacheProvider _sut;
    private readonly HybridCacheProvider _real;

    public HybridCacheProviderGuardTests()
    {
        _services = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider();
        _cache = _services.GetRequiredService<HybridCache>();
        _real = new HybridCacheProvider(
            _cache,
            Options.Create(new HybridCacheProviderOptions()),
            NullLogger<HybridCacheProvider>.Instance);
        _sut = new HybridCacheProvider(
            Substitute.For<HybridCache>(),
            Options.Create(new HybridCacheProviderOptions()),
            NullLogger<HybridCacheProvider>.Instance);
    }

    public void Dispose() => _services.Dispose();

    private static readonly Func<CancellationToken, Task<string>> Factory = _ => Task.FromResult("value");

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new HybridCacheProvider(
            null!,
            Options.Create(new HybridCacheProviderOptions()),
            NullLogger<HybridCacheProvider>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new HybridCacheProvider(_cache, null!, NullLogger<HybridCacheProvider>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new HybridCacheProvider(_cache, Options.Create(new HybridCacheProviderOptions()), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task GetAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.GetAsync<string>(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.SetAsync(null!, "value", TimeSpan.FromMinutes(1), CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task RemoveAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.RemoveAsync(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task RemoveByPatternAsync_NullPattern_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.RemoveByPatternAsync(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("pattern");
    }

    [Fact]
    public async Task RemoveByTagAsync_NullTag_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.RemoveByTagAsync(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("tag");
    }

    [Fact]
    public async Task ExistsAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.ExistsAsync(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task GetOrSetAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.GetOrSetAsync(null!, Factory, TimeSpan.FromMinutes(1), CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task GetOrSetAsync_NullFactory_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.GetOrSetAsync<string>("key", null!, TimeSpan.FromMinutes(1), CancellationToken.None));

        ex.ParamName.ShouldBe("factory");
    }

    [Fact]
    public async Task GetOrSetAsyncWithTags_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.GetOrSetAsync(null!, Factory, TimeSpan.FromMinutes(1), ["tag"], CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task GetOrSetAsyncWithTags_NullFactory_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.GetOrSetAsync<string>("key", null!, TimeSpan.FromMinutes(1), ["tag"], CancellationToken.None));

        ex.ParamName.ShouldBe("factory");
    }

    [Fact]
    public async Task SetWithSlidingExpirationAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.SetWithSlidingExpirationAsync(
                null!, "value", TimeSpan.FromMinutes(1), null, CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public async Task RefreshAsync_NullKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => _sut.RefreshAsync(null!, CancellationToken.None));

        ex.ParamName.ShouldBe("key");
    }

    [Theory]
    [InlineData("GetAsync")]
    [InlineData("SetAsync")]
    [InlineData("RemoveAsync")]
    [InlineData("RemoveByPatternAsync")]
    [InlineData("RemoveByTagAsync")]
    [InlineData("ExistsAsync")]
    [InlineData("GetOrSetAsync")]
    [InlineData("GetOrSetAsyncWithTags")]
    [InlineData("SetWithSlidingExpirationAsync")]
    [InlineData("RefreshAsync")]
    public async Task Operation_PreCancelledToken_ThrowsOperationCanceledException(string operation)
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => Invoke(operation, cts.Token));
    }

    private Task Invoke(string operation, CancellationToken token) => operation switch
    {
        "GetAsync" => _sut.GetAsync<string>("key", token),
        "SetAsync" => _sut.SetAsync("key", "value", null, token),
        "RemoveAsync" => _sut.RemoveAsync("key", token),
        "RemoveByPatternAsync" => _sut.RemoveByPatternAsync("key*", token),
        "RemoveByTagAsync" => _sut.RemoveByTagAsync("tag", token),
        "ExistsAsync" => _sut.ExistsAsync("key", token),
        "GetOrSetAsync" => _sut.GetOrSetAsync("key", Factory, null, token),
        "GetOrSetAsyncWithTags" => _sut.GetOrSetAsync("key", Factory, null, ["tag"], token),
        "SetWithSlidingExpirationAsync" =>
            _sut.SetWithSlidingExpirationAsync("key", "value", TimeSpan.FromMinutes(1), null, token),
        "RefreshAsync" => _sut.RefreshAsync("key", token),
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation."),
    };

    [Fact]
    public async Task RefreshAsync_ValidKey_ReturnsFalseBecauseRefreshIsUnsupported()
    {
        var refreshed = await _sut.RefreshAsync("key", CancellationToken.None);

        refreshed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_KeyNeverStored_ReturnsDefault()
    {
        var value = await _real.GetAsync<string>("missing-key", CancellationToken.None);

        value.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsAsync_StoredAndMissingKeys_ReportsPresence()
    {
        // Stored as object because ExistsAsync reads through GetAsync<object>; see the follow-up issue about
        // ExistsAsync missing entries stored under a concrete type.
        await _real.SetAsync<object>("present-key", "v", TimeSpan.FromMinutes(1), CancellationToken.None);

        (await _real.ExistsAsync("present-key", CancellationToken.None)).ShouldBeTrue();
        (await _real.ExistsAsync("absent-key", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetOrSetAsyncWithTags_RunsFactoryOnceAndServesTheCachedValue()
    {
        var calls = 0;
        Task<string> CountingFactory(CancellationToken _) => Task.FromResult($"value-{Interlocked.Increment(ref calls)}");

        var first = await _real.GetOrSetAsync(
            "tagged-key", CountingFactory, TimeSpan.FromMinutes(1), ["products"], CancellationToken.None);
        var second = await _real.GetOrSetAsync(
            "tagged-key", CountingFactory, TimeSpan.FromMinutes(1), ["products"], CancellationToken.None);

        first.ShouldBe("value-1");
        second.ShouldBe("value-1");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrSetAsync_WithoutTags_RunsFactoryOnceAndServesTheCachedValue()
    {
        var calls = 0;
        Task<string> CountingFactory(CancellationToken _) => Task.FromResult($"value-{Interlocked.Increment(ref calls)}");

        var first = await _real.GetOrSetAsync("plain-key", CountingFactory, null, CancellationToken.None);
        var second = await _real.GetOrSetAsync("plain-key", CountingFactory, null, CancellationToken.None);

        first.ShouldBe("value-1");
        second.ShouldBe("value-1");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task SetAsync_StoresTheValueReadableByKey()
    {
        await _real.SetAsync("set-key", "stored", TimeSpan.FromMinutes(1), CancellationToken.None);

        (await _real.GetAsync<string>("set-key", CancellationToken.None)).ShouldBe("stored");
    }

    [Fact]
    public async Task SetAsync_NullExpiration_StoresTheValueWithTheDefaultExpiration()
    {
        await _real.SetAsync("default-expiration-key", "stored", null, CancellationToken.None);

        (await _real.GetAsync<string>("default-expiration-key", CancellationToken.None)).ShouldBe("stored");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetWithSlidingExpirationAsync_StoresTheValueReadableByKey(bool withAbsoluteExpiration)
    {
        var key = withAbsoluteExpiration ? "sliding-absolute-key" : "sliding-key";
        TimeSpan? absolute = withAbsoluteExpiration ? TimeSpan.FromMinutes(1) : null;

        await _real.SetWithSlidingExpirationAsync(key, "stored", TimeSpan.FromMinutes(2), absolute, CancellationToken.None);

        (await _real.GetAsync<string>(key, CancellationToken.None)).ShouldBe("stored");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyOrWhitespaceKey_IsAcceptedBecauseOnlyNullIsRejected(string key)
    {
        // The provider only rejects null; whether the underlying HybridCache stores an empty key is its own
        // business, so the pin is that every operation accepts the key instead of throwing.
        await Should.NotThrowAsync(() => _real.SetAsync(key, "stored", TimeSpan.FromMinutes(1), CancellationToken.None));
        await Should.NotThrowAsync(() => _real.GetAsync<string>(key, CancellationToken.None));
        await Should.NotThrowAsync(() => _real.ExistsAsync(key, CancellationToken.None));
        await Should.NotThrowAsync(() => _real.RemoveAsync(key, CancellationToken.None));
        await Should.NotThrowAsync(() => _real.GetOrSetAsync(key, Factory, null, CancellationToken.None));
    }

    [Theory]
    [InlineData("tag:products")]
    [InlineData("#products")]
    [InlineData("track-*")]
    public async Task RemoveByPatternAsync_AfterTrackingTaggedKey_EvictsTheEntry(string pattern)
    {
        await TrackAndRemoveAsync("track-key", "products", () => _real.RemoveByPatternAsync(pattern, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_AfterTrackingTaggedKey_EvictsTheEntry()
    {
        await TrackAndRemoveAsync("removable-key", "orders", () => _real.RemoveAsync("removable-key", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByTagAsync_AfterTrackingTaggedKey_EvictsTheEntry()
    {
        await TrackAndRemoveAsync("tag-removed-key", "customers", () => _real.RemoveByTagAsync("customers", CancellationToken.None));
    }

    // Stores a tagged entry, proves it is served from the cache, runs the removal and proves the entry is gone
    // (the factory runs again), so a removal that does nothing makes the test fail.
    private async Task TrackAndRemoveAsync(string key, string tag, Func<Task> remove)
    {
        var calls = 0;
        Task<string> CountingFactory(CancellationToken _) => Task.FromResult($"value-{Interlocked.Increment(ref calls)}");

        (await _real.GetOrSetAsync(key, CountingFactory, TimeSpan.FromMinutes(1), [tag], CancellationToken.None)).ShouldBe("value-1");
        (await _real.GetOrSetAsync(key, CountingFactory, TimeSpan.FromMinutes(1), [tag], CancellationToken.None)).ShouldBe("value-1");

        await remove();

        (await _real.GetOrSetAsync(key, CountingFactory, TimeSpan.FromMinutes(1), [tag], CancellationToken.None)).ShouldBe("value-2");
    }
}
