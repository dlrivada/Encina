using Encina.Sharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Migrations.Strategies;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Sharding;

/// <summary>
/// Guard tests for <see cref="SequentialMigrationStrategy"/>: the strategy validates no argument itself, so these
/// tests pin the empty and null inputs it tolerates and the ones it refuses by failing on first use.
/// </summary>
public class SequentialMigrationStrategyGuardTests
{
    private readonly SequentialMigrationStrategy _sut = new();

    private static readonly MigrationOptions Options = new() { PerShardTimeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// An empty shard list yields an empty result and never calls the action or the progress tracker.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NoShards_ReturnsEmptyResult()
    {
        var calls = 0;

        var results = await _sut.ExecuteAsync(
            [],
            (_, _) => { calls++; return Task.FromResult<Either<EncinaError, Unit>>(unit); },
            Options,
            (_, _) => calls++,
            CancellationToken.None);

        results.ShouldBeEmpty();
        calls.ShouldBe(0);
    }

    /// <summary>
    /// A null shard list is not checked and fails when the strategy enumerates it.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullShards_ThrowsNullReferenceException()
    {
        var act = () => _sut.ExecuteAsync(
            null!, (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit), Options, (_, _) => { }, CancellationToken.None);

        await Should.ThrowAsync<NullReferenceException>(act);
    }

    /// <summary>
    /// A shard whose action succeeds is reported as succeeded and tracked.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SuccessfulShard_IsTracked()
    {
        var tracked = new List<string>();

        var results = await _sut.ExecuteAsync(
            [new ShardInfo("shard-1", "conn")],
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(unit),
            Options,
            (id, _) => tracked.Add(id),
            CancellationToken.None);

        results["shard-1"].Outcome.ShouldBe(MigrationOutcome.Succeeded);
        tracked.ShouldBe(["shard-1"]);
    }

    /// <summary>
    /// A failing shard stops the run when asked to and the remaining shards are left pending.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_FailureWithStopOnFirstFailure_MarksRemainingPending()
    {
        var options = new MigrationOptions { StopOnFirstFailure = true, PerShardTimeout = TimeSpan.FromSeconds(30) };
        var error = EncinaErrors.Create("test.migration", ErrorCategory.Internal, "failed");

        var results = await _sut.ExecuteAsync(
            [new ShardInfo("a", "c"), new ShardInfo("b", "c")],
            (_, _) => Task.FromResult<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)),
            options,
            (_, _) => { },
            CancellationToken.None);

        results["a"].Outcome.ShouldBe(MigrationOutcome.Failed);
        results["b"].Outcome.ShouldBe(MigrationOutcome.Pending);
    }

    /// <summary>
    /// An action that throws is reported as a failed shard, not as an exception.
    /// </summary>
    [Fact]
    public async Task ExecuteOnShardAsync_ThrowingAction_ReturnsFailedStatus()
    {
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            new ShardInfo("s", "c"),
            (_, _) => throw new InvalidOperationException("boom"),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.Error!.Value.GetCode().IfNone(string.Empty).ShouldBe(MigrationErrorCodes.MigrationFailed);
    }

    /// <summary>
    /// An action that outlives the per-shard timeout is reported as a failed shard with the timeout code.
    /// </summary>
    [Fact]
    public async Task ExecuteOnShardAsync_Timeout_ReturnsFailedStatus()
    {
        var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            new ShardInfo("s", "c"),
            async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return unit;
            },
            TimeSpan.FromMilliseconds(20),
            CancellationToken.None);

        status.Outcome.ShouldBe(MigrationOutcome.Failed);
        status.Error!.Value.GetCode().IfNone(string.Empty).ShouldBe(MigrationErrorCodes.MigrationTimeout);
    }

    /// <summary>
    /// A cancelled caller token propagates the cancellation.
    /// </summary>
    [Fact]
    public async Task ExecuteOnShardAsync_CallerCancelled_Propagates()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        var act = () => SequentialMigrationStrategy.ExecuteOnShardAsync(
            new ShardInfo("s", "c"),
            (_, token) => Task.FromCanceled<Either<EncinaError, Unit>>(token),
            TimeSpan.FromSeconds(30),
            source.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
    }
}
