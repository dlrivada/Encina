using Encina.Sharding;
using Encina.Sharding.Diagnostics;
using LanguageExt;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="InstrumentedShardRouter"/> (internal class, visible via InternalsVisibleTo).
/// </summary>
public sealed class InstrumentedShardRouterTests
{
    private static ShardTopology CreateTopology(params string[] shardIds)
    {
        var shards = shardIds.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
        return new ShardTopology(shards);
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.Throw<ArgumentNullException>(() => new InstrumentedShardRouter(null!, metrics, "hash"));
    }

    [Fact]
    public void Constructor_NullMetrics_ThrowsArgumentNullException()
    {
        var inner = Substitute.For<IShardRouter>();
        Should.Throw<ArgumentNullException>(() => new InstrumentedShardRouter(inner, null!, "hash"));
    }

    [Fact]
    public void Constructor_NullRouterType_ThrowsArgumentException()
    {
        var inner = Substitute.For<IShardRouter>();
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.Throw<ArgumentException>(() => new InstrumentedShardRouter(inner, metrics, null!));
    }

    [Fact]
    public void Constructor_EmptyRouterType_ThrowsArgumentException()
    {
        var inner = Substitute.For<IShardRouter>();
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        Should.Throw<ArgumentException>(() => new InstrumentedShardRouter(inner, metrics, ""));
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardId — delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardId("key-123")
            .Returns(Prelude.Right<EncinaError, string>("shard-1"));
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        var instrumented = new InstrumentedShardRouter(inner, metrics, "hash");

        // Act
        var result = instrumented.GetShardId("key-123");

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(id => id.ShouldBe("shard-1"));
        inner.Received(1).GetShardId("key-123");
    }

    [Fact]
    public void GetShardId_InnerReturnsError_PropagatesError()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        var error = EncinaErrors.Create(ShardingErrorCodes.ShardNotFound, "Not found");
        inner.GetShardId("bad-key")
            .Returns(Prelude.Left<EncinaError, string>(error));
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        var instrumented = new InstrumentedShardRouter(inner, metrics, "hash");

        // Act
        var result = instrumented.GetShardId("bad-key");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllShardIds — delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllShardIds_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetAllShardIds().Returns(new List<string> { "shard-1", "shard-2" });
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1", "shard-2"));
        var instrumented = new InstrumentedShardRouter(inner, metrics, "hash");

        // Act
        var result = instrumented.GetAllShardIds();

        // Assert
        result.Count.ShouldBe(2);
        inner.Received(1).GetAllShardIds();
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardConnectionString — delegation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardConnectionString_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IShardRouter>();
        inner.GetShardConnectionString("shard-1")
            .Returns(Prelude.Right<EncinaError, string>("conn-1"));
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        var instrumented = new InstrumentedShardRouter(inner, metrics, "hash");

        // Act
        var result = instrumented.GetShardConnectionString("shard-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        inner.Received(1).GetShardConnectionString("shard-1");
    }

    // ────────────────────────────────────────────────────────────
    //  RouterType property
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RouterType_ReturnsConfiguredType()
    {
        var inner = Substitute.For<IShardRouter>();
        var metrics = new ShardRoutingMetrics(CreateTopology("shard-1"));
        var instrumented = new InstrumentedShardRouter(inner, metrics, "range");

        instrumented.RouterType.ShouldBe("range");
    }
}
