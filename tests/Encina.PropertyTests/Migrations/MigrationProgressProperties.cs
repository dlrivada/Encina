using Encina.Sharding.Migrations;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Migrations;

/// <summary>
/// Property-based tests for <see cref="MigrationProgress"/> computed-property invariants.
/// Verifies that RemainingShards and IsFinished are always consistent with the
/// TotalShards, CompletedShards, and FailedShards counters.
/// </summary>
[Trait("Category", "Property")]
public sealed class MigrationProgressProperties
{
    private static readonly Gen<string> PhaseGen =
        Gen.Elements("Canary", "RollingBatch1", "RollingBatch2", "Completed", "RolledBack");

    #region RemainingShards Invariant

    /// <summary>
    /// RemainingShards + CompletedShards + FailedShards always equals TotalShards.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property RemainingShards_PlusCompletedPlusFailed_EqualsTotalShards()
    {
        return Prop.ForAll(Arb.From(BuildValidProgressGen()), progress =>
        {
            var sum = progress.RemainingShards + progress.CompletedShards + progress.FailedShards;
            return sum == progress.TotalShards;
        });
    }

    /// <summary>
    /// RemainingShards is always non-negative when completed + failed does not exceed total.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property RemainingShards_IsNonNegative_WhenValidInputs()
    {
        return Prop.ForAll(Arb.From(BuildValidProgressGen()), progress =>
            progress.RemainingShards >= 0);
    }

    #endregion

    #region IsFinished Invariant

    /// <summary>
    /// IsFinished is equivalent to (CompletedShards + FailedShards >= TotalShards).
    /// </summary>
    [Property(MaxTest = 200)]
    public Property IsFinished_EquivalentTo_CompletedPlusFailedEqualsOrExceedsTotal()
    {
        return Prop.ForAll(Arb.From(BuildValidProgressGen()), progress =>
        {
            var expected = progress.CompletedShards + progress.FailedShards >= progress.TotalShards;
            return progress.IsFinished == expected;
        });
    }

    /// <summary>
    /// IsFinished is true when completed + failed exactly equals total.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property IsFinished_True_WhenCompletedPlusFailedEqualsTotal()
    {
        var gen = Gen.Choose(1, 10).SelectMany(total =>
            Gen.Choose(0, total).SelectMany(completed =>
                PhaseGen.Select(phase =>
                    BuildProgress(total, completed, total - completed, phase))));

        return Prop.ForAll(Arb.From(gen), progress =>
            progress.IsFinished);
    }

    /// <summary>
    /// IsFinished is false when completed + failed is strictly less than total.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property IsFinished_False_WhenCompletedPlusFailedLessThanTotal()
    {
        // Ensure at least 1 remaining shard: total >= 2, completed + failed < total
        var gen = Gen.Choose(2, 10).SelectMany(total =>
        {
            var maxDone = total - 1;
            return Gen.Choose(0, maxDone).SelectMany(completed =>
            {
                var maxFailed = maxDone - completed;
                return Gen.Choose(0, maxFailed).SelectMany(failed =>
                    PhaseGen.Select(phase =>
                        BuildProgress(total, completed, failed, phase)));
            });
        });

        return Prop.ForAll(Arb.From(gen), progress =>
            !progress.IsFinished);
    }

    #endregion

    #region RemainingShards Boundary Cases

    /// <summary>
    /// When IsFinished is true, RemainingShards must be zero.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RemainingShards_IsZero_WhenIsFinished()
    {
        var gen = Gen.Choose(1, 10).SelectMany(total =>
            Gen.Choose(0, total).SelectMany(completed =>
                PhaseGen.Select(phase =>
                    BuildProgress(total, completed, total - completed, phase))));

        return Prop.ForAll(Arb.From(gen), progress =>
            progress.IsFinished && progress.RemainingShards == 0);
    }

    /// <summary>
    /// When no shards have completed or failed, RemainingShards equals TotalShards.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property RemainingShards_EqualsTotal_WhenNoCompletedOrFailed()
    {
        var gen = Gen.Choose(1, 20).SelectMany(total =>
            PhaseGen.Select(phase =>
                BuildProgress(total, 0, 0, phase)));

        return Prop.ForAll(Arb.From(gen), progress =>
            progress.RemainingShards == progress.TotalShards);
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generates valid MigrationProgress where completed + failed &lt;= total.
    /// </summary>
    private static Gen<MigrationProgress> BuildValidProgressGen()
    {
        return Gen.Choose(1, 20).SelectMany(total =>
            Gen.Choose(0, total).SelectMany(completed =>
            {
                var maxFailed = total - completed;
                return Gen.Choose(0, maxFailed).SelectMany(failed =>
                    PhaseGen.Select(phase =>
                        BuildProgress(total, completed, failed, phase)));
            }));
    }

    private static MigrationProgress BuildProgress(
        int total, int completed, int failed, string phase)
    {
        var dict = new Dictionary<string, ShardMigrationStatus>();
        for (var i = 0; i < total; i++)
        {
            var shardId = $"shard-{i}";
            MigrationOutcome outcome;
            if (i < completed)
                outcome = MigrationOutcome.Succeeded;
            else if (i < completed + failed)
                outcome = MigrationOutcome.Failed;
            else
                outcome = MigrationOutcome.Pending;

            dict[shardId] = new ShardMigrationStatus(
                shardId, outcome, TimeSpan.FromMilliseconds(i * 10));
        }

        return new MigrationProgress(
            Guid.NewGuid(), total, completed, failed, phase, dict);
    }

    #endregion
}
