using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for all resharding record types.
/// Validates construction, null guard validation, value equality, and computed properties.
/// </summary>
public sealed class ReshardingRecordTypeTests
{
    #region KeyRange

    [Fact]
    public void KeyRange_Construction_SetsProperties()
    {
        var range = new KeyRange(100UL, 500UL);

        range.RingStart.ShouldBe(100UL);
        range.RingEnd.ShouldBe(500UL);
    }

    [Fact]
    public void KeyRange_ValueEquality_SameValues_AreEqual()
    {
        var range1 = new KeyRange(0UL, 1000UL);
        var range2 = new KeyRange(0UL, 1000UL);

        range1.ShouldBe(range2);
    }

    [Fact]
    public void KeyRange_ValueEquality_DifferentValues_AreNotEqual()
    {
        var range1 = new KeyRange(0UL, 1000UL);
        var range2 = new KeyRange(0UL, 2000UL);

        range1.ShouldNotBe(range2);
    }

    #endregion

    #region ShardMigrationStep

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardMigrationStep_NullOrWhitespaceSourceShardId_ThrowsArgumentException(string? source)
    {
        var keyRange = ReshardingTestBuilders.CreateKeyRange();

        Should.Throw<ArgumentException>(() =>
            new ShardMigrationStep(source!, "shard-1", keyRange, 100));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardMigrationStep_NullOrWhitespaceTargetShardId_ThrowsArgumentException(string? target)
    {
        var keyRange = ReshardingTestBuilders.CreateKeyRange();

        Should.Throw<ArgumentException>(() =>
            new ShardMigrationStep("shard-0", target!, keyRange, 100));
    }

    [Fact]
    public void ShardMigrationStep_NullKeyRange_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardMigrationStep("shard-0", "shard-1", null!, 100));
    }

    [Fact]
    public void ShardMigrationStep_ValidConstruction_SetsAllProperties()
    {
        var keyRange = ReshardingTestBuilders.CreateKeyRange(10, 500);

        var step = new ShardMigrationStep("source-shard", "target-shard", keyRange, 2500);

        step.SourceShardId.ShouldBe("source-shard");
        step.TargetShardId.ShouldBe("target-shard");
        step.KeyRange.ShouldBe(keyRange);
        step.EstimatedRows.ShouldBe(2500);
    }

    #endregion

    #region EstimatedResources

    [Fact]
    public void EstimatedResources_Construction_SetsAllProperties()
    {
        var duration = TimeSpan.FromMinutes(30);

        var estimate = new EstimatedResources(10000, 5_000_000, duration);

        estimate.TotalRows.ShouldBe(10000);
        estimate.TotalBytes.ShouldBe(5_000_000);
        estimate.EstimatedDuration.ShouldBe(duration);
    }

    [Fact]
    public void EstimatedResources_ValueEquality_SameValues_AreEqual()
    {
        var duration = TimeSpan.FromMinutes(5);
        var est1 = new EstimatedResources(1000, 256000, duration);
        var est2 = new EstimatedResources(1000, 256000, duration);

        est1.ShouldBe(est2);
    }

    #endregion

    #region ReshardingPlan

    [Fact]
    public void ReshardingPlan_NullSteps_ThrowsArgumentNullException()
    {
        var estimate = ReshardingTestBuilders.CreateEstimate();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPlan(Guid.NewGuid(), null!, estimate));
    }

    [Fact]
    public void ReshardingPlan_NullEstimate_ThrowsArgumentNullException()
    {
        var steps = new List<ShardMigrationStep> { ReshardingTestBuilders.CreateMigrationStep() };

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPlan(Guid.NewGuid(), steps, null!));
    }

    [Fact]
    public void ReshardingPlan_ValidConstruction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var steps = new List<ShardMigrationStep>
        {
            ReshardingTestBuilders.CreateMigrationStep("s0", "s1"),
            ReshardingTestBuilders.CreateMigrationStep("s2", "s3"),
        };
        var estimate = ReshardingTestBuilders.CreateEstimate();

        var plan = new ReshardingPlan(id, steps, estimate);

        plan.Id.ShouldBe(id);
        plan.Steps.Count.ShouldBe(2);
        plan.Estimate.ShouldBe(estimate);
    }

    #endregion

    #region ReshardingState

    [Fact]
    public void ReshardingState_NullPlan_ThrowsArgumentNullException()
    {
        var progress = ReshardingTestBuilders.CreateProgress();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingState(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                null!,
                progress,
                LastCompletedPhase: null,
                StartedAtUtc: DateTime.UtcNow,
                Checkpoint: null));
    }

    [Fact]
    public void ReshardingState_NullProgress_ThrowsArgumentNullException()
    {
        var plan = ReshardingTestBuilders.CreatePlan();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingState(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                plan,
                null!,
                LastCompletedPhase: null,
                StartedAtUtc: DateTime.UtcNow,
                Checkpoint: null));
    }

    [Fact]
    public void ReshardingState_ValidConstruction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var plan = ReshardingTestBuilders.CreatePlan(id: id);
        var progress = ReshardingTestBuilders.CreateProgress(id: id, phase: ReshardingPhase.Verifying);
        var checkpoint = new ReshardingCheckpoint(500, "lsn:100");
        var startedAt = DateTime.UtcNow.AddMinutes(-5);

        var state = new ReshardingState(
            id,
            ReshardingPhase.Verifying,
            plan,
            progress,
            LastCompletedPhase: ReshardingPhase.Replicating,
            StartedAtUtc: startedAt,
            Checkpoint: checkpoint);

        state.Id.ShouldBe(id);
        state.CurrentPhase.ShouldBe(ReshardingPhase.Verifying);
        state.Plan.ShouldBe(plan);
        state.Progress.ShouldBe(progress);
        state.LastCompletedPhase.ShouldBe(ReshardingPhase.Replicating);
        state.StartedAtUtc.ShouldBe(startedAt);
        state.Checkpoint.ShouldBe(checkpoint);
    }

    #endregion

    #region ReshardingResult

    [Fact]
    public void ReshardingResult_NullPhaseHistory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingResult(Guid.NewGuid(), ReshardingPhase.Completed, null!, null));
    }

    [Fact]
    public void ReshardingResult_IsSuccess_TrueForCompleted()
    {
        var result = ReshardingTestBuilders.CreateResult(ReshardingPhase.Completed);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ReshardingResult_IsSuccess_FalseForFailed()
    {
        var result = ReshardingTestBuilders.CreateResult(ReshardingPhase.Failed);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ReshardingResult_IsSuccess_FalseForRolledBack()
    {
        var result = ReshardingTestBuilders.CreateResult(ReshardingPhase.RolledBack);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void ReshardingResult_ValidConstruction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var history = new List<PhaseHistoryEntry>
        {
            new(ReshardingPhase.Planning, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(-4)),
        };
        var rollback = new RollbackMetadata(
            ReshardingTestBuilders.CreatePlan(),
            ReshardingTestBuilders.CreateTopology(),
            ReshardingPhase.Copying);

        var result = new ReshardingResult(id, ReshardingPhase.RolledBack, history, rollback);

        result.Id.ShouldBe(id);
        result.FinalPhase.ShouldBe(ReshardingPhase.RolledBack);
        result.PhaseHistory.Count.ShouldBe(1);
        result.RollbackMetadata.ShouldNotBeNull();
        result.RollbackMetadata.ShouldBe(rollback);
    }

    #endregion

    #region ReshardingRequest

    [Fact]
    public void ReshardingRequest_NullOldTopology_ThrowsArgumentNullException()
    {
        var newTopology = ReshardingTestBuilders.CreateTopology();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingRequest(null!, newTopology));
    }

    [Fact]
    public void ReshardingRequest_NullNewTopology_ThrowsArgumentNullException()
    {
        var oldTopology = ReshardingTestBuilders.CreateTopology();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingRequest(oldTopology, null!));
    }

    [Fact]
    public void ReshardingRequest_ValidConstruction_SetsAllProperties()
    {
        var oldTopology = ReshardingTestBuilders.CreateTopology(2);
        var newTopology = ReshardingTestBuilders.CreateTopology(4);

        var request = new ReshardingRequest(oldTopology, newTopology);

        request.OldTopology.ShouldBe(oldTopology);
        request.NewTopology.ShouldBe(newTopology);
        request.EntityTypeConstraints.ShouldBeNull();
    }

    [Fact]
    public void ReshardingRequest_WithEntityTypeConstraints_SetsConstraints()
    {
        var oldTopology = ReshardingTestBuilders.CreateTopology(2);
        var newTopology = ReshardingTestBuilders.CreateTopology(4);
        IReadOnlySet<Type> constraints = new System.Collections.Generic.HashSet<Type> { typeof(string), typeof(int) };

        var request = new ReshardingRequest(oldTopology, newTopology, constraints);

        request.EntityTypeConstraints.ShouldNotBeNull();
        request.EntityTypeConstraints!.Count.ShouldBe(2);
    }

    #endregion

    #region RollbackMetadata

    [Fact]
    public void RollbackMetadata_NullOriginalPlan_ThrowsArgumentNullException()
    {
        var topology = ReshardingTestBuilders.CreateTopology();

        Should.Throw<ArgumentNullException>(() =>
            new RollbackMetadata(null!, topology, ReshardingPhase.Copying));
    }

    [Fact]
    public void RollbackMetadata_NullOldTopology_ThrowsArgumentNullException()
    {
        var plan = ReshardingTestBuilders.CreatePlan();

        Should.Throw<ArgumentNullException>(() =>
            new RollbackMetadata(plan, null!, ReshardingPhase.Copying));
    }

    [Fact]
    public void RollbackMetadata_ValidConstruction_SetsAllProperties()
    {
        var plan = ReshardingTestBuilders.CreatePlan();
        var topology = ReshardingTestBuilders.CreateTopology();

        var metadata = new RollbackMetadata(plan, topology, ReshardingPhase.Verifying);

        metadata.OriginalPlan.ShouldBe(plan);
        metadata.OldTopology.ShouldBe(topology);
        metadata.LastCompletedPhase.ShouldBe(ReshardingPhase.Verifying);
    }

    #endregion

    #region PhaseHistoryEntry

    [Fact]
    public void PhaseHistoryEntry_Duration_ReturnsCompletedMinusStarted()
    {
        var started = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc);
        var completed = new DateTime(2026, 2, 16, 10, 5, 30, DateTimeKind.Utc);

        var entry = new PhaseHistoryEntry(ReshardingPhase.Copying, started, completed);

        entry.Duration.ShouldBe(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void PhaseHistoryEntry_ValidConstruction_SetsAllProperties()
    {
        var started = DateTime.UtcNow.AddMinutes(-10);
        var completed = DateTime.UtcNow;

        var entry = new PhaseHistoryEntry(ReshardingPhase.Planning, started, completed);

        entry.Phase.ShouldBe(ReshardingPhase.Planning);
        entry.StartedAtUtc.ShouldBe(started);
        entry.CompletedAtUtc.ShouldBe(completed);
    }

    [Fact]
    public void PhaseHistoryEntry_ZeroDuration_WhenStartEqualsCompleted()
    {
        var timestamp = DateTime.UtcNow;

        var entry = new PhaseHistoryEntry(ReshardingPhase.CleaningUp, timestamp, timestamp);

        entry.Duration.ShouldBe(TimeSpan.Zero);
    }

    #endregion

    #region PhaseContext

    [Fact]
    public void PhaseContext_NullPlan_ThrowsArgumentNullException()
    {
        var options = ReshardingTestBuilders.CreateOptions();
        var progress = ReshardingTestBuilders.CreateProgress();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), null!, options, progress, null, services));
    }

    [Fact]
    public void PhaseContext_NullOptions_ThrowsArgumentNullException()
    {
        var plan = ReshardingTestBuilders.CreatePlan();
        var progress = ReshardingTestBuilders.CreateProgress();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, null!, progress, null, services));
    }

    [Fact]
    public void PhaseContext_NullProgress_ThrowsArgumentNullException()
    {
        var plan = ReshardingTestBuilders.CreatePlan();
        var options = ReshardingTestBuilders.CreateOptions();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, options, null!, null, services));
    }

    [Fact]
    public void PhaseContext_NullServices_ThrowsArgumentNullException()
    {
        var plan = ReshardingTestBuilders.CreatePlan();
        var options = ReshardingTestBuilders.CreateOptions();
        var progress = ReshardingTestBuilders.CreateProgress();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, options, progress, null, null!));
    }

    [Fact]
    public void PhaseContext_ValidConstruction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var plan = ReshardingTestBuilders.CreatePlan(id: id);
        var options = ReshardingTestBuilders.CreateOptions();
        var progress = ReshardingTestBuilders.CreateProgress(id: id);
        var checkpoint = new ReshardingCheckpoint(100, "pos:42");
        var services = Substitute.For<IReshardingServices>();

        var context = new PhaseContext(id, plan, options, progress, checkpoint, services);

        context.ReshardingId.ShouldBe(id);
        context.Plan.ShouldBe(plan);
        context.Options.ShouldBe(options);
        context.Progress.ShouldBe(progress);
        context.Checkpoint.ShouldBe(checkpoint);
        context.Services.ShouldBe(services);
    }

    [Fact]
    public void PhaseContext_NullCheckpoint_IsAllowed()
    {
        var context = ReshardingTestBuilders.CreatePhaseContext();

        context.Checkpoint.ShouldBeNull();
    }

    #endregion

    #region PhaseResult

    [Fact]
    public void PhaseResult_NullUpdatedProgress_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PhaseResult(PhaseStatus.Completed, null!));
    }

    [Fact]
    public void PhaseResult_ValidConstruction_SetsAllProperties()
    {
        var progress = ReshardingTestBuilders.CreateProgress(phase: ReshardingPhase.Copying, percent: 50.0);
        var checkpoint = new ReshardingCheckpoint(250, "cdc:99");

        var result = new PhaseResult(PhaseStatus.Completed, progress, checkpoint);

        result.Status.ShouldBe(PhaseStatus.Completed);
        result.UpdatedProgress.ShouldBe(progress);
        result.UpdatedCheckpoint.ShouldBe(checkpoint);
    }

    [Fact]
    public void PhaseResult_NullCheckpoint_DefaultsToNull()
    {
        var progress = ReshardingTestBuilders.CreateProgress();

        var result = new PhaseResult(PhaseStatus.Skipped, progress);

        result.UpdatedCheckpoint.ShouldBeNull();
    }

    #endregion

    #region ReshardingProgress

    [Fact]
    public void ReshardingProgress_NullPerStepProgress_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingProgress(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                25.0,
                null!));
    }

    [Fact]
    public void ReshardingProgress_ValidConstruction_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var perStep = new Dictionary<string, ShardMigrationProgress>
        {
            ["shard-0->shard-1"] = new ShardMigrationProgress(500, 100, false),
        };

        var progress = new ReshardingProgress(id, ReshardingPhase.Replicating, 65.5, perStep);

        progress.Id.ShouldBe(id);
        progress.CurrentPhase.ShouldBe(ReshardingPhase.Replicating);
        progress.OverallPercentComplete.ShouldBe(65.5);
        progress.PerStepProgress.Count.ShouldBe(1);
        progress.PerStepProgress["shard-0->shard-1"].RowsCopied.ShouldBe(500);
    }

    [Fact]
    public void ReshardingProgress_EmptyPerStepProgress_IsValid()
    {
        var progress = new ReshardingProgress(
            Guid.NewGuid(),
            ReshardingPhase.Planning,
            0.0,
            new Dictionary<string, ShardMigrationProgress>());

        progress.PerStepProgress.Count.ShouldBe(0);
    }

    #endregion

    #region ReshardingCheckpoint

    [Fact]
    public void ReshardingCheckpoint_BothNullFields_IsValid()
    {
        var checkpoint = new ReshardingCheckpoint(null, null);

        checkpoint.LastCopiedBatchPosition.ShouldBeNull();
        checkpoint.CdcPosition.ShouldBeNull();
    }

    [Fact]
    public void ReshardingCheckpoint_WithValues_SetsProperties()
    {
        var checkpoint = new ReshardingCheckpoint(42, "lsn:0/1234");

        checkpoint.LastCopiedBatchPosition.ShouldBe(42);
        checkpoint.CdcPosition.ShouldBe("lsn:0/1234");
    }

    [Fact]
    public void ReshardingCheckpoint_ValueEquality_SameValues_AreEqual()
    {
        var cp1 = new ReshardingCheckpoint(100, "pos:50");
        var cp2 = new ReshardingCheckpoint(100, "pos:50");

        cp1.ShouldBe(cp2);
    }

    #endregion

    #region ShardMigrationProgress

    [Fact]
    public void ShardMigrationProgress_DefaultValues_AreZeroAndFalse()
    {
        var progress = new ShardMigrationProgress(0, 0, false);

        progress.RowsCopied.ShouldBe(0);
        progress.RowsReplicated.ShouldBe(0);
        progress.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public void ShardMigrationProgress_WithValues_SetsProperties()
    {
        var progress = new ShardMigrationProgress(5000, 200, true);

        progress.RowsCopied.ShouldBe(5000);
        progress.RowsReplicated.ShouldBe(200);
        progress.IsVerified.ShouldBeTrue();
    }

    [Fact]
    public void ShardMigrationProgress_ValueEquality_SameValues_AreEqual()
    {
        var p1 = new ShardMigrationProgress(100, 50, true);
        var p2 = new ShardMigrationProgress(100, 50, true);

        p1.ShouldBe(p2);
    }

    #endregion
}
