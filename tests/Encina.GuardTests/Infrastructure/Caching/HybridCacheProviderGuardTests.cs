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
    private readonly HybridCacheProvider _sut;

    public HybridCacheProviderGuardTests()
    {
        _services = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider();
        _cache = _services.GetRequiredService<HybridCache>();
        _sut = new HybridCacheProvider(
            _cache,
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

    [Fact]
    public async Task EveryOperation_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var token = cts.Token;
        var tags = new[] { "tag" };

        var calls = new Func<Task>[]
        {
            () => _sut.GetAsync<string>("key", token),
            () => _sut.SetAsync("key", "value", null, token),
            () => _sut.RemoveAsync("key", token),
            () => _sut.RemoveByPatternAsync("key*", token),
            () => _sut.RemoveByTagAsync("tag", token),
            () => _sut.ExistsAsync("key", token),
            () => _sut.GetOrSetAsync("key", Factory, null, token),
            () => _sut.GetOrSetAsync("key", Factory, null, tags, token),
            () => _sut.SetWithSlidingExpirationAsync("key", "value", TimeSpan.FromMinutes(1), null, token),
            () => _sut.RefreshAsync("key", token),
        };

        foreach (var call in calls)
        {
            await Should.ThrowAsync<OperationCanceledException>(call);
        }
    }

    [Fact]
    public async Task RefreshAsync_ValidKey_ReturnsFalseBecauseRefreshIsUnsupported()
    {
        var refreshed = await _sut.RefreshAsync("key", CancellationToken.None);

        refreshed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_KeyNeverStored_ReturnsDefault()
    {
        var value = await _sut.GetAsync<string>("missing-key", CancellationToken.None);

        value.ShouldBeNull();
    }

    [Fact]
    public async Task GetOrSetAsyncWithTags_ValidArguments_ReturnsFactoryValue()
    {
        var value = await _sut.GetOrSetAsync(
            "tagged-key", Factory, TimeSpan.FromMinutes(1), ["products"], CancellationToken.None);

        value.ShouldBe("value");
    }

    [Fact]
    public async Task GetOrSetAsync_WithoutTags_ReturnsFactoryValue()
    {
        var value = await _sut.GetOrSetAsync("plain-key", Factory, null, CancellationToken.None);

        value.ShouldBe("value");
    }

    [Fact]
    public async Task GetAsync_AfterSetAsync_ReturnsStoredValue()
    {
        await _sut.SetAsync("stored-key", "stored", TimeSpan.FromMinutes(1), CancellationToken.None);

        var value = await _sut.GetAsync<string>("stored-key", CancellationToken.None);

        value.ShouldBe("stored");
    }

    [Theory]
    [InlineData("tag:products")]
    [InlineData("#products")]
    [InlineData("track-*")]
    public async Task RemoveByPatternAsync_AfterTrackingTaggedKey_AcceptsTagAndGlobPatterns(string pattern)
    {
        await _sut.GetOrSetAsync("track-key", Factory, null, ["products"], CancellationToken.None);

        await Should.NotThrowAsync(() => _sut.RemoveByPatternAsync(pattern, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_AfterTrackingTaggedKey_AcceptsTheKey()
    {
        await _sut.GetOrSetAsync("removable-key", Factory, null, ["orders"], CancellationToken.None);

        await Should.NotThrowAsync(() => _sut.RemoveAsync("removable-key", CancellationToken.None));
    }

    [Fact]
    public async Task RemoveByTagAsync_AfterTrackingTaggedKey_AcceptsTheTag()
    {
        await _sut.GetOrSetAsync("tag-removed-key", Factory, null, ["customers"], CancellationToken.None);

        await Should.NotThrowAsync(() => _sut.RemoveByTagAsync("customers", CancellationToken.None));
    }

    [Fact]
    public async Task SetAsyncAndSliding_ValidArguments_AreAccepted()
    {
        await _sut.SetAsync("k1", "v", null, CancellationToken.None);
        await _sut.SetWithSlidingExpirationAsync(
            "k2", "v", TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1), CancellationToken.None);
        await _sut.SetWithSlidingExpirationAsync("k3", "v", TimeSpan.FromMinutes(2), null, CancellationToken.None);

        (await _sut.ExistsAsync("never-stored", CancellationToken.None)).ShouldBeFalse();
    }
}
