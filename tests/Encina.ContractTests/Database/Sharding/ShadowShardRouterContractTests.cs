using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.Shadow;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that <see cref="ShadowShardRouterDecorator"/>
/// preserves the <see cref="IShardRouter"/> contract for all built-in routing strategies.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ShadowShardRouterContractTests
{
    // ── Hash router compatibility ──────────────────────────────────

    [Fact]
    public void HashRouter_DecoratedProductionRouting_ReturnsSameResultAsUndecoratedRouter()
    {
        // Arrange
        var topology = CreateTopology(5);
        var primaryRouter = new HashShardRouter(topology);
        var shadowRouter = new HashShardRouter(CreateTopology(3)); // Different topology
        var decorator = CreateDecorator(primaryRouter, shadowRouter);

        // Act & Assert — for multiple keys, decorator should match primary
        for (var i = 0; i < 100; i++)
        {
            var key = $"customer-{i}";
            var primaryResult = primaryRouter.GetShardId(key);
            var decoratorResult = decorator.GetShardId(key);

            decoratorResult.IsRight.ShouldBe(primaryResult.IsRight);

            string primaryId = string.Empty, decoratorId = string.Empty;
            _ = primaryResult.IfRight(id => primaryId = id);
            _ = decoratorResult.IfRight(id => decoratorId = id);
            decoratorId.ShouldBe(primaryId);
        }
    }

    [Fact]
    public void HashRouter_GetAllShardIds_MatchesPrimaryRouter()
    {
        var topology = CreateTopology(5);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, new HashShardRouter(CreateTopology(3)));

        decorator.GetAllShardIds().ShouldBe(primaryRouter.GetAllShardIds());
    }

    [Fact]
    public void HashRouter_GetShardConnectionString_MatchesPrimaryRouter()
    {
        var topology = CreateTopology(3);
        var primaryRouter = new HashShardRouter(topology);
        var decorator = CreateDecorator(primaryRouter, new HashShardRouter(CreateTopology(2)));

        foreach (var shardId in primaryRouter.GetAllShardIds())
        {
            var primaryConn = primaryRouter.GetShardConnectionString(shardId);
            var decoratorConn = decorator.GetShardConnectionString(shardId);

            string primaryVal = string.Empty, decoratorVal = string.Empty;
            _ = primaryConn.IfRight(c => primaryVal = c);
            _ = decoratorConn.IfRight(c => decoratorVal = c);
            decoratorVal.ShouldBe(primaryVal);
        }
    }

    // ── Range router compatibility ─────────────────────────────────

    [Fact]
    public void RangeRouter_DecoratedProductionRouting_ReturnsSameResultAsUndecoratedRouter()
    {
        // Arrange
        var topology = CreateTopology(3);
        var ranges = new[]
        {
            new ShardRange("A", "G", "shard-1"),
            new ShardRange("G", "N", "shard-2"),
            new ShardRange("N", "Z", "shard-3")
        };
        var primaryRouter = new RangeShardRouter(topology, ranges);
        var shadowRouter = new HashShardRouter(CreateTopology(2));
        var decorator = CreateDecorator(primaryRouter, shadowRouter);

        // Act & Assert
        foreach (var key in new[] { "Alpha", "Hotel", "November", "Zulu" })
        {
            var primaryResult = primaryRouter.GetShardId(key);
            var decoratorResult = decorator.GetShardId(key);

            string primaryId = string.Empty, decoratorId = string.Empty;
            _ = primaryResult.IfRight(id => primaryId = id);
            _ = decoratorResult.IfRight(id => decoratorId = id);
            decoratorId.ShouldBe(primaryId);
        }
    }

    // ── Directory router compatibility ─────────────────────────────

    [Fact]
    public void DirectoryRouter_DecoratedProductionRouting_ReturnsSameResultAsUndecoratedRouter()
    {
        // Arrange
        var topology = CreateTopology(3);
        var store = new InMemoryShardDirectoryStore();
        store.AddMapping("key-1", "shard-1");
        store.AddMapping("key-2", "shard-2");
        store.AddMapping("key-3", "shard-3");

        var primaryRouter = new DirectoryShardRouter(topology, store);
        var shadowRouter = new HashShardRouter(CreateTopology(2));
        var decorator = CreateDecorator(primaryRouter, shadowRouter);

        // Act & Assert
        foreach (var key in new[] { "key-1", "key-2", "key-3" })
        {
            var primaryResult = primaryRouter.GetShardId(key);
            var decoratorResult = decorator.GetShardId(key);

            string primaryId = string.Empty, decoratorId = string.Empty;
            _ = primaryResult.IfRight(id => primaryId = id);
            _ = decoratorResult.IfRight(id => decoratorId = id);
            decoratorId.ShouldBe(primaryId);
        }
    }

    // ── Geo router compatibility ───────────────────────────────────

    [Fact]
    public void GeoRouter_DecoratedProductionRouting_ReturnsSameResultAsUndecoratedRouter()
    {
        // Arrange
        var topology = CreateTopology(3);
        var regions = new[]
        {
            new GeoRegion("US", "shard-1"),
            new GeoRegion("EU", "shard-2"),
            new GeoRegion("AP", "shard-3")
        };
        var geoOptions = new GeoShardRouterOptions { DefaultRegion = "US" };
        var primaryRouter = new GeoShardRouter(topology, regions, ExtractRegion, geoOptions);
        var shadowRouter = new HashShardRouter(CreateTopology(2));
        var decorator = CreateDecorator(primaryRouter, shadowRouter);

        // Act & Assert
        foreach (var key in new[] { "US:customer-1", "EU:customer-2", "AP:customer-3" })
        {
            var primaryResult = primaryRouter.GetShardId(key);
            var decoratorResult = decorator.GetShardId(key);

            string primaryId = string.Empty, decoratorId = string.Empty;
            _ = primaryResult.IfRight(id => primaryId = id);
            _ = decoratorResult.IfRight(id => decoratorId = id);
            decoratorId.ShouldBe(primaryId);
        }
    }

    // ── IShadowShardRouter contract ────────────────────────────────

    [Fact]
    public void IShadowShardRouter_IsShadowEnabled_AlwaysTrue()
    {
        var decorator = CreateShadowRouter(
            new HashShardRouter(CreateTopology(3)),
            new HashShardRouter(CreateTopology(2)));

        decorator.IsShadowEnabled.ShouldBeTrue();
    }

    [Fact]
    public void IShadowShardRouter_ShadowTopology_ReturnsShadowTopology()
    {
        var shadowTopology = CreateTopology(2);
        var decorator = CreateShadowRouterWithTopology(
            new HashShardRouter(CreateTopology(3)),
            new HashShardRouter(shadowTopology),
            shadowTopology);

        decorator.ShadowTopology.ShouldBe(shadowTopology);
    }

    [Fact]
    public async Task IShadowShardRouter_CompareAsync_ReturnsValidComparisonResult()
    {
        var decorator = CreateShadowRouter(
            new HashShardRouter(CreateTopology(3)),
            new HashShardRouter(CreateTopology(3)));

        var result = await decorator.CompareAsync("test-key", CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShardKey.ShouldBe("test-key");
        result.ProductionShardId.ShouldNotBeNullOrEmpty();
        result.ShadowShardId.ShouldNotBeNullOrEmpty();
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static ShardTopology CreateTopology(int shardCount)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=test;Database=Shard{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    private static ShadowShardRouterDecorator CreateDecorator(
        IShardRouter primary, IShardRouter shadow)
    {
        var options = new ShadowShardingOptions
        {
            ShadowTopology = CreateTopology(2)
        };
        return new ShadowShardRouterDecorator(
            primary, shadow, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);
    }

    private static ShadowShardRouterDecorator CreateShadowRouter(
        IShardRouter primary, IShardRouter shadow) =>
        CreateDecorator(primary, shadow);

    private static ShadowShardRouterDecorator CreateShadowRouterWithTopology(
        IShardRouter primary, IShardRouter shadow, ShardTopology shadowTopology)
    {
        var options = new ShadowShardingOptions { ShadowTopology = shadowTopology };
        return new ShadowShardRouterDecorator(
            primary, shadow, options,
            NullLogger<ShadowShardRouterDecorator>.Instance);
    }

    private static string ExtractRegion(string key)
    {
        var colonIndex = key.IndexOf(':');
        return colonIndex >= 0 ? key[..colonIndex] : "US";
    }
}
