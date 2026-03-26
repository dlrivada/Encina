using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.Migrations.Strategies;

/// <summary>
/// Unit tests for <see cref="CanaryFirstStrategy"/>.
/// </summary>
public sealed class CanaryFirstStrategyTests
{
    private readonly CanaryFirstStrategy _sut = new();

    [Fact]
    public async Task ExecuteAsync_CanarySucceeds_MigratesAllShards()
    {
        // Arrange
        var shards = CreateShards("canary", "shard-2", "shard-3");
        var options = new MigrationOptions
        {
            MaxParallelism = 2,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };
        var progress = new List<string>();

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, _) => progress.Add(id),
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
        // Canary should be first
        progress[0].ShouldBe("canary");
    }

    [Fact]
    public async Task ExecuteAsync_CanaryFails_MarksRemainingAsPending()
    {
        // Arrange
        var shards = CreateShards("canary", "shard-2", "shard-3");
        var options = new MigrationOptions
        {
            MaxParallelism = 2,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                if (shard.ShardId == "canary")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Canary failed"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results["canary"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["shard-2"].Outcome.ShouldBe(MigrationOutcome.Pending);
        results["shard-3"].Outcome.ShouldBe(MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyShards_ReturnsEmpty()
    {
        // Arrange
        var options = new MigrationOptions { PerShardTimeout = TimeSpan.FromSeconds(30) };

        // Act
        var results = await _sut.ExecuteAsync(
            System.Array.Empty<ShardInfo>(),
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_SingleShard_OnlyRunsCanary()
    {
        // Arrange
        var shards = CreateShards("only-shard");
        var options = new MigrationOptions { PerShardTimeout = TimeSpan.FromSeconds(30) };
        var callCount = 0;

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) =>
            {
                callCount++;
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(1);
        results["only-shard"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        callCount.ShouldBe(1);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
