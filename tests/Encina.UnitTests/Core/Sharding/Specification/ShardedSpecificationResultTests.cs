using Encina.Sharding;

namespace Encina.UnitTests.Core.Sharding.Specification;

/// <summary>
/// Unit tests for <see cref="ShardedSpecificationResult{T}"/>.
/// </summary>
public sealed class ShardedSpecificationResultTests
{
    // ────────────────────────────────────────────────────────────
    //  IsComplete / IsPartial
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IsComplete_NoFailedShards_ReturnsTrue()
    {
        // Arrange
        var result = new ShardedSpecificationResult<string>(
            Items: ["item1", "item2"],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 1, ["shard-2"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(100),
            DurationPerShard: new Dictionary<string, TimeSpan>
            {
                ["shard-1"] = TimeSpan.FromMilliseconds(40),
                ["shard-2"] = TimeSpan.FromMilliseconds(60)
            },
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
        var result = new ShardedSpecificationResult<string>(
            Items: ["item1"],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(80),
            DurationPerShard: new Dictionary<string, TimeSpan>
            {
                ["shard-1"] = TimeSpan.FromMilliseconds(50)
            },
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
        var result = new ShardedSpecificationResult<string>(
            Items: [],
            ItemsPerShard: new Dictionary<string, int>(),
            TotalDuration: TimeSpan.FromMilliseconds(50),
            DurationPerShard: new Dictionary<string, TimeSpan>(),
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
        var result = new ShardedSpecificationResult<string>(
            Items: ["item1", "item2"],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 1, ["shard-2"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(120),
            DurationPerShard: new Dictionary<string, TimeSpan>
            {
                ["shard-1"] = TimeSpan.FromMilliseconds(40),
                ["shard-2"] = TimeSpan.FromMilliseconds(60)
            },
            FailedShards: [failure]);

        // Assert
        result.ShardsQueried.ShouldBe(3);
    }

    [Fact]
    public void ShardsQueried_EmptyResult_ReturnsZero()
    {
        // Arrange
        var result = new ShardedSpecificationResult<string>(
            Items: [],
            ItemsPerShard: new Dictionary<string, int>(),
            TotalDuration: TimeSpan.Zero,
            DurationPerShard: new Dictionary<string, TimeSpan>(),
            FailedShards: []);

        // Assert
        result.ShardsQueried.ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  Items
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Items_ContainsAllItems()
    {
        // Arrange
        var result = new ShardedSpecificationResult<string>(
            Items: ["alpha", "beta", "gamma"],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 2, ["shard-2"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(100),
            DurationPerShard: new Dictionary<string, TimeSpan>
            {
                ["shard-1"] = TimeSpan.FromMilliseconds(50),
                ["shard-2"] = TimeSpan.FromMilliseconds(30)
            },
            FailedShards: []);

        // Assert
        result.Items.Count.ShouldBe(3);
        result.Items[0].ShouldBe("alpha");
        result.Items[1].ShouldBe("beta");
        result.Items[2].ShouldBe("gamma");
    }

    // ────────────────────────────────────────────────────────────
    //  ItemsPerShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ItemsPerShard_ReflectsPerShardCounts()
    {
        // Arrange
        var itemsPerShard = new Dictionary<string, int>
        {
            ["shard-1"] = 5,
            ["shard-2"] = 3,
            ["shard-3"] = 7
        };
        var result = new ShardedSpecificationResult<int>(
            Items: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
            ItemsPerShard: itemsPerShard,
            TotalDuration: TimeSpan.FromMilliseconds(200),
            DurationPerShard: new Dictionary<string, TimeSpan>
            {
                ["shard-1"] = TimeSpan.FromMilliseconds(60),
                ["shard-2"] = TimeSpan.FromMilliseconds(40),
                ["shard-3"] = TimeSpan.FromMilliseconds(80)
            },
            FailedShards: []);

        // Assert
        result.ItemsPerShard["shard-1"].ShouldBe(5);
        result.ItemsPerShard["shard-2"].ShouldBe(3);
        result.ItemsPerShard["shard-3"].ShouldBe(7);
    }

    // ────────────────────────────────────────────────────────────
    //  DurationPerShard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void DurationPerShard_ReflectsPerShardDurations()
    {
        // Arrange
        var durationPerShard = new Dictionary<string, TimeSpan>
        {
            ["shard-1"] = TimeSpan.FromMilliseconds(50),
            ["shard-2"] = TimeSpan.FromMilliseconds(120)
        };
        var result = new ShardedSpecificationResult<string>(
            Items: ["a", "b"],
            ItemsPerShard: new Dictionary<string, int> { ["shard-1"] = 1, ["shard-2"] = 1 },
            TotalDuration: TimeSpan.FromMilliseconds(150),
            DurationPerShard: durationPerShard,
            FailedShards: []);

        // Assert
        result.DurationPerShard["shard-1"].ShouldBe(TimeSpan.FromMilliseconds(50));
        result.DurationPerShard["shard-2"].ShouldBe(TimeSpan.FromMilliseconds(120));
    }

    // ────────────────────────────────────────────────────────────
    //  Record equality
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        IReadOnlyList<string> items = ["item1"];
        IReadOnlyDictionary<string, int> itemsPerShard = new Dictionary<string, int> { ["shard-1"] = 1 };
        var totalDuration = TimeSpan.FromMilliseconds(100);
        IReadOnlyDictionary<string, TimeSpan> durationPerShard = new Dictionary<string, TimeSpan>
        {
            ["shard-1"] = TimeSpan.FromMilliseconds(100)
        };
        IReadOnlyList<ShardFailure> failures = [];

        var r1 = new ShardedSpecificationResult<string>(items, itemsPerShard, totalDuration, durationPerShard, failures);
        var r2 = new ShardedSpecificationResult<string>(items, itemsPerShard, totalDuration, durationPerShard, failures);

        // Assert
        r1.ShouldBe(r2);
    }
}
