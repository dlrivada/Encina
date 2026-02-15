using Encina.Sharding;
using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReplicationResult"/> and <see cref="ShardReplicationResult"/>.
/// </summary>
public sealed class ReplicationResultTests
{
    // ────────────────────────────────────────────────────────────
    //  IsComplete / IsPartial
    // ────────────────────────────────────────────────────────────

    #region IsComplete and IsPartial

    [Fact]
    public void IsComplete_NoFailures_ReturnsTrue()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 100,
            Duration: TimeSpan.FromMilliseconds(250),
            ShardResults:
            [
                new ShardReplicationResult("shard-1", 50, TimeSpan.FromMilliseconds(100)),
                new ShardReplicationResult("shard-2", 50, TimeSpan.FromMilliseconds(150))
            ],
            FailedShards: []);

        // Act & Assert
        result.IsComplete.ShouldBeTrue();
        result.IsPartial.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_SomeFailures_ReturnsTrue()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 50,
            Duration: TimeSpan.FromMilliseconds(250),
            ShardResults: [new ShardReplicationResult("shard-1", 50, TimeSpan.FromMilliseconds(100))],
            FailedShards: [new ShardFailure("shard-2", EncinaErrors.Create("test", "replication failed"))]);

        // Act & Assert
        result.IsPartial.ShouldBeTrue();
        result.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void IsPartial_AllFailed_ReturnsFalse()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 0,
            Duration: TimeSpan.FromMilliseconds(100),
            ShardResults: [],
            FailedShards: [new ShardFailure("shard-1", EncinaErrors.Create("test", "failed"))]);

        // Act & Assert
        result.IsPartial.ShouldBeFalse();
        result.IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void EmptyResult_NoShardsTargeted_IsComplete()
    {
        // Arrange
        var result = new ReplicationResult(0, TimeSpan.Zero, [], []);

        // Act & Assert
        result.IsComplete.ShouldBeTrue();
        result.IsPartial.ShouldBeFalse();
        result.TotalShardsTargeted.ShouldBe(0);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  TotalShardsTargeted
    // ────────────────────────────────────────────────────────────

    #region TotalShardsTargeted

    [Fact]
    public void TotalShardsTargeted_IncludesSuccessAndFailure()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 50,
            Duration: TimeSpan.FromMilliseconds(250),
            ShardResults: [new ShardReplicationResult("shard-1", 50, TimeSpan.FromMilliseconds(100))],
            FailedShards: [new ShardFailure("shard-2", EncinaErrors.Create("test", "failed"))]);

        // Act & Assert
        result.TotalShardsTargeted.ShouldBe(2);
    }

    [Fact]
    public void TotalShardsTargeted_OnlySuccessful_MatchesShardResults()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 100,
            Duration: TimeSpan.FromMilliseconds(200),
            ShardResults:
            [
                new ShardReplicationResult("shard-1", 50, TimeSpan.FromMilliseconds(100)),
                new ShardReplicationResult("shard-2", 50, TimeSpan.FromMilliseconds(100))
            ],
            FailedShards: []);

        // Act & Assert
        result.TotalShardsTargeted.ShouldBe(2);
    }

    [Fact]
    public void TotalShardsTargeted_OnlyFailed_MatchesFailedShards()
    {
        // Arrange
        var result = new ReplicationResult(
            RowsSynced: 0,
            Duration: TimeSpan.FromMilliseconds(100),
            ShardResults: [],
            FailedShards:
            [
                new ShardFailure("shard-1", EncinaErrors.Create("test", "failed")),
                new ShardFailure("shard-2", EncinaErrors.Create("test", "failed"))
            ]);

        // Act & Assert
        result.TotalShardsTargeted.ShouldBe(2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  RowsSynced and Duration
    // ────────────────────────────────────────────────────────────

    #region RowsSynced and Duration

    [Fact]
    public void RowsSynced_ReflectsPassedValue()
    {
        // Act
        var result = new ReplicationResult(999, TimeSpan.FromSeconds(3), [], []);

        // Assert
        result.RowsSynced.ShouldBe(999);
    }

    [Fact]
    public void Duration_ReflectsPassedValue()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(5.5);

        // Act
        var result = new ReplicationResult(0, duration, [], []);

        // Assert
        result.Duration.ShouldBe(duration);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  ShardReplicationResult
    // ────────────────────────────────────────────────────────────

    #region ShardReplicationResult

    [Fact]
    public void ShardReplicationResult_StoresAllProperties()
    {
        // Act
        var shardResult = new ShardReplicationResult("shard-0", 42, TimeSpan.FromMilliseconds(150));

        // Assert
        shardResult.ShardId.ShouldBe("shard-0");
        shardResult.RowsUpserted.ShouldBe(42);
        shardResult.Duration.ShouldBe(TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public void ShardReplicationResult_Equality_SameValues_AreEqual()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(100);
        var result1 = new ShardReplicationResult("shard-0", 10, duration);
        var result2 = new ShardReplicationResult("shard-0", 10, duration);

        // Act & Assert
        result1.ShouldBe(result2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  FailedShards
    // ────────────────────────────────────────────────────────────

    #region FailedShards

    [Fact]
    public void FailedShards_AccessibleFromResult()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "something went wrong");
        var result = new ReplicationResult(
            RowsSynced: 0,
            Duration: TimeSpan.FromMilliseconds(50),
            ShardResults: [],
            FailedShards: [new ShardFailure("shard-3", error)]);

        // Act & Assert
        result.FailedShards.Count.ShouldBe(1);
        result.FailedShards[0].ShardId.ShouldBe("shard-3");
    }

    #endregion
}
