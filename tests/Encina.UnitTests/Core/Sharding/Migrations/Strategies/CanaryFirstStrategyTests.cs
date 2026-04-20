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

    // ── Mutation-killing tests ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_CanaryFails_ProgressTrackerCalledForPendingShards()
    {
        // Kills: Statement mutation removing progressTracker call (L42)
        // Kills: Block removal mutation removing pending-marking loop (L37-43)
        var shards = CreateShards("canary", "b", "c");
        var options = new MigrationOptions { PerShardTimeout = TimeSpan.FromSeconds(30) };
        var progress = new List<(string Id, MigrationOutcome Outcome)>();

        await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "canary"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("canary fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, status) => progress.Add((id, status.Outcome)),
            CancellationToken.None);

        progress.ShouldContain(p => p.Id == "canary" && p.Outcome == MigrationOutcome.Failed);
        progress.ShouldContain(p => p.Id == "b" && p.Outcome == MigrationOutcome.Pending);
        progress.ShouldContain(p => p.Id == "c" && p.Outcome == MigrationOutcome.Pending);
        progress.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_CanarySucceeds_RemainingNotMarkedPending()
    {
        // Kills: Equality mutation canaryStatus.Outcome != Succeeded → == Succeeded (L35)
        // Kills: Negate expression on the canary check (L35)
        var shards = CreateShards("canary", "b");
        var options = new MigrationOptions { MaxParallelism = 2, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var executedShards = new List<string>();

        var results = await _sut.ExecuteAsync(
            shards,
            (s, _) => { executedShards.Add(s.ShardId); return Task.FromResult<Either<EncinaError, Unit>>(unit); },
            options,
            (_, _) => { },
            CancellationToken.None);

        executedShards.ShouldContain("canary");
        executedShards.ShouldContain("b"); // b was actually executed, not pending
        results["b"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results.Values.ShouldNotContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_SingleShardFails_NoPendingShards()
    {
        // Kills: Linq Skip(1) → Take(1) mutation (L38, L51)
        // Kills: shards.Count > 1 boundary (L49)
        var shards = CreateShards("only");
        var options = new MigrationOptions { PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail")),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(1);
        results["only"].Outcome.ShouldBe(MigrationOutcome.Failed);
        // No pending shards — only the canary was there
    }

    [Fact]
    public async Task ExecuteAsync_CanarySucceeds_ExactlyTwoShards_RemainingExecuted()
    {
        // Kills: Equality mutation shards.Count >= 1 vs > 1 (L49)
        var shards = CreateShards("canary", "second");
        var options = new MigrationOptions { MaxParallelism = 2, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(2);
        results["canary"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["second"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
