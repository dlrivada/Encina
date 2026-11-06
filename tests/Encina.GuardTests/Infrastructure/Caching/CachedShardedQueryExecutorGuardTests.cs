using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Encina.Sharding.Execution;

namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for <see cref="CachedShardedQueryExecutor"/>: constructor and public-method
/// argument validation, plus the pass-through and cache-interaction paths with valid arguments.
/// </summary>
public sealed class CachedShardedQueryExecutorGuardTests
{
    private readonly IShardedQueryExecutor _inner = Substitute.For<IShardedQueryExecutor>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IPubSubProvider _pubSub = Substitute.For<IPubSubProvider>();
    private readonly ScatterGatherCacheOptions _options = new();

    private static readonly Func<string, CancellationToken, Task<Either<EncinaError, IReadOnlyList<int>>>> QueryFactory =
        (_, _) => Task.FromResult(Either<EncinaError, IReadOnlyList<int>>.Right(new List<int> { 1 }));

    private CachedShardedQueryExecutor CreateSut(IPubSubProvider? pubSub = null) =>
        new(_inner, _cache, Options.Create(_options), NullLogger<CachedShardedQueryExecutor>.Instance, pubSub);

    private static ShardedQueryResult<int> ResultOf(params int[] items) =>
        new(items, new List<string> { "shard-1" }, new List<ShardFailure>());

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachedShardedQueryExecutor(
            null!, _cache, Options.Create(_options), NullLogger<CachedShardedQueryExecutor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachedShardedQueryExecutor(
            _inner, null!, Options.Create(_options), NullLogger<CachedShardedQueryExecutor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachedShardedQueryExecutor(
            _inner, _cache, null!, NullLogger<CachedShardedQueryExecutor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachedShardedQueryExecutor(_inner, _cache, Options.Create(_options), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_IsAccepted()
    {
        Should.NotThrow(() => CreateSut(pubSub: null));
    }

    [Fact]
    public async Task ExecuteCachedAsync_NullShardIds_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => CreateSut().ExecuteCachedAsync<int>(null!, "key", QueryFactory));

        ex.ParamName.ShouldBe("shardIds");
    }

    [Fact]
    public async Task ExecuteCachedAsync_NullQueryFactory_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => CreateSut().ExecuteCachedAsync<int>(["shard-1"], "key", null!));

        ex.ParamName.ShouldBe("queryFactory");
    }

    [Fact]
    public async Task ExecuteCachedAsync_NullCacheKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => CreateSut().ExecuteCachedAsync(["shard-1"], null!, QueryFactory));

        ex.ParamName.ShouldBe("cacheKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteCachedAsync_EmptyOrWhitespaceCacheKey_ThrowsArgumentException(string cacheKey)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => CreateSut().ExecuteCachedAsync(["shard-1"], cacheKey, QueryFactory));

        ex.ParamName.ShouldBe("cacheKey");
    }

    [Fact]
    public async Task ExecuteAllCachedAsync_NullCacheKey_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => CreateSut().ExecuteAllCachedAsync(null!, QueryFactory));

        ex.ParamName.ShouldBe("cacheKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAllCachedAsync_EmptyOrWhitespaceCacheKey_ThrowsArgumentException(string cacheKey)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => CreateSut().ExecuteAllCachedAsync(cacheKey, QueryFactory));

        ex.ParamName.ShouldBe("cacheKey");
    }

    [Fact]
    public async Task ExecuteAllCachedAsync_NullQueryFactory_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => CreateSut().ExecuteAllCachedAsync<int>("key", null!));

        ex.ParamName.ShouldBe("queryFactory");
    }

    [Fact]
    public async Task InvalidateAsync_NullPattern_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => CreateSut().InvalidateAsync(null!));

        ex.ParamName.ShouldBe("cacheKeyPattern");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidateAsync_EmptyOrWhitespacePattern_ThrowsArgumentException(string pattern)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => CreateSut().InvalidateAsync(pattern));

        ex.ParamName.ShouldBe("cacheKeyPattern");
    }

    [Fact]
    public async Task InvalidateShardAsync_NullShardId_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(() => CreateSut().InvalidateShardAsync(null!));

        ex.ParamName.ShouldBe("shardId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvalidateShardAsync_EmptyOrWhitespaceShardId_ThrowsArgumentException(string shardId)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() => CreateSut().InvalidateShardAsync(shardId));

        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task ExecuteAsyncAndExecuteAllAsync_PassThroughToInnerWithoutTouchingTheCache()
    {
        var expected = Either<EncinaError, ShardedQueryResult<int>>.Right(ResultOf(1));
        _inner.ExecuteAsync(Arg.Any<IEnumerable<string>>(), QueryFactory, Arg.Any<CancellationToken>())
            .Returns(expected);
        _inner.ExecuteAllAsync(QueryFactory, Arg.Any<CancellationToken>()).Returns(expected);
        var sut = CreateSut();

        var one = await sut.ExecuteAsync(["shard-1"], QueryFactory);
        var all = await sut.ExecuteAllAsync(QueryFactory);

        one.IsRight.ShouldBeTrue();
        all.IsRight.ShouldBeTrue();
        _cache.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteCachedAsync_CacheMissThenSmallResult_ExecutesInnerAndStoresResult()
    {
        _cache.GetAsync<ShardedQueryResult<int>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShardedQueryResult<int>?>(null));
        _inner.ExecuteAsync(Arg.Any<IEnumerable<string>>(), QueryFactory, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<int>>.Right(ResultOf(1, 2)));

        var result = await CreateSut().ExecuteCachedAsync(["shard-1"], "orders", QueryFactory);

        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).SetAsync(
            "shard:scatter:orders",
            Arg.Any<ShardedQueryResult<int>>(),
            _options.DefaultCacheDuration,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteCachedAsync_CacheHit_ReturnsCachedWithoutExecutingInner()
    {
        _cache.GetAsync<ShardedQueryResult<int>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShardedQueryResult<int>?>(ResultOf(9)));

        var result = await CreateSut().ExecuteCachedAsync(["shard-1"], "orders", QueryFactory);

        result.IsRight.ShouldBeTrue();
        await _inner.DidNotReceiveWithAnyArgs().ExecuteAsync<int>(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAllCachedAsync_ResultAboveMaxSize_IsNotCached()
    {
        _options.MaxCachedResultSize = 1;
        _cache.GetAsync<ShardedQueryResult<int>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShardedQueryResult<int>?>(null));
        _inner.ExecuteAllAsync(QueryFactory, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardedQueryResult<int>>.Right(ResultOf(1, 2, 3)));

        var result = await CreateSut().ExecuteAllCachedAsync("big", QueryFactory);

        result.IsRight.ShouldBeTrue();
        await _cache.DidNotReceiveWithAnyArgs().SetAsync<ShardedQueryResult<int>>(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAllCachedAsync_CacheHit_ReturnsCachedWithoutExecutingInner()
    {
        _cache.GetAsync<ShardedQueryResult<int>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShardedQueryResult<int>?>(ResultOf(9)));

        var result = await CreateSut().ExecuteAllCachedAsync("all", QueryFactory);

        result.IsRight.ShouldBeTrue();
        await _inner.DidNotReceiveWithAnyArgs().ExecuteAllAsync<int>(default!, default);
    }

    [Fact]
    public async Task InvalidateAsyncAndInvalidateShardAsync_WithPubSub_RemoveByPatternAndPublish()
    {
        var sut = CreateSut(_pubSub);

        await sut.InvalidateAsync("orders:*");
        await sut.InvalidateShardAsync("shard-1");

        await _cache.Received(2).RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _pubSub.Received(2).PublishAsync(
            _options.InvalidationChannel, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateAsyncAndInvalidateShardAsync_WithoutPubSub_OnlyRemoveByPattern()
    {
        var sut = CreateSut();

        await sut.InvalidateAsync("orders:*");
        await sut.InvalidateShardAsync("shard-1");

        await _cache.Received(2).RemoveByPatternAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _pubSub.ReceivedCalls().ShouldBeEmpty();
    }
}
