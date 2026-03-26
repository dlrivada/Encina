using Encina.Sharding.Migrations;

namespace Encina.UnitTests.Core.Sharding.Migrations;

/// <summary>
/// Unit tests for <see cref="MigrationResult"/> and related records.
/// </summary>
public sealed class MigrationResultTests
{
    [Fact]
    public void AllSucceeded_WhenAllShardsSucceeded_ReturnsTrue()
    {
        // Arrange
        var statuses = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(2))
        };

        var result = new MigrationResult(Guid.NewGuid(), statuses, TimeSpan.FromSeconds(3), DateTimeOffset.UtcNow);

        // Assert
        result.AllSucceeded.ShouldBeTrue();
        result.SucceededCount.ShouldBe(2);
        result.FailedCount.ShouldBe(0);
    }

    [Fact]
    public void AllSucceeded_WhenOneFailed_ReturnsFalse()
    {
        // Arrange
        var statuses = new Dictionary<string, ShardMigrationStatus>
        {
            ["shard-1"] = new("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1)),
            ["shard-2"] = new("shard-2", MigrationOutcome.Failed, TimeSpan.FromSeconds(2),
                EncinaError.New("Failed"))
        };

        var result = new MigrationResult(Guid.NewGuid(), statuses, TimeSpan.FromSeconds(3), DateTimeOffset.UtcNow);

        // Assert
        result.AllSucceeded.ShouldBeFalse();
        result.SucceededCount.ShouldBe(1);
        result.FailedCount.ShouldBe(1);
    }

    [Fact]
    public void ShardMigrationStatus_WithValidShardId_CreatesStatus()
    {
        // Act
        var status = new ShardMigrationStatus("shard-1", MigrationOutcome.Succeeded, TimeSpan.FromSeconds(1));

        // Assert
        status.ShardId.ShouldBe("shard-1");
        status.Outcome.ShouldBe(MigrationOutcome.Succeeded);
        status.Error.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardMigrationStatus_WithInvalidShardId_ThrowsArgumentException(string? shardId)
    {
        Assert.Throws<ArgumentException>(() =>
            new ShardMigrationStatus(shardId!, MigrationOutcome.Succeeded, TimeSpan.Zero));
    }

    [Fact]
    public void ShardMigrationStatus_WithError_StoresError()
    {
        // Arrange
        var error = EncinaError.New("Migration failed");

        // Act
        var status = new ShardMigrationStatus("shard-1", MigrationOutcome.Failed, TimeSpan.FromSeconds(5), error);

        // Assert
        status.Error.ShouldNotBeNull();
        status.Error!.Value.Message.ShouldBe("Migration failed");
    }
}

/// <summary>
/// Unit tests for <see cref="MigrationProgress"/> record.
/// </summary>
public sealed class MigrationProgressTests
{
    [Fact]
    public void RemainingShards_CalculatesCorrectly()
    {
        // Arrange
        var progress = new MigrationProgress(
            Guid.NewGuid(), TotalShards: 5, CompletedShards: 2, FailedShards: 1,
            "Rolling", new Dictionary<string, ShardMigrationStatus>());

        // Assert
        progress.RemainingShards.ShouldBe(2);
    }

    [Fact]
    public void IsFinished_WhenAllCompleted_ReturnsTrue()
    {
        // Arrange
        var progress = new MigrationProgress(
            Guid.NewGuid(), TotalShards: 3, CompletedShards: 3, FailedShards: 0,
            "Completed", new Dictionary<string, ShardMigrationStatus>());

        // Assert
        progress.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void IsFinished_WhenMixed_ReturnsTrue()
    {
        // Arrange
        var progress = new MigrationProgress(
            Guid.NewGuid(), TotalShards: 3, CompletedShards: 2, FailedShards: 1,
            "Completed", new Dictionary<string, ShardMigrationStatus>());

        // Assert
        progress.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void IsFinished_WhenStillInProgress_ReturnsFalse()
    {
        // Arrange
        var progress = new MigrationProgress(
            Guid.NewGuid(), TotalShards: 5, CompletedShards: 2, FailedShards: 0,
            "Running", new Dictionary<string, ShardMigrationStatus>());

        // Assert
        progress.IsFinished.ShouldBeFalse();
        progress.RemainingShards.ShouldBe(3);
    }
}
