using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Sharding.Migrations.Strategies;

/// <summary>
/// Unit tests for <see cref="SequentialMigrationStrategy"/>.
/// </summary>
public sealed class SequentialMigrationStrategyTests
{
    private readonly SequentialMigrationStrategy _sut = new();

    [Fact]
    public async Task ExecuteAsync_AllShardsSucceed_ReturnsAllSucceeded()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2", "shard-3");
        var options = new MigrationOptions
        {
            Strategy = MigrationStrategy.Sequential,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };
        var progress = new List<(string ShardId, ShardMigrationStatus Status)>();

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, status) => progress.Add((id, status)),
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
        progress.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAsync_StopOnFirstFailure_StopsAndMarksPending()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2", "shard-3");
        var options = new MigrationOptions
        {
            StopOnFirstFailure = true,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };
        var callCount = 0;

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                callCount++;
                if (shard.ShardId == "shard-2")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Migration failed on shard-2"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results["shard-1"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["shard-2"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["shard-3"].Outcome.ShouldBe(MigrationOutcome.Pending);
        callCount.ShouldBe(2); // shard-3 was never called
    }

    [Fact]
    public async Task ExecuteAsync_FailureWithoutStopOnFirst_ContinuesAll()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2", "shard-3");
        var options = new MigrationOptions
        {
            StopOnFirstFailure = false,
            PerShardTimeout = TimeSpan.FromSeconds(30)
        };
        var callCount = 0;

        // Act
        var results = await _sut.ExecuteAsync(
            shards,
            (shard, _) =>
            {
                callCount++;
                if (shard.ShardId == "shard-2")
                    return Task.FromResult<Either<EncinaError, Unit>>(
                        EncinaError.New("Failed"));
                return Task.FromResult<Either<EncinaError, Unit>>(unit);
            },
            options,
            (_, _) => { },
            CancellationToken.None);

        // Assert
        results.Count.ShouldBe(3);
        results["shard-1"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results["shard-2"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["shard-3"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        callCount.ShouldBe(3);
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
    public async Task ExecuteOnShardAsync_WhenActionThrows_ReturnsFailedStatus()
    {
        // Arrange
        var shard = new ShardInfo("shard-1", "conn-string-1");

        // Act
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            shard,
            (_, _) => throw new InvalidOperationException("Boom"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        // Assert
        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.ShardId.ShouldBe("shard-1");
        status.Error.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteOnShardAsync_WhenTimeout_ReturnsTimedOutStatus()
    {
        // Arrange
        var shard = new ShardInfo("shard-timeout", "conn-string");

        // Act
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            shard,
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return (Either<EncinaError, Unit>)unit;
            },
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert
        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.Error.ShouldNotBeNull();
        status.Error!.Value.Message.ShouldContain("timed out");
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_Throws()
    {
        // Arrange
        var shards = CreateShards("shard-1", "shard-2");
        var options = new MigrationOptions { PerShardTimeout = TimeSpan.FromSeconds(30) };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.ExecuteAsync(
                shards,
                (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
                options,
                (_, _) => { },
                cts.Token));
    }

    // ── Mutation-killing tests ──────────────────────────────────────────
    // Each test below targets a specific surviving mutant family.

    [Fact]
    public async Task ExecuteAsync_StopOnFirstFailure_ProgressTrackerCalledForPendingShards()
    {
        // Kills: Statement mutation removing progressTracker() call for pending shards (L36)
        // Kills: LogicalNotExpression mutation on !results.ContainsKey (L32)
        var shards = CreateShards("a", "b", "c");
        var options = new MigrationOptions { StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var progress = new List<(string Id, MigrationOutcome Outcome)>();

        await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "a"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (id, status) => progress.Add((id, status.Outcome)),
            CancellationToken.None);

        // Both b and c should be reported as Pending via progressTracker
        progress.ShouldContain(p => p.Id == "b" && p.Outcome == MigrationOutcome.Pending);
        progress.ShouldContain(p => p.Id == "c" && p.Outcome == MigrationOutcome.Pending);
        progress.Count.ShouldBe(3); // a=Failed, b=Pending, c=Pending
    }

    [Fact]
    public async Task ExecuteAsync_FailureWithoutStop_DoesNotMarkAnyPending()
    {
        // Kills: Equality mutation status.Outcome != Failed (L29)
        // Kills: Negate expression on the failure check (L29)
        var shards = CreateShards("a", "b");
        var options = new MigrationOptions { StopOnFirstFailure = false, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (s, _) => s.ShardId == "a"
                ? Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail"))
                : Task.FromResult<Either<EncinaError, Unit>>(unit),
            options,
            (_, _) => { },
            CancellationToken.None);

        results["a"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["b"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        results.Values.ShouldNotContain(s => s.Outcome == MigrationOutcome.Pending);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessWithStopOnFirstFailure_DoesNotStop()
    {
        // Kills: Equality mutation == -> != on Failed check (L29)
        var shards = CreateShards("a", "b", "c");
        var options = new MigrationOptions { StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var callCount = 0;

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => { callCount++; return Task.FromResult<Either<EncinaError, Unit>>(unit); },
            options,
            (_, _) => { },
            CancellationToken.None);

        callCount.ShouldBe(3); // All shards executed despite StopOnFirstFailure being true
        results.Values.ShouldAllBe(s => s.Outcome == MigrationOutcome.Succeeded);
    }

    [Fact]
    public async Task ExecuteOnShardAsync_Timeout_ErrorMessageContainsShardId()
    {
        // Kills: String mutation on shard ID in error message (L71)
        var shard = new ShardInfo("my-special-shard", "conn");

        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            shard,
            async (_, ct) => { await Task.Delay(TimeSpan.FromSeconds(10), ct); return (Either<EncinaError, Unit>)unit; },
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.Error!.Value.Message.ShouldContain("my-special-shard");
    }

    [Fact]
    public async Task ExecuteOnShardAsync_Exception_ErrorMessageContainsShardId()
    {
        // Kills: String mutation on shard ID in error message (L83)
        var shard = new ShardInfo("err-shard", "conn");

        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            shard,
            (_, _) => throw new InvalidOperationException("kaboom"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        status.Error!.Value.Message.ShouldContain("err-shard");
        status.Error!.Value.Message.ShouldContain("kaboom");
    }

    [Fact]
    public async Task ExecuteOnShardAsync_Success_ReturnsElapsedGreaterThanZero()
    {
        // Kills: Statement mutation removing elapsed measurement
        var shard = new ShardInfo("shard-1", "conn");

        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            shard,
            async (_, _) => { await Task.Delay(5, CancellationToken.None); return (Either<EncinaError, Unit>)unit; },
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Succeeded);
        status.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteOnShardAsync_CallerCancellation_PropagatesNotTimeout()
    {
        // Kills: Logical mutation on timeoutCts.IsCancellationRequested || !cancellationToken... (L66)
        var shard = new ShardInfo("shard-1", "conn");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            SequentialMigrationStrategy.ExecuteOnShardAsync(
                shard,
                async (_, ct) => { await Task.Delay(TimeSpan.FromSeconds(10), ct); return (Either<EncinaError, Unit>)unit; },
                TimeSpan.FromSeconds(30),
                cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_SingleShard_StopOnFirstFailure_NoRemaining()
    {
        // Kills: boundary condition on !results.ContainsKey when there are no remaining shards
        var shards = CreateShards("only");
        var options = new MigrationOptions { StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };

        var results = await _sut.ExecuteAsync(
            shards,
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(EncinaError.New("fail")),
            options,
            (_, _) => { },
            CancellationToken.None);

        results.Count.ShouldBe(1);
        results["only"].Outcome.ShouldBe(MigrationOutcome.Failed);
    }

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
