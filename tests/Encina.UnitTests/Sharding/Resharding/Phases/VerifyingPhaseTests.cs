using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="VerifyingPhase"/>.
/// Verifies data consistency checking, mismatch detection, sequential step verification, and error handling.
/// </summary>
public sealed class VerifyingPhaseTests
{
    #region Test Setup

    private static VerifyingPhase CreateSut()
    {
        var logger = Substitute.For<ILogger>();
        return new VerifyingPhase(logger);
    }

    private static PhaseContext CreateVerifyContext(
        IReshardingServices? services = null,
        int stepCount = 1)
    {
        var resolvedServices = services ?? Substitute.For<IReshardingServices>();
        var id = Guid.NewGuid();

        var steps = Enumerable.Range(0, stepCount)
            .Select(i => CreateMigrationStep(
                source: $"shard-{i}",
                target: $"shard-{i + stepCount}",
                estimatedRows: 1000))
            .ToList();

        var totalRows = steps.Sum(s => s.EstimatedRows);
        var plan = new ReshardingPlan(id, steps, CreateEstimate(totalRows, totalRows * 256));
        var options = CreateOptions();
        var progress = CreateProgress(id: id, phase: ReshardingPhase.Verifying);

        return new PhaseContext(id, plan, options, progress, null, resolvedServices);
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

    #region ExecuteAsync - All Steps Consistent

    [Fact]
    public async Task ExecuteAsync_AllStepsConsistent_ReturnsCompleted()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(true, 1000, 1000)));

        var context = CreateVerifyContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);

        var stepKey = $"shard-0\u2192shard-1";
        phaseResult.UpdatedProgress.PerStepProgress.ShouldContainKey(stepKey);
        phaseResult.UpdatedProgress.PerStepProgress[stepKey].IsVerified.ShouldBeTrue();
    }

    #endregion

    #region ExecuteAsync - Verification Infrastructure Fails

    [Fact]
    public async Task ExecuteAsync_VerificationInfrastructureFails_ReturnsLeftWithVerificationFailed()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.VerificationFailed, "Infrastructure failure")));

        var context = CreateVerifyContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.VerificationFailed);
    }

    #endregion

    #region ExecuteAsync - Data Mismatch

    [Fact]
    public async Task ExecuteAsync_DataMismatch_ReturnsLeftWithVerificationFailedAndDetails()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(false, 1000, 999, "Checksum mismatch in block 42")));

        var context = CreateVerifyContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.VerificationFailed);
        error.Message.ShouldContain("1000");
        error.Message.ShouldContain("999");
    }

    #endregion

    #region ExecuteAsync - Multiple Steps Verified Sequentially

    [Fact]
    public async Task ExecuteAsync_MultipleSteps_VerifiesEachSequentially()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(true, 1000, 1000)));

        var context = CreateVerifyContext(services: services, stepCount: 2);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.Received(2).VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_CancellationBeforeStep_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = CreateVerifyContext(services: services);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync(context, cts.Token));
    }

    #endregion

    #region ExecuteAsync - Progress Percentage

    [Fact]
    public async Task ExecuteAsync_CompletedSuccessfully_SetsOverallPercentTo75()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.VerifyDataConsistencyAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<VerificationMode>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, VerificationResult>.Right(
                new VerificationResult(true, 1000, 1000)));

        var context = CreateVerifyContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(75.0);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new VerifyingPhase(null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsVerifyingPhase()
    {
        var sut = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.Verifying);
    }

    #endregion
}
