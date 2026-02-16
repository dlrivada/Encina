using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static LanguageExt.Prelude;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="ReshardingPhaseExecutor"/>.
/// Validates sequential phase execution, crash recovery resume, state persistence,
/// transition validation, callback handling, and error propagation.
/// </summary>
public sealed class ReshardingPhaseExecutorTests
{
    #region Test Helpers

    private static readonly ReshardingPhase[] AllExecutionPhases =
    [
        ReshardingPhase.Copying,
        ReshardingPhase.Replicating,
        ReshardingPhase.Verifying,
        ReshardingPhase.CuttingOver,
        ReshardingPhase.CleaningUp,
    ];

    private static IReshardingPhase CreateMockPhase(
        ReshardingPhase phase,
        Either<EncinaError, PhaseResult> result)
    {
        var mock = Substitute.For<IReshardingPhase>();
        mock.Phase.Returns(phase);
        mock.ExecuteAsync(Arg.Any<PhaseContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
        return mock;
    }

    private static IReshardingPhase CreateSuccessPhase(ReshardingPhase phase, Guid? id = null)
    {
        var progress = CreateProgress(id: id, phase: phase, percent: 100.0);
        var result = new PhaseResult(PhaseStatus.Completed, progress);
        return CreateMockPhase(phase, Either<EncinaError, PhaseResult>.Right(result));
    }

    private static Dictionary<ReshardingPhase, IReshardingPhase> CreateAllSuccessPhases(Guid? id = null)
    {
        return AllExecutionPhases.ToDictionary(
            p => p,
            p => CreateSuccessPhase(p, id));
    }

    private static (ReshardingPhaseExecutor Executor, IReshardingStateStore StateStore) CreateSut()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(unit));

        var logger = Substitute.For<ILogger>();
        var executor = new ReshardingPhaseExecutor(stateStore, logger);
        return (executor, stateStore);
    }

    #endregion

    #region ExecuteAllPhasesAsync

    [Fact]
    public async Task ExecuteAllPhasesAsync_NullState_ThrowsArgumentNullException()
    {
        var (executor, _) = CreateSut();
        var phases = CreateAllSuccessPhases();
        var context = CreatePhaseContext();
        var options = CreateOptions();

        await Should.ThrowAsync<ArgumentNullException>(
            () => executor.ExecuteAllPhasesAsync(null!, phases, context, options));
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_NullPhases_ThrowsArgumentNullException()
    {
        var (executor, _) = CreateSut();
        var state = CreateState();
        var context = CreatePhaseContext(id: state.Id);
        var options = CreateOptions();

        await Should.ThrowAsync<ArgumentNullException>(
            () => executor.ExecuteAllPhasesAsync(state, null!, context, options));
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_NullContext_ThrowsArgumentNullException()
    {
        var (executor, _) = CreateSut();
        var state = CreateState();
        var phases = CreateAllSuccessPhases(id: state.Id);
        var options = CreateOptions();

        await Should.ThrowAsync<ArgumentNullException>(
            () => executor.ExecuteAllPhasesAsync(state, phases, null!, options));
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_NullOptions_ThrowsArgumentNullException()
    {
        var (executor, _) = CreateSut();
        var state = CreateState();
        var phases = CreateAllSuccessPhases(id: state.Id);
        var context = CreatePhaseContext(id: state.Id);

        await Should.ThrowAsync<ArgumentNullException>(
            () => executor.ExecuteAllPhasesAsync(state, phases, context, null!));
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_AllPhasesSucceed_ReturnsFivePhaseHistoryEntries()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        var history = ExtractRight(result);
        history.Count.ShouldBe(5);
        history[0].Phase.ShouldBe(ReshardingPhase.Copying);
        history[1].Phase.ShouldBe(ReshardingPhase.Replicating);
        history[2].Phase.ShouldBe(ReshardingPhase.Verifying);
        history[3].Phase.ShouldBe(ReshardingPhase.CuttingOver);
        history[4].Phase.ShouldBe(ReshardingPhase.CleaningUp);
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_PhaseFails_ReturnsLeftWithError()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        var failError = EncinaErrors.Create(
            ReshardingErrorCodes.CopyFailed, "Copy phase failed.");
        var failingPhase = CreateMockPhase(
            ReshardingPhase.Copying,
            Either<EncinaError, PhaseResult>.Left(failError));

        var phases = CreateAllSuccessPhases(id: reshardingId);
        phases[ReshardingPhase.Copying] = failingPhase;

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.CopyFailed);
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_PhaseAborted_ReturnsLeftWithCutoverAborted()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        var abortedProgress = CreateProgress(id: reshardingId, phase: ReshardingPhase.CuttingOver, percent: 50.0);
        var abortedResult = new PhaseResult(PhaseStatus.Aborted, abortedProgress);
        var abortedPhase = CreateMockPhase(
            ReshardingPhase.CuttingOver,
            Either<EncinaError, PhaseResult>.Right(abortedResult));

        var phases = CreateAllSuccessPhases(id: reshardingId);
        phases[ReshardingPhase.CuttingOver] = abortedPhase;

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.CutoverAborted);
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_PersistsStateAfterEachPhase()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, stateStore) = CreateSut();
        var state = CreateState(id: reshardingId);
        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        await stateStore.Received(5).SaveStateAsync(
            Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_InvokesOnPhaseCompletedCallback()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        var callbackInvocations = new List<ReshardingPhase>();
        options.OnPhaseCompleted = (phase, _) =>
        {
            callbackInvocations.Add(phase);
            return Task.CompletedTask;
        };

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsRight.ShouldBeTrue();
        callbackInvocations.Count.ShouldBe(5);
        callbackInvocations.ShouldBe(AllExecutionPhases.ToList());
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_CallbackThrows_ContinuesNonFatal()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        options.OnPhaseCompleted = (_, _) =>
            throw new InvalidOperationException("Callback failure");

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert - all phases still succeed despite callback failures
        result.IsRight.ShouldBeTrue();
        var history = ExtractRight(result);
        history.Count.ShouldBe(5);
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_CrashRecovery_ResumesFromLastCompletedPhase()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();

        // State indicates Replicating was the last completed phase
        var plan = CreatePlan(id: reshardingId);
        var progress = CreateProgress(id: reshardingId, phase: ReshardingPhase.Replicating, percent: 40.0);
        var state = new ReshardingState(
            reshardingId,
            ReshardingPhase.Verifying,
            plan,
            progress,
            LastCompletedPhase: ReshardingPhase.Replicating,
            StartedAtUtc: DateTime.UtcNow,
            Checkpoint: null);

        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert - should only execute Verifying, CuttingOver, CleaningUp (3 phases)
        result.IsRight.ShouldBeTrue();
        var history = ExtractRight(result);
        history.Count.ShouldBe(3);
        history[0].Phase.ShouldBe(ReshardingPhase.Verifying);
        history[1].Phase.ShouldBe(ReshardingPhase.CuttingOver);
        history[2].Phase.ShouldBe(ReshardingPhase.CleaningUp);

        // Verify that Copying and Replicating phases were NOT executed
        await phases[ReshardingPhase.Copying]
            .DidNotReceive()
            .ExecuteAsync(Arg.Any<PhaseContext>(), Arg.Any<CancellationToken>());
        await phases[ReshardingPhase.Replicating]
            .DidNotReceive()
            .ExecuteAsync(Arg.Any<PhaseContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_MissingPhaseImplementation_ReturnsLeftInvalidTransition()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        // Create phases but omit Replicating
        var phases = CreateAllSuccessPhases(id: reshardingId);
        phases.Remove(ReshardingPhase.Replicating);

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.InvalidPhaseTransition);
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_StateSaveFails_ReturnsLeftWithError()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var stateStore = Substitute.For<IReshardingStateStore>();
        var saveError = EncinaErrors.Create(
            ReshardingErrorCodes.StateStoreFailed, "Save failed.");
        stateStore.SaveStateAsync(Arg.Any<ReshardingState>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(saveError));

        var logger = Substitute.For<ILogger>();
        var executor = new ReshardingPhaseExecutor(stateStore, logger);

        var state = CreateState(id: reshardingId);
        var phases = CreateAllSuccessPhases(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        // Act
        var result = await executor.ExecuteAllPhasesAsync(state, phases, context, options);

        // Assert - fails on first save attempt after the first phase
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAllPhasesAsync_CancellationMidPhase_ThrowsOperationCanceledException()
    {
        // Arrange
        var reshardingId = Guid.NewGuid();
        var (executor, _) = CreateSut();
        var state = CreateState(id: reshardingId);
        var context = CreatePhaseContext(id: reshardingId);
        var options = CreateOptions();

        var cts = new CancellationTokenSource();

        // First phase succeeds, then cancel before second phase
        var copyingPhase = CreateSuccessPhase(ReshardingPhase.Copying, id: reshardingId);

        var replicatingPhase = Substitute.For<IReshardingPhase>();
        replicatingPhase.Phase.Returns(ReshardingPhase.Replicating);
        replicatingPhase.ExecuteAsync(Arg.Any<PhaseContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callInfo.Arg<CancellationToken>().ThrowIfCancellationRequested();
                var progress = CreateProgress(id: reshardingId, phase: ReshardingPhase.Replicating, percent: 100.0);
                return Either<EncinaError, PhaseResult>.Right(new PhaseResult(PhaseStatus.Completed, progress));
            });

        var phases = CreateAllSuccessPhases(id: reshardingId);
        phases[ReshardingPhase.Copying] = copyingPhase;
        phases[ReshardingPhase.Replicating] = replicatingPhase;

        // Cancel after first phase completes
        options.OnPhaseCompleted = (phase, _) =>
        {
            if (phase == ReshardingPhase.Copying)
            {
                cts.Cancel();
            }
            return Task.CompletedTask;
        };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => executor.ExecuteAllPhasesAsync(state, phases, context, options, cts.Token));
    }

    #endregion

    #region ValidateTransition

    [Fact]
    public void ValidateTransition_CopyingWithNullPrevious_ReturnsRight()
    {
        var result = ReshardingPhaseExecutor.ValidateTransition(null, ReshardingPhase.Copying);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ValidateTransition_ReplicatingAfterCopying_ReturnsRight()
    {
        var result = ReshardingPhaseExecutor.ValidateTransition(
            ReshardingPhase.Copying, ReshardingPhase.Replicating);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ValidateTransition_VerifyingAfterReplicating_ReturnsRight()
    {
        var result = ReshardingPhaseExecutor.ValidateTransition(
            ReshardingPhase.Replicating, ReshardingPhase.Verifying);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ValidateTransition_CuttingOverAfterVerifying_ReturnsRight()
    {
        var result = ReshardingPhaseExecutor.ValidateTransition(
            ReshardingPhase.Verifying, ReshardingPhase.CuttingOver);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ValidateTransition_CleaningUpAfterCuttingOver_ReturnsRight()
    {
        var result = ReshardingPhaseExecutor.ValidateTransition(
            ReshardingPhase.CuttingOver, ReshardingPhase.CleaningUp);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ValidateTransition_SkippedPhase_ReturnsLeftInvalidTransition()
    {
        // Skipping from Copying directly to Verifying (missing Replicating)
        var result = ReshardingPhaseExecutor.ValidateTransition(
            ReshardingPhase.Copying, ReshardingPhase.Verifying);

        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.InvalidPhaseTransition);
    }

    [Fact]
    public void ValidateTransition_InvalidTargetPhase_ReturnsLeftInvalidTransition()
    {
        // Completed is not a valid execution phase
        var result = ReshardingPhaseExecutor.ValidateTransition(
            null, ReshardingPhase.Completed);

        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReshardingErrorCodes.InvalidPhaseTransition);
    }

    #endregion
}
