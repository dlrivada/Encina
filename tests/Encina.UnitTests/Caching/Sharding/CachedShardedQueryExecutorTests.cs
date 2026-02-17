using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Encina.Sharding.Execution;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="CachedShardedQueryExecutor"/>.
/// </summary>
public sealed class CachedShardedQueryExecutorTests
{
    private readonly IShardedQueryExecutor _inner;
    private readonly ICacheProvider _cache;
    private readonly IOptions<ScatterGatherCacheOptions> _options;
    private readonly ILogger<CachedShardedQueryExecutor> _logger;

    public CachedShardedQueryExecutorTests()
    {
        _inner = Substitute.For<IShardedQueryExecutor>();
        _cache = Substitute.For<ICacheProvider>();
        _options = Options.Create(new ScatterGatherCacheOptions());
        _logger = NullLogger<CachedShardedQueryExecutor>.Instance;
    }

    private CachedShardedQueryExecutor CreateExecutor(
        ScatterGatherCacheOptions? options = null,
        IPubSubProvider? pubSub = null)
    {
        var opts = options is not null ? Options.Create(options) : _options;
        return new CachedShardedQueryExecutor(_inner, _cache, opts, _logger, pubSub);
    }

    private static ShardedQueryResult<string> CreateResult(params string[] items)
    {
        return new ShardedQueryResult<string>(
            items.ToList(),
            ["shard-1"],
            []);
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor — null guards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardedQueryExecutor(null!, _cache, _options, _logger));
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardedQueryExecutor(_inner, null!, _options, _logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardedQueryExecutor(_inner, _cache, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardedQueryExecutor(_inner, _cache, _options, null!));
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAsync — pass-through (no caching)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DelegatesToInnerWithoutCaching()
    {
        var expected = CreateResult("item-1");
        _inner.ExecuteAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<string>>.Right(expected));

        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string> { "item-1" }));

        var result = await executor.ExecuteAsync(["shard-1"], factory);

        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>());

        // Cache should NOT be called for non-cached execution
        await _cache.DidNotReceive().GetAsync<ShardedQueryResult<string>>(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAllAsync — pass-through (no caching)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAllAsync_DelegatesToInnerWithoutCaching()
    {
        var expected = CreateResult("item-1");
        _inner.ExecuteAllAsync(
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<string>>.Right(expected));

        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string> { "item-1" }));

        var result = await executor.ExecuteAllAsync(factory);

        result.IsRight.ShouldBeTrue();
        await _cache.DidNotReceive().GetAsync<ShardedQueryResult<string>>(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteCachedAsync — cache miss
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCachedAsync_CacheMiss_DelegatesToInnerAndCaches()
    {
        var expected = CreateResult("item-1");

        _cache.GetAsync<ShardedQueryResult<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ShardedQueryResult<string>?)null);

        _inner.ExecuteAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<string>>.Right(expected));

        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string> { "item-1" }));

        var result = await executor.ExecuteCachedAsync(["shard-1"], "test-query", factory);

        result.IsRight.ShouldBeTrue();

        // Should have checked cache
        await _cache.Received(1).GetAsync<ShardedQueryResult<string>>(
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Should have stored in cache
        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<ShardedQueryResult<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteCachedAsync — cache hit
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCachedAsync_CacheHit_ReturnsCachedResultWithoutCallingInner()
    {
        var cached = CreateResult("cached-item");

        _cache.GetAsync<ShardedQueryResult<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);

        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string>()));

        var result = await executor.ExecuteCachedAsync(["shard-1"], "test-query", factory);

        result.IsRight.ShouldBeTrue();

        // Inner executor should NOT have been called
        await _inner.DidNotReceive().ExecuteAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteCachedAsync — result size limit
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCachedAsync_ExceedsMaxSize_DoesNotCache()
    {
        var largeResult = new ShardedQueryResult<string>(
            Enumerable.Range(0, 20_000).Select(i => $"item-{i}").ToList(),
            ["shard-1"],
            []);

        _cache.GetAsync<ShardedQueryResult<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ShardedQueryResult<string>?)null);

        _inner.ExecuteAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<string>>.Right(largeResult));

        var executor = CreateExecutor(new ScatterGatherCacheOptions { MaxCachedResultSize = 10_000 });

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string>()));

        await executor.ExecuteCachedAsync(["shard-1"], "large-query", factory);

        // Should NOT have stored in cache (exceeded max size)
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<ShardedQueryResult<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteCachedAsync — null guards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteCachedAsync_NullShardIds_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string>()));

        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteCachedAsync(null!, "key", factory));
    }

    [Fact]
    public async Task ExecuteCachedAsync_NullCacheKey_ThrowsArgumentException()
    {
        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string>()));

        await Should.ThrowAsync<ArgumentException>(() =>
            executor.ExecuteCachedAsync(["shard-1"], null!, factory));
    }

    [Fact]
    public async Task ExecuteCachedAsync_EmptyCacheKey_ThrowsArgumentException()
    {
        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string>()));

        await Should.ThrowAsync<ArgumentException>(() =>
            executor.ExecuteCachedAsync(["shard-1"], "", factory));
    }

    [Fact]
    public async Task ExecuteCachedAsync_NullQueryFactory_ThrowsArgumentNullException()
    {
        var executor = CreateExecutor();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            executor.ExecuteCachedAsync<string>(["shard-1"], "key", null!));
    }

    // ────────────────────────────────────────────────────────────
    //  ExecuteAllCachedAsync — cache miss delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAllCachedAsync_CacheMiss_DelegatesToInner()
    {
        var expected = CreateResult("item-1");

        _cache.GetAsync<ShardedQueryResult<string>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ShardedQueryResult<string>?)null);

        _inner.ExecuteAllAsync(
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<string>>.Right(expected));

        var executor = CreateExecutor();

        Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>> factory =
            (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<string>>.Right(
                (IReadOnlyList<string>)new List<string> { "item-1" }));

        var result = await executor.ExecuteAllCachedAsync("all-query", factory);

        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).ExecuteAllAsync(
            Arg.Any<Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<string>>>>>(),
            Arg.Any<CancellationToken>());
    }

    // ────────────────────────────────────────────────────────────
    //  InvalidateAsync
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateAsync_RemovesCacheByPattern()
    {
        var executor = CreateExecutor();

        await executor.InvalidateAsync("orders:*");

        await _cache.Received(1).RemoveByPatternAsync(
            "shard:scatter:orders:*",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAsync_NullPattern_ThrowsArgumentException()
    {
        var executor = CreateExecutor();

        await Should.ThrowAsync<ArgumentException>(() => executor.InvalidateAsync(null!));
    }

    [Fact]
    public async Task InvalidateAsync_WithPubSub_PublishesInvalidationMessage()
    {
        var pubSub = Substitute.For<IPubSubProvider>();
        var executor = CreateExecutor(pubSub: pubSub);

        await executor.InvalidateAsync("orders:*");

        await pubSub.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAsync_WithoutPubSub_DoesNotThrow()
    {
        var executor = CreateExecutor(pubSub: null);

        await Should.NotThrowAsync(() => executor.InvalidateAsync("orders:*"));
    }

    // ────────────────────────────────────────────────────────────
    //  InvalidateShardAsync
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateShardAsync_RemovesCacheByShardPattern()
    {
        var executor = CreateExecutor();

        await executor.InvalidateShardAsync("shard-1");

        await _cache.Received(1).RemoveByPatternAsync(
            "shard:scatter:shard-1:*",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateShardAsync_NullShardId_ThrowsArgumentException()
    {
        var executor = CreateExecutor();

        await Should.ThrowAsync<ArgumentException>(() => executor.InvalidateShardAsync(null!));
    }
}
