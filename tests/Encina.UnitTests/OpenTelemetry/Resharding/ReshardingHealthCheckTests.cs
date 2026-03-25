using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Resharding;
using LanguageExt;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingHealthCheck"/>.
/// </summary>
public sealed class ReshardingHealthCheckTests
{
    private readonly IReshardingStateStore _stateStore;
    private readonly ReshardingHealthCheckOptions _options;

    public ReshardingHealthCheckTests()
    {
        _stateStore = Substitute.For<IReshardingStateStore>();
        _options = new ReshardingHealthCheckOptions
        {
            MaxReshardingDuration = TimeSpan.FromHours(2),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static ReshardingState CreateState(
        Guid? id = null,
        ReshardingPhase phase = ReshardingPhase.Copying,
        DateTime? startedAtUtc = null)
    {
        var plan = new ReshardingPlan(
            Guid.NewGuid(),
            [],
            new EstimatedResources(0, 0, TimeSpan.Zero));

        var progress = new ReshardingProgress(
            Guid.NewGuid(),
            phase,
            0.0,
            new Dictionary<string, ShardMigrationProgress>());

        return new ReshardingState(
            id ?? Guid.NewGuid(),
            phase,
            plan,
            progress,
            null,
            startedAtUtc ?? DateTime.UtcNow,
            null);
    }

    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReshardingHealthCheck(_stateStore, null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NoActiveResharding_ReturnsHealthy()
    {
        // Arrange
        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                (IReadOnlyList<ReshardingState>)[]));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["activeCount"].ShouldBe(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ActiveInProgressResharding_ReturnsDegraded()
    {
        // Arrange
        var state = CreateState(
            phase: ReshardingPhase.Copying,
            startedAtUtc: DateTime.UtcNow.AddMinutes(-30));

        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                (IReadOnlyList<ReshardingState>)[state]));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["activeCount"].ShouldBe(1);
        result.Data["inProgressCount"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_FailedResharding_ReturnsUnhealthy()
    {
        // Arrange
        var state = CreateState(
            phase: ReshardingPhase.Failed,
            startedAtUtc: DateTime.UtcNow.AddMinutes(-60));

        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                (IReadOnlyList<ReshardingState>)[state]));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["failedCount"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_OverdueResharding_ReturnsUnhealthy()
    {
        // Arrange
        var state = CreateState(
            phase: ReshardingPhase.Copying,
            startedAtUtc: DateTime.UtcNow.AddHours(-3)); // exceeds 2h max

        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                (IReadOnlyList<ReshardingState>)[state]));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["overdueCount"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_StoreReturnsError_ReturnsUnhealthy()
    {
        // Arrange
        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Left(
                EncinaError.New("Database connection failed")));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed to query resharding state");
    }

    [Fact]
    public async Task CheckHealthAsync_Timeout_ReturnsUnhealthy()
    {
        // Arrange
        var shortTimeoutOptions = new ReshardingHealthCheckOptions
        {
            Timeout = TimeSpan.FromMilliseconds(1)
        };

        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(5000, callInfo.Arg<CancellationToken>());
                return Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                    (IReadOnlyList<ReshardingState>)[]);
            });

        var healthCheck = new ReshardingHealthCheck(_stateStore, shortTimeoutOptions);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }

    [Fact]
    public async Task CheckHealthAsync_MixedFailedAndOverdue_ReturnsUnhealthyWithBothReasons()
    {
        // Arrange
        var failedState = CreateState(
            phase: ReshardingPhase.Failed,
            startedAtUtc: DateTime.UtcNow.AddMinutes(-30));

        var overdueState = CreateState(
            phase: ReshardingPhase.Replicating,
            startedAtUtc: DateTime.UtcNow.AddHours(-5));

        _stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<ReshardingState>>.Right(
                (IReadOnlyList<ReshardingState>)[failedState, overdueState]));

        var healthCheck = new ReshardingHealthCheck(_stateStore, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["failedCount"].ShouldBe(1);
        result.Data["overdueCount"].ShouldBe(1);
    }
}
