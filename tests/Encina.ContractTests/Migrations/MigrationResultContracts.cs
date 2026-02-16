using Encina.Sharding.Migrations;

namespace Encina.ContractTests.Migrations;

/// <summary>
/// Contract tests verifying that <see cref="MigrationResult"/> computed properties
/// are always consistent with the underlying <see cref="MigrationResult.PerShardStatus"/> data.
/// </summary>
[Trait("Category", "Contract")]
public sealed class MigrationResultContracts
{
    [Fact]
    public void AllSucceeded_WhenAllShardsSucceeded_ReturnsTrue()
    {
        // Arrange
        var perShardStatus = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(3)),
            ["shard-3"] = new("shard-3", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1))
        };

        var result = new MigrationResult(
            Guid.NewGuid(),
            perShardStatus,
            TimeSpan.FromSeconds(6),
            DateTimeOffset.UtcNow);

        // Act & Assert
        result.AllSucceeded.ShouldBeTrue();
    }

    [Fact]
    public void AllSucceeded_WhenAnyShardsNotSucceeded_ReturnsFalse()
    {
        // Arrange
        var perShardStatus = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Failed, TimeSpan.FromSeconds(3), EncinaError.New("Test failure")),
            ["shard-3"] = new("shard-3", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1))
        };

        var result = new MigrationResult(
            Guid.NewGuid(),
            perShardStatus,
            TimeSpan.FromSeconds(6),
            DateTimeOffset.UtcNow);

        // Act & Assert
        result.AllSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void SucceededCount_MatchesActualSucceededShards()
    {
        // Arrange
        var perShardStatus = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Failed, TimeSpan.FromSeconds(3), EncinaError.New("Failure")),
            ["shard-3"] = new("shard-3", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1)),
            ["shard-4"] = new("shard-4", MigrationOutcome.Pending, TimeSpan.Zero),
            ["shard-5"] = new("shard-5", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(4))
        };

        var result = new MigrationResult(
            Guid.NewGuid(),
            perShardStatus,
            TimeSpan.FromSeconds(10),
            DateTimeOffset.UtcNow);

        // Act & Assert
        result.SucceededCount.ShouldBe(3);
    }

    [Fact]
    public void FailedCount_MatchesActualFailedShards()
    {
        // Arrange
        var perShardStatus = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Failed, TimeSpan.FromSeconds(3), EncinaError.New("Failure 1")),
            ["shard-3"] = new("shard-3", MigrationOutcome.Failed, TimeSpan.FromSeconds(1), EncinaError.New("Failure 2")),
            ["shard-4"] = new("shard-4", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(4))
        };

        var result = new MigrationResult(
            Guid.NewGuid(),
            perShardStatus,
            TimeSpan.FromSeconds(10),
            DateTimeOffset.UtcNow);

        // Act & Assert
        result.FailedCount.ShouldBe(2);
    }

    [Fact]
    public void SucceededCount_PlusFailedCount_PlusPending_EqualsTotalShards()
    {
        // Arrange
        var perShardStatus = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Failed, TimeSpan.FromSeconds(3), EncinaError.New("Failure")),
            ["shard-3"] = new("shard-3", MigrationOutcome.Pending, TimeSpan.Zero),
            ["shard-4"] = new("shard-4", MigrationOutcome.InProgress, TimeSpan.FromSeconds(1)),
            ["shard-5"] = new("shard-5", MigrationOutcome.RolledBack, TimeSpan.FromSeconds(5)),
            ["shard-6"] = new("shard-6", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2))
        };

        var result = new MigrationResult(
            Guid.NewGuid(),
            perShardStatus,
            TimeSpan.FromSeconds(13),
            DateTimeOffset.UtcNow);

        // Act
        var totalShards = result.PerShardStatus.Count;
        var succeededCount = result.SucceededCount;
        var failedCount = result.FailedCount;
        var otherCount = totalShards - succeededCount - failedCount;

        // Assert — SucceededCount + FailedCount + others must equal total
        (succeededCount + failedCount + otherCount).ShouldBe(totalShards);

        // Also verify the individual counts match what we set up
        succeededCount.ShouldBe(2);
        failedCount.ShouldBe(1);
        otherCount.ShouldBe(3); // Pending + InProgress + RolledBack
    }
}
