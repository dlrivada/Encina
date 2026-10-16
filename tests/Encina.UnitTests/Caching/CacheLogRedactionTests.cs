using Encina.Caching;
using Encina.Caching.Hybrid;
using Encina.Caching.Memory;
using Encina.Caching.Redis;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Encina.Sharding.Execution;
using Encina.Testing;
using Encina.Testing.Fakes.Providers;
using Encina.Testing.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// No cache log entry may contain a user id or a tenant id (#1953): hit, miss, store, failure,
/// invalidation and the provider-level key logs all write the key with its identifying segments redacted.
/// </summary>
public sealed class CacheLogRedactionTests
{
    private const string UserId = "alice@example.org";
    private const string TenantId = "tenant-acme";

    [Cache(DurationSeconds = 300, VaryByUser = true, VaryByTenant = true)]
    public sealed record PerUserQuery(string Value) : IRequest<string>;

    [InvalidatesCache("orders:{Id}:*")]
    public sealed record ChangeOrder(string Id) : IRequest<string>;

    // ────────────────────────────────────────────────────────────
    //  The helper
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("encina:t:tenant-acme:u:alice@example.org:Q:3F2A", "encina:t:***:u:***:Q:3F2A")]
    [InlineData("encina:t:tenant-acme:Q:3F2A", "encina:t:***:Q:3F2A")]
    [InlineData("t:tenant-acme:*", "t:***:*")]
    [InlineData("encina:t:*:Q:*", "encina:t:***:Q:*")]
    [InlineData("encina:orders:42", "encina:orders:42")]
    public void Redact_MasksTheTenantAndUserSegments(string key, string expected) =>
        CacheKeyRedactor.Redact(key).ShouldBe(expected);

    [Fact]
    public void Redact_WithKnownIdsContainingTheSeparator_MasksThemWhole()
    {
        var key = "encina:t:acme:eu:u:auth:alice:Q:3F2A";

        var redacted = CacheKeyRedactor.Redact(key, tenantId: "acme:eu", userId: "auth:alice");

        redacted.ShouldBe("encina:t:***:u:***:Q:3F2A");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_WithoutKey_ReturnsEmpty(string? key) =>
        CacheKeyRedactor.Redact(key).ShouldBeEmpty();

    // ────────────────────────────────────────────────────────────
    //  QueryCachingPipelineBehavior: hit, miss, store, failure
    // ────────────────────────────────────────────────────────────

    private static QueryCachingPipelineBehavior<PerUserQuery, string> CreateQueryBehavior(
        ICacheProvider cacheProvider,
        FakeLogger<QueryCachingPipelineBehavior<PerUserQuery, string>> logger)
    {
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        return new QueryCachingPipelineBehavior<PerUserQuery, string>(
            cacheProvider, new DefaultCacheKeyGenerator(options), options, logger);
    }

    private static ValueTask<Either<EncinaError, string>> Fresh() =>
        ValueTask.FromResult(Right<EncinaError, string>("fresh"));

    private static IRequestContext Caller() =>
        TestRequestContext.For(TestIdentity.User(UserId), tenantId: TenantId);

    private static void ShouldHoldNoIdentifier(FakeLogger logger)
    {
        var records = logger.Collector.GetSnapshot();
        records.ShouldNotBeEmpty();
        foreach (var record in records)
        {
            record.Message.ShouldNotContain(UserId);
            record.Message.ShouldNotContain(TenantId);
            foreach (var pair in record.StructuredState ?? [])
            {
                (pair.Value ?? string.Empty).ShouldNotContain(UserId);
                (pair.Value ?? string.Empty).ShouldNotContain(TenantId);
            }
        }
    }

    [Fact]
    public async Task QueryCache_MissStoreAndHit_LogTheRedactedKeyOnly()
    {
        using var cacheProvider = new FakeCacheProvider();
        var logger = new FakeLogger<QueryCachingPipelineBehavior<PerUserQuery, string>>();
        var sut = CreateQueryBehavior(cacheProvider, logger);

        await sut.Handle(new PerUserQuery("q"), Caller(), Fresh, CancellationToken.None);   // miss + store
        await sut.Handle(new PerUserQuery("q"), Caller(), Fresh, CancellationToken.None);   // hit

        var snapshot = logger.Collector.GetSnapshot();
        snapshot.ShouldContain(r => r.Id.Id == 3508);
        snapshot.ShouldContain(r => r.Id.Id == 3509);
        snapshot.ShouldContain(r => r.Id.Id == 3510);
        snapshot.ShouldAllBe(r => r.Message.Contains(":t:***:u:***:PerUserQuery:"));
        ShouldHoldNoIdentifier(logger);
    }

    [Fact]
    public async Task QueryCache_FailureWarning_LogsTheRedactedKeyOnly()
    {
        var cacheProvider = new FailingCacheProvider(failOnGet: true, $"boom {UserId}");
        var logger = new FakeLogger<QueryCachingPipelineBehavior<PerUserQuery, string>>();
        var sut = CreateQueryBehavior(cacheProvider, logger);

        await sut.Handle(new PerUserQuery("q"), Caller(), Fresh, CancellationToken.None);

        var warning = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 3511);
        warning.Message.ShouldContain(":t:***:u:***:PerUserQuery:");
        // ForLogging(): type and stack trace only, never the message.
        (warning.Exception?.ToString() ?? string.Empty).ShouldNotContain(UserId);
        ShouldHoldNoIdentifier(logger);
    }

    [Fact]
    public async Task QueryCache_StoreFailureWarning_LogsTheRedactedKeyOnly()
    {
        var failing = new FailingCacheProvider(failOnGet: false, $"boom {TenantId}");
        var logger = new FakeLogger<QueryCachingPipelineBehavior<PerUserQuery, string>>();
        var sut = CreateQueryBehavior(failing, logger);

        await sut.Handle(new PerUserQuery("q"), Caller(), Fresh, CancellationToken.None);

        logger.Collector.GetSnapshot().ShouldContain(r => r.Id.Id == 3511);
        ShouldHoldNoIdentifier(logger);
    }

    // A provider whose reads (or writes) fail with a message that carries an identifier.
    private sealed class FailingCacheProvider(bool failOnGet, string message) : ICacheProvider
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            failOnGet ? throw new InvalidOperationException(message) : Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken) =>
            failOnGet ? Task.CompletedTask : throw new InvalidOperationException(message);

        public Task SetWithSlidingExpirationAsync<T>(
            string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);

        public Task RemoveAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<T> GetOrSetAsync<T>(
            string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration, CancellationToken cancellationToken) =>
            factory(cancellationToken);

        public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    // ────────────────────────────────────────────────────────────
    //  CacheInvalidationPipelineBehavior
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Invalidation_LogsTheRedactedPatternOnly()
    {
        using var cacheProvider = new FakeCacheProvider();
        var pubSub = new FakePubSubProvider();
        var logger = new FakeLogger<CacheInvalidationPipelineBehavior<ChangeOrder, string>>();
        var options = Options.Create(new CachingOptions { EnableCacheInvalidation = true, EnablePubSubInvalidation = true });
        var sut = new CacheInvalidationPipelineBehavior<ChangeOrder, string>(
            cacheProvider, new DefaultCacheKeyGenerator(options), options, logger, pubSub);

        await sut.Handle(new ChangeOrder("7"), Caller(), Fresh, CancellationToken.None);

        var snapshot = logger.Collector.GetSnapshot();
        snapshot.ShouldContain(r => r.Id.Id == 3500);
        snapshot.ShouldContain(r => r.Id.Id == 3501);
        snapshot.ShouldAllBe(r => r.Message.Contains(":t:***:orders:7:*"));
        ShouldHoldNoIdentifier(logger);
    }

    // ────────────────────────────────────────────────────────────
    //  Scatter-gather executor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScatterGather_LogsTheRedactedKeyOnly()
    {
        var inner = Substitute.For<IShardedQueryExecutor>();
        var cache = Substitute.For<ICacheProvider>();
        var logger = new FakeLogger<CachedShardedQueryExecutor>();
        var sut = new CachedShardedQueryExecutor(inner, cache, Options.Create(new ScatterGatherCacheOptions()), logger);
        cache.GetAsync<ShardedQueryResult<string>>(default!, default)
            .ReturnsForAnyArgs(new ShardedQueryResult<string>(["x"], ["shard-1"], []));

        await sut.ExecuteCachedAsync<string>(
            ["shard-1"],
            $"t:{TenantId}:u:{UserId}:report",
            (_, _) => throw new InvalidOperationException("not reached"));
        await sut.InvalidateAsync($"t:{TenantId}:*");

        logger.Collector.GetSnapshot().Count.ShouldBe(2);
        ShouldHoldNoIdentifier(logger);
    }

    // ────────────────────────────────────────────────────────────
    //  Providers: Memory, Redis (also Valkey, Dragonfly, Garnet, KeyDB), Hybrid
    // ────────────────────────────────────────────────────────────

    private static string RealKey => $"encina:t:{TenantId}:u:{UserId}:PerUserQuery:3F2A";

    [Fact]
    public async Task MemoryProvider_LogsTheRedactedKeyOnly()
    {
        using var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            Options.Create(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        var logger = new FakeLogger<MemoryCacheProvider>();
        var sut = new MemoryCacheProvider(memoryCache, Options.Create(new global::Encina.Caching.Memory.MemoryCacheOptions()), logger);

        await sut.GetAsync<string>(RealKey, default);                                          // miss
        await sut.SetAsync(RealKey, "v", TimeSpan.FromMinutes(1), default);                    // set
        await sut.GetAsync<string>(RealKey, default);                                          // hit
        await sut.SetWithSlidingExpirationAsync(RealKey, "v", TimeSpan.FromMinutes(1), null, default);
        await sut.RemoveAsync(RealKey, default);
        await sut.RemoveByPatternAsync($"encina:t:{TenantId}:*", default);

        logger.Collector.Count.ShouldBeGreaterThanOrEqualTo(6);
        ShouldHoldNoIdentifier(logger);
    }

    [Fact]
    public async Task RedisProvider_LogsTheRedactedKeyOnly()
    {
        var connection = Substitute.For<IConnectionMultiplexer>();
        var database = Substitute.For<IDatabase>();
        var server = Substitute.For<IServer>();
        connection.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        connection.GetEndPoints(Arg.Any<bool>()).Returns([new System.Net.DnsEndPoint("localhost", 6379)]);
        connection.GetServer(Arg.Any<System.Net.EndPoint>(), Arg.Any<object?>()).Returns(server);
        var logger = new FakeLogger<RedisCacheProvider>();
        var sut = new RedisCacheProvider(connection, Options.Create(new RedisCacheOptions { KeyPrefix = "test" }), logger);

        await sut.GetAsync<string>(RealKey, default);                                          // miss
        database.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns((RedisValue)"\"v\"");
        await sut.GetAsync<string>(RealKey, default);                                          // hit
        await sut.SetAsync(RealKey, "v", TimeSpan.FromMinutes(1), default);
        await sut.SetWithSlidingExpirationAsync(RealKey, "v", TimeSpan.FromMinutes(1), null, default);
        await sut.RemoveAsync(RealKey, default);
        await sut.RemoveByPatternAsync($"encina:t:{TenantId}:*", default);

        logger.Collector.Count.ShouldBeGreaterThanOrEqualTo(6);
        ShouldHoldNoIdentifier(logger);
    }

    [Fact]
    public async Task HybridProvider_LogsTheRedactedKeyOnly()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddDistributedMemoryCache();
        using var serviceProvider = services.BuildServiceProvider();
        var logger = new FakeLogger<HybridCacheProvider>();
        var sut = new HybridCacheProvider(
            serviceProvider.GetRequiredService<HybridCache>(),
            Options.Create(new HybridCacheProviderOptions()),
            logger);

        await sut.GetAsync<string>(RealKey, default);                                          // miss
        await sut.SetAsync(RealKey, "v", TimeSpan.FromMinutes(1), default);
        await sut.GetAsync<string>(RealKey, default);                                          // hit
        await sut.SetWithSlidingExpirationAsync(RealKey, "v", TimeSpan.FromMinutes(1), null, default);
        await sut.RemoveAsync(RealKey, default);
        await sut.RemoveByPatternAsync($"encina:t:{TenantId}:*", default);

        logger.Collector.Count.ShouldBeGreaterThanOrEqualTo(5);
        ShouldHoldNoIdentifier(logger);
    }
}
