using Encina.Sharding;
using Encina.Sharding.Resharding;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for resharding record types, verifying value equality,
/// validation invariants, and computed property correctness across random inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class ReshardingPropertyTests
{
    // ── KeyRange value equality ─────────────────────────────────────

    [Property(MaxTest = 200)]
    public Property Property_KeyRange_SameStartEnd_AreEqual()
    {
        var gen = Gen.Choose(0, 10000).Two().Select(pair =>
        {
            var (start, end) = pair;
            var a = new KeyRange((ulong)start, (ulong)end);
            var b = new KeyRange((ulong)start, (ulong)end);
            return a.Equals(b) && a == b && a.GetHashCode() == b.GetHashCode();
        });

        return Prop.ForAll(Arb.From(gen), result => result);
    }

    [Property(MaxTest = 200)]
    public Property Property_KeyRange_DifferentStartOrEnd_AreNotEqual()
    {
        var gen = Gen.Choose(0, 5000).SelectMany(start =>
            Gen.Choose(5001, 10000).Select(differentEnd =>
            {
                var a = new KeyRange((ulong)start, (ulong)start);
                var b = new KeyRange((ulong)start, (ulong)differentEnd);
                return !a.Equals(b) && a != b;
            }));

        return Prop.ForAll(Arb.From(gen), result => result);
    }

    // ── ShardMigrationStep validation ───────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_ShardMigrationStep_RejectsNullSourceShardId(PositiveInt estimatedRows)
    {
        try
        {
            _ = new ShardMigrationStep(null!, "target-1", new KeyRange(0, 100), estimatedRows.Get);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    [Property(MaxTest = 100)]
    public bool Property_ShardMigrationStep_RejectsEmptyTargetShardId(PositiveInt estimatedRows)
    {
        try
        {
            _ = new ShardMigrationStep("source-1", "", new KeyRange(0, 100), estimatedRows.Get);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    // ── ReshardingPlan equality by Id ───────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_ReshardingPlan_EqualityBasedOnAllFields()
    {
        var id = Guid.NewGuid();
        var steps = Array.Empty<ShardMigrationStep>() as IReadOnlyList<ShardMigrationStep>;
        var estimate = new EstimatedResources(100, 2048, TimeSpan.FromMinutes(5));

        var a = new ReshardingPlan(id, steps, estimate);
        var b = new ReshardingPlan(id, steps, estimate);

        // Records with same Id, same Steps reference, same Estimate are equal
        return a == b && a.GetHashCode() == b.GetHashCode();
    }

    // ── ReshardingProgress OverallPercentComplete ───────────────────

    [Property(MaxTest = 200)]
    public Property Property_ReshardingProgress_OverallPercentComplete_IsPreserved()
    {
        var gen = Gen.Choose(0, 10000).Select(i =>
        {
            var percent = i / 100.0;
            var progress = new ReshardingProgress(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                percent,
                new Dictionary<string, ShardMigrationProgress>());

            return Math.Abs(progress.OverallPercentComplete - percent) < 0.0001;
        });

        return Prop.ForAll(Arb.From(gen), result => result);
    }

    // ── PhaseHistoryEntry Duration ──────────────────────────────────

    [Property(MaxTest = 200)]
    public Property Property_PhaseHistoryEntry_Duration_IsCompletedMinusStarted()
    {
        var gen = Gen.Choose(0, 100000).Two().Select(pair =>
        {
            var (startOffset, durationSeconds) = pair;
            var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var started = baseDate.AddSeconds(startOffset);
            var completed = started.AddSeconds(Math.Abs(durationSeconds));

            var entry = new PhaseHistoryEntry(
                ReshardingPhase.Copying,
                started,
                completed);

            var expectedDuration = completed - started;
            return entry.Duration == expectedDuration;
        });

        return Prop.ForAll(Arb.From(gen), result => result);
    }

    // ── ReshardingResult IsSuccess ──────────────────────────────────

    [Property(MaxTest = 100)]
    public bool Property_ReshardingResult_IsSuccess_TrueOnlyWhenCompleted()
    {
        var result = new ReshardingResult(
            Guid.NewGuid(),
            ReshardingPhase.Completed,
            Array.Empty<PhaseHistoryEntry>(),
            null);

        return result.IsSuccess;
    }

    [Property(MaxTest = 100)]
    public bool Property_ReshardingResult_IsSuccess_FalseForFailed()
    {
        var result = new ReshardingResult(
            Guid.NewGuid(),
            ReshardingPhase.Failed,
            Array.Empty<PhaseHistoryEntry>(),
            null);

        return !result.IsSuccess;
    }

    [Property(MaxTest = 100)]
    public bool Property_ReshardingResult_IsSuccess_FalseForRolledBack()
    {
        var plan = new ReshardingPlan(
            Guid.NewGuid(),
            Array.Empty<ShardMigrationStep>(),
            new EstimatedResources(0, 0, TimeSpan.Zero));

        var shards = new[] { new ShardInfo("shard-1", "Server=test;Database=db1") };
        var topology = new ShardTopology(shards);

        var rollback = new RollbackMetadata(plan, topology, ReshardingPhase.Copying);

        var result = new ReshardingResult(
            Guid.NewGuid(),
            ReshardingPhase.RolledBack,
            Array.Empty<PhaseHistoryEntry>(),
            rollback);

        return !result.IsSuccess;
    }

    // ── ReshardingCheckpoint equality ────────────────────────────────

    [Property(MaxTest = 200)]
    public Property Property_ReshardingCheckpoint_SameValues_AreEqual()
    {
        var gen = Gen.Choose(0, 100000).Select(i =>
        {
            var batchPos = (long?)i;
            var cdcPos = "lsn:12345";
            var a = new ReshardingCheckpoint(batchPos, cdcPos);
            var b = new ReshardingCheckpoint(batchPos, cdcPos);

            return a.Equals(b) && a == b && a.GetHashCode() == b.GetHashCode();
        });

        return Prop.ForAll(Arb.From(gen), result => result);
    }

    // ── EstimatedResources equality ─────────────────────────────────

    [Property(MaxTest = 200)]
    public Property Property_EstimatedResources_SameValues_AreEqual()
    {
        var gen = Gen.Choose(0, 100000).Three().Select(triple =>
        {
            var (rows, bytes, durationSecs) = triple;
            var duration = TimeSpan.FromSeconds(Math.Abs(durationSecs) % 3600);
            var a = new EstimatedResources(rows, bytes, duration);
            var b = new EstimatedResources(rows, bytes, duration);

            return a.Equals(b) && a == b && a.GetHashCode() == b.GetHashCode();
        });

        return Prop.ForAll(Arb.From(gen), result => result);
    }
}
