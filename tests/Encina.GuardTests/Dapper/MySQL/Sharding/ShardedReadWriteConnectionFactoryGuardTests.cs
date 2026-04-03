using Encina.Dapper.MySQL.Sharding;
using Encina.Messaging.ReadWriteSeparation;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.Sharding.ReplicaSelection;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Sharding;

/// <summary>
/// Guard tests for <see cref="ShardedReadWriteConnectionFactory"/> to verify null parameter handling.
/// </summary>
public class ShardedReadWriteConnectionFactoryGuardTests
{
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ShardedReadWriteOptions();
        var healthTracker = Substitute.For<IReplicaHealthTracker>();

        // Act & Assert
        var act = () => new ShardedReadWriteConnectionFactory(null!, options, healthTracker);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology();
        var healthTracker = Substitute.For<IReplicaHealthTracker>();

        // Act & Assert
        var act = () => new ShardedReadWriteConnectionFactory(topology, null!, healthTracker);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullHealthTracker_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology();
        var options = new ShardedReadWriteOptions();

        // Act & Assert
        var act = () => new ShardedReadWriteConnectionFactory(topology, options, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("healthTracker");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var topology = CreateTopology();
        var options = new ShardedReadWriteOptions();
        var healthTracker = Substitute.For<IReplicaHealthTracker>();

        // Act & Assert
        Should.NotThrow(() => new ShardedReadWriteConnectionFactory(topology, options, healthTracker));
    }

    private static ShardTopology CreateTopology() => new(Array.Empty<ShardInfo>());
}
