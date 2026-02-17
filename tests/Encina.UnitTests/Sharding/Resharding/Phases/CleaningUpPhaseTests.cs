using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="CleaningUpPhase"/>.
/// Verifies source data cleanup, progress tracking, error handling, and cancellation.
/// </summary>
public sealed class CleaningUpPhaseTests
{
    #region Test Setup

    private static CleaningUpPhase CreateSut()
    {
        var logger = Substitute.For<ILogger>();
        return new CleaningUpPhase(logger);
    }

    private static PhaseContext CreateCleanupContext(
        IReshardingServices? services = null,
        int stepCount = 1)
    {
        var resolvedServices = services ?? Substitute.For<IReshardingServices>();
        var id = Guid.NewGuid();
        var plan = CreatePlan(stepCount: stepCount, id: id);
        var options = CreateOptions();
        var progress = CreateProgress(id: id, phase: ReshardingPhase.CleaningUp, percent: 90.0);

        return new PhaseContext(id, plan, options, progress, null, resolvedServices);
    }

    private static IReshardingServices CreateSuccessfulCleanupServices(long rowsDeleted = 5000)
    {
        var services = Substitute.For<IReshardingServices>();

        services.CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(rowsDeleted));

        return services;
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CleaningUpPhase(null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsCleaningUp()
    {
        var sut = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.CleaningUp);
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

    #region ExecuteAsync - Successful Cleanup

    [Fact]
    public async Task ExecuteAsync_SuccessfulCleanup_ReturnsCompletedAt100Percent()
    {
        // Arrange
        var sut = CreateSut();
        var services = CreateSuccessfulCleanupServices(rowsDeleted: 3000);
        var context = CreateCleanupContext(services: services, stepCount: 1);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(100.0);
        phaseResult.UpdatedProgress.CurrentPhase.ShouldBe(ReshardingPhase.CleaningUp);
    }

    #endregion

    #region ExecuteAsync - Cleanup Failure

    [Fact]
    public async Task ExecuteAsync_CleanupFails_ReturnsLeftWithCleanupFailed()
    {
        // Arrange
        var sut = CreateSut();
        var services = Substitute.For<IReshardingServices>();

        services.CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.CleanupFailed, "Delete failed")));

        var context = CreateCleanupContext(services: services, stepCount: 1);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CleanupFailed);
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var sut = CreateSut();
        var services = CreateSuccessfulCleanupServices();
        var context = CreateCleanupContext(services: services, stepCount: 2);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync(context, cts.Token));
    }

    #endregion

    #region ExecuteAsync - Multiple Steps

    [Fact]
    public async Task ExecuteAsync_MultipleSteps_CallsCleanupForEachStep()
    {
        // Arrange
        var sut = CreateSut();
        var services = CreateSuccessfulCleanupServices(rowsDeleted: 1000);
        var context = CreateCleanupContext(services: services, stepCount: 3);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.Received(3).CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Second Step Fails

    [Fact]
    public async Task ExecuteAsync_SecondStepFails_ReturnsLeftAfterFirstSucceeds()
    {
        // Arrange
        var sut = CreateSut();
        var services = Substitute.For<IReshardingServices>();
        var callCount = 0;

        services.CleanupSourceDataAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return Either<EncinaError, long>.Right(500);
                }

                return Either<EncinaError, long>.Left(
                    EncinaErrors.Create(ReshardingErrorCodes.CleanupFailed, "Step 2 failed"));
            });

        var context = CreateCleanupContext(services: services, stepCount: 2);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CleanupFailed);
        error.Message.ShouldContain("500 rows total");
    }

    #endregion
}
