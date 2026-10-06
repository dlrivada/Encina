using Encina.Caching;
using Encina.Testing;
using Encina.Testing.Fakes.Providers;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Caching;

/// <summary>
/// <see cref="QueryCachingPipelineBehavior{TRequest, TResponse}"/> with <c>VaryByUser</c>: a per-user
/// entry is read and written only for an authenticated user; any other identity bypasses the cache.
/// </summary>
public sealed class QueryCachingVaryByUserTests
{
    [Cache(DurationSeconds = 300, VaryByUser = true)]
    public sealed record PerUserQuery(string Value) : IRequest<string>;

    private readonly FakeLogger<QueryCachingPipelineBehavior<PerUserQuery, string>> _logger = new();

    private QueryCachingPipelineBehavior<PerUserQuery, string> CreateBehavior(ICacheProvider cacheProvider)
    {
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        return new QueryCachingPipelineBehavior<PerUserQuery, string>(
            cacheProvider, new DefaultCacheKeyGenerator(options), options, _logger);
    }

    [Fact]
    public async Task AnAnonymousIdentity_BypassesTheCache_AndLogsWhyWithTheKindOnly()
    {
        var cacheProvider = Substitute.For<ICacheProvider>();
        var sut = CreateBehavior(cacheProvider);
        var calls = 0;

        var result = await sut.Handle(
            new PerUserQuery("q"),
            RequestContext.CreateForTest(tenantId: "tenant-1"),
            () =>
            {
                calls++;
                return ValueTask.FromResult(Right<EncinaError, string>("fresh"));
            },
            CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("fresh");
        calls.ShouldBe(1);
        cacheProvider.ReceivedCalls().ShouldBeEmpty();
        var record = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 3512);
        record.Level.ShouldBe(LogLevel.Debug);
        record.Message.ShouldContain("Anonymous");
    }

    [Fact]
    public async Task AForeignContextWithoutIdentity_BypassesTheCache()
    {
        var cacheProvider = Substitute.For<ICacheProvider>();
        var sut = CreateBehavior(cacheProvider);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");

        var result = await sut.Handle(
            new PerUserQuery("q"), context, () => ValueTask.FromResult(Right<EncinaError, string>("fresh")), CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("fresh");
        cacheProvider.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AUserIdentity_IsCachedUnderItsOwnKey_AndAnotherUserDoesNotReadIt()
    {
        using var cacheProvider = new FakeCacheProvider();
        var sut = CreateBehavior(cacheProvider);

        var alice = await sut.Handle(
            new PerUserQuery("q"),
            TestRequestContext.For(TestIdentity.User("alice")),
            () => ValueTask.FromResult(Right<EncinaError, string>("alice-data")),
            CancellationToken.None);
        var bob = await sut.Handle(
            new PerUserQuery("q"),
            TestRequestContext.For(TestIdentity.User("bob")),
            () => ValueTask.FromResult(Right<EncinaError, string>("bob-data")),
            CancellationToken.None);

        alice.ShouldBeSuccess().ShouldBe("alice-data");
        bob.ShouldBeSuccess().ShouldBe("bob-data");
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3512);
    }

    [Fact]
    public async Task ADeclaredServiceScope_BypassesTheCache_AndLogsWhyWithTheKindOnly()
    {
        // The service identity comes from a declared test service opened with RunAsServiceAsync.
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        var cacheProvider = Substitute.For<ICacheProvider>();
        var sut = CreateBehavior(cacheProvider);
        var calls = 0;

        var outcome = await host.Factory.RunAsServiceAsync(global::Encina.UnitTests.Core.Identity.ScopeTestHost.Job, async (context, ct) =>
            await sut.Handle(
                new PerUserQuery("q"),
                context,
                () =>
                {
                    calls++;
                    return ValueTask.FromResult(Right<EncinaError, string>("fresh"));
                },
                ct));

        outcome.ShouldBeSuccess().ShouldBe("fresh");
        calls.ShouldBe(1);
        cacheProvider.ReceivedCalls().ShouldBeEmpty();
        var record = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == 3512);
        record.Message.ShouldContain("Service");
        record.Message.ShouldNotContain(global::Encina.UnitTests.Core.Identity.ScopeTestHost.Job);
    }

    [Fact]
    public async Task ABuiltServiceIdentity_BypassesTheCache()
    {
        var cacheProvider = Substitute.For<ICacheProvider>();
        var sut = CreateBehavior(cacheProvider);

        var result = await sut.Handle(
            new PerUserQuery("q"),
            TestRequestContext.For(TestIdentity.Service("nightly-report", roles: ["reporter"])),
            () => ValueTask.FromResult(Right<EncinaError, string>("fresh")),
            CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("fresh");
        cacheProvider.ReceivedCalls().ShouldBeEmpty();
    }

    // ── #1931: the identity ends between two reads ───────────────────────

    /// <summary>
    /// An in-memory provider that records every key it is asked for or given, and can hold a read
    /// until <see cref="ReleaseReads"/> so a test ends the identity scope at a chosen point.
    /// </summary>
    private sealed class RecordingCacheProvider : ICacheProvider
    {
        private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);
        private readonly TaskCompletionSource _reads = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _readStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingCacheProvider(bool holdReads = false)
        {
            if (!holdReads)
            {
                _reads.SetResult();
            }
        }

        public List<string> ReadKeys { get; } = [];

        public List<string> WrittenKeys { get; } = [];

        public Task ReadStarted => _readStarted.Task;

        public void ReleaseReads() => _reads.TrySetResult();

        public void Seed<T>(string key, T value) => _entries[key] = value;

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            ReadKeys.Add(key);
            _readStarted.TrySetResult();
            await _reads.Task;
            return _entries.TryGetValue(key, out var value) ? (T?)value : default;
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
        {
            WrittenKeys.Add(key);
            _entries[key] = value;
            return Task.CompletedTask;
        }

        public Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration, CancellationToken cancellationToken) =>
            SetAsync(key, value, slidingExpiration, cancellationToken);

        public Task RemoveAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) => Task.FromResult(_entries.ContainsKey(key));

        public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration, CancellationToken cancellationToken) =>
            factory(cancellationToken);

        public Task<bool> RefreshAsync(string key, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private static void NoUserLessKey(IEnumerable<string> keys) =>
        keys.ShouldAllBe(key => !key.Contains("u::", StringComparison.Ordinal) && !key.EndsWith("u:", StringComparison.Ordinal));

    [Fact]
    public async Task TheIdentityReadsAnonymousAfterTheFirstRead_NoCacheReadOrWrite_AndTheHandlerRuns()
    {
        var cacheProvider = new RecordingCacheProvider();
        var sut = CreateBehavior(cacheProvider);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");
        context.Metadata.Returns(new Dictionary<string, object?>());

        // The first read (the snapshot) is alice; every later read is Anonymous, as after a scope end.
        context.Identity.Returns(TestIdentity.User("alice"), RequestIdentity.Anonymous);
        var calls = 0;

        var result = await sut.Handle(
            new PerUserQuery("q"),
            context,
            () =>
            {
                calls++;
                return ValueTask.FromResult(Right<EncinaError, string>("fresh"));
            },
            CancellationToken.None);

        result.ShouldBeSuccess().ShouldBe("fresh");
        calls.ShouldBe(1);
        cacheProvider.ReadKeys.ShouldBeEmpty();
        cacheProvider.WrittenKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task AScopeThatEndsWhileTheCacheIsRead_DoesNotServeTheUsersEntry_NorWrite()
    {
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        var cacheProvider = new RecordingCacheProvider(holdReads: true);
        var sut = CreateBehavior(cacheProvider);
        Task<Either<EncinaError, string>>? pending = null;
        var calls = 0;

        // Inside alice's scope the query starts, builds alice's key and blocks in the cache read;
        // the scope then ends before the read returns alice's cached entry.
        (await host.Factory.RunAsPrincipalAsync(
            global::Encina.UnitTests.Core.Identity.ScopeTestHost.UserPrincipal("alice"),
            async (context, ct) =>
            {
                pending = sut.Handle(
                    new PerUserQuery("q"),
                    context,
                    () =>
                    {
                        calls++;
                        return ValueTask.FromResult(Right<EncinaError, string>("fresh"));
                    },
                    ct).AsTask();
                await cacheProvider.ReadStarted;
                cacheProvider.Seed(cacheProvider.ReadKeys.Single(), new CacheEntry<string> { Value = "alice-private", CachedAtUtc = DateTime.UnixEpoch });
                return Right<EncinaError, int>(0);
            })).ShouldBeSuccess();

        cacheProvider.ReleaseReads();
        var result = await pending!;

        result.ShouldBeSuccess().ShouldBe("fresh");
        calls.ShouldBe(1);
        cacheProvider.WrittenKeys.ShouldBeEmpty();
        cacheProvider.ReadKeys.ShouldHaveSingleItem().ShouldContain("u:alice:");
        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 3508);
    }

    [Fact]
    public async Task AScopeThatEndsWhileTheHandlerRuns_DoesNotCacheTheResponse()
    {
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        var cacheProvider = new RecordingCacheProvider();
        var sut = CreateBehavior(cacheProvider);
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Either<EncinaError, string>>? pending = null;

        (await host.Factory.RunAsPrincipalAsync(
            global::Encina.UnitTests.Core.Identity.ScopeTestHost.UserPrincipal("alice"),
            async (context, ct) =>
            {
                pending = sut.Handle(
                    new PerUserQuery("q"),
                    context,
                    async () =>
                    {
                        handlerStarted.SetResult();
                        await handlerRelease.Task;
                        return Right<EncinaError, string>("computed-after-scope");
                    },
                    ct).AsTask();
                await handlerStarted.Task;
                return Right<EncinaError, int>(0);
            })).ShouldBeSuccess();

        handlerRelease.SetResult();
        var result = await pending!;

        result.ShouldBeSuccess().ShouldBe("computed-after-scope");
        cacheProvider.ReadKeys.ShouldHaveSingleItem();
        cacheProvider.WrittenKeys.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoUserLessVaryByUserKey_IsEverReadOrWritten_EvenWithANaiveCustomGenerator()
    {
        var cacheProvider = new RecordingCacheProvider();
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });

        // A naive generator that never refuses: it would write "u::" for an identity read as Anonymous.
        var naive = Substitute.For<ICacheKeyGenerator>();
        naive.GenerateKey<PerUserQuery, string>(Arg.Any<PerUserQuery>(), Arg.Any<IRequestContext>())
            .Returns(call => $"encina:u:{call.Arg<IRequestContext>().Identity?.UserId}:PerUserQuery");
        var sut = new QueryCachingPipelineBehavior<PerUserQuery, string>(cacheProvider, naive, options, _logger);
        var host = new global::Encina.UnitTests.Core.Identity.ScopeTestHost();
        var racing = Substitute.For<IRequestContext>();
        racing.CorrelationId.Returns("corr-race");
        racing.Metadata.Returns(new Dictionary<string, object?>());
        racing.Identity.Returns(TestIdentity.User("carol"), RequestIdentity.Anonymous);
        IRequestContext? ended = null;
        await host.InUserScope("dave", context =>
        {
            ended = context;
            return Task.FromResult(0);
        });

        IRequestContext[] contexts =
        [
            TestRequestContext.For(TestIdentity.User("alice")),
            RequestContext.CreateForTest(),
            TestRequestContext.For(TestIdentity.Service("svc")),
            racing,
            ended!
        ];
        foreach (var context in contexts)
        {
            (await sut.Handle(new PerUserQuery("q"), context, () => ValueTask.FromResult(Right<EncinaError, string>("v")), CancellationToken.None))
                .ShouldBeSuccess();
        }

        NoUserLessKey(cacheProvider.ReadKeys);
        NoUserLessKey(cacheProvider.WrittenKeys);
        cacheProvider.WrittenKeys.ShouldHaveSingleItem().ShouldContain("u:alice:");
    }

    [Fact]
    public async Task ACustomKeyGenerator_ReadsThePinnedIdentity_NotASecondLiveRead()
    {
        var cacheProvider = new RecordingCacheProvider();
        var options = Options.Create(new CachingOptions { EnableQueryCaching = true });
        RequestIdentity? seenByGenerator = null;
        var generator = Substitute.For<ICacheKeyGenerator>();
        generator.GenerateKey<PerUserQuery, string>(Arg.Any<PerUserQuery>(), Arg.Any<IRequestContext>())
            .Returns(call =>
            {
                var keyContext = call.Arg<IRequestContext>();
                seenByGenerator = keyContext.Identity;
                keyContext.WithTenantId("t").Identity.ShouldBeSameAs(seenByGenerator);
                keyContext.WithMetadata("k", 1).WithIdempotencyKey("i").Identity.ShouldBeSameAs(seenByGenerator);
                return $"key:{keyContext.CorrelationId}:{keyContext.TenantId}:{keyContext.CausationId}:{keyContext.IdempotencyKey}:{keyContext.Timestamp:O}:{keyContext.Metadata.Count}";
            });
        var sut = new QueryCachingPipelineBehavior<PerUserQuery, string>(cacheProvider, generator, options, _logger);
        var alice = TestIdentity.User("alice");
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");
        context.Metadata.Returns(new Dictionary<string, object?>());
        context.WithTenantId(Arg.Any<string?>()).Returns(context);
        context.WithMetadata(Arg.Any<string>(), Arg.Any<object?>()).Returns(context);
        context.WithIdempotencyKey(Arg.Any<string?>()).Returns(context);
        context.Identity.Returns(alice, RequestIdentity.Anonymous);

        (await sut.Handle(new PerUserQuery("q"), context, () => ValueTask.FromResult(Right<EncinaError, string>("v")), CancellationToken.None))
            .ShouldBeSuccess();

        seenByGenerator.ShouldBeSameAs(alice);
        cacheProvider.WrittenKeys.ShouldBeEmpty();
    }
}
