using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardTopology"/>.
/// </summary>
public sealed class ShardTopologyTests
{
    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ValidShards_CreatesTopology()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-1", "conn1"),
            new ShardInfo("shard-2", "conn2")
        };

        // Act
        var topology = new ShardTopology(shards);

        // Assert
        topology.Count.ShouldBe(2);
    }

    [Fact]
    public void Constructor_NullShards_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new ShardTopology(null!));
    }

    [Fact]
    public void Constructor_DuplicateShardIds_ThrowsArgumentException()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("shard-1", "conn1"),
            new ShardInfo("shard-1", "conn2")
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => new ShardTopology(shards));
    }

    [Fact]
    public void Constructor_DuplicateShardIdsCaseInsensitive_ThrowsArgumentException()
    {
        // Arrange
        var shards = new[]
        {
            new ShardInfo("Shard-1", "conn1"),
            new ShardInfo("shard-1", "conn2")
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() => new ShardTopology(shards));
    }

    [Fact]
    public void Constructor_EmptyCollection_CreatesEmptyTopology()
    {
        // Act
        var topology = new ShardTopology([]);

        // Assert
        topology.Count.ShouldBe(0);
        topology.AllShardIds.ShouldBeEmpty();
    }

    // ────────────────────────────────────────────────────────────
    //  AllShardIds / ActiveShardIds
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AllShardIds_ReturnsAllShards()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-1", "conn1", IsActive: true),
            new ShardInfo("shard-2", "conn2", IsActive: false));

        // Act
        var allIds = topology.AllShardIds;

        // Assert
        allIds.Count.ShouldBe(2);
        allIds.ShouldContain("shard-1");
        allIds.ShouldContain("shard-2");
    }

    [Fact]
    public void ActiveShardIds_ReturnsOnlyActiveShards()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("active-1", "conn1", IsActive: true),
            new ShardInfo("inactive-1", "conn2", IsActive: false),
            new ShardInfo("active-2", "conn3", IsActive: true));

        // Act
        var activeIds = topology.ActiveShardIds;

        // Assert
        activeIds.Count.ShouldBe(2);
        activeIds.ShouldContain("active-1");
        activeIds.ShouldContain("active-2");
        activeIds.ShouldNotContain("inactive-1");
    }

    [Fact]
    public void ActiveShardIds_AllInactive_ReturnsEmpty()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-1", "conn1", IsActive: false),
            new ShardInfo("shard-2", "conn2", IsActive: false));

        // Act
        var activeIds = topology.ActiveShardIds;

        // Assert
        activeIds.ShouldBeEmpty();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShard_ExistingShard_ReturnsRight()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act
        var result = topology.GetShard("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(s => s.ConnectionString.ShouldBe("conn1"));
    }

    [Fact]
    public void GetShard_CaseInsensitive_ReturnsRight()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("Shard-1", "conn1"));

        // Act
        var result = topology.GetShard("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void GetShard_NonExistentShard_ReturnsLeft()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act
        var result = topology.GetShard("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetShard_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => topology.GetShard(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  GetConnectionString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetConnectionString_ExistingShard_ReturnsConnectionString()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "Server=db;Database=shard1"));

        // Act
        var result = topology.GetConnectionString("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(cs => cs.ShouldBe("Server=db;Database=shard1"));
    }

    [Fact]
    public void GetConnectionString_NonExistentShard_ReturnsLeft()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act
        var result = topology.GetConnectionString("nonexistent");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => topology.GetConnectionString(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllShards / GetActiveShards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllShards_ReturnsAllShardInfos()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("shard-1", "conn1"),
            new ShardInfo("shard-2", "conn2", IsActive: false));

        // Act
        var allShards = topology.GetAllShards();

        // Assert
        allShards.Count.ShouldBe(2);
    }

    [Fact]
    public void GetActiveShards_ReturnsOnlyActiveShardInfos()
    {
        // Arrange
        var topology = CreateTopology(
            new ShardInfo("active", "conn1", IsActive: true),
            new ShardInfo("inactive", "conn2", IsActive: false));

        // Act
        var activeShards = topology.GetActiveShards();

        // Assert
        activeShards.Count.ShouldBe(1);
        activeShards[0].ShardId.ShouldBe("active");
    }

    // ────────────────────────────────────────────────────────────
    //  ContainsShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ContainsShard_ExistingShard_ReturnsTrue()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act & Assert
        topology.ContainsShard("shard-1").ShouldBeTrue();
    }

    [Fact]
    public void ContainsShard_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("Shard-1", "conn1"));

        // Act & Assert
        topology.ContainsShard("shard-1").ShouldBeTrue();
    }

    [Fact]
    public void ContainsShard_NonExistentShard_ReturnsFalse()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act & Assert
        topology.ContainsShard("nonexistent").ShouldBeFalse();
    }

    [Fact]
    public void ContainsShard_NullShardId_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology(new ShardInfo("shard-1", "conn1"));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => topology.ContainsShard(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);
}
