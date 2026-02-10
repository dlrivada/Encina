using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding.Configuration;

/// <summary>
/// Unit tests for <see cref="ShardingOptions{TEntity}"/>.
/// </summary>
public sealed class ShardingOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  AddShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddShard_ValidParameters_AddsShard()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        var result = options.AddShard("shard-1", "conn1");

        // Assert
        result.ShouldBeSameAs(options); // Fluent chaining
        options.Shards.Count.ShouldBe(1);
        options.Shards["shard-1"].ConnectionString.ShouldBe("conn1");
    }

    [Fact]
    public void AddShard_MultipleShards_AddsAll()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        options.AddShard("shard-1", "conn1")
            .AddShard("shard-2", "conn2")
            .AddShard("shard-3", "conn3");

        // Assert
        options.Shards.Count.ShouldBe(3);
    }

    [Fact]
    public void AddShard_DuplicateShardId_OverwritesPrevious()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        options.AddShard("shard-1", "conn1")
            .AddShard("shard-1", "conn2");

        // Assert
        options.Shards.Count.ShouldBe(1);
        options.Shards["shard-1"].ConnectionString.ShouldBe("conn2");
    }

    [Fact]
    public void AddShard_CaseInsensitiveShardId_OverwritesPrevious()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        options.AddShard("Shard-1", "conn1")
            .AddShard("shard-1", "conn2");

        // Assert
        options.Shards.Count.ShouldBe(1);
    }

    [Fact]
    public void AddShard_WithWeightAndIsActive_SetsProperties()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        options.AddShard("shard-1", "conn1", weight: 3, isActive: false);

        // Assert
        options.Shards["shard-1"].Weight.ShouldBe(3);
        options.Shards["shard-1"].IsActive.ShouldBeFalse();
    }

    [Fact]
    public void AddShard_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.AddShard(null!, "conn1"));
    }

    [Fact]
    public void AddShard_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.AddShard("shard-1", null!));
    }

    // ────────────────────────────────────────────────────────────
    //  UseHashRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UseHashRouting_SetsRouterFactory()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        var result = options.UseHashRouting();

        // Assert
        result.ShouldBeSameAs(options);
        options.RouterFactory.ShouldNotBeNull();
    }

    [Fact]
    public void UseHashRouting_WithOptions_ConfiguresOptions()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act
        options.UseHashRouting(o => o.VirtualNodesPerShard = 200);

        // Assert
        options.RouterFactory.ShouldNotBeNull();
    }

    // ────────────────────────────────────────────────────────────
    //  UseRangeRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UseRangeRouting_ValidRanges_SetsRouterFactory()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        var ranges = new[]
        {
            new ShardRange("A", "M", "shard-1"),
            new ShardRange("M", null, "shard-2")
        };

        // Act
        var result = options.UseRangeRouting(ranges);

        // Assert
        result.ShouldBeSameAs(options);
        options.RouterFactory.ShouldNotBeNull();
    }

    [Fact]
    public void UseRangeRouting_NullRanges_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.UseRangeRouting(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  UseDirectoryRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UseDirectoryRouting_ValidStore_SetsRouterFactory()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        var store = Substitute.For<IShardDirectoryStore>();

        // Act
        var result = options.UseDirectoryRouting(store);

        // Assert
        result.ShouldBeSameAs(options);
        options.RouterFactory.ShouldNotBeNull();
    }

    [Fact]
    public void UseDirectoryRouting_NullStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.UseDirectoryRouting(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  UseGeoRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UseGeoRouting_ValidParameters_SetsRouterFactory()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        var regions = new[] { new GeoRegion("us-east", "shard-1") };
        string RegionResolver(string key) => "us-east";

        // Act
        var result = options.UseGeoRouting(regions, RegionResolver);

        // Assert
        result.ShouldBeSameAs(options);
        options.RouterFactory.ShouldNotBeNull();
    }

    [Fact]
    public void UseGeoRouting_NullRegions_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.UseGeoRouting(null!, _ => "us-east"));
    }

    [Fact]
    public void UseGeoRouting_NullResolver_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        var regions = new[] { new GeoRegion("us-east", "shard-1") };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.UseGeoRouting(regions, null!));
    }

    // ────────────────────────────────────────────────────────────
    //  UseCustomRouting
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void UseCustomRouting_ValidFactory_SetsRouterFactory()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        var mockRouter = Substitute.For<IShardRouter>();

        // Act
        var result = options.UseCustomRouting(_ => mockRouter);

        // Assert
        result.ShouldBeSameAs(options);
        options.RouterFactory.ShouldNotBeNull();
    }

    [Fact]
    public void UseCustomRouting_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => options.UseCustomRouting(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  BuildTopology
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void BuildTopology_WithShards_ReturnsTopologyContainingAllShards()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        options.AddShard("shard-1", "conn1")
            .AddShard("shard-2", "conn2");

        // Act
        var topology = options.BuildTopology();

        // Assert
        topology.AllShardIds.Count.ShouldBe(2);
        topology.ContainsShard("shard-1").ShouldBeTrue();
        topology.ContainsShard("shard-2").ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  BuildRouter
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void BuildRouter_WithHashRouting_ReturnsHashShardRouter()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        options.AddShard("shard-1", "conn1")
            .UseHashRouting();
        var topology = options.BuildTopology();

        // Act
        var router = options.BuildRouter(topology);

        // Assert
        router.ShouldBeOfType<HashShardRouter>();
    }

    [Fact]
    public void BuildRouter_NoRoutingConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();
        options.AddShard("shard-1", "conn1");
        var topology = options.BuildTopology();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => options.BuildRouter(topology));
    }

    // ────────────────────────────────────────────────────────────
    //  ScatterGather defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ScatterGather_DefaultsAreSet()
    {
        // Arrange
        var options = new ShardingOptions<TestOrder>();

        // Assert
        options.ScatterGather.ShouldNotBeNull();
        options.ScatterGather.MaxParallelism.ShouldBe(-1);
        options.ScatterGather.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.ScatterGather.AllowPartialResults.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity
    // ────────────────────────────────────────────────────────────

    private sealed class TestOrder
    {
        public string OrderId { get; set; } = default!;
    }
}
