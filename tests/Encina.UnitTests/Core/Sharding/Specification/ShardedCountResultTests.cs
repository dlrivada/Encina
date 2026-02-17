using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Specification;

/// <summary>
/// Unit tests for <see cref="ShardedCountResult"/>.
/// </summary>
public sealed class ShardedCountResultTests
{
    // ────────────────────────────────────────────────────────────
    //  IsComplete / IsPartial
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsComplete_NoFailedShards_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedCountResult(
            TotalCount: 150,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 80, ["shard-2"] = 70 },
            FailedShards: []);

        // Assert
        result.IsComplete.ShouldBeTrue();
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_SomeFailedShards_ReturnsTrue()
    {
        // Arrange
        var failure = new ShardFailure("shard-2", default);
        var result = new ShardedCountResult(
            TotalCount: 80,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 80 },
            FailedShards: [failure]);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeTrue();
    }

    [Fact]
    public void IsPartial_AllFailed_ReturnsFalse()
    {
        // Arrange
        var failure = new ShardFailure("shard-1", default);
        var result = new ShardedCountResult(
            TotalCount: 0,
            CountPerShard: new Dictionary<string, long>(),
            FailedShards: [failure]);

        // Assert
        result.IsComplete.ShouldBeFalse();
        result.IsPartial.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  ShardsQueried
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ShardsQueried_ReturnsSumOfSuccessAndFailed()
    {
        // Arrange
        var failure = new ShardFailure("shard-3", default);
        var result = new ShardedCountResult(
            TotalCount: 150,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 80, ["shard-2"] = 70 },
            FailedShards: [failure]);

        // Assert
        result.ShardsQueried.ShouldBe(3);
    }

    // ────────────────────────────────────────────────────────────
    //  TotalCount / CountPerShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void TotalCount_SumsAllShardCounts()
    {
        // Arrange
        var result = new ShardedCountResult(
            TotalCount: 250,
            CountPerShard: new Dictionary<string, long> { ["shard-1"] = 100, ["shard-2"] = 80, ["shard-3"] = 70 },
            FailedShards: []);

        // Assert
        result.TotalCount.ShouldBe(250);
    }

    [Fact]
    public void CountPerShard_ReflectsIndividualCounts()
    {
        // Arrange
        var countPerShard = new Dictionary<string, long>
        {
            ["shard-1"] = 42,
            ["shard-2"] = 58,
            ["shard-3"] = 100
        };
        var result = new ShardedCountResult(
            TotalCount: 200,
            CountPerShard: countPerShard,
            FailedShards: []);

        // Assert
        result.CountPerShard["shard-1"].ShouldBe(42);
        result.CountPerShard["shard-2"].ShouldBe(58);
        result.CountPerShard["shard-3"].ShouldBe(100);
    }
}
