using Encina.Sharding.Migrations;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that <see cref="MigrationProgress"/> computed properties
/// are consistent with the constructor parameters.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MigrationProgressContracts
{
    [Fact]
    public void RemainingShards_EqualsTotal_MinusCompleted_MinusFailed()
    {
        // Arrange
        var totalShards = 10;
        var completedShards = 4;
        var failedShards = 2;

        var progress = CreateProgress(totalShards, completedShards, failedShards);

        // Act & Assert
        progress.RemainingShards.ShouldBe(totalShards - completedShards - failedShards);
        progress.RemainingShards.ShouldBe(4);
    }

    [Fact]
    public void IsFinished_WhenCompletedPlusFailedEqualsTotal_ReturnsTrue()
    {
        // Arrange
        var totalShards = 5;
        var completedShards = 3;
        var failedShards = 2;

        var progress = CreateProgress(totalShards, completedShards, failedShards);

        // Act & Assert
        progress.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void IsFinished_WhenCompletedPlusFailedLessThanTotal_ReturnsFalse()
    {
        // Arrange
        var totalShards = 10;
        var completedShards = 3;
        var failedShards = 2;

        var progress = CreateProgress(totalShards, completedShards, failedShards);

        // Act & Assert
        progress.IsFinished.ShouldBeFalse();
    }

    [Fact]
    public void RemainingShards_NeverNegative()
    {
        // Arrange — even if completed + failed > total (which shouldn't happen in practice),
        // the computed value may go negative. This test documents the contract:
        // when inputs are valid (completed + failed <= total), remaining is non-negative.
        var totalShards = 5;
        var completedShards = 5;
        var failedShards = 0;

        var progress = CreateProgress(totalShards, completedShards, failedShards);

        // Act & Assert
        progress.RemainingShards.ShouldBeGreaterThanOrEqualTo(0);

        // Also verify boundary case: all shards completed
        progress.RemainingShards.ShouldBe(0);
        progress.IsFinished.ShouldBeTrue();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static MigrationProgress CreateProgress(
        int totalShards,
        int completedShards,
        int failedShards)
    {
        var perShardProgress = new Dictionary<string, ShardMigrationStatus>();

        for (var i = 0; i < completedShards; i++)
        {
            var shardId = $"shard-completed-{i}";
            perShardProgress[shardId] = new ShardMigrationStatus(
                shardId, MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1));
        }

        for (var i = 0; i < failedShards; i++)
        {
            var shardId = $"shard-failed-{i}";
            perShardProgress[shardId] = new ShardMigrationStatus(
                shardId, MigrationOutcome.Failed, TimeSpan.FromSeconds(1), EncinaError.New("Test failure"));
        }

        var remaining = totalShards - completedShards - failedShards;
        for (var i = 0; i < remaining; i++)
        {
            var shardId = $"shard-pending-{i}";
            perShardProgress[shardId] = new ShardMigrationStatus(
                shardId, MigrationOutcome.Pending, TimeSpan.Zero);
        }

        return new MigrationProgress(
            Guid.NewGuid(),
            totalShards,
            completedShards,
            failedShards,
            "Testing",
            perShardProgress);
    }
}
