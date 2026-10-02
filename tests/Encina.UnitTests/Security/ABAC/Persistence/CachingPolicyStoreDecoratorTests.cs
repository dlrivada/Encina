#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Caching;
using Encina.Diagnostics;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests for the Option-returning cache-aside reads of <see cref="CachingPolicyStoreDecorator"/>.
/// </summary>
public class CachingPolicyStoreDecoratorTests
{
    private const string Sentinel = "sentinel-secret-message-1557";

    private readonly IPolicyStore _inner = Substitute.For<IPolicyStore>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly FakeLogger<CachingPolicyStoreDecorator> _logger = new();
    private readonly CachingPolicyStoreDecorator _sut;

    public CachingPolicyStoreDecoratorTests()
    {
        _sut = new CachingPolicyStoreDecorator(_inner, _cache, null, new PolicyCachingOptions(), _logger);
    }

    [Fact]
    public async Task GetPolicyAsync_CacheHit_ReturnsCachedWithoutQueryingInner()
    {
        var policy = CreatePolicy("p-hit");
        _cache.GetAsync<Policy>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(policy);

        var result = await _sut.GetPolicyAsync("p-hit");

        result.IsRight.ShouldBeTrue();
        result.IfRight(o => o.IsSome.ShouldBeTrue());
        await _inner.DidNotReceive().GetPolicyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<Policy>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicySetAsync_MissWithSome_ReturnsAndCachesValue()
    {
        var policySet = CreatePolicySet("ps-miss");
        _cache.GetAsync<PolicySet>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((PolicySet?)null);
        _inner.GetPolicySetAsync("ps-miss", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<PolicySet>>(Some(policySet)));

        var result = await _sut.GetPolicySetAsync("ps-miss");

        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).SetAsync(
            Arg.Is<string>(k => k.Contains("ps-miss")), policySet, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyAsync_MissWithNone_DoesNotCache()
    {
        _cache.GetAsync<Policy>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Policy?)null);
        _inner.GetPolicyAsync("p-none", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<Policy>>(Option<Policy>.None));

        var result = await _sut.GetPolicyAsync("p-none");

        result.IsRight.ShouldBeTrue();
        result.IfRight(o => o.IsNone.ShouldBeTrue());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<Policy>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyAsync_InnerReturnsError_PropagatesErrorWithoutCaching()
    {
        _cache.GetAsync<Policy>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Policy?)null);
        var error = EncinaError.New("store.failed");
        _inner.GetPolicyAsync("p-err", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<Policy>>(error));

        var result = await _sut.GetPolicyAsync("p-err");

        result.IsLeft.ShouldBeTrue();
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<Policy>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyAsync_CacheReadThrows_FallsBackToInnerAndLogsRedacted()
    {
        var policy = CreatePolicy("p-readfail");
        _cache.GetAsync<Policy>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Policy?>(_ => throw new InvalidOperationException(Sentinel));
        _inner.GetPolicyAsync("p-readfail", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<Policy>>(Some(policy)));

        var result = await _sut.GetPolicyAsync("p-readfail");

        result.IsRight.ShouldBeTrue();
        await _inner.Received(1).GetPolicyAsync("p-readfail", Arg.Any<CancellationToken>());
        AssertLoggedRedacted();
    }

    [Fact]
    public async Task GetPolicyAsync_CacheWriteThrows_StillReturnsValueAndLogsRedacted()
    {
        var policy = CreatePolicy("p-writefail");
        _cache.GetAsync<Policy>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Policy?)null);
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<Policy>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException(Sentinel)));
        _inner.GetPolicyAsync("p-writefail", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<Policy>>(Some(policy)));

        var result = await _sut.GetPolicyAsync("p-writefail");

        result.IsRight.ShouldBeTrue();
        AssertLoggedRedacted();
    }

    private void AssertLoggedRedacted()
    {
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(Sentinel);
        entry.Message.ShouldNotContain(Sentinel);
    }

    private static PolicySet CreatePolicySet(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreatePolicy(string id) => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };
}
