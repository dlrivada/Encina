using Encina.Sharding;
using Encina.Sharding.Diagnostics;

namespace Encina.GuardTests.Sharding;

/// <summary>
/// Guard tests for <see cref="InstrumentedShardRouter"/> and <see cref="InstrumentedShardRouter{TEntity}"/>.
/// </summary>
public sealed class InstrumentedShardRouterGuardTests
{
    private static ShardRoutingMetrics CreateMetrics() =>
        new(new ShardTopology([new ShardInfo("shard-1", "Server=localhost")]));

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedShardRouter(null!, CreateMetrics(), "hash");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullMetrics_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedShardRouter(Substitute.For<IShardRouter>(), null!, "hash");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("metrics");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidRouterType_ThrowsArgumentException(string? routerType)
    {
        var act = () => new InstrumentedShardRouter(Substitute.For<IShardRouter>(), CreateMetrics(), routerType!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("routerType");
    }

    [Fact]
    public void Constructor_ValidArguments_ExposesRouterType()
    {
        var sut = new InstrumentedShardRouter(Substitute.For<IShardRouter>(), CreateMetrics(), "hash");

        sut.RouterType.ShouldBe("hash");
    }

    [Fact]
    public void GenericConstructor_NullInner_ThrowsArgumentNullException()
    {
        var baseRouter = new InstrumentedShardRouter(Substitute.For<IShardRouter>(), CreateMetrics(), "hash");

        var act = () => new InstrumentedShardRouter<string>(null!, baseRouter);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void GenericConstructor_NullInstrumentedBase_ThrowsArgumentNullException()
    {
        var act = () => new InstrumentedShardRouter<string>(Substitute.For<IShardRouter<string>>(), null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("instrumentedBase");
    }

    [Fact]
    public void GenericConstructor_ValidArguments_DoesNotThrow()
    {
        var baseRouter = new InstrumentedShardRouter(Substitute.For<IShardRouter>(), CreateMetrics(), "hash");

        Should.NotThrow(() => new InstrumentedShardRouter<string>(Substitute.For<IShardRouter<string>>(), baseRouter));
    }
}
