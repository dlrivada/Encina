using Encina.OpenTelemetry.Migrations;
using Encina.Sharding.Migrations;
using LanguageExt;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Migrations;

/// <summary>
/// Unit tests for <see cref="SchemaDriftHealthCheck"/>.
/// </summary>
public sealed class SchemaDriftHealthCheckTests
{
    private readonly IShardedMigrationCoordinator _coordinator;
    private readonly SchemaDriftHealthCheckOptions _options;

    public SchemaDriftHealthCheckTests()
    {
        _coordinator = Substitute.For<IShardedMigrationCoordinator>();
        _options = new SchemaDriftHealthCheckOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            CriticalTables = []
        };
    }

    [Fact]
    public void Constructor_NullCoordinator_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new SchemaDriftHealthCheck(null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new SchemaDriftHealthCheck(_coordinator, null!));
    }

    [Fact]
    public async Task CheckHealthAsync_NoDrift_ReturnsHealthy()
    {
        // Arrange
        var report = new SchemaDriftReport(
            Diffs: [],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        _coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, SchemaDriftReport>.Right(report));

        var healthCheck = new SchemaDriftHealthCheck(_coordinator, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("No schema drift");
    }

    [Fact]
    public async Task CheckHealthAsync_DriftOnNonCriticalTables_ReturnsDegraded()
    {
        // Arrange
        var report = new SchemaDriftReport(
            Diffs:
            [
                new ShardSchemaDiff(
                    "shard-1",
                    "shard-baseline",
                    [new TableDiff("logs", TableDiffType.Modified)])
            ],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        _coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, SchemaDriftReport>.Right(report));

        var healthCheck = new SchemaDriftHealthCheck(_coordinator, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("No critical tables affected");
    }

    [Fact]
    public async Task CheckHealthAsync_DriftOnCriticalTables_ReturnsUnhealthy()
    {
        // Arrange
        var optionsWithCritical = new SchemaDriftHealthCheckOptions
        {
            CriticalTables = ["orders", "payments"]
        };

        var report = new SchemaDriftReport(
            Diffs:
            [
                new ShardSchemaDiff(
                    "shard-1",
                    "shard-baseline",
                    [new TableDiff("orders", TableDiffType.Modified)])
            ],
            DetectedAtUtc: DateTimeOffset.UtcNow);

        _coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, SchemaDriftReport>.Right(report));

        var healthCheck = new SchemaDriftHealthCheck(_coordinator, optionsWithCritical);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("critical table");
    }

    [Fact]
    public async Task CheckHealthAsync_DetectionFails_ReturnsUnhealthy()
    {
        // Arrange
        _coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, SchemaDriftReport>.Left(
                EncinaError.New("Connection failed")));

        var healthCheck = new SchemaDriftHealthCheck(_coordinator, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Schema drift detection failed");
    }

    [Fact]
    public async Task CheckHealthAsync_Timeout_ReturnsUnhealthy()
    {
        // Arrange
        var shortTimeoutOptions = new SchemaDriftHealthCheckOptions
        {
            Timeout = TimeSpan.FromMilliseconds(1)
        };

        _coordinator.DetectDriftAsync(Arg.Any<DriftDetectionOptions>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(5000, callInfo.Arg<CancellationToken>());
                return Either<EncinaError, SchemaDriftReport>.Right(
                    new SchemaDriftReport([], DateTimeOffset.UtcNow));
            });

        var healthCheck = new SchemaDriftHealthCheck(_coordinator, shortTimeoutOptions);

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }
}
