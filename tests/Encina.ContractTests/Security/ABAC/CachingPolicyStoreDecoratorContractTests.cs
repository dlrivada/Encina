using Encina.Caching;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Security.ABAC;

/// <summary>
/// Behavioral contract tests for <see cref="CachingPolicyStoreDecorator"/>.
/// Verifies the cache-aside read pattern (cache hit/miss), write-through invalidation,
/// pass-through for count/exists operations, PubSub invalidation publishing, and
/// resilience (cache failure fallback to inner store).
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "ABAC")]
public sealed class CachingPolicyStoreDecoratorContractTests
{
    private readonly IPolicyStore _innerStore = Substitute.For<IPolicyStore>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly IPubSubProvider _pubSub = Substitute.For<IPubSubProvider>();

    private readonly PolicyCachingOptions _options = new()
    {
        Enabled = true,
        Duration = TimeSpan.FromMinutes(5),
        EnablePubSubInvalidation = true,
        InvalidationChannel = "abac:test:invalidate",
        CacheKeyPrefix = "abac-test"
    };

    private CachingPolicyStoreDecorator CreateDecorator(IPubSubProvider? pubSub = null)
    {
        return new CachingPolicyStoreDecorator(
            _innerStore,
            _cache,
            pubSub ?? _pubSub,
            _options,
            NullLogger<CachingPolicyStoreDecorator>.Instance);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PolicySet MakePolicySet(string id = "ps-1") => new()
    {
        Id = id,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Target = null,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy MakePolicy(string id = "p-1") => new()
    {
        Id = id,
        IsEnabled = true,
        Algorithm = CombiningAlgorithmId.PermitOverrides,
        Target = null,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    // ── Contract: Cache-aside read (GetAllPolicySetsAsync) ──────────

    [Fact]
    public async Task GetAllPolicySetsAsync_CacheMiss_ShouldDelegateToInnerStore()
    {
        // Arrange
        IReadOnlyList<PolicySet> expected = [MakePolicySet()];

        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(1);
                return factory(CancellationToken.None);
            });

        _innerStore.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(expected));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetAllPolicySetsAsync();

        // Assert
        result.IsRight.ShouldBeTrue("Cache miss must fall through to inner store");

        var sets = result.Match(Right: v => v, Left: _ => []);
        sets.Count.ShouldBe(1);
        sets[0].Id.ShouldBe("ps-1");
    }

    // ── Contract: Write-through invalidation on save ────────────────

    [Fact]
    public async Task SavePolicySetAsync_ShouldInvalidateCache_AndPublishPubSub()
    {
        // Arrange
        var ps = MakePolicySet("save-ps");
        _innerStore.SavePolicySetAsync(ps, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.SavePolicySetAsync(ps);

        // Assert
        result.IsRight.ShouldBeTrue("Save should succeed when inner store succeeds");

        // Verify cache invalidation was attempted
        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains("save-ps")),
            Arg.Any<CancellationToken>());

        // Verify PubSub publish was attempted
        await _pubSub.Received().PublishAsync(
            "abac:test:invalidate",
            Arg.Any<PolicyCacheInvalidationMessage>(),
            Arg.Any<CancellationToken>());
    }

    // ── Contract: Delete invalidates cache ───────────────────────────

    [Fact]
    public async Task DeletePolicySetAsync_ShouldInvalidateCache()
    {
        // Arrange
        _innerStore.DeletePolicySetAsync("del-ps", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.DeletePolicySetAsync("del-ps");

        // Assert
        result.IsRight.ShouldBeTrue("Delete should succeed when inner store succeeds");

        await _cache.Received().RemoveAsync(
            Arg.Is<string>(k => k.Contains("del-ps")),
            Arg.Any<CancellationToken>());
    }

    // ── Contract: Pass-through for ExistsPolicySetAsync ──────────────

    [Fact]
    public async Task ExistsPolicySetAsync_ShouldDelegateDirectlyToInnerStore()
    {
        // Arrange
        _innerStore.ExistsPolicySetAsync("exist-ps", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.ExistsPolicySetAsync("exist-ps");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: v => v, Left: _ => false).ShouldBeTrue(
            "Exists must pass through to inner store without caching");
    }

    // ── Contract: Pass-through for GetPolicySetCountAsync ────────────

    [Fact]
    public async Task GetPolicySetCountAsync_ShouldDelegateDirectlyToInnerStore()
    {
        // Arrange
        _innerStore.GetPolicySetCountAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(42));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetPolicySetCountAsync();

        // Assert
        var count = result.Match(Right: v => v, Left: _ => -1);
        count.ShouldBe(42,
            "Count must pass through to inner store for fresh data (health checks)");
    }

    // ── Contract: Cache failure falls back to inner store ────────────

    [Fact]
    public async Task GetAllPolicySetsAsync_CacheFailure_ShouldFallbackToInnerStore()
    {
        // Arrange
        IReadOnlyList<PolicySet> expected = [MakePolicySet("fallback-ps")];

        _cache.GetOrSetAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Cache is down"));

        _innerStore.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(expected));

        var decorator = CreateDecorator();

        // Act
        var result = await decorator.GetAllPolicySetsAsync();

        // Assert
        result.IsRight.ShouldBeTrue(
            "Cache failure must fall back to inner store, not propagate exception");

        var sets = result.Match(Right: v => v, Left: _ => []);
        sets[0].Id.ShouldBe("fallback-ps");
    }

    // ── Contract: PubSub null is tolerated ──────────────────────────

    [Fact]
    public async Task SavePolicySetAsync_NoPubSub_ShouldSucceedWithoutPublishing()
    {
        // Arrange
        var ps = MakePolicySet("no-pubsub-ps");
        _innerStore.SavePolicySetAsync(ps, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var decorator = CreateDecorator(pubSub: null);

        // Act
        var result = await decorator.SavePolicySetAsync(ps);

        // Assert
        result.IsRight.ShouldBeTrue(
            "Save must succeed even without PubSub provider (null is tolerated)");
    }
}
