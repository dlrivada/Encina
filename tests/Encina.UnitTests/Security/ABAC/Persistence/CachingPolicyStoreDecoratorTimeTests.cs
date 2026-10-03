#pragma warning disable CA2012 // Use ValueTasks correctly -- NSubstitute mock setup pattern

using Encina.Caching;
using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Regression tests for #1708: <see cref="CachingPolicyStoreDecorator"/> stamps its invalidation
/// messages from the injected <see cref="TimeProvider"/>.
/// </summary>
public sealed class CachingPolicyStoreDecoratorTimeTests
{
    [Fact]
    public async Task SavePolicyAsync_PublishesInvalidationStampedByTheTimeProvider()
    {
        var inner = Substitute.For<IPolicyStore>();
        inner.SavePolicyAsync(Arg.Any<Policy>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));
        var pubSub = Substitute.For<IPubSubProvider>();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 3, 12, 30, 0, TimeSpan.Zero));
        var sut = new CachingPolicyStoreDecorator(
            inner,
            Substitute.For<ICacheProvider>(),
            pubSub,
            new PolicyCachingOptions { Enabled = true, EnablePubSubInvalidation = true },
            NullLogger<CachingPolicyStoreDecorator>.Instance,
            time);

        var result = await sut.SavePolicyAsync(CreatePolicy("p-time"));

        result.IsRight.ShouldBeTrue();
        await pubSub.Received(1).PublishAsync(
            Arg.Any<string>(),
            Arg.Is<PolicyCacheInvalidationMessage>(m =>
                m.EntityId == "p-time" && m.TimestampUtc == time.GetUtcNow().UtcDateTime),
            Arg.Any<CancellationToken>());
    }

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
