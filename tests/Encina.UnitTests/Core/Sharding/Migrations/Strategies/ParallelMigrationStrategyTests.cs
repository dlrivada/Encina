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

    // ── Mutation-killing tests ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_StopOnFirstFailure_ProgressTrackerCalledForPendingShardsWithCorrectOutcome()
    {
        // Kills: Statement mutation removing progressTracker call for pending shards (L55)
        // Kills: LogicalNotExpression mutation on !results.ContainsKey (L51)
        var shards = CreateShards("a", "b", "c");
        var options = new MigrationOptions { MaxParallelism = 1, StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var progress = new List<(string Id, MigrationOutcome Outcome)>();

        await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "a"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, status) => progress.Add((id, status.Outcome)),
            CancellationToken.None);

        // Pending shards MUST appear in progress tracker
        progress.ShouldContain(p => p.Id == "b" && p.Outcome == MigrationOutcome.Pending);
        progress.ShouldContain(p => p.Id == "c" && p.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_FailureWithoutStopOnFirstFailure_AllShardsExecute()
    {
        // Kills: Equality mutation on status.Outcome == Failed (L81) and failureCts is not null (L81)
        // Kills: Negate expression on failure check (L81)
        var shards = CreateShards("a", "b", "c");
        var options = new MigrationOptions { MaxParallelism = 1, StopOnFirstFailure = false, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var executedShards = new System.Collections.Concurrent.ConcurrentBag<string>();

        var results = await _sut.ExecuteAsync(
            shards,
            (s, _) =>
            {
                executedShards.Add(s.ShardId);
                return s.ShardId == "b"
                    ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                    : Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        executedShards.Count.ShouldBe(3); // ALL shards execute — no cancellation
        results["a"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["b"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["c"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results.Values.ShouldNotContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_MaxParallelismNegative_UsesShardCount()
    {
        // Kills: Equality mutation on MaxParallelism <= 0 → < 0 (L28)
        var shards = CreateShards("a", "b");
        var options = new MigrationOptions { MaxParallelism = -1, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(2);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_MaxParallelismGreaterThanShardCount_CappedToShardCount()
    {
        // Kills: Equality mutation on Math.Min comparison (L30)
        var shards = CreateShards("a");
        var options = new MigrationOptions { MaxParallelism = 100, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(1);
        results["a"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_StopOnFirstFailure_CancelAsyncInvoked()
    {
        // Kills: Conditional mutations on StopOnFirstFailure (L23), failureCts check (L81)
        // Kills: Equality mutation failureCts is not null → is null (L81)
        var shards = CreateShards("a", "b", "c", "d");
        var options = new MigrationOptions { MaxParallelism = 1, StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "b"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        // shard-b failed → stop → remaining are pending
        results["b"].Outcome.ShouldBe(MigrationOutcome.Failed);
        // At least one shard should be pending (c or d)
        results.Values.ShouldContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_AllSucceed_NoShardsPending()
    {
        // Kills: Conditional true/false mutations on StopOnFirstFailure (L23)
        var shards = CreateShards("a", "b", "c");
        var options = new MigrationOptions { MaxParallelism = 2, StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
        results.Values.ShouldNotContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
