using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="CachedShardDirectoryStore"/>.
/// </summary>
public sealed class CachedShardDirectoryStoreTests
{
    private readonly IShardDirectoryStore _inner;
    private readonly ICacheProvider _cache;
    private readonly IOptions<DirectoryCacheOptions> _options;
    private readonly ILogger<CachedShardDirectoryStore> _logger;

    public CachedShardDirectoryStoreTests()
    {
        _inner = Substitute.For<IShardDirectoryStore>();
        _cache = Substitute.For<ICacheProvider>();
        _options = Options.Create(new DirectoryCacheOptions());
        _logger = NullLogger<CachedShardDirectoryStore>.Instance;
    }

    private CachedShardDirectoryStore CreateStore(
        DirectoryCacheOptions? options = null,
        IPubSubProvider? pubSub = null)
    {
        var opts = options is not null ? Options.Create(options) : _options;
        return new CachedShardDirectoryStore(_inner, _cache, opts, _logger, pubSub);
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor — null guards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardDirectoryStore(null!, _cache, _options, _logger));
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardDirectoryStore(_inner, null!, _options, _logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardDirectoryStore(_inner, _cache, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardDirectoryStore(_inner, _cache, _options, null!));
    }

    // ────────────────────────────────────────────────────────────
    //  GetMapping — L1 cache behavior
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetMapping_NullKey_ThrowsArgumentException()
    {
        var store = CreateStore();
        Should.Throw<ArgumentException>(() => store.GetMapping(null!));
    }

    [Fact]
    public void GetMapping_EmptyKey_ThrowsArgumentException()
    {
        var store = CreateStore();
        Should.Throw<ArgumentException>(() => store.GetMapping(""));
    }

    [Fact]
    public void GetMapping_CacheMiss_DelegatesToInner()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var store = CreateStore();

        var result = store.GetMapping("key-1");

        result.ShouldBe("shard-1");
        _inner.Received(1).GetMapping("key-1");
    }

    [Fact]
    public void GetMapping_CacheHit_DoesNotDelegateToInner()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var store = CreateStore();

        // First call populates L1 cache
        store.GetMapping("key-1");

        // Second call should use L1 cache
        var result = store.GetMapping("key-1");

        result.ShouldBe("shard-1");
        _inner.Received(1).GetMapping("key-1"); // Only called once
    }

    [Fact]
    public void GetMapping_InnerReturnsNull_DoesNotCacheNull()
    {
        _inner.GetMapping("missing-key").Returns((string?)null);
        var store = CreateStore();

        var result = store.GetMapping("missing-key");

        result.ShouldBeNull();

        // Second call should still go to inner (null not cached)
        store.GetMapping("missing-key");
        _inner.Received(2).GetMapping("missing-key");
    }

    // ────────────────────────────────────────────────────────────
    //  AddMapping — invalidation strategies
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddMapping_NullKey_ThrowsArgumentException()
    {
        var store = CreateStore();
        Should.Throw<ArgumentException>(() => store.AddMapping(null!, "shard-1"));
    }

    [Fact]
    public void AddMapping_NullShardId_ThrowsArgumentException()
    {
        var store = CreateStore();
        Should.Throw<ArgumentException>(() => store.AddMapping("key-1", null!));
    }

    [Fact]
    public void AddMapping_AlwaysDelegatesToInner()
    {
        var store = CreateStore();

        store.AddMapping("key-1", "shard-1");

        _inner.Received(1).AddMapping("key-1", "shard-1");
    }

    [Fact]
    public void AddMapping_ImmediateStrategy_InvalidatesL1Cache()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var opts = new DirectoryCacheOptions { InvalidationStrategy = CacheInvalidationStrategy.Immediate };
        var store = CreateStore(opts);

        // Populate L1
        store.GetMapping("key-1");
        _inner.Received(1).GetMapping("key-1");

        // Add mapping invalidates L1
        _inner.GetMapping("key-1").Returns("shard-2");
        store.AddMapping("key-1", "shard-2");

        // Next read goes to inner (L1 was invalidated)
        var result = store.GetMapping("key-1");
        result.ShouldBe("shard-2");
        _inner.Received(2).GetMapping("key-1");
    }

    [Fact]
    public void AddMapping_WriteThroughStrategy_UpdatesL1Cache()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var opts = new DirectoryCacheOptions { InvalidationStrategy = CacheInvalidationStrategy.WriteThrough };
        var store = CreateStore(opts);

        // Populate L1
        store.GetMapping("key-1");

        // Add mapping updates L1 in place
        store.AddMapping("key-1", "shard-2");

        // Next read uses L1 (no inner call)
        var result = store.GetMapping("key-1");
        result.ShouldBe("shard-2");
        _inner.Received(1).GetMapping("key-1"); // Only the initial call
    }

    [Fact]
    public void AddMapping_LazyStrategy_DoesNotTouchL1Cache()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var opts = new DirectoryCacheOptions { InvalidationStrategy = CacheInvalidationStrategy.Lazy };
        var store = CreateStore(opts);

        // Populate L1
        store.GetMapping("key-1");

        // Add mapping does NOT touch L1
        store.AddMapping("key-1", "shard-2");

        // L1 still has old value
        var result = store.GetMapping("key-1");
        result.ShouldBe("shard-1");
        _inner.Received(1).GetMapping("key-1");
    }

    // ────────────────────────────────────────────────────────────
    //  RemoveMapping — invalidation behavior
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveMapping_NullKey_ThrowsArgumentException()
    {
        var store = CreateStore();
        Should.Throw<ArgumentException>(() => store.RemoveMapping(null!));
    }

    [Fact]
    public void RemoveMapping_DelegatesToInner()
    {
        _inner.RemoveMapping("key-1").Returns(true);
        var store = CreateStore();

        var result = store.RemoveMapping("key-1");

        result.ShouldBeTrue();
        _inner.Received(1).RemoveMapping("key-1");
    }

    [Fact]
    public void RemoveMapping_ImmediateStrategy_InvalidatesL1Cache()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var opts = new DirectoryCacheOptions { InvalidationStrategy = CacheInvalidationStrategy.Immediate };
        var store = CreateStore(opts);

        // Populate L1
        store.GetMapping("key-1");

        // Remove invalidates L1
        _inner.RemoveMapping("key-1").Returns(true);
        _inner.GetMapping("key-1").Returns((string?)null);
        store.RemoveMapping("key-1");

        // Next read goes to inner
        store.GetMapping("key-1");
        _inner.Received(2).GetMapping("key-1");
    }

    [Fact]
    public void RemoveMapping_LazyStrategy_DoesNotTouchL1Cache()
    {
        _inner.GetMapping("key-1").Returns("shard-1");
        var opts = new DirectoryCacheOptions { InvalidationStrategy = CacheInvalidationStrategy.Lazy };
        var store = CreateStore(opts);

        // Populate L1
        store.GetMapping("key-1");

        // Remove with Lazy does NOT touch L1
        _inner.RemoveMapping("key-1").Returns(true);
        store.RemoveMapping("key-1");

        // L1 still has old value
        var result = store.GetMapping("key-1");
        result.ShouldBe("shard-1");
        _inner.Received(1).GetMapping("key-1");
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllMappings — bypasses cache
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllMappings_DelegatesToInner()
    {
        var mappings = new Dictionary<string, string> { ["k1"] = "s1", ["k2"] = "s2" };
        _inner.GetAllMappings().Returns(mappings);
        var store = CreateStore();

        var result = store.GetAllMappings();

        result.Count.ShouldBe(2);
        _inner.Received(1).GetAllMappings();
    }

    // ────────────────────────────────────────────────────────────
    //  RefreshL1FromInnerAsync
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshL1FromInnerAsync_RepopulatesL1CacheFromInner()
    {
        var mappings = new Dictionary<string, string> { ["k1"] = "s1", ["k2"] = "s2" };
        _inner.GetAllMappings().Returns(mappings);
        _inner.GetMapping("k1").Returns("s1");
        var store = CreateStore();

        await store.RefreshL1FromInnerAsync(CancellationToken.None);

        // L1 should now be populated — next GetMapping should not call inner
        var result = store.GetMapping("k1");
        result.ShouldBe("s1");
        _inner.Received(0).GetMapping("k1"); // Served from L1
    }

    [Fact]
    public async Task RefreshL1FromInnerAsync_UpdatesL2Cache()
    {
        _inner.GetAllMappings().Returns(new Dictionary<string, string>());
        var store = CreateStore();

        await store.RefreshL1FromInnerAsync(CancellationToken.None);

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshL1FromInnerAsync_InnerThrows_DoesNotThrow()
    {
        _inner.GetAllMappings().Returns(_ => throw new InvalidOperationException("Inner failed"));
        var store = CreateStore();

        await Should.NotThrowAsync(() => store.RefreshL1FromInnerAsync(CancellationToken.None));
    }
}
