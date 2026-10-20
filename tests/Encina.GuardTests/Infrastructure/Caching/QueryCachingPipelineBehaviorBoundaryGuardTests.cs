using Encina.Testing.Identity;

namespace Encina.GuardTests.Infrastructure.Caching;

/// <summary>
/// Guard tests for the fail-closed boundaries of <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/>:
/// missing tenant, non-user identity, disabled caching and failing cache providers.
/// </summary>
public class QueryCachingPipelineBehaviorBoundaryGuardTests
{
    private readonly FakeCache _cache = new();
    private readonly IOptions<CachingOptions> _options = Options.Create(new CachingOptions { EnableQueryCaching = true });

    private QueryCachingPipelineBehavior<TRequest, string> Create<TRequest>(
        IOptions<CachingOptions>? options = null, bool multiTenant = false)
        where TRequest : IRequest<string> =>
        new(
            _cache,
            new DefaultCacheKeyGenerator(options ?? _options),
            options ?? _options,
            NullLogger<QueryCachingPipelineBehavior<TRequest, string>>.Instance,
            null,
            multiTenant ? new MultiTenancyMarker() : null);

    private static IRequestContext Context(string? tenant, RequestIdentity identity)
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenant);
        context.Identity.Returns(identity);
        context.CorrelationId.Returns("corr");
        return context;
    }

    private static RequestHandlerCallback<string> Next(Action? onCall = null) => () =>
    {
        onCall?.Invoke();
        return ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("value"));
    };

    [Fact]
    public async Task Handle_CachingDisabled_BypassesCacheWithoutTouchingProvider()
    {
        var behavior = Create<TenantQuery>(Options.Create(new CachingOptions { EnableQueryCaching = false }));

        var result = await behavior.Handle(new TenantQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _cache.Gets.ShouldBe(0);
        _cache.Sets.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_MultiTenantWithoutTenant_BypassesCache()
    {
        var behavior = Create<TenantQuery>(multiTenant: true);
        var calls = 0;

        await behavior.Handle(new TenantQuery(), Context(null, TestIdentity.User("u")), Next(() => calls++), CancellationToken.None);

        calls.ShouldBe(1);
        _cache.Gets.ShouldBe(0);
        _cache.Sets.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_VaryByUserWithServiceIdentity_BypassesCache()
    {
        var behavior = Create<UserQuery>();

        await behavior.Handle(new UserQuery(), Context("t", TestIdentity.Service("svc")), Next(), CancellationToken.None);

        _cache.Gets.ShouldBe(0);
        _cache.Sets.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_CacheMiss_StoresSuccessfulResultWithAbsoluteExpiration()
    {
        var behavior = Create<TenantQuery>();

        var result = await behavior.Handle(new TenantQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        _cache.Sets.ShouldBe(1);
        _cache.SlidingSets.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_CacheMissWithSlidingExpiration_StoresWithSlidingExpiration()
    {
        var behavior = Create<SlidingQuery>();

        await behavior.Handle(new SlidingQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None);

        _cache.SlidingSets.ShouldBe(1);
        _cache.Sets.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_SecondCall_ServesCachedValueWithoutRunningHandler()
    {
        var behavior = Create<SlidingQuery>();
        var context = Context("t", TestIdentity.User("u"));
        var calls = 0;
        await behavior.Handle(new SlidingQuery(), context, Next(() => calls++), CancellationToken.None);

        var result = await behavior.Handle(new SlidingQuery(), context, Next(() => calls++), CancellationToken.None);

        calls.ShouldBe(1);
        result.IsRight.ShouldBeTrue();
        _cache.Refreshes.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_CacheReadFails_RunsHandlerWhenErrorsAreNotThrown()
    {
        var behavior = Create<TenantQuery>();
        _cache.ThrowOnGet = true;

        var result = await behavior.Handle(new TenantQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CacheReadFailsAndThrowOnErrors_Propagates()
    {
        var behavior = Create<TenantQuery>(Options.Create(new CachingOptions { EnableQueryCaching = true, ThrowOnCacheErrors = true }));
        _cache.ThrowOnGet = true;

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(new TenantQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CacheWriteFails_StillReturnsResult()
    {
        var behavior = Create<TenantQuery>();
        _cache.ThrowOnSet = true;

        var result = await behavior.Handle(new TenantQuery(), Context("t", TestIdentity.User("u")), Next(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Cache(DurationSeconds = 60)]
    private sealed record TenantQuery : IRequest<string>;

    [Cache(DurationSeconds = 60, VaryByUser = true)]
    private sealed record UserQuery : IRequest<string>;

    [Cache(DurationSeconds = 60, SlidingExpiration = true)]
    private sealed record SlidingQuery : IRequest<string>;

    private sealed class FakeCache : ICacheProvider
    {
        private readonly Dictionary<string, object?> _store = [];

        public int Gets { get; private set; }
        public int Sets { get; private set; }
        public int SlidingSets { get; private set; }
        public int Refreshes { get; private set; }
        public bool ThrowOnGet { get; set; }
        public bool ThrowOnSet { get; set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            Gets++;
            if (ThrowOnGet)
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult(_store.TryGetValue(key, out var v) ? (T?)v : default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
        {
            Sets++;
            if (ThrowOnSet)
            {
                throw new InvalidOperationException("boom");
            }

            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration, CancellationToken cancellationToken)
        {
            SlidingSets++;
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken)
        {
            Refreshes++;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_store.ContainsKey(key));

        public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration, CancellationToken cancellationToken) =>
            factory(cancellationToken);
    }
}
