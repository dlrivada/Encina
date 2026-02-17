using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.Shadow;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for shadow sharding production path isolation.
/// Verifies that the decorator never alters production routing decisions.
/// </summary>
[Trait("Category", "Property")]
public sealed class ShadowShardingPropertyTests
{
    // ── Production path isolation ──────────────────────────────────

    [Property(MaxTest = 200)]
    public bool Property_GetShardId_AlwaysMatchesPrimaryRouter(NonEmptyString key, PositiveInt shardCount)
    {
        var clampedCount = Math.Clamp(shardCount.Get, 2, 20);
        var topology = CreateTopology(clampedCount);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, clampedCount);

        var primaryResult = primaryRouter.GetShardId(key.Get);
        var decoratorResult = decorator.GetShardId(key.Get);

        return ResultsAreEqual(primaryResult, decoratorResult);
    }

    [Property(MaxTest = 200)]
    public bool Property_GetShardId_DeterministicWithDecorator(NonEmptyString key)
    {
        var topology = CreateTopology(5);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, 5);

        var result1 = decorator.GetShardId(key.Get);
        var result2 = decorator.GetShardId(key.Get);

        return ResultsAreEqual(result1, result2);
    }

    [Property(MaxTest = 100)]
    public bool Property_GetAllShardIds_AlwaysMatchesPrimaryRouter(PositiveInt shardCount)
    {
        var clampedCount = Math.Clamp(shardCount.Get, 2, 20);
        var topology = CreateTopology(clampedCount);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, clampedCount);

        var primaryIds = primaryRouter.GetAllShardIds();
        var decoratorIds = decorator.GetAllShardIds();

        return primaryIds.SequenceEqual(decoratorIds);
    }

    [Property(MaxTest = 100)]
    public bool Property_GetShardConnectionString_AlwaysMatchesPrimaryRouter(PositiveInt shardCount)
    {
        var clampedCount = Math.Clamp(shardCount.Get, 2, 20);
        var topology = CreateTopology(clampedCount);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, clampedCount);

        foreach (var shardId in primaryRouter.GetAllShardIds())
        {
            var primaryConn = primaryRouter.GetShardConnectionString(shardId);
            var decoratorConn = decorator.GetShardConnectionString(shardId);

            if (!ResultsAreEqual(primaryConn, decoratorConn))
            {
                return false;
            }
        }

        return true;
    }

    // ── Shadow operations independence ──────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_ShadowRouting_NeverAffectsProductionResult(NonEmptyString key)
    {
        var prodTopology = CreateTopology(5);
        var shadowTopology = CreateTopology(3); // Different shard count

        var primaryRouter = new HashShardRouter(prodTopology);
        var shadowRouter = new HashShardRouter(shadowTopology);
        var options = new ShadowShardingOptions { ShadowTopology = shadowTopology };
        var decorator = new ShadowShardRouterDecorator(
            primaryRouter, shadowRouter, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);

        // Execute production routing
        var productionBefore = primaryRouter.GetShardId(key.Get);
        var decoratorResult = decorator.GetShardId(key.Get);
        var productionAfter = primaryRouter.GetShardId(key.Get);

        // Production result should be identical before and after shadow operations
        return ResultsAreEqual(productionBefore, decoratorResult) &&
               ResultsAreEqual(productionBefore, productionAfter);
    }

    // ── ShadowComparisonResult invariants ──────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_CompareAsync_RoutingMatchTrueOnlyWhenBothAgree(NonEmptyString key)
    {
        var topology = CreateTopology(5);
        var primaryRouter = new HashShardRouter(topology);
        var shadowRouter = new HashShardRouter(topology); // Same topology = should match
        var options = new ShadowShardingOptions { ShadowTopology = topology };
        var decorator = new ShadowShardRouterDecorator(
            primaryRouter, shadowRouter, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);

        var comparison = decorator.CompareAsync(key.Get, CancellationToken.None).GetAwaiter().GetResult();

        // Same topology -> routing must match
        return comparison.RoutingMatch &&
               comparison.ProductionShardId == comparison.ShadowShardId;
    }

    [Property(MaxTest = 100)]
    public bool Property_CompareAsync_DifferentTopologiesMayMismatch(NonEmptyString key)
    {
        var prodTopology = CreateTopology(5);
        var shadowTopology = CreateTopology(3);
        var primaryRouter = new HashShardRouter(prodTopology);
        var shadowRouter = new HashShardRouter(shadowTopology);
        var options = new ShadowShardingOptions { ShadowTopology = shadowTopology };
        var decorator = new ShadowShardRouterDecorator(
            primaryRouter, shadowRouter, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);

        var comparison = decorator.CompareAsync(key.Get, CancellationToken.None).GetAwaiter().GetResult();

        // The invariant: RoutingMatch reflects whether IDs actually match
        var actualMatch = string.Equals(
            comparison.ProductionShardId,
            comparison.ShadowShardId,
            StringComparison.Ordinal);

        return comparison.RoutingMatch == actualMatch;
    }

    [Property(MaxTest = 100)]
    public bool Property_CompareAsync_LatencyDifferenceIsConsistent(NonEmptyString key)
    {
        var topology = CreateTopology(5);
        var primaryRouter = new HashShardRouter(topology);
        var shadowRouter = new HashShardRouter(topology);
        var options = new ShadowShardingOptions { ShadowTopology = topology };
        var decorator = new ShadowShardRouterDecorator(
            primaryRouter, shadowRouter, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);

        var comparison = decorator.CompareAsync(key.Get, CancellationToken.None).GetAwaiter().GetResult();

        // LatencyDifference should be ShadowLatency - ProductionLatency
        var expectedDiff = comparison.ShadowLatency - comparison.ProductionLatency;
        return comparison.LatencyDifference == expectedDiff;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static ShardTopology CreateTopology(int shardCount)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=test;Database=Shard{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    private static ShadowShardRouterDecorator CreateDecorator(IShardRouter primary, int shadowShardCount)
    {
        var shadowTopology = CreateTopology(Math.Max(2, shadowShardCount - 1));
        var shadowRouter = new HashShardRouter(shadowTopology);
        var options = new ShadowShardingOptions { ShadowTopology = shadowTopology };
        return new ShadowShardRouterDecorator(
            primary, shadowRouter, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);
    }

    private static bool ResultsAreEqual(
        LanguageExt.Either<EncinaError, string> a,
        LanguageExt.Either<EncinaError, string> b)
    {
        if (a.IsRight != b.IsRight) return false;
        if (a.IsLeft) return true;

        string aVal = string.Empty, bVal = string.Empty;
        _ = a.IfRight(v => aVal = v);
        _ = b.IfRight(v => bVal = v);

        return string.Equals(aVal, bVal, StringComparison.Ordinal);
    }
}
