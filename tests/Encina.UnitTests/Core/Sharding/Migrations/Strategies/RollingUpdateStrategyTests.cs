using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.Migrations.Strategies;

/// <summary>
/// Unit tests for <see cref="RollingUpdateStrategy"/>.
/// </summary>
public sealed class RollingUpdateStrategyTests
{
    private readonly RollingUpdateStrategy _sut = new();

    [Fact]
    public async Task ExecuteAsync_AllBatchesSucceed_ReturnsAllSucceeded()
    {
        // Arrange
        var shards = CreateShards("s1", "s2", "s3", "s4");
        var options = new MigrationOptions
        {
            MaxParallelism = 2,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(4);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_BatchFailsWithStopOnFirst_MarksRemainingPending()
    {
        // Arrange: 4 shards, batch size 2. Shard s2 in batch 1 fails.
        var shards = CreateShards("s1", "s2", "s3", "s4");
        var options = new MigrationOptions
        {
            MaxParallelism = 2,
            StopOnFirstFailure = true,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                if (shard.ShardId == "s2")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Batch 1 failure"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(4);
        results["s1"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["s2"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["s3"].Outcome.ShouldBe(MigrationOutcome.Pending);
        results["s4"].Outcome.ShouldBe(MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_BatchFailsWithoutStopOnFirst_ContinuesNextBatch()
    {
        // Arrange
        var shards = CreateShards("s1", "s2", "s3", "s4");
        var options = new MigrationOptions
        {
            MaxParallelism = 2,
            StopOnFirstFailure = false,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                if (shard.ShardId == "s2")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Failure"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(4);
        results["s1"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["s2"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["s3"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["s4"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroParallelism_RunsAllAtOnce()
    {
        // Arrange
        var shards = CreateShards("s1", "s2", "s3");
        var options = new MigrationOptions
        {
            MaxParallelism = 0,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
