using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="CuttingOverPhase"/>.
/// Verifies cutover predicate handling, replication lag checks, topology swap, timeout, and error handling.
/// </summary>
public sealed class CuttingOverPhaseTests
{
    #region Test Setup

    private static (CuttingOverPhase Phase, IShardTopologyProvider TopologyProvider) CreateSut()
    {
        var logger = Substitute.For<ILogger>();
        var topologyProvider = Substitute.For<IShardTopologyProvider>();
        topologyProvider.GetTopology().Returns(CreateTopology(3));
        var phase = new CuttingOverPhase(logger, topologyProvider);
        return (phase, topologyProvider);
    }

    private static PhaseContext CreateCutoverContext(
        IReshardingServices? services = null,
        Func<ReshardingPlan, CancellationToken, Task<bool>>? onCutoverStarting = null,
        TimeSpan? cutoverTimeout = null)
    {
        var resolvedServices = services ?? Substitute.For<IReshardingServices>();
        var id = Guid.NewGuid();
        var plan = CreatePlan(stepCount: 1, id: id);
        var options = CreateOptions();

        if (onCutoverStarting is not null)
        {
            options.OnCutoverStarting = onCutoverStarting;
        }

        if (cutoverTimeout.HasValue)
        {
            options.CutoverTimeout = cutoverTimeout.Value;
        }

        var progress = CreateProgress(id: id, phase: ReshardingPhase.CuttingOver, percent: 75.0);

        return new PhaseContext(id, plan, options, progress, null, resolvedServices);
    }

    private static IReshardingServices CreateSuccessfulServices()
    {
        var services = Substitute.For<IReshardingServices>();

        services.GetReplicationLagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TimeSpan>.Right(TimeSpan.Zero));

        services.SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        return services;
    }

    #endregion

    #region ExecuteAsync - Null Guard

    [Fact]
    public async Task ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var (sut, _) = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.ExecuteAsync(null!));
    }

    #endregion

    #region ExecuteAsync - No Predicate

    [Fact]
    public async Task ExecuteAsync_NoPredicate_ProceedsDirectly()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = CreateSuccessfulServices();
        var context = CreateCutoverContext(services: services, onCutoverStarting: null);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        await services.Received(1).SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Predicate Returns True

    [Fact]
    public async Task ExecuteAsync_PredicateReturnsTrue_ProceedsWithCutover()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = CreateSuccessfulServices();
        var context = CreateCutoverContext(
            services: services,
            onCutoverStarting: (_, _) => Task.FromResult(true));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        await services.Received(1).SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Predicate Returns False

    [Fact]
    public async Task ExecuteAsync_PredicateReturnsFalse_ReturnsAbortedStatus()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = CreateSuccessfulServices();
        var context = CreateCutoverContext(
            services: services,
            onCutoverStarting: (_, _) => Task.FromResult(false));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Aborted);
        await services.DidNotReceive().SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Predicate Throws

    [Fact]
    public async Task ExecuteAsync_PredicateThrows_ReturnsLeftWithCutoverFailed()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = CreateSuccessfulServices();
        var context = CreateCutoverContext(
            services: services,
            onCutoverStarting: (_, _) =>
                throw new InvalidOperationException("Predicate explosion"));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CutoverFailed);
    }

    #endregion

    #region ExecuteAsync - Replication Lag Check Fails

    [Fact]
    public async Task ExecuteAsync_ReplicationLagCheckFails_ReturnsLeftWithCutoverFailed()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = Substitute.For<IReshardingServices>();

        services.GetReplicationLagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TimeSpan>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.CutoverFailed, "Lag check failed")));

        var context = CreateCutoverContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CutoverFailed);
    }

    #endregion

    #region ExecuteAsync - Topology Swap Fails

    [Fact]
    public async Task ExecuteAsync_TopologySwapFails_ReturnsLeftWithCutoverFailed()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = Substitute.For<IReshardingServices>();

        services.GetReplicationLagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, TimeSpan>.Right(TimeSpan.Zero));

        services.SwapTopologyAsync(
            Arg.Any<ShardTopology>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(
                EncinaErrors.Create(ReshardingErrorCodes.CutoverFailed, "Swap failed")));

        var context = CreateCutoverContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CutoverFailed);
    }

    #endregion

    #region ExecuteAsync - Topology Swap Succeeds At 90 Percent

    [Fact]
    public async Task ExecuteAsync_TopologySwapSucceeds_ReturnsCompletedAt90Percent()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = CreateSuccessfulServices();
        var context = CreateCutoverContext(services: services);

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsRight.ShouldBeTrue();
        var phaseResult = ExtractRight(result);
        phaseResult.Status.ShouldBe(PhaseStatus.Completed);
        phaseResult.UpdatedProgress.OverallPercentComplete.ShouldBe(90.0);
    }

    #endregion

    #region ExecuteAsync - Timeout

    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsLeftWithCutoverTimeout()
    {
        // Arrange
        var (sut, _) = CreateSut();
        var services = Substitute.For<IReshardingServices>();

        // Make GetReplicationLagAsync delay long enough to trigger the 1ms timeout
        services.GetReplicationLagAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return Either<EncinaError, TimeSpan>.Right(TimeSpan.Zero);
            });

        var context = CreateCutoverContext(
            services: services,
            cutoverTimeout: TimeSpan.FromMilliseconds(1));

        // Act
        var result = await sut.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.CutoverTimeout);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var topologyProvider = Substitute.For<IShardTopologyProvider>();

        Should.Throw<ArgumentNullException>(() =>
            new CuttingOverPhase(null!, topologyProvider));
    }

    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
    {
        var logger = Substitute.For<ILogger>();

        Should.Throw<ArgumentNullException>(() =>
            new CuttingOverPhase(logger, null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsCuttingOverPhase()
    {
        var (sut, _) = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.CuttingOver);
    }

    #endregion
}
