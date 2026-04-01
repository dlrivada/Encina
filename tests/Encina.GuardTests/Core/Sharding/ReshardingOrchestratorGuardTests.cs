using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Routing;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ReshardingOrchestrator"/>.
/// Verifies null parameter handling for the constructor and public methods.
/// </summary>
public sealed class ReshardingOrchestratorGuardTests
{
    private readonly IShardRebalancer _rebalancer = Substitute.For<IShardRebalancer>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReshardingStateStore _stateStore = Substitute.For<IReshardingStateStore>();
    private readonly IReshardingServices _services = Substitute.For<IReshardingServices>();
    private readonly ReshardingOptions _options = new();
    private readonly ILogger<ReshardingOrchestrator> _logger = NullLogger<ReshardingOrchestrator>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws when rebalancer is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRebalancer_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(null!, _topologyProvider, _stateStore, _services, _options, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when topology provider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(_rebalancer, null!, _stateStore, _services, _options, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when state store is null.
    /// </summary>
    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(_rebalancer, _topologyProvider, null!, _services, _options, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when services is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(_rebalancer, _topologyProvider, _stateStore, null!, _options, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(_rebalancer, _topologyProvider, _stateStore, _services, null!, _logger));
    }

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingOrchestrator(_rebalancer, _topologyProvider, _stateStore, _services, _options, null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with all valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_Succeeds()
    {
        var orchestrator = CreateOrchestrator();
        orchestrator.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor accepts optional null TimeProvider (uses System default).
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemDefault()
    {
        var orchestrator = new ReshardingOrchestrator(
            _rebalancer, _topologyProvider, _stateStore, _services, _options, _logger, timeProvider: null);

        orchestrator.ShouldNotBeNull();
    }

    #endregion

    #region PlanAsync Guards

    /// <summary>
    /// Verifies that PlanAsync throws when request is null.
    /// </summary>
    [Fact]
    public async Task PlanAsync_NullRequest_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            orchestrator.PlanAsync(null!));
    }

    #endregion

    #region ExecuteAsync Guards

    /// <summary>
    /// Verifies that ExecuteAsync throws when plan is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullPlan_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            orchestrator.ExecuteAsync(null!, _options));
    }

    /// <summary>
    /// Verifies that ExecuteAsync throws when options is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullOptions_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();
        var plan = CreateTestPlan();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            orchestrator.ExecuteAsync(plan, null!));
    }

    #endregion

    #region RollbackAsync Guards

    /// <summary>
    /// Verifies that RollbackAsync throws when result is null.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_NullResult_ThrowsArgumentNullException()
    {
        var orchestrator = CreateOrchestrator();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            orchestrator.RollbackAsync(null!));
    }

    /// <summary>
    /// Verifies that RollbackAsync returns error when no rollback metadata is available.
    /// </summary>
    [Fact]
    public async Task RollbackAsync_NoRollbackMetadata_ReturnsLeftError()
    {
        var orchestrator = CreateOrchestrator();
        var result = new ReshardingResult(
            Guid.NewGuid(),
            ReshardingPhase.Completed,
            new List<PhaseHistoryEntry>(),
            RollbackMetadata: null);

        var rollbackResult = await orchestrator.RollbackAsync(result);

        rollbackResult.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetProgressAsync Guards

    /// <summary>
    /// Verifies that GetProgressAsync returns error for unknown resharding ID.
    /// </summary>
    [Fact]
    public async Task GetProgressAsync_UnknownReshardingId_ReturnsLeftError()
    {
        _stateStore.GetStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ReshardingState>.Left(
                EncinaErrors.Create("test", "not found")));

        var orchestrator = CreateOrchestrator();

        var result = await orchestrator.GetProgressAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Helpers

    private ReshardingOrchestrator CreateOrchestrator() =>
        new(_rebalancer, _topologyProvider, _stateStore, _services, _options, _logger);

    private static ReshardingPlan CreateTestPlan()
    {
        return new ReshardingPlan(
            Guid.NewGuid(),
            new List<ShardMigrationStep>(),
            new EstimatedResources(0, 0, TimeSpan.Zero));
    }

    #endregion
}
