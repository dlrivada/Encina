using Encina.ADO.PostgreSQL.Sharding;
using Encina.Sharding;
using Encina.Sharding.Data;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL.Sharding;

/// <summary>
/// Guard tests for <see cref="ShardedConnectionFactory"/> to verify null parameter handling.
/// </summary>
public class ShardedConnectionFactoryGuardTests
{
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        // Arrange
        var router = Substitute.For<IShardRouter>();

        // Act & Assert
        var act = () => new ShardedConnectionFactory(null!, router);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullRouter_ThrowsArgumentNullException()
    {
        // Arrange
        var topology = CreateTopology();

        // Act & Assert
        var act = () => new ShardedConnectionFactory(topology, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("router");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var topology = CreateTopology();
        var router = Substitute.For<IShardRouter>();

        // Act & Assert
        Should.NotThrow(() => new ShardedConnectionFactory(topology, router));
    }

    private static ShardTopology CreateTopology() => new(Array.Empty<ShardInfo>());
}
