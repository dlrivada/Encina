using Encina.Sharding;
using Encina.Sharding.Shadow;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShadowShardRouterDecorator"/>.
/// </summary>
public sealed class ShadowShardRouterDecoratorGuardTests
{
    private static ShadowShardingOptions CreateOptions() =>
        new() { ShadowTopology = new ShardTopology([new ShardInfo("shadow-1", "conn-1")]) };

    private static ShadowShardRouterDecorator CreateDecorator() =>
        new(
            Substitute.For<IShardRouter>(),
            Substitute.For<IShardRouter>(),
            CreateOptions(),
            NullLogger<ShadowShardRouterDecorator>.Instance);

    [Fact]
    public void Constructor_NullPrimary_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShadowShardRouterDecorator(
            null!, Substitute.For<IShardRouter>(), CreateOptions(), NullLogger<ShadowShardRouterDecorator>.Instance));
        ex.ParamName.ShouldBe("primary");
    }

    [Fact]
    public void Constructor_NullShadowRouter_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShadowShardRouterDecorator(
            Substitute.For<IShardRouter>(), null!, CreateOptions(), NullLogger<ShadowShardRouterDecorator>.Instance));
        ex.ParamName.ShouldBe("shadowRouter");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShadowShardRouterDecorator(
            Substitute.For<IShardRouter>(), Substitute.For<IShardRouter>(), null!, NullLogger<ShadowShardRouterDecorator>.Instance));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShadowShardRouterDecorator(
            Substitute.For<IShardRouter>(), Substitute.For<IShardRouter>(), CreateOptions(), null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task RouteShadowAsync_NullShardKey_ThrowsArgumentNullException()
    {
        var decorator = CreateDecorator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await decorator.RouteShadowAsync((string)null!, CancellationToken.None));
        ex.ParamName.ShouldBe("shardKey");
    }

    [Fact]
    public async Task CompareAsync_NullShardKey_ThrowsArgumentNullException()
    {
        var decorator = CreateDecorator();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await decorator.CompareAsync(null!, CancellationToken.None));
        ex.ParamName.ShouldBe("shardKey");
    }
}
