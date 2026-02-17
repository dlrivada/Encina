using Encina.Sharding;
using Encina.Sharding.Resharding;
using Encina.Sharding.Resharding.Phases;
using Encina.Sharding.Routing;
using LanguageExt;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Phases;

/// <summary>
/// Unit tests for <see cref="PlanningPhase"/>.
/// Verifies plan generation from topology differences, row estimation, and error handling.
/// </summary>
public sealed class PlanningPhaseTests
{
    #region Test Setup

    private static (PlanningPhase Phase, IShardRebalancer Rebalancer, IReshardingServices Services) CreateSut()
    {
        var rebalancer = Substitute.For<IShardRebalancer>();
        var services = Substitute.For<IReshardingServices>();
        var phase = new PlanningPhase(rebalancer, services);
        return (phase, rebalancer, services);
    }

    private static ReshardingRequest CreateRequest(
        int oldShardCount = 2,
        int newShardCount = 3)
    {
        return new ReshardingRequest(
            CreateTopology(oldShardCount),
            CreateTopology(newShardCount));
    }

    #endregion

    #region GeneratePlanAsync

    [Fact]
    public async Task GeneratePlanAsync_ValidRequest_ReturnsRightWithPlan()
    {
        // Arrange
        var (sut, rebalancer, services) = CreateSut();
        var request = CreateRequest();

        var affectedRanges = new List<AffectedKeyRange>
        {
            new(0, 500, "shard-0", "shard-2"),
            new(500, 1000, "shard-1", "shard-2"),
        };

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(), Arg.Any<ShardTopology>())
            .Returns(affectedRanges);

        services.EstimateRowCountAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Right(1000L));

        // Act
        var result = await sut.GeneratePlanAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var plan = ExtractRight(result);
        plan.ShouldNotBeNull();
        plan.Steps.Count.ShouldBe(2);
        plan.Steps[0].SourceShardId.ShouldBe("shard-0");
        plan.Steps[0].TargetShardId.ShouldBe("shard-2");
        plan.Steps[1].SourceShardId.ShouldBe("shard-1");
        plan.Steps[1].TargetShardId.ShouldBe("shard-2");
        plan.Estimate.TotalRows.ShouldBe(2000L);
    }

    [Fact]
    public async Task GeneratePlanAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var (sut, _, _) = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            sut.GeneratePlanAsync(null!));
    }

    [Fact]
    public async Task GeneratePlanAsync_IdenticalTopologies_ReturnsLeftTopologiesIdentical()
    {
        // Arrange
        var (sut, rebalancer, _) = CreateSut();
        var request = CreateRequest();

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(), Arg.Any<ShardTopology>())
            .Returns(new List<AffectedKeyRange>());

        // Act
        var result = await sut.GeneratePlanAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.TopologiesIdentical);
    }

    [Fact]
    public async Task GeneratePlanAsync_RebalancerThrows_ReturnsLeftPlanGenerationFailed()
    {
        // Arrange
        var (sut, rebalancer, _) = CreateSut();
        var request = CreateRequest();

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(), Arg.Any<ShardTopology>())
            .Returns<IReadOnlyList<AffectedKeyRange>>(_ =>
                throw new InvalidOperationException("Rebalancer failure"));

        // Act
        var result = await sut.GeneratePlanAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.PlanGenerationFailed);
    }

    [Fact]
    public async Task GeneratePlanAsync_EstimateFailsForStep_DefaultsToZeroRows()
    {
        // Arrange
        var (sut, rebalancer, services) = CreateSut();
        var request = CreateRequest();

        var affectedRanges = new List<AffectedKeyRange>
        {
            new(0, 500, "shard-0", "shard-2"),
        };

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(), Arg.Any<ShardTopology>())
            .Returns(affectedRanges);

        services.EstimateRowCountAsync(
            Arg.Any<string>(), Arg.Any<KeyRange>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, long>.Left(
                EncinaErrors.Create("estimate.failed", "Estimation failed")));

        // Act
        var result = await sut.GeneratePlanAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        var plan = ExtractRight(result);
        plan.Steps.Count.ShouldBe(1);
        plan.Steps[0].EstimatedRows.ShouldBe(0L);
    }

    [Fact]
    public async Task GeneratePlanAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var (sut, rebalancer, services) = CreateSut();
        var request = CreateRequest();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var affectedRanges = new List<AffectedKeyRange>
        {
            new(0, 500, "shard-0", "shard-2"),
        };

        rebalancer.CalculateAffectedKeyRanges(
            Arg.Any<ShardTopology>(), Arg.Any<ShardTopology>())
            .Returns(affectedRanges);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            sut.GeneratePlanAsync(request, cts.Token));
    }

    #endregion

    #region IReshardingPhase.ExecuteAsync

    [Fact]
    public async Task ExecuteAsync_AlwaysReturnsLeftWithInvalidPhaseTransition()
    {
        // Arrange
        var (sut, _, services) = CreateSut();
        IReshardingPhase phase = sut;
        var context = CreatePhaseContext(services: services);

        // Act
        var result = await phase.ExecuteAsync(context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = ExtractLeft(result);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ReshardingErrorCodes.InvalidPhaseTransition);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullRebalancer_ThrowsArgumentNullException()
    {
        var services = Substitute.For<IReshardingServices>();

        Should.Throw<ArgumentNullException>(() =>
            new PlanningPhase(null!, services));
    }

    [Fact]
    public void Constructor_NullServices_ThrowsArgumentNullException()
    {
        var rebalancer = Substitute.For<IShardRebalancer>();

        Should.Throw<ArgumentNullException>(() =>
            new PlanningPhase(rebalancer, null!));
    }

    #endregion

    #region Phase Property

    [Fact]
    public void Phase_ReturnsPlanningPhase()
    {
        var (sut, _, _) = CreateSut();

        sut.Phase.ShouldBe(ReshardingPhase.Planning);
    }

    #endregion
}
