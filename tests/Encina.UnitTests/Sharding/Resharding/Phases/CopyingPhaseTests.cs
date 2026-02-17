using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="CopyingPhase"/>.
/// Verifies batch copying, checkpoint resumption, progress tracking, and error handling.
/// </summary>
public sealed class CopyingPhaseTests
{
    #region Test Setup

    private static CopyingPhase CreateSut()
    {
        var logger = Substitute.For<ILogger>();
        return new CopyingPhase(logger);
    }

    private static PhaseContext CreateCopyContext(
        IReshardingServices? services = null,
        int stepCount = 1,
        ReshardingCheckpoint? checkpoint = null,
        long estimatedRows = 1000)
    {
        var resolvedServices = services ?? Substitute.For<IReshardingServices>();
        var id = Guid.NewGuid();

        var steps = Enumerable.Range(0, stepCount)
            .Select(i => CreateMigrationStep(
                source: $"shard-{i}",
                target: $"shard-{i + stepCount}",
                estimatedRows: estimatedRows))
            .ToList();

        var totalRows = steps.Sum(s => s.EstimatedRows);
        var plan = new ReshardingPlan(id, steps, CreateEstimate(totalRows, totalRows * 256));
        var options = CreateOptions();
        var progress = CreateProgress(id: id, phase: ReshardingPhase.Copying);

        return new PhaseContext(id, plan, options, progress, checkpoint, resolvedServices);
    }

    #endregion

    #region ExecuteAsync - Null Guard

    [Fact]
    public async Task ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.ExecuteAsync(null!));
    }

    #endregion

    #region ExecuteAsync - Single Step Single Batch

    [Fact]
    public async Task ExecuteAsync_SingleStepSingleBatch_CopiesAllRows()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(1000L, 1000L, false)));

        var context = CreateCopyContext(services: services, estimatedRows: 1000);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(40.0, 0.01);
    }

    #endregion

    #region ExecuteAsync - Multiple Batches

    [Fact]
    public async Task ExecuteAsync_MultipleBatches_AccumulatesRowsCopied()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();
        var callCount = 0;

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Either<EncinaError, CopyBatchResult>.Right(
                        new CopyBatchResult(500L, 500L, true));
                }

                return Either<EncinaError, CopyBatchResult>.Right(
                    new CopyBatchResult(500L, 1000L, false));
            });

        var context = CreateCopyContext(services: services, estimatedRows: 1000);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        callCount.ShouldBe(2);

        // Verify per-step progress shows total rows copied
        var stepKey = $"shard-0\u2192shard-1";
        phaseResult.UpdatedProgress.PerStepProgress.ShouldContainKey(stepKey);
        phaseResult.UpdatedProgress.PerStepProgress[stepKey].RowsCopied.ShouldBe(1000L);
    }

    #endregion

    #region ExecuteAsync - Resume From Checkpoint

    [Fact]
    public async Task ExecuteAsync_ResumesFromCheckpoint_UsesLastBatchPosition()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Is<long?>(500L), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(500L, 1000L, false)));

        var checkpoint = new ReshardingCheckpoint(500L, null);
        var context = CreateCopyContext(services: services, checkpoint: checkpoint);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.Received(1).CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Is<long?>(500L), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Copy Batch Fails

    [Fact]
    public async Task ExecuteAsync_CopyBatchFails_ReturnsLeftWithCopyFailed()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.CopyFailed, "Bulk copy error")));

        var context = CreateCopyContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CopyFailed);
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_CancellationDuringBatch_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                cts.Cancel();
                return Either<EncinaError, CopyBatchResult>.Right(
                    new CopyBatchResult(500L, 500L, true));
            });

        var context = CreateCopyContext(services: services);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync(context, cts.Token));
    }

    #endregion

    #region ExecuteAsync - Zero Estimated Rows

    [Fact]
    public async Task ExecuteAsync_ZeroEstimatedRows_SetsProgressTo40Percent()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        // With 0 estimated rows, CopyBatch still returns 0/HasMore=false
        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(0L, 0L, false)));

        var context = CreateCopyContext(services: services, estimatedRows: 0);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(40.0, 0.01);
    }

    #endregion

    #region ExecuteAsync - Multiple Steps

    [Fact]
    public async Task ExecuteAsync_MultipleSteps_TracksPerStepProgress()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.CopyBatchAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, CopyBatchResult>.Right(
                new CopyBatchResult(1000L, 1000L, false)));

        var context = CreateCopyContext(services: services, stepCount: 2, estimatedRows: 1000);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.UpdatedProgress.PerStepProgress.Count.ShouldBe(2);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CopyingPhase(null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsCopyingPhase()
    {
        var sut = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.Copying);
    }

    #endregion
}
