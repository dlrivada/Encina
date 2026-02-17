using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="ReplicatingPhase"/>.
/// Verifies CDC replication convergence, checkpoint handling, progress, and error paths.
/// </summary>
public sealed class ReplicatingPhaseTests
{
    #region Test Setup

    private static ReplicatingPhase CreateSut()
    {
        var logger = Substitute.For<ILogger>();
        return new ReplicatingPhase(logger);
    }

    private static PhaseContext CreateReplicateContext(
        IReshardingServices? services = null,
        int stepCount = 1,
        ReshardingCheckpoint? checkpoint = null,
        TimeSpan? cdcLagThreshold = null)
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

        if (cdcLagThreshold.HasValue)
        {
            options.CdcLagThreshold = cdcLagThreshold.Value;
        }

        var progress = CreateProgress(id: id, phase: ReshardingPhase.Replicating);

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

    #region ExecuteAsync - Single Pass Convergence

    [Fact]
    public async Task ExecuteAsync_LagBelowThreshold_CompletesAfterSinglePass()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0L, "pos-100", TimeSpan.FromSeconds(1))));

        var context = CreateReplicateContext(
            services: services,
            cdcLagThreshold: TimeSpan.FromSeconds(5));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        phaseResult.UpdatedProgress.CurrentPhase.ShouldBe(ReshardingPhase.Replicating);
    }

    #endregion

    #region ExecuteAsync - Multiple Passes Until Converged

    [Fact]
    public async Task ExecuteAsync_MultiplePassesNeeded_IteratesUntilConverged()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();
        var callCount = 0;

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    // First pass: high lag, rows replicated
                    return Either<EncinaError, ReplicationResult>.Right(
                        new ReplicationResult(100L, "pos-100", TimeSpan.FromSeconds(30)));
                }

                // Second pass: low lag, zero rows
                return Either<EncinaError, ReplicationResult>.Right(
                    new ReplicationResult(0L, "pos-200", TimeSpan.FromSeconds(1)));
            });

        var context = CreateReplicateContext(
            services: services,
            cdcLagThreshold: TimeSpan.FromSeconds(5));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        callCount.ShouldBe(2);
    }

    #endregion

    #region ExecuteAsync - Replication Fails

    [Fact]
    public async Task ExecuteAsync_ReplicationFails_ReturnsLeftWithReplicationFailed()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.ReplicationFailed, "CDC error")));

        var context = CreateReplicateContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.ReplicationFailed);
    }

    #endregion

    #region ExecuteAsync - Resume From CDC Position

    [Fact]
    public async Task ExecuteAsync_ResumesFromCdcPosition_UsesCheckpointPosition()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Is<string?>("pos-100"), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0L, "pos-200", TimeSpan.FromSeconds(1))));

        var checkpoint = new ReshardingCheckpoint(null, "pos-100");
        var context = CreateReplicateContext(
            services: services,
            checkpoint: checkpoint,
            cdcLagThreshold: TimeSpan.FromSeconds(5));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        await services.Received(1).ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Is<string?>("pos-100"), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Updates CDC Position In Checkpoint

    [Fact]
    public async Task ExecuteAsync_UpdatesCdcPositionInCheckpoint()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0L, "pos-200", TimeSpan.FromSeconds(1))));

        var context = CreateReplicateContext(
            services: services,
            cdcLagThreshold: TimeSpan.FromSeconds(5));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.UpdatedCheckpoint.ShouldNotBeNull();
        phaseResult.UpdatedCheckpoint!.CdcPosition.ShouldBe("pos-200");
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_CancellationDuringReplication_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Either<EncinaError, ReplicationResult>.Right(
                    new ReplicationResult(100L, "pos-100", TimeSpan.FromSeconds(30)));
            });

        var context = CreateReplicateContext(services: services);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.ExecuteAsync(context, cts.Token));
    }

    #endregion

    #region ExecuteAsync - Progress Percentage

    [Fact]
    public async Task ExecuteAsync_CompletedSuccessfully_SetsOverallPercentTo60()
    {
        // Arrange
        var services = Substitute.For<IReshardingServices>();
        var sut = CreateSut();

        services.ReplicateChangesAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<KeyRange>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReplicationResult>.Right(
                new ReplicationResult(0L, "pos-100", TimeSpan.FromSeconds(1))));

        var context = CreateReplicateContext(
            services: services,
            cdcLagThreshold: TimeSpan.FromSeconds(5));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(60.0);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReplicatingPhase(null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsReplicatingPhase()
    {
        var sut = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.Replicating);
    }

    #endregion
}
