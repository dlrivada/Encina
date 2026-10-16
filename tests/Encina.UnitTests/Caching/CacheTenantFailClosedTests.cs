using Encina.Caching;
using Encina.Testing;
using Encina.Testing.Fakes.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// With multi-tenancy enabled (the <see cref="MultiTenancyMarker"/> registered) and no resolved tenant,
/// query caching and cache invalidation fail closed for the cache (#1913): no read, no write, no removal,
/// a log with an EventId and no ids. Without multi-tenancy the behavior is unchanged.
/// </summary>
public sealed class CacheTenantFailClosedTests : IDisposable
{
    [Cache(DurationSeconds = 300)]
    public sealed record TenantQuery(string Value) : IRequest<string>;

    [Cache(DurationSeconds = 300, VaryByTenant = false)]
    public sealed record SharedQuery(string Value) : IRequest<string>;

    [InvalidatesCache(KeyPattern = "orders:*")]
    public sealed record InvalidatingCommand : IRequest<string>;

    private readonly FakeCacheProvider _cache = new();
    private readonly FakeLogger<QueryCachingPipelineBehavior<TenantQuery, string>> _queryLogger = new();
    private readonly FakeLogger<CacheInvalidationPipelineBehavior<InvalidatingCommand, string>> _invalidationLogger = new();
    private readonly IOptions<CachingOptions> _options = Options.Create(new CachingOptions
    {
        EnableQueryCaching = true,
        EnableCacheInvalidation = true,
        EnablePubSubInvalidation = true
    });

    public void Dispose() => _cache.Dispose();

    private QueryCachingPipelineBehavior<TenantQuery, string> TenantBehavior(bool multiTenant) =>
        new(_cache, new DefaultCacheKeyGenerator(_options), _options, _queryLogger, null, multiTenant ? new MultiTenancyMarker() : null);

    private static RequestHandlerCallback<string> Handler(Func<string> produce, Action onCall) => () =>
    {
        onCall();
        return ValueTask.FromResult(Right<EncinaError, string>(produce()));
    };

    [Fact]
    public async Task TwoTenantLessCalls_WithTenancyOn_NeverShareACachedResponse()
    {
        var sut = TenantBehavior(multiTenant: true);
        var calls = 0;

        var first = await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "tenant-a-data", () => calls++), CancellationToken.None);
        var second = await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "tenant-b-data", () => calls++), CancellationToken.None);

        first.ShouldBeSuccess().ShouldBe("tenant-a-data");
        second.ShouldBeSuccess().ShouldBe("tenant-b-data");
        calls.ShouldBe(2);
        _cache.Count.ShouldBe(0);
        _cache.GetOperations.ShouldBeEmpty();
        _cache.CachedKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABlankTenant_WithTenancyOn_IsTreatedAsMissing()
    {
        var sut = TenantBehavior(multiTenant: true);
        var calls = 0;

        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(tenantId: "  "), Handler(() => "x", () => calls++), CancellationToken.None);
        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(tenantId: "  "), Handler(() => "x", () => calls++), CancellationToken.None);

        calls.ShouldBe(2);
        _cache.Count.ShouldBe(0);
    }

    [Fact]
    public async Task TheBypass_IsLogged_WithTheEventIdAndTheRequestTypeOnly()
    {
        var sut = TenantBehavior(multiTenant: true);

        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "x", () => { }), CancellationToken.None);

        var record = _queryLogger.Collector.GetSnapshot().Single(r => r.Id.Id == 3513);
        record.Level.ShouldBe(LogLevel.Debug);
        record.Message.ShouldContain(nameof(TenantQuery));
        record.StructuredState!.Select(p => p.Key).ShouldBe(["RequestType", "{OriginalFormat}"], ignoreOrder: true);
    }

    [Fact]
    public async Task ATenantQuery_WithATenant_UnderTenancyOn_IsCachedPerTenant()
    {
        var sut = TenantBehavior(multiTenant: true);
        var calls = 0;

        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(tenantId: "tenant-1"), Handler(() => "one", () => calls++), CancellationToken.None);
        var again = await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(tenantId: "tenant-1"), Handler(() => "never", () => calls++), CancellationToken.None);
        var other = await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(tenantId: "tenant-2"), Handler(() => "two", () => calls++), CancellationToken.None);

        again.ShouldBeSuccess().ShouldBe("one");
        other.ShouldBeSuccess().ShouldBe("two");
        calls.ShouldBe(2);
        _queryLogger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3513);
    }

    [Fact]
    public async Task WithTenancyOff_ATenantLessCall_IsCachedAsBefore()
    {
        var sut = TenantBehavior(multiTenant: false);
        var calls = 0;

        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "first", () => calls++), CancellationToken.None);
        var second = await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "never", () => calls++), CancellationToken.None);

        second.ShouldBeSuccess().ShouldBe("first");
        calls.ShouldBe(1);
        _queryLogger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3513);
    }

    [Fact]
    public async Task AQueryThatDoesNotVaryByTenant_IsCached_EvenWithTenancyOnAndNoTenant()
    {
        var logger = new FakeLogger<QueryCachingPipelineBehavior<SharedQuery, string>>();
        var sut = new QueryCachingPipelineBehavior<SharedQuery, string>(
            _cache, new DefaultCacheKeyGenerator(_options), _options, logger, null, new MultiTenancyMarker());
        var calls = 0;

        await sut.Handle(new SharedQuery("q"), RequestContext.CreateForTest(), Handler(() => "shared", () => calls++), CancellationToken.None);
        var second = await sut.Handle(new SharedQuery("q"), RequestContext.CreateForTest(), Handler(() => "never", () => calls++), CancellationToken.None);

        second.ShouldBeSuccess().ShouldBe("shared");
        calls.ShouldBe(1);
    }

    [Cache(DurationSeconds = 300, SlidingExpiration = true)]
    public sealed record SlidingQuery(string Value) : IRequest<string>;

    [Fact]
    public async Task ASlidingEntry_IsStoredAndRefreshedOnHit_UnderTenancyOn()
    {
        var sut = new QueryCachingPipelineBehavior<SlidingQuery, string>(
            _cache, new DefaultCacheKeyGenerator(_options), _options,
            new FakeLogger<QueryCachingPipelineBehavior<SlidingQuery, string>>(), null, new MultiTenancyMarker());
        var calls = 0;

        await sut.Handle(new SlidingQuery("q"), RequestContext.CreateForTest(tenantId: "t"), Handler(() => "v", () => calls++), CancellationToken.None);
        var second = await sut.Handle(new SlidingQuery("q"), RequestContext.CreateForTest(tenantId: "t"), Handler(() => "never", () => calls++), CancellationToken.None);

        second.ShouldBeSuccess().ShouldBe("v");
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task ACacheError_WithThrowOnCacheErrors_Propagates_AndWithoutItRunsTheQuery()
    {
        var throwing = Options.Create(new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = true });
        var quiet = Options.Create(new CachingOptions { EnableQueryCaching = true });
        _cache.SimulateErrors = true;
        var strict = new QueryCachingPipelineBehavior<TenantQuery, string>(
            _cache, new DefaultCacheKeyGenerator(throwing), throwing, _queryLogger, null, new MultiTenancyMarker());
        var lenient = new QueryCachingPipelineBehavior<TenantQuery, string>(
            _cache, new DefaultCacheKeyGenerator(quiet), quiet, _queryLogger, null, new MultiTenancyMarker());
        var context = RequestContext.CreateForTest(tenantId: "t");

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await strict.Handle(new TenantQuery("q"), context, Handler(() => "x", () => { }), CancellationToken.None));
        var result = await lenient.Handle(new TenantQuery("q"), context, Handler(() => "x", () => { }), CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("x");
    }

    private readonly FakePubSubProvider _pubSub = new();

    private async Task SeedTenantEntriesAsync()
    {
        var prefix = _options.Value.KeyPrefix;
        foreach (var key in new[] { $"{prefix}:t:a:orders:1", $"{prefix}:t:b:orders:1", $"{prefix}:orders:1" })
        {
            await _cache.SetAsync(key, "v", TimeSpan.FromMinutes(5), CancellationToken.None);
        }
    }

    private CacheInvalidationPipelineBehavior<InvalidatingCommand, string> InvalidationBehavior(bool multiTenant) =>
        new(_cache, new DefaultCacheKeyGenerator(_options), _options, _invalidationLogger, _pubSub, multiTenant ? new MultiTenancyMarker() : null);

    private static RequestHandlerCallback<string> Ok() => () => ValueTask.FromResult(Right<EncinaError, string>("done"));

    [Fact]
    public async Task TheContainer_InjectsTheRegisteredMarker_IntoTheCachingBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICacheProvider>(_cache);
        services.AddEncinaCaching(o => o.EnableQueryCaching = true);
        services.AddSingleton<MultiTenancyMarker>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var sut = scope.ServiceProvider.GetServices<IPipelineBehavior<TenantQuery, string>>()
            .OfType<QueryCachingPipelineBehavior<TenantQuery, string>>().Single();
        var calls = 0;

        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "x", () => calls++), CancellationToken.None);
        await sut.Handle(new TenantQuery("q"), RequestContext.CreateForTest(), Handler(() => "x", () => calls++), CancellationToken.None);

        calls.ShouldBe(2);
        _cache.Count.ShouldBe(0);
    }

    [Fact]
    public async Task AnInvalidationWithoutATenant_UnderTenancyOn_RemovesNothing_AndIsLogged()
    {
        await SeedTenantEntriesAsync();
        var sut = InvalidationBehavior(multiTenant: true);

        var result = await sut.Handle(new InvalidatingCommand(), RequestContext.CreateForTest(), Ok(), CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("done");
        _cache.RemovedKeys.ShouldBeEmpty();
        _cache.Count.ShouldBe(3);
        _pubSub.TotalPublishedCount.ShouldBe(0);
        var record = _invalidationLogger.Collector.GetSnapshot().Single(r => r.Id.Id == 3514);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(nameof(InvalidatingCommand));
        record.StructuredState!.Select(p => p.Key).ShouldBe(["RequestType", "{OriginalFormat}"], ignoreOrder: true);
    }

    [Fact]
    public async Task AnInvalidationWithATenant_UnderTenancyOn_RemovesOnlyThatTenantsEntries()
    {
        await SeedTenantEntriesAsync();
        var sut = InvalidationBehavior(multiTenant: true);
        var prefix = _options.Value.KeyPrefix;

        await sut.Handle(new InvalidatingCommand(), RequestContext.CreateForTest(tenantId: "a"), Ok(), CancellationToken.None);

        _cache.RemovedKeys.ShouldBe([$"{prefix}:t:a:orders:1"]);
        _cache.Count.ShouldBe(2);
        _invalidationLogger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3514);
    }

    [Fact]
    public async Task WithTenancyOff_AnInvalidationWithoutATenant_RunsAsBefore()
    {
        await SeedTenantEntriesAsync();
        var sut = InvalidationBehavior(multiTenant: false);
        var prefix = _options.Value.KeyPrefix;

        await sut.Handle(new InvalidatingCommand(), RequestContext.CreateForTest(), Ok(), CancellationToken.None);

        _cache.RemovedKeys.ShouldBe([$"{prefix}:orders:1"]);
        _invalidationLogger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3514);
    }
}
