using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding.Routing;

public sealed class HashShardRouterTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);

    private static ShardInfo Shard(string id, bool isActive = true)
        => new(id, $"Server={id};Database=db;", IsActive: isActive);

    private static ShardTopology ThreeShardTopology()
        => CreateTopology(
            Shard("shard-a"),
            Shard("shard-b"),
            Shard("shard-c"));

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidTopology_CreatesRouter()
    {
        // Arrange
        var topology = ThreeShardTopology();

        // Act
        var router = new HashShardRouter(topology);

        // Assert
        router.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new HashShardRouter(null!));
    }

    [Fact]
    public void Constructor_CustomOptions_RespectsVirtualNodesPerShard()
    {
        // Arrange
        var topology = CreateTopology(Shard("shard-a"));
        var options = new HashShardRouterOptions { VirtualNodesPerShard = 50 };

        // Act
        var router = new HashShardRouter(topology, options);

        // Assert - router created without exception and routes correctly
        var result = router.GetShardId("any-key");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_UsesDefaultVirtualNodes()
    {
        // Arrange
        var topology = CreateTopology(Shard("shard-a"));

        // Act
        var router = new HashShardRouter(topology, options: null);

        // Assert - routes without error (default 150 virtual nodes used)
        var result = router.GetShardId("test-key");
        result.IsRight.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - Determinism
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_SameKey_AlwaysReturnsSameShardId()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());
        const string key = "order-12345";

        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => router.GetShardId(key))
            .ToList();

        // Assert - all calls return Right with the same shard
        var firstShardId = string.Empty;
        _ = results[0].IfRight(id => firstShardId = id);
        firstShardId.ShouldNotBeNullOrWhiteSpace();

        foreach (var result in results)
        {
            result.IsRight.ShouldBeTrue();
            _ = result.IfRight(id => id.ShouldBe(firstShardId));
        }
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("user-2")]
    [InlineData("order-abc")]
    [InlineData("")]
    [InlineData("  ")]
    public void GetShardId_VariousKeys_ReturnsDeterministicResult(string key)
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());

        // Act
        var result1 = router.GetShardId(key);
        var result2 = router.GetShardId(key);

        // Assert
        result1.IsRight.ShouldBeTrue();
        result2.IsRight.ShouldBeTrue();

        var shard1 = string.Empty;
        var shard2 = string.Empty;
        _ = result1.IfRight(id => shard1 = id);
        _ = result2.IfRight(id => shard2 = id);

        shard1.ShouldBe(shard2);
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - Coverage (all keys map to valid shard)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_AnyKey_ReturnsShardFromTopology()
    {
        // Arrange
        var topology = ThreeShardTopology();
        var router = new HashShardRouter(topology);
        var validShardIds = topology.AllShardIds.ToHashSet();
        var keys = Enumerable.Range(0, 200).Select(i => $"key-{i}").ToList();

        // Act & Assert
        foreach (var key in keys)
        {
            var result = router.GetShardId(key);
            result.IsRight.ShouldBeTrue();
            result.IfRight(shardId => validShardIds.ShouldContain(shardId));
        }
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - Distribution
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_ManyKeys_DistributesAcrossMultipleShards()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());
        var assignedShards = new System.Collections.Generic.HashSet<string>();

        // Act - hash 1000 keys and collect unique shard assignments
        for (var i = 0; i < 1000; i++)
        {
            var result = router.GetShardId($"entity-{i}");
            _ = result.IfRight(id => assignedShards.Add(id));
        }

        // Assert - keys should distribute across all 3 shards
        assignedShards.Count.ShouldBe(3);
    }

    [Fact]
    public void GetShardId_ManyKeys_NoSingleShardGetsAllTraffic()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());
        var shardCounts = new Dictionary<string, int>();

        // Act
        for (var i = 0; i < 1000; i++)
        {
            router.GetShardId($"item-{i}").IfRight(id =>
            {
                if (!shardCounts.TryAdd(id, 1))
                {
                    shardCounts[id]++;
                }
            });
        }

        // Assert - no single shard gets more than 60% of keys (reasonable threshold for 3 shards)
        foreach (var count in shardCounts.Values)
        {
            count.ShouldBeLessThan(600);
        }

        // Each shard should get at least some keys (> 5%)
        foreach (var count in shardCounts.Values)
        {
            count.ShouldBeGreaterThan(50);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - Null key
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - Empty/whitespace keys (valid, just hashed)
    // ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void GetShardId_EmptyOrWhitespaceKey_StillRoutes(string key)
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());

        // Act
        var result = router.GetShardId(key);

        // Assert - empty/whitespace strings are valid hash inputs
        result.IsRight.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId - No active shards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_AllShardsInactive_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(
            Shard("shard-a", isActive: false),
            Shard("shard-b", isActive: false));
        var router = new HashShardRouter(topology);

        // Act
        var result = router.GetShardId("some-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.NoActiveShards));
        });
    }

    [Fact]
    public void GetShardId_EmptyTopology_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(); // no shards at all
        var router = new HashShardRouter(topology);

        // Act
        var result = router.GetShardId("any-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllShardIds
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllShardIds_ReturnsAllShardIdsFromTopology()
    {
        // Arrange
        var topology = ThreeShardTopology();
        var router = new HashShardRouter(topology);

        // Act
        var shardIds = router.GetAllShardIds();

        // Assert
        shardIds.Count.ShouldBe(3);
        shardIds.ShouldContain("shard-a");
        shardIds.ShouldContain("shard-b");
        shardIds.ShouldContain("shard-c");
    }

    [Fact]
    public void GetAllShardIds_IncludesInactiveShards()
    {
        // Arrange
        var topology = CreateTopology(
            Shard("active-1"),
            Shard("inactive-1", isActive: false));
        var router = new HashShardRouter(topology);

        // Act
        var shardIds = router.GetAllShardIds();

        // Assert - AllShardIds includes both active and inactive
        shardIds.Count.ShouldBe(2);
        shardIds.ShouldContain("active-1");
        shardIds.ShouldContain("inactive-1");
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardConnectionString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardConnectionString_ValidShardId_ReturnsConnectionString()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-a", "Server=alpha;Database=db;"));
        var router = new HashShardRouter(topology);

        // Act
        var result = router.GetShardConnectionString("shard-a");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(cs => cs.ShouldBe("Server=alpha;Database=db;"));
    }

    [Fact]
    public void GetShardConnectionString_InvalidShardId_ReturnsLeftError()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());

        // Act
        var result = router.GetShardConnectionString("nonexistent-shard");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.ShardNotFound));
        });
    }

    [Fact]
    public void GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new HashShardRouter(ThreeShardTopology());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Virtual Nodes - Distribution quality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void VirtualNodes_HigherCount_ProducesBetterDistribution()
    {
        // Arrange
        var topology = ThreeShardTopology();
        var lowVnRouter = new HashShardRouter(topology, new HashShardRouterOptions { VirtualNodesPerShard = 1 });
        var highVnRouter = new HashShardRouter(topology, new HashShardRouterOptions { VirtualNodesPerShard = 500 });

        // Act - measure distribution with 3000 keys
        var lowCounts = CountDistribution(lowVnRouter, 3000);
        var highCounts = CountDistribution(highVnRouter, 3000);

        // Assert - high VN should have lower standard deviation (more even)
        var lowStdDev = StandardDeviation(lowCounts.Values);
        var highStdDev = StandardDeviation(highCounts.Values);

        // High VN should produce more uniform distribution
        highStdDev.ShouldBeLessThan(lowStdDev + 1); // +1 tolerance for edge cases
    }

    private static Dictionary<string, int> CountDistribution(HashShardRouter router, int keyCount)
    {
        var counts = new Dictionary<string, int>();
        for (var i = 0; i < keyCount; i++)
        {
            router.GetShardId($"dist-key-{i}").IfRight(id =>
            {
                if (!counts.TryAdd(id, 1))
                {
                    counts[id]++;
                }
            });
        }

        return counts;
    }

    private static double StandardDeviation(IEnumerable<int> values)
    {
        var list = values.ToList();
        var avg = list.Average();
        var sumOfSquares = list.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumOfSquares / list.Count);
    }

    // ────────────────────────────────────────────────────────────
    //  CalculateAffectedKeyRanges
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateAffectedKeyRanges_AddingShard_ProducesAffectedRanges()
    {
        // Arrange
        var oldTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"));
        var newTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"), Shard("shard-c"));
        var router = new HashShardRouter(oldTopology);

        // Act
        var affected = router.CalculateAffectedKeyRanges(oldTopology, newTopology);

        // Assert - adding a shard must cause some key ranges to be reassigned
        affected.ShouldNotBeNull();
        affected.Count.ShouldBeGreaterThan(0);

        // All affected ranges should have shard-c as the new shard for at least some
        affected.Any(r => r.NewShardId == "shard-c").ShouldBeTrue();
    }

    [Fact]
    public void CalculateAffectedKeyRanges_RemovingShard_ProducesAffectedRanges()
    {
        // Arrange
        var oldTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"), Shard("shard-c"));
        var newTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"));
        var router = new HashShardRouter(oldTopology);

        // Act
        var affected = router.CalculateAffectedKeyRanges(oldTopology, newTopology);

        // Assert - removing shard-c should cause ranges to move away from it
        affected.ShouldNotBeNull();
        affected.Count.ShouldBeGreaterThan(0);

        // Some ranges previously owned by shard-c should now go to other shards
        affected.Any(r => r.PreviousShardId == "shard-c").ShouldBeTrue();
    }

    [Fact]
    public void CalculateAffectedKeyRanges_SameTopology_ReturnsEmptyList()
    {
        // Arrange
        var topology1 = CreateTopology(Shard("shard-a"), Shard("shard-b"));
        var topology2 = CreateTopology(Shard("shard-a"), Shard("shard-b"));
        var router = new HashShardRouter(topology1);

        // Act
        var affected = router.CalculateAffectedKeyRanges(topology1, topology2);

        // Assert
        affected.ShouldNotBeNull();
        affected.Count.ShouldBe(0);
    }

    [Fact]
    public void CalculateAffectedKeyRanges_NullOldTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = ThreeShardTopology();
        var router = new HashShardRouter(topology);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.CalculateAffectedKeyRanges(null!, topology));
    }

    [Fact]
    public void CalculateAffectedKeyRanges_NullNewTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = ThreeShardTopology();
        var router = new HashShardRouter(topology);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.CalculateAffectedKeyRanges(topology, null!));
    }

    [Fact]
    public void CalculateAffectedKeyRanges_AffectedRangesHaveDistinctPreviousAndNewShard()
    {
        // Arrange
        var oldTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"));
        var newTopology = CreateTopology(Shard("shard-a"), Shard("shard-b"), Shard("shard-c"));
        var router = new HashShardRouter(oldTopology);

        // Act
        var affected = router.CalculateAffectedKeyRanges(oldTopology, newTopology);

        // Assert - each affected range must have different previous vs new shard
        foreach (var range in affected)
        {
            range.PreviousShardId.ShouldNotBe(range.NewShardId);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  ComputeHash (internal, visible via InternalsVisibleTo)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeHash_SameKey_AlwaysReturnsSameHash()
    {
        // Arrange
        const string key = "deterministic-key";

        // Act
        var hash1 = HashShardRouter.ComputeHash(key);
        var hash2 = HashShardRouter.ComputeHash(key);
        var hash3 = HashShardRouter.ComputeHash(key);

        // Assert
        hash1.ShouldBe(hash2);
        hash2.ShouldBe(hash3);
    }

    [Fact]
    public void ComputeHash_DifferentKeys_ProduceDifferentHashes()
    {
        // Act
        var hash1 = HashShardRouter.ComputeHash("key-alpha");
        var hash2 = HashShardRouter.ComputeHash("key-beta");
        var hash3 = HashShardRouter.ComputeHash("key-gamma");

        // Assert
        hash1.ShouldNotBe(hash2);
        hash2.ShouldNotBe(hash3);
        hash1.ShouldNotBe(hash3);
    }

    [Fact]
    public void ComputeHash_EmptyString_ReturnsConsistentValue()
    {
        // Act
        var hash1 = HashShardRouter.ComputeHash(string.Empty);
        var hash2 = HashShardRouter.ComputeHash(string.Empty);

        // Assert
        hash1.ShouldBe(hash2);
    }

    // ────────────────────────────────────────────────────────────
    //  Inactive Shards - only active shards receive traffic
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_MixedActiveInactive_OnlyRoutesToActiveShards()
    {
        // Arrange
        var topology = CreateTopology(
            Shard("active-1"),
            Shard("active-2"),
            Shard("inactive-1", isActive: false),
            Shard("inactive-2", isActive: false));
        var router = new HashShardRouter(topology);
        var routedShards = new System.Collections.Generic.HashSet<string>();

        // Act - route many keys
        for (var i = 0; i < 500; i++)
        {
            _ = router.GetShardId($"key-{i}").IfRight(id => routedShards.Add(id));
        }

        // Assert - traffic goes only to active shards
        routedShards.ShouldNotContain("inactive-1");
        routedShards.ShouldNotContain("inactive-2");
        routedShards.ShouldContain("active-1");
        routedShards.ShouldContain("active-2");
    }

    // ────────────────────────────────────────────────────────────
    //  Single Shard - all keys route to it
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_SingleShard_AllKeysRouteToThatShard()
    {
        // Arrange
        var topology = CreateTopology(Shard("only-shard"));
        var router = new HashShardRouter(topology);

        // Act & Assert
        for (var i = 0; i < 100; i++)
        {
            var result = router.GetShardId($"key-{i}");
            result.IsRight.ShouldBeTrue();
            result.IfRight(id => id.ShouldBe("only-shard"));
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Interface compliance
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void HashShardRouter_ImplementsIShardRouter()
    {
        // Arrange & Act
        var router = new HashShardRouter(ThreeShardTopology());

        // Assert
        router.ShouldBeAssignableTo<IShardRouter>();
    }

    [Fact]
    public void HashShardRouter_ImplementsIShardRebalancer()
    {
        // Arrange & Act
        var router = new HashShardRouter(ThreeShardTopology());

        // Assert
        router.ShouldBeAssignableTo<IShardRebalancer>();
    }
}
