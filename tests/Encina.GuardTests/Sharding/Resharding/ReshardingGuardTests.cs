using Encina.OpenTelemetry.Resharding;
using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using Encina.Sharding.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Sharding.Resharding;

/// <summary>
/// Guard clause tests for resharding types.
/// Verifies null/invalid parameter handling across constructors and methods.
/// </summary>
public sealed class ReshardingGuardTests
{
    #region ReshardingOrchestrator Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null rebalancer.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullRebalancer_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                null!,
                Substitute.For<IShardTopologyProvider>(),
                Substitute.For<IReshardingStateStore>(),
                Substitute.For<IReshardingServices>(),
                new ReshardingOptions(),
                NullLogger<ReshardingOrchestrator>.Instance));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null topology provider.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullTopologyProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                Substitute.For<IShardRebalancer>(),
                null!,
                Substitute.For<IReshardingStateStore>(),
                Substitute.For<IReshardingServices>(),
                new ReshardingOptions(),
                NullLogger<ReshardingOrchestrator>.Instance));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null state store.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullStateStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                Substitute.For<IShardRebalancer>(),
                Substitute.For<IShardTopologyProvider>(),
                null!,
                Substitute.For<IReshardingServices>(),
                new ReshardingOptions(),
                NullLogger<ReshardingOrchestrator>.Instance));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null services.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                Substitute.For<IShardRebalancer>(),
                Substitute.For<IShardTopologyProvider>(),
                Substitute.For<IReshardingStateStore>(),
                null!,
                new ReshardingOptions(),
                NullLogger<ReshardingOrchestrator>.Instance));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null options.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                Substitute.For<IShardRebalancer>(),
                Substitute.For<IShardTopologyProvider>(),
                Substitute.For<IReshardingStateStore>(),
                Substitute.For<IReshardingServices>(),
                null!,
                NullLogger<ReshardingOrchestrator>.Instance));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingOrchestrator"/> rejects null logger.
    /// </summary>
    [Fact]
    public void ReshardingOrchestrator_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(
                Substitute.For<IShardRebalancer>(),
                Substitute.For<IShardTopologyProvider>(),
                Substitute.For<IReshardingStateStore>(),
                Substitute.For<IReshardingServices>(),
                new ReshardingOptions(),
                null!));
    }

    #endregion

    #region PlanningPhase Guards

    /// <summary>
    /// Verifies that <see cref="PlanningPhase"/> rejects null rebalancer.
    /// </summary>
    [Fact]
    public void PlanningPhase_NullRebalancer_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PlanningPhase(null!, Substitute.For<IReshardingServices>()));
    }

    /// <summary>
    /// Verifies that <see cref="PlanningPhase"/> rejects null services.
    /// </summary>
    [Fact]
    public void PlanningPhase_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PlanningPhase(Substitute.For<IShardRebalancer>(), null!));
    }

    #endregion

    #region CopyingPhase Guards

    /// <summary>
    /// Verifies that <see cref="CopyingPhase"/> rejects null logger.
    /// </summary>
    [Fact]
    public void CopyingPhase_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CopyingPhase(null!));
    }

    #endregion

    #region ReplicatingPhase Guards

    /// <summary>
    /// Verifies that <see cref="ReplicatingPhase"/> rejects null logger.
    /// </summary>
    [Fact]
    public void ReplicatingPhase_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReplicatingPhase(null!));
    }

    #endregion

    #region VerifyingPhase Guards

    /// <summary>
    /// Verifies that <see cref="VerifyingPhase"/> rejects null logger.
    /// </summary>
    [Fact]
    public void VerifyingPhase_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new VerifyingPhase(null!));
    }

    #endregion

    #region CuttingOverPhase Guards

    /// <summary>
    /// Verifies that <see cref="CuttingOverPhase"/> rejects null logger.
    /// </summary>
    [Fact]
    public void CuttingOverPhase_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CuttingOverPhase(null!, Substitute.For<IShardTopologyProvider>()));
    }

    /// <summary>
    /// Verifies that <see cref="CuttingOverPhase"/> rejects null topology provider.
    /// </summary>
    [Fact]
    public void CuttingOverPhase_NullTopologyProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CuttingOverPhase(Substitute.For<ILogger>(), null!));
    }

    #endregion

    #region CleaningUpPhase Guards

    /// <summary>
    /// Verifies that <see cref="CleaningUpPhase"/> rejects null logger.
    /// </summary>
    [Fact]
    public void CleaningUpPhase_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CleaningUpPhase(null!));
    }

    #endregion

    #region ReshardingPhaseExecutor Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingPhaseExecutor"/> rejects null state store.
    /// </summary>
    [Fact]
    public void ReshardingPhaseExecutor_NullStateStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPhaseExecutor(null!, Substitute.For<ILogger>()));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingPhaseExecutor"/> rejects null logger.
    /// </summary>
    [Fact]
    public void ReshardingPhaseExecutor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPhaseExecutor(Substitute.For<IReshardingStateStore>(), null!));
    }

    #endregion

    #region ReshardingHealthCheck Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingHealthCheck"/> rejects null state store.
    /// </summary>
    [Fact]
    public void ReshardingHealthCheck_NullStateStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(null!, new ReshardingHealthCheckOptions()));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingHealthCheck"/> rejects null options.
    /// </summary>
    [Fact]
    public void ReshardingHealthCheck_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(Substitute.For<IReshardingStateStore>(), null!));
    }

    #endregion

    #region ReshardingMetrics Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingMetrics"/> rejects null callbacks.
    /// </summary>
    [Fact]
    public void ReshardingMetrics_NullCallbacks_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetrics(null!));
    }

    #endregion

    #region ReshardingMetricsCallbacks Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingMetricsCallbacks"/> rejects null rowsPerSecondCallback.
    /// </summary>
    [Fact]
    public void ReshardingMetricsCallbacks_NullRowsPerSecondCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(null!, () => 0.0, () => 0));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingMetricsCallbacks"/> rejects null cdcLagMsCallback.
    /// </summary>
    [Fact]
    public void ReshardingMetricsCallbacks_NullCdcLagMsCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(() => 0.0, null!, () => 0));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingMetricsCallbacks"/> rejects null activeReshardingCountCallback.
    /// </summary>
    [Fact]
    public void ReshardingMetricsCallbacks_NullActiveReshardingCountCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingMetricsCallbacks(() => 0.0, () => 0.0, null!));
    }

    #endregion

    #region ReshardingBuilder Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingBuilder.OnPhaseCompleted"/> rejects null callback.
    /// </summary>
    [Fact]
    public void ReshardingBuilder_OnPhaseCompleted_NullCallback_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            builder.OnPhaseCompleted(null!));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingBuilder.OnCutoverStarting"/> rejects null predicate.
    /// </summary>
    [Fact]
    public void ReshardingBuilder_OnCutoverStarting_NullPredicate_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            builder.OnCutoverStarting(null!));
    }

    #endregion

    #region ShardMigrationStep Guards

    /// <summary>
    /// Verifies that <see cref="ShardMigrationStep"/> rejects null, empty, or whitespace SourceShardId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardMigrationStep_NullOrWhitespaceSourceShardId_ThrowsArgumentException(string? sourceShardId)
    {
        var keyRange = new KeyRange(0, 100);

        Should.Throw<ArgumentException>(() =>
            new ShardMigrationStep(sourceShardId!, "target-shard", keyRange, 100));
    }

    /// <summary>
    /// Verifies that <see cref="ShardMigrationStep"/> rejects null, empty, or whitespace TargetShardId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShardMigrationStep_NullOrWhitespaceTargetShardId_ThrowsArgumentException(string? targetShardId)
    {
        var keyRange = new KeyRange(0, 100);

        Should.Throw<ArgumentException>(() =>
            new ShardMigrationStep("source-shard", targetShardId!, keyRange, 100));
    }

    /// <summary>
    /// Verifies that <see cref="ShardMigrationStep"/> rejects null KeyRange.
    /// </summary>
    [Fact]
    public void ShardMigrationStep_NullKeyRange_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardMigrationStep("source-shard", "target-shard", null!, 100));
    }

    #endregion

    #region ReshardingPlan Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingPlan"/> rejects null Steps.
    /// </summary>
    [Fact]
    public void ReshardingPlan_NullSteps_ThrowsArgumentNullException()
    {
        var estimate = new EstimatedResources(1000, 256_000, TimeSpan.FromMinutes(1));

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPlan(Guid.NewGuid(), null!, estimate));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingPlan"/> rejects null Estimate.
    /// </summary>
    [Fact]
    public void ReshardingPlan_NullEstimate_ThrowsArgumentNullException()
    {
        var steps = new List<ShardMigrationStep>
        {
            new("source-shard", "target-shard", new KeyRange(0, 100), 500)
        };

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingPlan(Guid.NewGuid(), steps, null!));
    }

    #endregion

    #region ReshardingState Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingState"/> rejects null Plan.
    /// </summary>
    [Fact]
    public void ReshardingState_NullPlan_ThrowsArgumentNullException()
    {
        var progress = new ReshardingProgress(
            Guid.NewGuid(),
            ReshardingPhase.Copying,
            0.0,
            new Dictionary<string, ShardMigrationProgress>());

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingState(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                null!,
                progress,
                null,
                DateTime.UtcNow,
                null));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingState"/> rejects null Progress.
    /// </summary>
    [Fact]
    public void ReshardingState_NullProgress_ThrowsArgumentNullException()
    {
        var plan = CreateValidPlan();

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingState(
                Guid.NewGuid(),
                ReshardingPhase.Copying,
                plan,
                null!,
                null,
                DateTime.UtcNow,
                null));
    }

    #endregion

    #region ReshardingResult Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingResult"/> rejects null PhaseHistory.
    /// </summary>
    [Fact]
    public void ReshardingResult_NullPhaseHistory_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingResult(Guid.NewGuid(), ReshardingPhase.Completed, null!, null));
    }

    #endregion

    #region ReshardingRequest Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingRequest"/> rejects null OldTopology.
    /// </summary>
    [Fact]
    public void ReshardingRequest_NullOldTopology_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingRequest(null!, topology));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingRequest"/> rejects null NewTopology.
    /// </summary>
    [Fact]
    public void ReshardingRequest_NullNewTopology_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());

        Should.Throw<ArgumentNullException>(() =>
            new ReshardingRequest(topology, null!));
    }

    #endregion

    #region RollbackMetadata Guards

    /// <summary>
    /// Verifies that <see cref="RollbackMetadata"/> rejects null OriginalPlan.
    /// </summary>
    [Fact]
    public void RollbackMetadata_NullOriginalPlan_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());

        Should.Throw<ArgumentNullException>(() =>
            new RollbackMetadata(null!, topology, ReshardingPhase.Copying));
    }

    /// <summary>
    /// Verifies that <see cref="RollbackMetadata"/> rejects null OldTopology.
    /// </summary>
    [Fact]
    public void RollbackMetadata_NullOldTopology_ThrowsArgumentNullException()
    {
        var plan = CreateValidPlan();

        Should.Throw<ArgumentNullException>(() =>
            new RollbackMetadata(plan, null!, ReshardingPhase.Copying));
    }

    #endregion

    #region PhaseContext Guards

    /// <summary>
    /// Verifies that <see cref="PhaseContext"/> rejects null Plan.
    /// </summary>
    [Fact]
    public void PhaseContext_NullPlan_ThrowsArgumentNullException()
    {
        var options = new ReshardingOptions();
        var progress = CreateValidProgress();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), null!, options, progress, null, services));
    }

    /// <summary>
    /// Verifies that <see cref="PhaseContext"/> rejects null Options.
    /// </summary>
    [Fact]
    public void PhaseContext_NullOptions_ThrowsArgumentNullException()
    {
        var plan = CreateValidPlan();
        var progress = CreateValidProgress();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, null!, progress, null, services));
    }

    /// <summary>
    /// Verifies that <see cref="PhaseContext"/> rejects null Progress.
    /// </summary>
    [Fact]
    public void PhaseContext_NullProgress_ThrowsArgumentNullException()
    {
        var plan = CreateValidPlan();
        var options = new ReshardingOptions();
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, options, null!, null, services));
    }

    /// <summary>
    /// Verifies that <see cref="PhaseContext"/> rejects null Services.
    /// </summary>
    [Fact]
    public void PhaseContext_NullServices_ThrowsArgumentNullException()
    {
        var plan = CreateValidPlan();
        var options = new ReshardingOptions();
        var progress = CreateValidProgress();

        Should.Throw<ArgumentNullException>(() =>
            new PhaseContext(Guid.NewGuid(), plan, options, progress, null, null!));
    }

    #endregion

    #region PhaseResult Guards

    /// <summary>
    /// Verifies that <see cref="PhaseResult"/> rejects null UpdatedProgress.
    /// </summary>
    [Fact]
    public void PhaseResult_NullUpdatedProgress_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PhaseResult(PhaseStatus.Completed, null!));
    }

    #endregion

    #region ReshardingProgress Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingProgress"/> rejects null PerStepProgress.
    /// </summary>
    [Fact]
    public void ReshardingProgress_NullPerStepProgress_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingProgress(Guid.NewGuid(), ReshardingPhase.Copying, 0.0, null!));
    }

    #endregion

    #region ReshardingServiceCollectionExtensions Guards

    /// <summary>
    /// Verifies that <see cref="ReshardingServiceCollectionExtensions.AddResharding"/> rejects null services.
    /// </summary>
    [Fact]
    public void AddResharding_NullServices_ThrowsArgumentNullException()
    {
        var builder = new ReshardingBuilder();

        Should.Throw<ArgumentNullException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(null!, builder));
    }

    /// <summary>
    /// Verifies that <see cref="ReshardingServiceCollectionExtensions.AddResharding"/> rejects null builder.
    /// </summary>
    [Fact]
    public void AddResharding_NullBuilder_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            ReshardingServiceCollectionExtensions.AddResharding(services, null!));
    }

    #endregion

    #region Helpers

    private static ReshardingPlan CreateValidPlan()
    {
        var steps = new List<ShardMigrationStep>
        {
            new("source-shard", "target-shard", new KeyRange(0, 100), 500)
        };
        var estimate = new EstimatedResources(500, 128_000, TimeSpan.FromMinutes(1));
        return new ReshardingPlan(Guid.NewGuid(), steps, estimate);
    }

    private static ReshardingProgress CreateValidProgress()
    {
        return new ReshardingProgress(
            Guid.NewGuid(),
            ReshardingPhase.Copying,
            0.0,
            new Dictionary<string, ShardMigrationProgress>());
    }

    #endregion
}
