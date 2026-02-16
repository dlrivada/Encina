using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;
using LanguageExt;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using static Encina.UnitTests.Sharding.Resharding.ReshardingTestBuilders;

namespace Encina.UnitTests.Sharding.Resharding.Observability;

/// <summary>
/// Unit tests for <see cref="ReshardingHealthCheck"/>.
/// Validates health status classification based on active resharding state,
/// error handling for state store failures, and timeout behavior.
/// </summary>
public sealed class ReshardingHealthCheckTests
{
    #region Constructor Validation

    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ReshardingHealthCheckOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(null!, options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(stateStore, null!));
    }

    #endregion

    #region CheckHealthAsync - Healthy

    [Fact]
    public async Task CheckHealthAsync_NoActiveResharding_ReturnsHealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                Array.Empty<ReshardingState>()));

        var options = new ReshardingHealthCheckOptions();
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("No active resharding operations.");
        result.Data.ShouldContainKey("activeCount");
        ((int)result.Data["activeCount"]).ShouldBe(0);
    }

    #endregion

    #region CheckHealthAsync - Degraded

    [Fact]
    public async Task CheckHealthAsync_ActiveInProgress_ReturnsDegraded()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        var activeState = CreateState(phase: ReshardingPhase.Copying);
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                new List<ReshardingState> { activeState }));

        var options = new ReshardingHealthCheckOptions();
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("1 resharding operation(s) in progress");
        result.Data.ShouldContainKey("activeCount");
        ((int)result.Data["activeCount"]).ShouldBe(1);
        ((int)result.Data["inProgressCount"]).ShouldBe(1);
        ((int)result.Data["failedCount"]).ShouldBe(0);
        ((int)result.Data["overdueCount"]).ShouldBe(0);
    }

    #endregion

    #region CheckHealthAsync - Unhealthy

    [Fact]
    public async Task CheckHealthAsync_FailedState_ReturnsUnhealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        var failedState = CreateState(phase: ReshardingPhase.Failed);
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                new List<ReshardingState> { failedState }));

        var options = new ReshardingHealthCheckOptions();
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("1 failed without rollback");
        result.Data.ShouldContainKey("failedCount");
        ((int)result.Data["failedCount"]).ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_OverdueState_ReturnsUnhealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();

        // Create a state that started 3 hours ago (exceeds default 2h max)
        var overdueId = Guid.NewGuid();
        var plan = CreatePlan(id: overdueId);
        var progress = CreateProgress(id: overdueId, phase: ReshardingPhase.Copying);
        var overdueState = new ReshardingState(
            overdueId,
            ReshardingPhase.Copying,
            plan,
            progress,
            LastCompletedPhase: null,
            StartedAtUtc: DateTime.UtcNow.AddHours(-3),
            Checkpoint: null);

        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                new List<ReshardingState> { overdueState }));

        var options = new ReshardingHealthCheckOptions
        {
            MaxReshardingDuration = TimeSpan.FromHours(2)
        };
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("exceeded max duration");
        result.Data.ShouldContainKey("overdueCount");
        ((int)result.Data["overdueCount"]).ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_StateStoreError_ReturnsUnhealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Left(
                EncinaError.New("Database connection failed")));

        var options = new ReshardingHealthCheckOptions();
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed to query resharding state");
        result.Description!.ShouldContain("Database connection failed");
        result.Data.ShouldContainKey("error");
    }

    [Fact]
    public async Task CheckHealthAsync_Timeout_ReturnsUnhealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var ct = callInfo.ArgAt<CancellationToken>(0);
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                    Array.Empty<ReshardingState>());
            });

        var options = new ReshardingHealthCheckOptions
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }

    #endregion

    #region CheckHealthAsync - Mixed States

    [Fact]
    public async Task CheckHealthAsync_MixedFailedAndInProgress_ReturnsUnhealthy()
    {
        // Arrange
        var stateStore = Substitute.For<IReshardingStateStore>();
        var inProgressState = CreateState(phase: ReshardingPhase.Copying);
        var failedState = CreateState(phase: ReshardingPhase.Failed);
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                new List<ReshardingState> { inProgressState, failedState }));

        var options = new ReshardingHealthCheckOptions();
        var sut = new ReshardingHealthCheck(stateStore, options);
        var context = new HealthCheckContext();

        // Act
        var result = await sut.CheckHealthAsync(context);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("1 failed without rollback");
        ((int)result.Data["activeCount"]).ShouldBe(2);
        ((int)result.Data["inProgressCount"]).ShouldBe(1);
        ((int)result.Data["failedCount"]).ShouldBe(1);
    }

    #endregion
}
