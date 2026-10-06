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
}
