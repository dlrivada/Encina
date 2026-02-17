using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Routing;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Sharding.Resharding;

/// <summary>
/// Integration tests for the resharding workflow. Uses a real
/// <see cref="InMemoryReshardingStateStore"/> to validate state
/// persistence and crash recovery across the full 6-phase workflow.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ReshardingWorkflowIntegrationTests
{
    #region Test Helpers

    private static ShardTopology CreateTopology(int shardCount = 3)
    {
        var shards = Enumerable.Range(0, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"Server=localhost;Database=shard_{i}"))
            .ToList();

        return new ShardTopology(shards);
    }

    private static (IReshardingOrchestrator Orchestrator, InMemoryReshardingStateStore StateStore)
        CreateOrchestrator(
            IReshardingServices? services = null,
            IShardRebalancer? rebalancer = null,
            ShardTopology? topology = null)
    {
        var stateStore = new InMemoryReshardingStateStore();
        var resolvedTopology = topology ?? CreateTopology();

        var topologyProvider = Substitute.For<IShardTopologyProvider>();
        topologyProvider.GetTopology().Returns(resolvedTopology);

        var resolvedRebalancer = rebalancer ?? CreateSuccessfulRebalancer(resolvedTopology);
        var resolvedServices = services ?? CreateSuccessfulServices();

        var options = new ReshardingOptions();
        var logger = new NullLogger<ReshardingOrchestrator>();

        var orchestrator = new ReshardingOrchestrator(
            resolvedRebalancer,
            topologyProvider,
            stateStore,
            resolvedServices,
            options,
            logger);

        return (orchestrator, stateStore);
    }

    private static IShardRebalancer CreateSuccessfulRebalancer(ShardTopology topology)
    {
        var rebalancer = Substitute.For<IShardRebalancer>();

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(),
            Arg.Any<ShardTopology>())
            .Returns(new List<AffectedKeyRange>
            {
                new AffectedKeyRange(0, 500, "shard-0", "shard-3"),
                new AffectedKeyRange(500, 1000, "shard-1", "shard-4")
            });

        return rebalancer;
    }

    private static IReshardingServices CreateSuccessfulServices()
    {
        var services = Substitute.For<IReshardingServices>();

        // EstimateRowCountAsync
        services.EstimateRowCountAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(5000));

        // CopyBatchAsync - return no more rows after first batch
        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(5000, 5000, false)));

        // ReplicateChangesAsync - converge immediately
        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0, "pos-final", TimeSpan.Zero)));

        // GetReplicationLagAsync
        services.GetReplicationLagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TimeSpan>.Right(TimeSpan.Zero));

        // VerifyDataConsistencyAsync
        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(true, 5000, 5000)));

        // SwapTopologyAsync
        services.SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        // CleanupSourceDataAsync
        services.CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(5000));

        return services;
    }

    #endregion

    #region PlanAsync - Generates Plan With Steps

    [Fact]
    public async Task PlanAsync_ValidTopologies_ReturnsReshardingPlan()
    {
        // Arrange
        var (orchestrator, _) = CreateOrchestrator();
        var request = new ReshardingRequest(CreateTopology(3), CreateTopology(5));

        // Act
        var result = await orchestrator.PlanAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var plan = result.Match(Right: p => p, Left: _ => null!);
        plan.ShouldNotBeNull();
        plan.Steps.Count.ShouldBeGreaterThan(0);
    }

    #endregion

    #region ExecuteAsync - Full Workflow Completes

    [Fact]
    public async Task ExecuteAsync_FullWorkflow_CompletesSuccessfully()
    {
        // Arrange
        var (orchestrator, stateStore) = CreateOrchestrator();
        var request = new ReshardingRequest(CreateTopology(3), CreateTopology(5));

        var planResult = await orchestrator.PlanAsync(request);
        planResult.IsRight.ShouldBeTrue();
        var plan = planResult.Match(Right: p => p, Left: _ => null!);

        // Act
        var result = await orchestrator.ExecuteAsync(plan, new ReshardingOptions());

        // Assert
        result.IsRight.ShouldBeTrue();
        var reshardingResult = result.Match(Right: r => r, Left: _ => null!);
        reshardingResult.FinalPhase.ShouldBe(ReshardingPhase.Completed);
        reshardingResult.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region ExecuteAsync - State Persisted To Store

    [Fact]
    public async Task ExecuteAsync_Completes_PersistsCompletedStateToStore()
    {
        // Arrange
        var (orchestrator, stateStore) = CreateOrchestrator();
        var request = new ReshardingRequest(CreateTopology(3), CreateTopology(5));

        var planResult = await orchestrator.PlanAsync(request);
        var plan = planResult.Match(Right: p => p, Left: _ => null!);

        // Act
        await orchestrator.ExecuteAsync(plan, new ReshardingOptions());

        // Assert
        var storedState = await stateStore.GetStateAsync(plan.Id);
        storedState.IsRight.ShouldBeTrue();
        var state = storedState.Match(Right: s => s, Left: _ => null!);
        state.CurrentPhase.ShouldBe(ReshardingPhase.Completed);
    }

    #endregion

    #region ExecuteAsync - Concurrent Resharding Rejected

    [Fact]
    public async Task ExecuteAsync_ConcurrentResharding_ReturnsConcurrentNotAllowed()
    {
        // Arrange
        var slowServices = Substitute.For<IReshardingServices>();

        // Make CopyBatchAsync take forever to simulate an in-progress resharding
        slowServices.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return Either<EncinaError, CopyBatchResult>.Right(
                    new CopyBatchResult(0, 0, false));
            });

        slowServices.EstimateRowCountAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(5000));

        var (orchestrator, _) = CreateOrchestrator(services: slowServices);
        var request = new ReshardingRequest(CreateTopology(3), CreateTopology(5));

        var plan1Result = await orchestrator.PlanAsync(request);
        var plan1 = plan1Result.Match(Right: p => p, Left: _ => null!);

        var plan2Result = await orchestrator.PlanAsync(request);
        var plan2 = plan2Result.Match(Right: p => p, Left: _ => null!);

        // Act - Start first resharding (will block)
        using var cts = new CancellationTokenSource();
        var task1 = orchestrator.ExecuteAsync(plan1, new ReshardingOptions(), cts.Token);

        // Small delay to ensure first resharding has started
        await Task.Delay(50);

        // Try second resharding
        var result2 = await orchestrator.ExecuteAsync(plan2, new ReshardingOptions());

        // Assert
        result2.IsLeft.ShouldBeTrue();
        var error = result2.Match(Right: _ => default!, Left: e => e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.ConcurrentReshardingNotAllowed);

        // Cleanup
        cts.Cancel();
        try { await task1; } catch (OperationCanceledException) { }
    }

    #endregion

    #region GetProgressAsync - Active Operation

    [Fact]
    public async Task GetProgressAsync_UnknownId_ReturnsNotFound()
    {
        // Arrange
        var (orchestrator, _) = CreateOrchestrator();

        // Act
        var result = await orchestrator.GetProgressAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => default!, Left: e => e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.ReshardingNotFound);
    }

    #endregion

    #region GetProgressAsync - From State Store

    [Fact]
    public async Task GetProgressAsync_CompletedOperation_ReturnsFromStateStore()
    {
        // Arrange
        var (orchestrator, stateStore) = CreateOrchestrator();
        var request = new ReshardingRequest(CreateTopology(3), CreateTopology(5));

        var planResult = await orchestrator.PlanAsync(request);
        var plan = planResult.Match(Right: p => p, Left: _ => null!);
        await orchestrator.ExecuteAsync(plan, new ReshardingOptions());

        // Act - After completion, progress should come from state store
        var result = await orchestrator.GetProgressAsync(plan.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        var progress = result.Match(Right: p => p, Left: _ => null!);
        progress.CurrentPhase.ShouldBe(ReshardingPhase.Completed);
    }

    #endregion

    #region RollbackAsync - No RollbackMetadata

    [Fact]
    public async Task RollbackAsync_NoRollbackMetadata_ReturnsRollbackNotAvailable()
    {
        // Arrange
        var (orchestrator, _) = CreateOrchestrator();
        var result = new ReshardingResult(
            Guid.NewGuid(),
            ReshardingPhase.Completed,
            [],
            null);

        // Act
        var rollbackResult = await orchestrator.RollbackAsync(result);

        // Assert
        rollbackResult.IsLeft.ShouldBeTrue();
        var error = rollbackResult.Match(Right: _ => default!, Left: e => e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.RollbackNotAvailable);
    }

    #endregion

    #region RollbackAsync - Successful Rollback

    [Fact]
    public async Task RollbackAsync_WithRollbackMetadata_RestoresTopologyAndCleansUp()
    {
        // Arrange
        var services = CreateSuccessfulServices();
        var (orchestrator, stateStore) = CreateOrchestrator(services: services);
        var oldTopology = CreateTopology(3);
        var plan = new ReshardingPlan(
            Guid.NewGuid(),
            [new ShardMigrationStep("shard-0", "shard-3", new KeyRange(0, 500), 5000)],
            new EstimatedResources(5000, 1_280_000, TimeSpan.FromMinutes(5)));

        // Persist a state to allow rollback to update it
        var state = new ReshardingState(
            plan.Id,
            ReshardingPhase.Failed,
            plan,
            new ReshardingProgress(plan.Id, ReshardingPhase.Failed, 50.0,
                new Dictionary<string, ShardMigrationProgress>()),
            ReshardingPhase.Verifying,
            DateTime.UtcNow,
            null);
        await stateStore.SaveStateAsync(state);

        var rollbackMetadata = new RollbackMetadata(plan, oldTopology, ReshardingPhase.CuttingOver);
        var failedResult = new ReshardingResult(plan.Id, ReshardingPhase.Failed, [], rollbackMetadata);

        // Act
        var result = await orchestrator.RollbackAsync(failedResult);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Verify topology was restored
        await services.Received(1).SwapTopologyAsync(
            Arg.Is<ShardTopology>(t => t == oldTopology),
            Arg.Any<CancellationToken>());

        // Verify target shard cleanup was called
        await services.Received(1).CleanupSourceDataAsync(
            "shard-3", // Target shard (rollback cleans from target)
            Arg.Any<KeyRange>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());

        // Verify state was updated to RolledBack
        var storedState = await stateStore.GetStateAsync(plan.Id);
        storedState.IsRight.ShouldBeTrue();
        var updatedState = storedState.Match(Right: s => s, Left: _ => null!);
        updatedState.CurrentPhase.ShouldBe(ReshardingPhase.RolledBack);
    }

    #endregion

    #region InMemoryStateStore - CRUD Operations

    [Fact]
    public async Task StateStore_SaveAndGet_RoundTrips()
    {
        // Arrange
        var stateStore = new InMemoryReshardingStateStore();
        var id = Guid.NewGuid();
        var plan = new ReshardingPlan(
            id,
            [new ShardMigrationStep("s0", "s1", new KeyRange(0, 100), 1000)],
            new EstimatedResources(1000, 256000, TimeSpan.FromMinutes(1)));
        var progress = new ReshardingProgress(
            id, ReshardingPhase.Copying, 50.0,
            new Dictionary<string, ShardMigrationProgress>());
        var state = new ReshardingState(id, ReshardingPhase.Copying, plan, progress, null, DateTime.UtcNow, null);

        // Act
        await stateStore.SaveStateAsync(state);
        var result = await stateStore.GetStateAsync(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        var retrieved = result.Match(Right: s => s, Left: _ => null!);
        retrieved.Id.ShouldBe(id);
        retrieved.CurrentPhase.ShouldBe(ReshardingPhase.Copying);
    }

    [Fact]
    public async Task StateStore_GetNonExistent_ReturnsNotFound()
    {
        // Arrange
        var stateStore = new InMemoryReshardingStateStore();

        // Act
        var result = await stateStore.GetStateAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StateStore_GetActiveReshardingsAsync_FiltersTerminalStates()
    {
        // Arrange
        var stateStore = new InMemoryReshardingStateStore();

        var CreateMinimalState = (Guid id, ReshardingPhase phase) =>
        {
            var plan = new ReshardingPlan(
                id,
                [new ShardMigrationStep("s0", "s1", new KeyRange(0, 100), 1000)],
                new EstimatedResources(1000, 256000, TimeSpan.FromMinutes(1)));
            var progress = new ReshardingProgress(
                id, phase, 50.0, new Dictionary<string, ShardMigrationProgress>());
            return new ReshardingState(id, phase, plan, progress, null, DateTime.UtcNow, null);
        };

        await stateStore.SaveStateAsync(CreateMinimalState(Guid.NewGuid(), ReshardingPhase.Copying));
        await stateStore.SaveStateAsync(CreateMinimalState(Guid.NewGuid(), ReshardingPhase.Verifying));
        await stateStore.SaveStateAsync(CreateMinimalState(Guid.NewGuid(), ReshardingPhase.Completed));
        await stateStore.SaveStateAsync(CreateMinimalState(Guid.NewGuid(), ReshardingPhase.Failed));
        await stateStore.SaveStateAsync(CreateMinimalState(Guid.NewGuid(), ReshardingPhase.RolledBack));

        // Act
        var result = await stateStore.GetActiveReshardingsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var active = result.Match(Right: a => a, Left: _ => []);
        active.Count.ShouldBe(2); // Only Copying and Verifying
    }

    [Fact]
    public async Task StateStore_DeleteAsync_RemovesState()
    {
        // Arrange
        var stateStore = new InMemoryReshardingStateStore();
        var id = Guid.NewGuid();
        var plan = new ReshardingPlan(
            id,
            [new ShardMigrationStep("s0", "s1", new KeyRange(0, 100), 1000)],
            new EstimatedResources(1000, 256000, TimeSpan.FromMinutes(1)));
        var progress = new ReshardingProgress(
            id, ReshardingPhase.Completed, 100.0,
            new Dictionary<string, ShardMigrationProgress>());
        var state = new ReshardingState(id, ReshardingPhase.Completed, plan, progress, null, DateTime.UtcNow, null);

        await stateStore.SaveStateAsync(state);

        // Act
        var deleteResult = await stateStore.DeleteStateAsync(id);

        // Assert
        deleteResult.IsRight.ShouldBeTrue();
        var getResult = await stateStore.GetStateAsync(id);
        getResult.IsLeft.ShouldBeTrue();
    }

    #endregion
}
