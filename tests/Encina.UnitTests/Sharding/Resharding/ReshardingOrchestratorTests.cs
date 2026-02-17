using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using Encina.Sharding.Routing;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Sharding.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingOrchestrator"/>.
/// Validates plan generation, full execution workflow, rollback logic, and progress retrieval.
/// </summary>
public sealed class ReshardingOrchestratorTests
{
    #region Test Helpers

    private static (
        ReshardingOrchestrator Orchestrator,
        IShardRebalancer Rebalancer,
        IShardTopologyProvider TopologyProvider,
        IReshardingStateStore StateStore,
        IReshardingServices Services) CreateSut(ReshardingOptions? options = null)
    {
        var rebalancer = Substitute.For<IShardRebalancer>();
        var topologyProvider = Substitute.For<IShardTopologyProvider>();
        var stateStore = Substitute.For<IReshardingStateStore>();
        var services = Substitute.For<IReshardingServices>();
        var logger = Substitute.For<ILogger<ReshardingOrchestrator>>();
        var resolvedOptions = options ?? CreateOptions();

        topologyProvider.GetTopology().Returns(CreateTopology());

        var orchestrator = new ReshardingOrchestrator(
            rebalancer,
            topologyProvider,
            stateStore,
            services,
            resolvedOptions,
            logger);

        return (orchestrator, rebalancer, topologyProvider, stateStore, services);
    }

    private static List<AffectedKeyRange> CreateAffectedRanges(int count = 2)
    {
        return Enumerable.Range(0, count)
            .Select(i => new AffectedKeyRange(
                (ulong)(i * 1000),
                (ulong)((i + 1) * 1000),
                $"shard-{i}",
                $"shard-{i + count}"))
            .ToList();
    }

    /// <summary>
    /// Configures all services to return success for a full end-to-end run.
    /// CopyBatchAsync returns no more rows (single batch completes immediately).
    /// ReplicateChangesAsync returns zero lag and zero rows (caught up immediately).
    /// VerifyDataConsistencyAsync returns consistent.
    /// GetReplicationLagAsync returns zero.
    /// SwapTopologyAsync returns Right.
    /// CleanupSourceDataAsync returns Right with 0 rows.
    /// </summary>
    private static void ConfigureServicesForFullSuccess(
        IReshardingServices services,
        IReshardingStateStore stateStore)
    {
        services.CopyBatchAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(0, 0, false)));

        services.ReplicateChangesAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0, null, TimeSpan.Zero)));

        services.VerifyDataConsistencyAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(true, 0, 0)));

        services.GetReplicationLagAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TimeSpan>.Right(TimeSpan.Zero));

        services.SwapTopologyAsync(
                Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(0L));

        services.EstimateRowCountAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(1000L));

        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        stateStore.GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.Arg<Guid>();
                return Either<EncinaError, ReshardingState>.Right(CreateState(id: id));
            });
    }

    #endregion

    #region PlanAsync

    [Fact]
    public async Task PlanAsync_ValidRequest_ReturnsRightWithPlan()
    {
        // Arrange
        var (orchestrator, rebalancer, _, _, services) = CreateSut();
        var oldTopology = CreateTopology(2);
        var newTopology = CreateTopology(4);
        var request = new ReshardingRequest(oldTopology, newTopology);

        var affectedRanges = CreateAffectedRanges(2);
        rebalancer.CalculateAffectedKeyRanges(oldTopology, newTopology)
            .Returns(affectedRanges);

        services.EstimateRowCountAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(1000L));

        // Act
        var result = await orchestrator.PlanAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var plan = ExtractRight(result);
        plan.Steps.Count.ShouldBe(2);
        plan.Estimate.TotalRows.ShouldBe(2000L);
    }

    [Fact]
    public async Task PlanAsync_NullRequest_ThrowsArgumentNullException()
    {
        var (orchestrator, _, _, _, _) = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.PlanAsync(null!));
    }

    [Fact]
    public async Task PlanAsync_IdenticalTopologies_ReturnsLeftTopologiesIdentical()
    {
        // Arrange
        var (orchestrator, rebalancer, _, _, _) = CreateSut();
        var topology = CreateTopology(3);
        var request = new ReshardingRequest(topology, topology);

        rebalancer.CalculateAffectedKeyRanges(topology, topology)
            .Returns(new List<AffectedKeyRange>());

        // Act
        var result = await orchestrator.PlanAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.TopologiesIdentical);
    }

    [Fact]
    public async Task PlanAsync_RebalancerThrows_ReturnsLeftPlanGenerationFailed()
    {
        // Arrange
        var (orchestrator, rebalancer, _, _, _) = CreateSut();
        var oldTopology = CreateTopology(2);
        var newTopology = CreateTopology(4);
        var request = new ReshardingRequest(oldTopology, newTopology);

        rebalancer.CalculateAffectedKeyRanges(oldTopology, newTopology)
            .Returns(_ => throw new InvalidOperationException("Rebalancer failure"));

        // Act
        var result = await orchestrator.PlanAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.PlanGenerationFailed);
    }

    #endregion

    #region ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_NullPlan_ThrowsArgumentNullException()
    {
        var (orchestrator, _, _, _, _) = CreateSut();
        var options = CreateOptions();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.ExecuteAsync(null!, options));
    }

    [Fact]
    public async Task ExecuteAsync_NullOptions_ThrowsArgumentNullException()
    {
        var (orchestrator, _, _, _, _) = CreateSut();
        var plan = CreatePlan();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.ExecuteAsync(plan, null!));
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentExecution_ReturnsLeftConcurrentNotAllowed()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        ConfigureServicesForFullSuccess(services, stateStore);

        var plan = CreatePlan();
        var options = CreateOptions();

        // Make the first call hang on the first CopyBatchAsync call
        var tcs = new TaskCompletionSource<Either<EncinaError, CopyBatchResult>>();
        var callCount = 0;

        services.CopyBatchAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    return tcs.Task;
                }
                return Task.FromResult(
                    Either<EncinaError, CopyBatchResult>.Right(
                        new CopyBatchResult(0, 0, false)));
            });

        // Act - start first call (will hang)
        var firstTask = orchestrator.ExecuteAsync(plan, options);

        // Small delay to ensure the first call enters execution
        await Task.Delay(50);

        // Second call should fail immediately
        var secondResult = await orchestrator.ExecuteAsync(CreatePlan(), options);

        // Assert
        secondResult.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(secondResult);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.ConcurrentReshardingNotAllowed);

        // Cleanup: unblock the first call
        tcs.SetResult(Either<EncinaError, CopyBatchResult>.Right(
            new CopyBatchResult(0, 0, false)));
        await firstTask;
    }

    [Fact]
    public async Task ExecuteAsync_InitialStateSaveFails_ReturnsLeftWithError()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan();
        var options = CreateOptions();

        var saveError = EncinaErrors.Create(
            ReshardingErrorCodes.StateStoreFailed, "Initial save failed.");
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(saveError));

        // Act
        var result = await orchestrator.ExecuteAsync(plan, options);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AllPhasesComplete_ReturnsRightWithCompletedResult()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        ConfigureServicesForFullSuccess(services, stateStore);

        var plan = CreatePlan();
        var options = CreateOptions();

        // Act
        var result = await orchestrator.ExecuteAsync(plan, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        var reshardingResult = ExtractRight(result);
        reshardingResult.FinalPhase.ShouldBe(ReshardingPhase.Completed);
        reshardingResult.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region RollbackAsync

    [Fact]
    public async Task RollbackAsync_NullResult_ThrowsArgumentNullException()
    {
        var (orchestrator, _, _, _, _) = CreateSut();

        await Should.ThrowAsync<ArgumentNullException>(
            () => orchestrator.RollbackAsync(null!));
    }

    [Fact]
    public async Task RollbackAsync_NoRollbackMetadata_ReturnsLeftRollbackNotAvailable()
    {
        // Arrange
        var (orchestrator, _, _, _, _) = CreateSut();
        var resultWithNoMetadata = CreateResult(
            finalPhase: ReshardingPhase.Failed,
            rollback: null);

        // Act
        var result = await orchestrator.RollbackAsync(resultWithNoMetadata);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.RollbackNotAvailable);
    }

    [Fact]
    public async Task RollbackAsync_CutoverCompleted_RestoresOriginalTopology()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan();
        var oldTopology = CreateTopology(2);

        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.CuttingOver);
        var failedResult = new ReshardingResult(
            Guid.NewGuid(), ReshardingPhase.Failed, [], rollbackMetadata);

        services.SwapTopologyAsync(Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));
        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(0L));

        stateStore.GetStateAsync(failedResult.Id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(
                CreateState(id: failedResult.Id)));
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.Received(1).SwapTopologyAsync(
            oldTopology, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_BeforeCutover_SkipsTopologyRestore()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan();
        var oldTopology = CreateTopology(2);

        // LastCompletedPhase is Copying (before CuttingOver)
        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.Copying);
        var failedResult = new ReshardingResult(
            Guid.NewGuid(), ReshardingPhase.Failed, [], rollbackMetadata);

        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(0L));

        stateStore.GetStateAsync(failedResult.Id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(
                CreateState(id: failedResult.Id)));
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.DidNotReceive().SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_TopologyRestoreFails_ReturnsLeftRollbackFailed()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan();
        var oldTopology = CreateTopology(2);

        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.CuttingOver);
        var failedResult = new ReshardingResult(
            Guid.NewGuid(), ReshardingPhase.Failed, [], rollbackMetadata);

        var swapError = EncinaErrors.Create("swap.failed", "Topology swap failed.");
        services.SwapTopologyAsync(Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(swapError));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.RollbackFailed);
    }

    [Fact]
    public async Task RollbackAsync_CleansUpTargetShards()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan(stepCount: 3);
        var oldTopology = CreateTopology(2);

        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.Copying);
        var failedResult = new ReshardingResult(
            Guid.NewGuid(), ReshardingPhase.Failed, [], rollbackMetadata);

        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(100L));

        stateStore.GetStateAsync(failedResult.Id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(
                CreateState(id: failedResult.Id)));
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Cleanup should be called for each step's TargetShardId
        await services.Received(3).CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());

        // Verify each target shard was cleaned up
        foreach (var step in plan.Steps)
        {
            await services.Received(1).CleanupSourceDataAsync(
                step.TargetShardId,
                step.KeyRange,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task RollbackAsync_CleanupFails_ContinuesBestEffort()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan(stepCount: 2);
        var oldTopology = CreateTopology(2);

        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.Copying);
        var failedResult = new ReshardingResult(
            Guid.NewGuid(), ReshardingPhase.Failed, [], rollbackMetadata);

        var cleanupError = EncinaErrors.Create("cleanup.failed", "Cleanup failed.");
        var callIndex = 0;

        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var idx = Interlocked.Increment(ref callIndex);
                if (idx == 1)
                {
                    return Either<EncinaError, long>.Left(cleanupError);
                }
                return Either<EncinaError, long>.Right(50L);
            });

        stateStore.GetStateAsync(failedResult.Id, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(
                CreateState(id: failedResult.Id)));
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert - still returns Right because cleanup is best-effort
        result.IsRight.ShouldBeTrue();
        // Both cleanup calls should have been attempted
        await services.Received(2).CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RollbackAsync_UpdatesStatesToRolledBack()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        var plan = CreatePlan();
        var oldTopology = CreateTopology(2);
        var reshardingId = Guid.NewGuid();

        var rollbackMetadata = new RollbackMetadata(
            plan, oldTopology, ReshardingPhase.Copying);
        var failedResult = new ReshardingResult(
            reshardingId, ReshardingPhase.Failed, [], rollbackMetadata);

        services.CleanupSourceDataAsync(
                Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(0L));

        var existingState = CreateState(id: reshardingId, phase: ReshardingPhase.Failed);
        stateStore.GetStateAsync(reshardingId, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(existingState));
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsRight.ShouldBeTrue();
        await stateStore.Received().SaveStateAsync(
            Arg.Is<ReshardingState>(s =>
                s.Id == reshardingId &&
                s.CurrentPhase == ReshardingPhase.RolledBack),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetProgressAsync

    [Fact]
    public async Task GetProgressAsync_NoActiveOperation_FallsBackToStateStore()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, _) = CreateSut();
        var reshardingId = Guid.NewGuid();
        var progress = CreateProgress(id: reshardingId, phase: ReshardingPhase.Copying, percent: 42.0);
        var state = new ReshardingState(
            reshardingId,
            ReshardingPhase.Copying,
            CreatePlan(id: reshardingId),
            progress,
            null,
            DateTime.UtcNow,
            null);

        stateStore.GetStateAsync(reshardingId, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Right(state));

        // Act
        var result = await orchestrator.GetProgressAsync(reshardingId);

        // Assert
        result.IsRight.ShouldBeTrue();
        var retrievedProgress = ExtractRight(result);
        retrievedProgress.Id.ShouldBe(reshardingId);
        retrievedProgress.OverallPercentComplete.ShouldBe(42.0);
        retrievedProgress.CurrentPhase.ShouldBe(ReshardingPhase.Copying);
    }

    [Fact]
    public async Task GetProgressAsync_NotFound_ReturnsLeftReshardingNotFound()
    {
        // Arrange
        var (orchestrator, _, _, stateStore, _) = CreateSut();
        var reshardingId = Guid.NewGuid();

        stateStore.GetStateAsync(reshardingId, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Left(
                EncinaErrors.Create(
                    ReshardingErrorCodes.ReshardingNotFound,
                    "Not found.")));

        // Act
        var result = await orchestrator.GetProgressAsync(reshardingId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.ReshardingNotFound);
    }

    [Fact]
    public async Task GetProgressAsync_ActiveOperation_ReturnsProgressFromCache()
    {
        // Arrange - start an execution to populate _activeOperations
        var (orchestrator, _, _, stateStore, services) = CreateSut();
        ConfigureServicesForFullSuccess(services, stateStore);

        var plan = CreatePlan();
        var options = CreateOptions();

        // Make CopyBatchAsync hang so the operation stays active
        var copyTcs = new TaskCompletionSource<Either<EncinaError, CopyBatchResult>>();
        services.CopyBatchAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
                Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(copyTcs.Task);

        // Start execution (will hang on CopyBatchAsync)
        var executeTask = orchestrator.ExecuteAsync(plan, options);

        // Give it a moment to enter the execution and populate _activeOperations
        await Task.Delay(50);

        // Act - query progress while operation is active
        var progressResult = await orchestrator.GetProgressAsync(plan.Id);

        // Assert - should get progress from the in-memory cache
        progressResult.IsRight.ShouldBeTrue();
        var progress = ExtractRight(progressResult);
        progress.Id.ShouldBe(plan.Id);
        progress.CurrentPhase.ShouldBe(ReshardingPhase.Copying);

        // Cleanup
        copyTcs.SetResult(Either<EncinaError, CopyBatchResult>.Right(
            new CopyBatchResult(0, 0, false)));
        await executeTask;
    }

    #endregion
}
