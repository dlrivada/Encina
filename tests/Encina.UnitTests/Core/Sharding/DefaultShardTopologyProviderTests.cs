using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="DefaultShardTopologyProvider"/>.
/// </summary>
public sealed class DefaultShardTopologyProviderTests
{
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DefaultShardTopologyProvider(null!));
    }

    [Fact]
    public void Constructor_ValidTopology_DoesNotThrow()
    {
        // Arrange
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn1")]);

        // Act & Assert
        Should.NotThrow(() => new DefaultShardTopologyProvider(topology));
    }

    [Fact]
    public void GetTopology_ReturnsSameTopology()
    {
        // Arrange
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn1")]);
        var provider = new DefaultShardTopologyProvider(topology);

        // Act
        var result = provider.GetTopology();

        // Assert
        result.ShouldBeSameAs(topology);
    }

    [Fact]
    public void GetTopology_CalledMultipleTimes_AlwaysReturnsSameInstance()
    {
        // Arrange
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn1")]);
        var provider = new DefaultShardTopologyProvider(topology);

        // Act
        var result1 = provider.GetTopology();
        var result2 = provider.GetTopology();

        // Assert
        result1.ShouldBeSameAs(result2);
    }
}
