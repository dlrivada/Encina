using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.Migrations.Strategies;

/// <summary>
/// Unit tests for <see cref="ParallelMigrationStrategy"/>.
/// </summary>
public sealed class ParallelMigrationStrategyTests
{
    private readonly ParallelMigrationStrategy _sut = new();

    [Fact]
    public async Task ExecuteAsync_AllShardsSucceed_ReturnsAllSucceeded()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2", "shard-3");
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
        results.Count.ShouldBe(3);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_WithStopOnFirstFailure_CancelsRemaining()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2", "shard-3", "shard-4");
        var options = new MigrationOptions
        {
            MaxParallelism = 1,
            StopOnFirstFailure = true,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                if (shard.ShardId == "shard-2")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Failed"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(4);
        results["shard-2"].Outcome.ShouldBe(MigrationOutcome.Failed);
        // At least some remaining should be pending
        results.Values.Count(s => s.Outcome == MigrationOutcome.Pending
                                 || s.Outcome == MigrationOutcome.Succeeded
                                 || s.Outcome == MigrationOutcome.Failed)
            .ShouldBe(4);
    }

    [Fact]
    public async Task ExecuteAsync_WithZeroParallelism_UsesShardCount()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2");
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
        results.Count.ShouldBe(2);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
