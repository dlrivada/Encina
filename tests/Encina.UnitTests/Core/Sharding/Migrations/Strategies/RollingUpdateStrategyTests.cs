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

    // ── Mutation-killing tests ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_FailureWithStop_ProgressTrackerCalledForPendingShards()
    {
        // Kills: Statement mutation removing progressTracker call for pending (L63)
        var shards = CreateShards("s1", "s2", "s3", "s4");
        var options = new MigrationOptions { MaxParallelism = 2, StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var progress = new List<(string Id, MigrationOutcome Outcome)>();

        await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "s2"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, status) => progress.Add((id, status.Outcome)),
            CancellationToken.None);

        progress.ShouldContain(p => p.Id == "s3" && p.Outcome == MigrationOutcome.Pending);
        progress.ShouldContain(p => p.Id == "s4" && p.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_FailureWithoutStop_NoPendingShards()
    {
        // Kills: Logical mutation batchFailed || StopOnFirstFailure (L54)
        // Kills: Negate expression on the failure+stop check (L54)
        var shards = CreateShards("s1", "s2", "s3");
        var options = new MigrationOptions { MaxParallelism = 1, StopOnFirstFailure = false, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "s1"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results["s1"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["s2"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["s3"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results.Values.ShouldNotContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_AllSucceedWithStop_NoPending()
    {
        // Kills: Equality mutation on Outcome != Failed → == Failed (L52)
        var shards = CreateShards("s1", "s2", "s3");
        var options = new MigrationOptions { MaxParallelism = 1, StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var executedCount = 0;

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => { executedCount++; return Task.FromResult<Either<EncinaError, Unit>>(unit); },
            options,
            (_, _) => { },
            CancellationToken.None);

        executedCount.ShouldBe(3); // All 3 executed despite StopOnFirstFailure
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_ExactBatchBoundary_AllShardsProcessed()
    {
        // Kills: Equality mutation batchIndex <= batches.Count → < batches.Count (L28)
        // Kills: Arithmetic mutation on batchIndex++ (L28)
        var shards = CreateShards("s1", "s2", "s3", "s4");
        var options = new MigrationOptions { MaxParallelism = 2, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(4); // All 4 processed in exactly 2 batches
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_UnevenBatches_LastBatchSmallerButProcessed()
    {
        // Kills: Linq Skip mutation (L57), GroupBy/Select chain mutations
        var shards = CreateShards("s1", "s2", "s3");
        var options = new MigrationOptions { MaxParallelism = 2, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(3); // batch1=[s1,s2], batch2=[s3]
        results["s3"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteAsync_ProgressTrackerCalledPerShard()
    {
        // Kills: Statement mutation removing progressTracker call (L40)
        var shards = CreateShards("s1", "s2");
        var options = new MigrationOptions { MaxParallelism = 1, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var trackedIds = new List<string>();

        await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, _) => trackedIds.Add(id),
            CancellationToken.None);

        trackedIds.ShouldContain("s1");
        trackedIds.ShouldContain("s2");
        trackedIds.Count.ShouldBe(2);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
