#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Caching;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Security.ABAC.Persistence;

/// <summary>
/// Guard clause tests for <see cref="CachingPolicyStoreDecorator"/>.
/// Covers constructor guards, cache-aside read behavior, write-through invalidation,
/// and resilience (cache failure fallback to inner store).
/// </summary>
public class CachingPolicyStoreDecoratorGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new CachingPolicyStoreDecorator(
            null!,
            Substitute.For<ICacheProvider>(),
            null,
            new PolicyCachingOptions(),
            NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new CachingPolicyStoreDecorator(
            Substitute.For<IPolicyStore>(),
            null!,
            null,
            new PolicyCachingOptions(),
            NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CachingPolicyStoreDecorator(
            Substitute.For<IPolicyStore>(),
            Substitute.For<ICacheProvider>(),
            null,
            null!,
            NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>());

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new CachingPolicyStoreDecorator(
            Substitute.For<IPolicyStore>(),
            Substitute.For<ICacheProvider>(),
            null,
            new PolicyCachingOptions(),
            null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullPubSub_DoesNotThrow()
    {
        // PubSub is optional (null is allowed)
        var act = () => new CachingPolicyStoreDecorator(
            Substitute.For<IPolicyStore>(),
            Substitute.For<ICacheProvider>(),
            null,
            new PolicyCachingOptions(),
            NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>());

        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_WithPubSub_DoesNotThrow()
    {
        var act = () => new CachingPolicyStoreDecorator(
            Substitute.For<IPolicyStore>(),
            Substitute.For<ICacheProvider>(),
            Substitute.For<IPubSubProvider>(),
            new PolicyCachingOptions(),
            NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>());

        Should.NotThrow(act);
    }

    #endregion

    #region GetAllPolicySetsAsync — Cache-Aside Behavior

    [Fact]
    public async Task GetAllPolicySetsAsync_DelegatesToInnerOnCacheMiss()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        IReadOnlyList<PolicySet> expected = new List<PolicySet> { CreatePolicySet("ps-1") };
        inner.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(expected));

        var cache = Substitute.For<ICacheProvider>();
        cache.GetOrSetAsync<IReadOnlyList<PolicySet>>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var factory = callInfo.ArgAt<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(1);
                return factory(CancellationToken.None);
            });

        var sut = CreateDecorator(inner, cache);

        // Act
        var result = await sut.GetAllPolicySetsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetAllPolicySetsAsync — Cache Failure Fallback

    [Fact]
    public async Task GetAllPolicySetsAsync_CacheFailure_FallsBackToInner()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        IReadOnlyList<PolicySet> expected = new List<PolicySet> { CreatePolicySet("ps-fallback") };
        inner.GetAllPolicySetsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<PolicySet>>(expected));

        var cache = Substitute.For<ICacheProvider>();
        cache.GetOrSetAsync<IReadOnlyList<PolicySet>>(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<IReadOnlyList<PolicySet>>>>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache down"));

        var sut = CreateDecorator(inner, cache);

        // Act
        var result = await sut.GetAllPolicySetsAsync();

        // Assert
        result.IsRight.ShouldBeTrue("cache failure should fall back to inner store");
    }

    #endregion

    #region SavePolicySetAsync — Write-Through Invalidation

    [Fact]
    public async Task SavePolicySetAsync_SuccessfulWrite_InvalidatesCache()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var sut = CreateDecorator(inner, cache);
        var ps = CreatePolicySet("save-ps");

        // Act
        var result = await sut.SavePolicySetAsync(ps);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cache.Received().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePolicySetAsync_InnerFails_DoesNotInvalidateCache()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("write-fail", "Write failed")));

        var cache = Substitute.For<ICacheProvider>();
        var sut = CreateDecorator(inner, cache);
        var ps = CreatePolicySet("fail-ps");

        // Act
        var result = await sut.SavePolicySetAsync(ps);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeletePolicySetAsync — Write-Through Invalidation

    [Fact]
    public async Task DeletePolicySetAsync_SuccessfulDelete_InvalidatesCache()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.DeletePolicySetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var sut = CreateDecorator(inner, cache);

        // Act
        var result = await sut.DeletePolicySetAsync("del-ps");

        // Assert
        result.IsRight.ShouldBeTrue();
        await cache.Received().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SavePolicyAsync — Write-Through Invalidation

    [Fact]
    public async Task SavePolicyAsync_SuccessfulWrite_InvalidatesCache()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var sut = CreateDecorator(inner, cache);
        var pol = CreatePolicy("save-pol");

        // Act
        var result = await sut.SavePolicyAsync(pol);

        // Assert
        result.IsRight.ShouldBeTrue();
        await cache.Received().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeletePolicyAsync — Write-Through Invalidation

    [Fact]
    public async Task DeletePolicyAsync_SuccessfulDelete_InvalidatesCache()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.DeletePolicyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var sut = CreateDecorator(inner, cache);

        // Act
        var result = await sut.DeletePolicyAsync("del-pol");

        // Assert
        result.IsRight.ShouldBeTrue();
        await cache.Received().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Pass-Through Operations — Delegate Directly

    [Fact]
    public async Task ExistsPolicySetAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IPolicyStore>();
        inner.ExistsPolicySetAsync("exists-ps", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var sut = CreateDecorator(inner);

        var result = await sut.ExistsPolicySetAsync("exists-ps");

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => false, Right: v => v).ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicySetCountAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IPolicyStore>();
        inner.GetPolicySetCountAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(5));

        var sut = CreateDecorator(inner);

        var result = await sut.GetPolicySetCountAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => -1, Right: v => v).ShouldBe(5);
    }

    [Fact]
    public async Task ExistsPolicyAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IPolicyStore>();
        inner.ExistsPolicyAsync("exists-pol", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var sut = CreateDecorator(inner);

        var result = await sut.ExistsPolicyAsync("exists-pol");

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => true, Right: v => v).ShouldBeFalse();
    }

    [Fact]
    public async Task GetPolicyCountAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IPolicyStore>();
        inner.GetPolicyCountAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(10));

        var sut = CreateDecorator(inner);

        var result = await sut.GetPolicyCountAsync();

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => -1, Right: v => v).ShouldBe(10);
    }

    #endregion

    #region PubSub Invalidation — Publish on Write

    [Fact]
    public async Task SavePolicySetAsync_WithPubSub_PublishesInvalidation()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var pubSub = Substitute.For<IPubSubProvider>();
        var options = new PolicyCachingOptions { EnablePubSubInvalidation = true };
        var sut = CreateDecorator(inner, cache, pubSub, options);
        var ps = CreatePolicySet("pubsub-ps");

        // Act
        await sut.SavePolicySetAsync(ps);

        // Assert
        await pubSub.Received().PublishAsync(
            Arg.Any<string>(),
            Arg.Any<PolicyCacheInvalidationMessage>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePolicySetAsync_PubSubFailure_DoesNotThrow()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        var pubSub = Substitute.For<IPubSubProvider>();
        pubSub.PublishAsync(Arg.Any<string>(), Arg.Any<PolicyCacheInvalidationMessage>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("PubSub down"));

        var options = new PolicyCachingOptions { EnablePubSubInvalidation = true };
        var sut = CreateDecorator(inner, cache, pubSub, options);
        var ps = CreatePolicySet("pubsub-fail-ps");

        // Act
        var act = async () => await sut.SavePolicySetAsync(ps);

        // Assert
        await Should.NotThrowAsync(act);
    }

    #endregion

    #region Cache Invalidation Failure — Resilience

    [Fact]
    public async Task SavePolicySetAsync_CacheInvalidationFailure_DoesNotThrow()
    {
        // Arrange
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicySetAsync(Arg.Any<PolicySet>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var cache = Substitute.For<ICacheProvider>();
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cache invalidation failed"));

        var sut = CreateDecorator(inner, cache);
        var ps = CreatePolicySet("cache-inv-fail");

        // Act
        var act = async () => await sut.SavePolicySetAsync(ps);

        // Assert
        await Should.NotThrowAsync(act);
    }

    #endregion

    // ── Helpers ──────────────────────────────────────────────────────

    private static CachingPolicyStoreDecorator CreateDecorator(
        IPolicyStore? inner = null,
        ICacheProvider? cache = null,
        IPubSubProvider? pubSub = null,
        PolicyCachingOptions? options = null)
    {
        inner ??= Substitute.For<IPolicyStore>();
        cache ??= Substitute.For<ICacheProvider>();
        options ??= new PolicyCachingOptions();
        var logger = NullLoggerFactory.Instance.CreateLogger<CachingPolicyStoreDecorator>();
        return new CachingPolicyStoreDecorator(inner, cache, pubSub, options, logger);
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
