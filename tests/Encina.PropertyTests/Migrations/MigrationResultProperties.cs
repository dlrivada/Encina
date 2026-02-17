using Encina.Sharding.Migrations;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Migrations;

/// <summary>
/// Property-based tests for <see cref="MigrationResult"/> computed-property invariants.
/// Verifies that SucceededCount, FailedCount, and AllSucceeded are always consistent
/// with the underlying <see cref="MigrationResult.PerShardStatus"/> dictionary.
/// </summary>
[Trait("Category", "Property")]
public sealed class MigrationResultProperties
{
    #region Count Invariants

    /// <summary>
    /// SucceededCount + FailedCount + other outcomes always equals PerShardStatus.Count.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property SucceededPlusFailedPlusOther_AlwaysEqualsTotalCount()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
        {
            var result = CreateResult(perShardStatus);

            var succeededCount = result.SucceededCount;
            var failedCount = result.FailedCount;
            var otherCount = perShardStatus.Values.Count(s =>
                s.Outcome != MigrationOutcome.Succeeded &&
                s.Outcome != MigrationOutcome.Failed);

            return succeededCount + failedCount + otherCount == perShardStatus.Count;
        });
    }

    /// <summary>
    /// SucceededCount is always non-negative.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property SucceededCount_IsNonNegative()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
            CreateResult(perShardStatus).SucceededCount >= 0);
    }

    /// <summary>
    /// FailedCount is always non-negative.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property FailedCount_IsNonNegative()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
            CreateResult(perShardStatus).FailedCount >= 0);
    }

    /// <summary>
    /// SucceededCount never exceeds the total number of shards.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property SucceededCount_NeverExceedsTotalCount()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
            CreateResult(perShardStatus).SucceededCount <= perShardStatus.Count);
    }

    /// <summary>
    /// FailedCount never exceeds the total number of shards.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property FailedCount_NeverExceedsTotalCount()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
            CreateResult(perShardStatus).FailedCount <= perShardStatus.Count);
    }

    #endregion

    #region AllSucceeded Invariants

    /// <summary>
    /// AllSucceeded is true if and only if SucceededCount equals total shard count.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property AllSucceeded_TrueIff_SucceededCountEqualsTotalCount()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
        {
            var result = CreateResult(perShardStatus);
            var expected = result.SucceededCount == perShardStatus.Count;
            return result.AllSucceeded == expected;
        });
    }

    /// <summary>
    /// When AllSucceeded is true, FailedCount must be zero.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property AllSucceeded_Implies_FailedCountIsZero()
    {
        return Prop.ForAll(Arb.From(BuildPerShardStatusGen()), perShardStatus =>
        {
            var result = CreateResult(perShardStatus);
            // If AllSucceeded is false, property is vacuously true
            if (!result.AllSucceeded) return true;
            return result.FailedCount == 0;
        });
    }

    /// <summary>
    /// AllSucceeded is true when every shard has Succeeded outcome.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllSucceeded_True_WhenAllShardsSucceeded()
    {
        var gen = Gen.Choose(1, 5).Select(count =>
        {
            var dict = new Dictionary<string, ShardMigrationStatus>();
            for (var i = 0; i < count; i++)
            {
                var shardId = $"shard-{i}";
                dict[shardId] = new ShardMigrationStatus(
                    shardId, MigrationOutcome.Succeeded, TimeSpan.FromMilliseconds(i * 10));
            }

            return (IReadOnlyDictionary<string, ShardMigrationStatus>)dict;
        });

        return Prop.ForAll(Arb.From(gen), perShardStatus =>
            CreateResult(perShardStatus).AllSucceeded);
    }

    /// <summary>
    /// AllSucceeded is false when any single shard has Failed outcome.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property AllSucceeded_False_WhenAnySingleShardFailed()
    {
        var gen = Gen.Choose(2, 5).SelectMany(count =>
            Gen.Choose(0, count - 1).Select(failedIndex =>
            {
                var dict = new Dictionary<string, ShardMigrationStatus>();
                for (var i = 0; i < count; i++)
                {
                    var shardId = $"shard-{i}";
                    var outcome = i == failedIndex
                        ? MigrationOutcome.Failed
                        : MigrationOutcome.Succeeded;
                    dict[shardId] = new ShardMigrationStatus(
                        shardId, outcome, TimeSpan.FromMilliseconds(i * 10));
                }

                return (IReadOnlyDictionary<string, ShardMigrationStatus>)dict;
            }));

        return Prop.ForAll(Arb.From(gen), perShardStatus =>
            !CreateResult(perShardStatus).AllSucceeded);
    }

    #endregion

    #region Helpers

    private static MigrationResult CreateResult(
        IReadOnlyDictionary<string, ShardMigrationStatus> perShardStatus)
    {
        return new MigrationResult(
            Guid.NewGuid(), perShardStatus, TimeSpan.FromSeconds(1), DateTimeOffset.UtcNow);
    }

    #endregion

    #region Generators

    private static Gen<IReadOnlyDictionary<string, ShardMigrationStatus>> BuildPerShardStatusGen()
    {
        var outcomeGen = Gen.Elements(
            MigrationOutcome.Succeeded,
            MigrationOutcome.Failed,
            MigrationOutcome.Pending,
            MigrationOutcome.InProgress,
            MigrationOutcome.RolledBack);

        return Gen.Choose(1, 5).SelectMany(count =>
            Gen.ArrayOf(outcomeGen, count).Select(outcomes =>
            {
                var dict = new Dictionary<string, ShardMigrationStatus>();
                for (var i = 0; i < count; i++)
                {
                    var shardId = $"shard-{i}";
                    dict[shardId] = new ShardMigrationStatus(
                        shardId, outcomes[i], TimeSpan.FromMilliseconds(i * 10));
                }

                return (IReadOnlyDictionary<string, ShardMigrationStatus>)dict;
            }));
    }

    #endregion
}
