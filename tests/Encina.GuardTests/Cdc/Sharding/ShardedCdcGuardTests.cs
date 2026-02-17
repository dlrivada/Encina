using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;
using Encina.Cdc.Processing;
using Encina.Cdc.Sharding;
using Encina.Sharding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Cdc.Sharding;

/// <summary>
/// Guard clause tests for sharded CDC types.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class ShardedCdcGuardTests
{
    #region InMemoryShardedCdcPositionStore Guards

    [Fact]
    public async Task GetPositionAsync_NullShardId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync(null!, "connector"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task GetPositionAsync_EmptyShardId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("", "connector"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task GetPositionAsync_WhitespaceShardId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("   ", "connector"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task GetPositionAsync_NullConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("shard", null!));
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public async Task GetPositionAsync_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("shard", ""));
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public async Task SavePositionAsync_NullShardId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.SavePositionAsync(null!, "connector", new TestCdcPosition(1)));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task SavePositionAsync_NullConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.SavePositionAsync("shard", null!, new TestCdcPosition(1)));
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public async Task SavePositionAsync_NullPosition_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => store.SavePositionAsync("shard", "connector", null!));
        ex.ParamName.ShouldBe("position");
    }

    [Fact]
    public async Task DeletePositionAsync_NullShardId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.DeletePositionAsync(null!, "connector"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public async Task DeletePositionAsync_NullConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.DeletePositionAsync("shard", null!));
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public async Task GetAllPositionsAsync_NullConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetAllPositionsAsync(null!));
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public async Task GetAllPositionsAsync_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new InMemoryShardedCdcPositionStore();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(
            () => store.GetAllPositionsAsync(""));
        ex.ParamName.ShouldBe("connectorId");
    }

    #endregion

    #region ShardedCdcConnector Guards

    [Fact]
    public void ShardedCdcConnector_NullConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var topology = new ShardTopology([]);
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        var logger = NullLogger<ShardedCdcConnector>.Instance;

        // Act
        var act = () => new ShardedCdcConnector(null!, factory, provider, logger);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public void ShardedCdcConnector_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var topology = new ShardTopology([]);
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        var logger = NullLogger<ShardedCdcConnector>.Instance;

        // Act
        var act = () => new ShardedCdcConnector("", factory, provider, logger);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    [Fact]
    public void ShardedCdcConnector_NullFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology([]);
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        var logger = NullLogger<ShardedCdcConnector>.Instance;

        // Act
        var act = () => new ShardedCdcConnector("test", null!, provider, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectorFactory");
    }

    [Fact]
    public void ShardedCdcConnector_NullTopologyProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        var logger = NullLogger<ShardedCdcConnector>.Instance;

        // Act
        var act = () => new ShardedCdcConnector("test", factory, null!, logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("topologyProvider");
    }

    [Fact]
    public void ShardedCdcConnector_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology([]);
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        // Act
        var act = () => new ShardedCdcConnector("test", factory, provider, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void ShardedCdcConnector_AddConnector_NullShardInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var topology = new ShardTopology([]);
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        var connector = new ShardedCdcConnector("test", factory, provider, NullLogger<ShardedCdcConnector>.Instance);

        // Act
        Action act = () => connector.AddConnector(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("shardInfo");
    }

    #endregion

    #region ShardedCdcProcessor Guards

    [Fact]
    public void ShardedCdcProcessor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<ShardedCdcProcessor>.Instance;
        var options = new CdcOptions();

        // Act
        var act = () => new ShardedCdcProcessor(null!, logger, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void ShardedCdcProcessor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sp = Substitute.For<IServiceProvider>();
        var options = new CdcOptions();

        // Act
        var act = () => new ShardedCdcProcessor(sp, null!, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void ShardedCdcProcessor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var sp = Substitute.For<IServiceProvider>();
        var logger = NullLogger<ShardedCdcProcessor>.Instance;

        // Act
        var act = () => new ShardedCdcProcessor(sp, logger, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region ShardedCdcHealthCheck Guards

    [Fact]
    public void ShardedCdcHealthCheck_NullConnector_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = Substitute.For<IShardedCdcPositionStore>();

        // Act
        var act = () => new ShardedCdcHealthCheck(null!, store);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connector");
    }

    [Fact]
    public void ShardedCdcHealthCheck_NullPositionStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connector = Substitute.For<IShardedCdcConnector>();

        // Act
        var act = () => new ShardedCdcHealthCheck(connector, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("positionStore");
    }

    #endregion

    #region Test Position Helper

    private sealed class TestCdcPosition : CdcPosition
    {
        public long Value { get; }

        public TestCdcPosition(long value) => Value = value;

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion
}
