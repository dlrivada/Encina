using Encina.Sharding;
using Encina.Sharding.Shadow;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShadowShardingOptions"/>.
/// Validates parameter boundaries and null checks on all public setters.
/// </summary>
public sealed class ShadowShardingOptionsGuardTests
{
    // ── ShadowTopology ─────────────────────────────────────────────

    [Fact]
    public void ShadowTopology_NullValue_ThrowsArgumentNullException()
    {
        var options = new ShadowShardingOptions();
        var ex = Should.Throw<ArgumentNullException>(() => options.ShadowTopology = null!);
        ex.ParamName.ShouldBe("value");
    }

    [Fact]
    public void ShadowTopology_ValidValue_SetsProperty()
    {
        var options = new ShadowShardingOptions();
        var topology = CreateTestTopology();

        options.ShadowTopology = topology;

        options.ShadowTopology.ShouldBe(topology);
    }

    // ── ShadowReadPercentage ───────────────────────────────────────

    [Fact]
    public void ShadowReadPercentage_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShadowShardingOptions();
        Should.Throw<ArgumentOutOfRangeException>(() => options.ShadowReadPercentage = -1);
    }

    [Fact]
    public void ShadowReadPercentage_GreaterThan100_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShadowShardingOptions();
        Should.Throw<ArgumentOutOfRangeException>(() => options.ShadowReadPercentage = 101);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void ShadowReadPercentage_ValidRange_SetsProperty(int percentage)
    {
        var options = new ShadowShardingOptions();

        options.ShadowReadPercentage = percentage;

        options.ShadowReadPercentage.ShouldBe(percentage);
    }

    // ── ShadowWriteTimeout ─────────────────────────────────────────

    [Fact]
    public void ShadowWriteTimeout_Zero_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShadowShardingOptions();
        Should.Throw<ArgumentOutOfRangeException>(() => options.ShadowWriteTimeout = TimeSpan.Zero);
    }

    [Fact]
    public void ShadowWriteTimeout_Negative_ThrowsArgumentOutOfRangeException()
    {
        var options = new ShadowShardingOptions();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            options.ShadowWriteTimeout = TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void ShadowWriteTimeout_ValidValue_SetsProperty()
    {
        var options = new ShadowShardingOptions();
        var timeout = TimeSpan.FromSeconds(10);

        options.ShadowWriteTimeout = timeout;

        options.ShadowWriteTimeout.ShouldBe(timeout);
    }

    // ── Default values ─────────────────────────────────────────────

    [Fact]
    public void Defaults_DualWriteEnabled_IsTrue()
    {
        var options = new ShadowShardingOptions();
        options.DualWriteEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_ShadowReadPercentage_IsZero()
    {
        var options = new ShadowShardingOptions();
        options.ShadowReadPercentage.ShouldBe(0);
    }

    [Fact]
    public void Defaults_CompareResults_IsTrue()
    {
        var options = new ShadowShardingOptions();
        options.CompareResults.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_ShadowWriteTimeout_IsFiveSeconds()
    {
        var options = new ShadowShardingOptions();
        options.ShadowWriteTimeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Defaults_DiscrepancyHandler_IsNull()
    {
        var options = new ShadowShardingOptions();
        options.DiscrepancyHandler.ShouldBeNull();
    }

    [Fact]
    public void Defaults_ShadowTopology_IsNull()
    {
        var options = new ShadowShardingOptions();
        options.ShadowTopology.ShouldBeNull();
    }

    [Fact]
    public void Defaults_ShadowRouterFactory_IsNull()
    {
        var options = new ShadowShardingOptions();
        options.ShadowRouterFactory.ShouldBeNull();
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static ShardTopology CreateTestTopology()
    {
        var shards = new[]
        {
            new ShardInfo("shard-1", "Server=test;Database=Shard1"),
            new ShardInfo("shard-2", "Server=test;Database=Shard2")
        };
        return new ShardTopology(shards);
    }
}
