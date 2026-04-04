using Encina.OpenTelemetry;
using Encina.OpenTelemetry.Migrations;
using Encina.OpenTelemetry.Resharding;
using Encina.Sharding.Migrations;
using Encina.Sharding.Resharding;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests for health check classes: <see cref="ReshardingHealthCheck"/> and
/// <see cref="SchemaDriftHealthCheck"/>. Tests cover constructor null guards
/// and method invocation with mocked dependencies.
/// </summary>
public sealed class HealthCheckGuardTests
{
    #region ReshardingHealthCheck Constructor Guards

    [Fact]
    public void ReshardingHealthCheck_NullStateStore_ThrowsArgumentNullException()
    {
        var act = () => new ReshardingHealthCheck(
            stateStore: null!,
            options: new ReshardingHealthCheckOptions());

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("stateStore");
    }

    [Fact]
    public void ReshardingHealthCheck_NullOptions_ThrowsArgumentNullException()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();

        var act = () => new ReshardingHealthCheck(
            stateStore: stateStore,
            options: null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void ReshardingHealthCheck_ValidParams_DoesNotThrow()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();

        Should.NotThrow(() => new ReshardingHealthCheck(
            stateStore,
            new ReshardingHealthCheckOptions()));
    }

    [Fact]
    public void ReshardingHealthCheck_WithTimeProvider_DoesNotThrow()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();

        Should.NotThrow(() => new ReshardingHealthCheck(
            stateStore,
            new ReshardingHealthCheckOptions(),
            timeProvider: TimeProvider.System));
    }

    #endregion

    #region ReshardingHealthCheck Method

    [Fact]
    public async Task ReshardingHealthCheck_CheckHealthAsync_WhenNoActiveResharding_ReturnsHealthy()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IReadOnlyList<ReshardingState>>(
                new List<ReshardingState>()));

        var sut = new ReshardingHealthCheck(stateStore, new ReshardingHealthCheckOptions());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("resharding", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task ReshardingHealthCheck_CheckHealthAsync_WhenStoreReturnsError_ReturnsUnhealthy()
    {
        var stateStore = Substitute.For<IReshardingStateStore>();
        stateStore.GetActiveReshardingsAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Left<EncinaError, IReadOnlyList<ReshardingState>>(
                EncinaError.New("db-failure")));

        var sut = new ReshardingHealthCheck(stateStore, new ReshardingHealthCheckOptions());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("resharding", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region SchemaDriftHealthCheck Constructor Guards

    [Fact]
    public void SchemaDriftHealthCheck_NullCoordinator_ThrowsArgumentNullException()
    {
        var act = () => new SchemaDriftHealthCheck(
            coordinator: null!,
            options: new SchemaDriftHealthCheckOptions());

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("coordinator");
    }

    [Fact]
    public void SchemaDriftHealthCheck_NullOptions_ThrowsArgumentNullException()
    {
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();

        var act = () => new SchemaDriftHealthCheck(
            coordinator: coordinator,
            options: null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void SchemaDriftHealthCheck_ValidParams_DoesNotThrow()
    {
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();

        Should.NotThrow(() => new SchemaDriftHealthCheck(
            coordinator,
            new SchemaDriftHealthCheckOptions()));
    }

    #endregion

    #region SchemaDriftHealthCheck Method

    [Fact]
    public async Task SchemaDriftHealthCheck_CheckHealthAsync_WhenNoDrift_ReturnsHealthy()
    {
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();
        var report = new SchemaDriftReport(
            Diffs: [],
            DetectedAtUtc: DateTimeOffset.UtcNow);
        coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, SchemaDriftReport>(report));

        var sut = new SchemaDriftHealthCheck(coordinator, new SchemaDriftHealthCheckOptions());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("schema-drift", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task SchemaDriftHealthCheck_CheckHealthAsync_WhenCoordinatorReturnsError_ReturnsUnhealthy()
    {
        var coordinator = Substitute.For<IShardedMigrationCoordinator>();
        coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Prelude.Left<EncinaError, SchemaDriftReport>(
                EncinaError.New("coordinator-failure")));

        var sut = new SchemaDriftHealthCheck(coordinator, new SchemaDriftHealthCheckOptions());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("schema-drift", sut, null, null)
        };

        var result = await sut.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region Options Defaults

    [Fact]
    public void ReshardingHealthCheckOptions_DefaultValues()
    {
        var options = new ReshardingHealthCheckOptions();

        options.MaxReshardingDuration.ShouldBe(TimeSpan.FromHours(2));
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void SchemaDriftHealthCheckOptions_DefaultValues()
    {
        var options = new SchemaDriftHealthCheckOptions();

        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.BaselineShardId.ShouldBeNull();
        options.CriticalTables.ShouldBeEmpty();
    }

    [Fact]
    public void EncinaOpenTelemetryOptions_DefaultValues()
    {
        var options = new EncinaOpenTelemetryOptions();

        options.ServiceName.ShouldBe("Encina");
        options.ServiceVersion.ShouldBe("1.0.0");
        options.EnableMessagingEnrichers.ShouldBeTrue();
    }

    #endregion
}
