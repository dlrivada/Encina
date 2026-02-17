using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardInfo"/>.
/// </summary>
public sealed class ShardInfoTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var shard = new ShardInfo("shard-1", "Server=localhost;Database=shard1");

        // Assert
        shard.ShardId.ShouldBe("shard-1");
        shard.ConnectionString.ShouldBe("Server=localhost;Database=shard1");
        shard.Weight.ShouldBe(1);
        shard.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_CustomWeightAndActive_SetsCorrectly()
    {
        // Act
        var shard = new ShardInfo("shard-2", "conn2", Weight: 5, IsActive: false);

        // Assert
        shard.Weight.ShouldBe(5);
        shard.IsActive.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceShardId_ThrowsArgumentException(string? shardId)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ShardInfo(shardId!, "conn"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespaceConnectionString_ThrowsArgumentException(string? connString)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ShardInfo("shard-1", connString!));
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var shard1 = new ShardInfo("shard-1", "conn1", Weight: 2, IsActive: true);
        var shard2 = new ShardInfo("shard-1", "conn1", Weight: 2, IsActive: true);

        // Assert
        shard1.ShouldBe(shard2);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var shard1 = new ShardInfo("shard-1", "conn1");
        var shard2 = new ShardInfo("shard-2", "conn2");

        // Assert
        shard1.ShouldNotBe(shard2);
    }

    [Fact]
    public void WithExpression_ModifyWeight_CreatesNewInstance()
    {
        // Arrange
        var original = new ShardInfo("shard-1", "conn1", Weight: 1);

        // Act
        var modified = original with { Weight = 10 };

        // Assert
        modified.Weight.ShouldBe(10);
        modified.ShardId.ShouldBe("shard-1");
        original.Weight.ShouldBe(1);
    }

    [Fact]
    public void DefaultWeight_IsOne()
    {
        // Act
        var shard = new ShardInfo("shard-1", "conn1");

        // Assert
        shard.Weight.ShouldBe(1);
    }
}
