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

    private static List<ShardInfo> CreateShards(params string[] ids)
    {
        return ids.Select(id => new ShardInfo(id, $"conn-{id}")).ToList();
    }
}
