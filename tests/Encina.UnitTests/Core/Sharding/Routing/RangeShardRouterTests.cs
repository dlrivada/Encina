using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding.Routing;

public sealed class RangeShardRouterTests
{
    #region Helper Methods

    private static ShardTopology CreateTopology(params ShardInfo[] shards) =>
        new(shards);

    private static ShardTopology CreateDefaultTopology() =>
        CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"),
            new ShardInfo("shard-2", "Server=db2;Database=shard2"));

    private static List<ShardRange> CreateDefaultRanges() =>
    [
        new ShardRange("A", "H", "shard-0"),
        new ShardRange("H", "P", "shard-1"),
        new ShardRange("P", null, "shard-2"),
    ];

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidInputs_CreatesRouter()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();

        // Act
        var router = new RangeShardRouter(topology, ranges);

        // Assert
        router.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var ranges = CreateDefaultRanges();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RangeShardRouter(null!, ranges))
            .ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullRanges_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RangeShardRouter(topology, null!))
            .ParamName.ShouldBe("ranges");
    }

    [Fact]
    public void Constructor_OverlappingRanges_ThrowsArgumentException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var overlappingRanges = new List<ShardRange>
        {
            new("A", "M", "shard-0"),
            new("K", "Z", "shard-1"), // Overlaps: K < M
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => new RangeShardRouter(topology, overlappingRanges));
    }

    [Fact]
    public void Constructor_UnboundedRangeNotLast_ThrowsArgumentException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = new List<ShardRange>
        {
            new("A", null, "shard-0"), // Unbounded, but there's another range after
            new("M", "Z", "shard-1"),
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => new RangeShardRouter(topology, ranges));
    }

    #endregion

    #region GetShardId Basic Routing

    [Fact]
    public void GetShardId_KeyInFirstRange_ReturnsCorrectShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardId("B");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-0"));
    }

    [Fact]
    public void GetShardId_KeyInMiddleRange_ReturnsCorrectShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardId("J");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-1"));
    }

    [Fact]
    public void GetShardId_KeyInLastRange_ReturnsCorrectShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardId("Z");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-2"));
    }

    #endregion

    #region GetShardId Boundary Conditions

    [Fact]
    public void GetShardId_KeyExactlyAtStartKey_ReturnsContainingRange()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act - "H" is the start of the second range (inclusive)
        var result = router.GetShardId("H");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-1"));
    }

    [Fact]
    public void GetShardId_KeyJustBeforeEndKey_ReturnsContainingRange()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act - "G" is within ["A", "H") since EndKey is exclusive
        var result = router.GetShardId("G");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-0"));
    }

    [Fact]
    public void GetShardId_KeyAtStartOfFirstRange_ReturnsFirstShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act - "A" is the exact start of the first range (inclusive)
        var result = router.GetShardId("A");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-0"));
    }

    #endregion

    #region GetShardId Open-Ended Range

    [Fact]
    public void GetShardId_OpenEndedRange_CatchesAllRemainingKeys()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges(); // Last range: "P" to null (unbounded)
        var router = new RangeShardRouter(topology, ranges);

        // Act - A very high key well beyond any explicit range boundary
        var result = router.GetShardId("ZZZZZZ");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-2"));
    }

    [Fact]
    public void GetShardId_OpenEndedRange_AtStartKey_ReturnsCorrectShard()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act - "P" is the start of the open-ended range (inclusive)
        var result = router.GetShardId("P");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("shard-2"));
    }

    #endregion

    #region GetShardId Outside Range (Error Cases)

    [Fact]
    public void GetShardId_KeyBeforeAllRanges_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"));
        var ranges = new List<ShardRange>
        {
            new("M", "S", "shard-0"),
            new("S", "Z", "shard-1"),
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act - "A" is before the first range starting at "M"
        var result = router.GetShardId("A");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetShardId_KeyInGapBetweenRanges_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"));
        var ranges = new List<ShardRange>
        {
            new("A", "F", "shard-0"),
            new("M", "Z", "shard-1"), // Gap: "F" to "M"
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act - "H" falls in the gap
        var result = router.GetShardId("H");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardId Error Codes

    [Fact]
    public void GetShardId_KeyOutsideRange_ErrorHasKeyOutsideRangeCode()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"));
        var ranges = new List<ShardRange>
        {
            new("M", "Z", "shard-0"),
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardId("A");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.KeyOutsideRange));
        });
    }

    [Fact]
    public void GetShardId_EmptyRanges_ReturnsNoActiveShardsError()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"));
        var ranges = new List<ShardRange>();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardId("anything");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error =>
        {
            var code = error.GetCode();
            code.IsSome.ShouldBeTrue();
            code.IfSome(c => c.ShouldBe(ShardingErrorCodes.NoActiveShards));
        });
    }

    #endregion

    #region GetAllShardIds

    [Fact]
    public void GetAllShardIds_ReturnsAllShardIdsFromTopology()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var shardIds = router.GetAllShardIds();

        // Assert
        shardIds.ShouldNotBeNull();
        shardIds.Count.ShouldBe(3);
        shardIds.ShouldContain("shard-0");
        shardIds.ShouldContain("shard-1");
        shardIds.ShouldContain("shard-2");
    }

    #endregion

    #region GetShardConnectionString

    [Fact]
    public void GetShardConnectionString_ValidShardId_ReturnsConnectionString()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var result = router.GetShardConnectionString("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(cs => cs.ShouldBe("Server=db1;Database=shard1"));
    }

    [Fact]
    public void GetShardConnectionString_InvalidShardId_ReturnsLeftError()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

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
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
    }

    #endregion

    #region Custom Comparer

    [Fact]
    public void GetShardId_OrdinalIgnoreCaseComparer_TreatsKeysAsCaseInsensitive()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"));
        var ranges = new List<ShardRange>
        {
            new("a", "m", "shard-0"),
            new("m", null, "shard-1"),
        };
        var router = new RangeShardRouter(topology, ranges, StringComparer.OrdinalIgnoreCase);

        // Act - "D" (uppercase) should be in the same range as "d" (lowercase)
        var resultUpper = router.GetShardId("D");
        var resultLower = router.GetShardId("d");

        // Assert
        resultUpper.IsRight.ShouldBeTrue();
        resultLower.IsRight.ShouldBeTrue();
        resultUpper.IfRight(id => id.ShouldBe("shard-0"));
        resultLower.IfRight(id => id.ShouldBe("shard-0"));
    }

    [Fact]
    public void GetShardId_DefaultOrdinalComparer_TreatsKeysCaseSensitively()
    {
        // Arrange - With ordinal comparer, uppercase letters sort before lowercase
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"));
        var ranges = new List<ShardRange>
        {
            new("A", "Z", "shard-0"),  // Uppercase range
            new("a", null, "shard-1"), // Lowercase range
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var resultUpper = router.GetShardId("M");
        var resultLower = router.GetShardId("m");

        // Assert - Different shards because ordinal comparison is case-sensitive
        resultUpper.IsRight.ShouldBeTrue();
        resultLower.IsRight.ShouldBeTrue();
        resultUpper.IfRight(id => id.ShouldBe("shard-0"));
        resultLower.IfRight(id => id.ShouldBe("shard-1"));
    }

    #endregion

    #region Multiple Ranges Same Shard

    [Fact]
    public void GetShardId_MultipleRangesPointToSameShard_RoutesCorrectly()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-0", "Server=db0;Database=shard0"),
            new ShardInfo("shard-1", "Server=db1;Database=shard1"));
        var ranges = new List<ShardRange>
        {
            new("A", "H", "shard-0"),
            new("H", "P", "shard-1"),
            new("P", null, "shard-0"), // Same shard as the first range
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var resultFirst = router.GetShardId("C");
        var resultMiddle = router.GetShardId("K");
        var resultLast = router.GetShardId("R");

        // Assert
        resultFirst.IsRight.ShouldBeTrue();
        resultFirst.IfRight(id => id.ShouldBe("shard-0"));

        resultMiddle.IsRight.ShouldBeTrue();
        resultMiddle.IfRight(id => id.ShouldBe("shard-1"));

        resultLast.IsRight.ShouldBeTrue();
        resultLast.IfRight(id => id.ShouldBe("shard-0"));
    }

    #endregion

    #region Single Range

    [Fact]
    public void GetShardId_SingleOpenEndedRange_RoutesAllKeysToOneShard()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-only", "Server=db0;Database=only"));
        var ranges = new List<ShardRange>
        {
            new("A", null, "shard-only"), // Single open-ended range from "A"
        };
        var router = new RangeShardRouter(topology, ranges);

        // Act
        var resultA = router.GetShardId("A");
        var resultM = router.GetShardId("M");
        var resultZ = router.GetShardId("ZZZZZ");

        // Assert
        resultA.IsRight.ShouldBeTrue();
        resultA.IfRight(id => id.ShouldBe("shard-only"));

        resultM.IsRight.ShouldBeTrue();
        resultM.IfRight(id => id.ShouldBe("shard-only"));

        resultZ.IsRight.ShouldBeTrue();
        resultZ.IfRight(id => id.ShouldBe("shard-only"));
    }

    #endregion

    #region Null Shard Key

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateDefaultTopology();
        var ranges = CreateDefaultRanges();
        var router = new RangeShardRouter(topology, ranges);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
    }

    #endregion
}
