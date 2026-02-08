using Encina.Database;
using Encina.Messaging.Health;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Unit tests for <see cref="DatabasePoolHealthCheck"/> and <see cref="DatabasePoolHealthCheckOptions"/>.
/// </summary>
public sealed class DatabasePoolHealthCheckTests
{
    #region Constructor

    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DatabasePoolHealthCheck(null!));
    }

    [Fact]
    public void Constructor_WithDefaults_SetsNameAndTags()
    {
        // Arrange
        var monitor = Substitute.For<IDatabaseHealthMonitor>();

        // Act
        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Assert
        healthCheck.Name.ShouldBe(DatabasePoolHealthCheck.DefaultName);
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("pool");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void Constructor_WithCustomOptions_SetsNameAndTags()
    {
        // Arrange
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        var options = new DatabasePoolHealthCheckOptions
        {
            Name = "custom-pool",
            Tags = ["custom-tag"]
        };

        // Act
        var healthCheck = new DatabasePoolHealthCheck(monitor, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-pool");
        healthCheck.Tags.ShouldContain("custom-tag");
    }

    #endregion

    #region CheckHealthAsync — Circuit Breaker

    [Fact]
    public async Task CheckHealthAsync_CircuitOpen_ReturnsUnhealthy()
    {
        // Arrange
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(true);
        monitor.ProviderName.Returns("test-provider");

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("Circuit breaker is open");
    }

    #endregion

    #region CheckHealthAsync — Monitor Health

    [Fact]
    public async Task CheckHealthAsync_MonitorUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(false);
        monitor.ProviderName.Returns("test-provider");
        monitor.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(DatabaseHealthResult.Unhealthy("Database down"));

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Database down");
    }

    #endregion

    #region CheckHealthAsync — Pool Utilization Thresholds

    [Fact]
    public async Task CheckHealthAsync_UtilizationBelowDegraded_ReturnsHealthy()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(poolUtilization: 0.5);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_UtilizationAtDegradedThreshold_ReturnsDegraded()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(poolUtilization: 0.8);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("degraded");
    }

    [Fact]
    public async Task CheckHealthAsync_UtilizationAboveDegradedBelowUnhealthy_ReturnsDegraded()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(poolUtilization: 0.9);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_UtilizationAtUnhealthyThreshold_ReturnsUnhealthy()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(poolUtilization: 0.95);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain("unhealthy");
    }

    [Fact]
    public async Task CheckHealthAsync_UtilizationAboveUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(poolUtilization: 1.0);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region CheckHealthAsync — Custom Thresholds

    [Fact]
    public async Task CheckHealthAsync_CustomThresholds_UsesConfiguredValues()
    {
        // Arrange — 70% utilization should be degraded with 0.7 threshold
        var monitor = CreateHealthyMonitor(poolUtilization: 0.7);

        var options = new DatabasePoolHealthCheckOptions
        {
            DegradedThreshold = 0.7,
            UnhealthyThreshold = 0.9
        };
        var healthCheck = new DatabasePoolHealthCheck(monitor, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_CustomThresholds_HealthyBelowDegraded()
    {
        // Arrange — 60% utilization should be healthy with 0.7 threshold
        var monitor = CreateHealthyMonitor(poolUtilization: 0.6);

        var options = new DatabasePoolHealthCheckOptions
        {
            DegradedThreshold = 0.7,
            UnhealthyThreshold = 0.9
        };
        var healthCheck = new DatabasePoolHealthCheck(monitor, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    #endregion

    #region CheckHealthAsync — Data Dictionary

    [Fact]
    public async Task CheckHealthAsync_ReturnsPoolDataInResult()
    {
        // Arrange
        var monitor = CreateHealthyMonitor(
            active: 5, idle: 10, total: 15, pending: 2, maxPool: 100);

        var healthCheck = new DatabasePoolHealthCheck(monitor);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldContainKey("provider");
        result.Data.ShouldContainKey("activeConnections");
        result.Data.ShouldContainKey("idleConnections");
        result.Data.ShouldContainKey("totalConnections");
        result.Data.ShouldContainKey("pendingRequests");
        result.Data.ShouldContainKey("maxPoolSize");
        result.Data.ShouldContainKey("poolUtilization");
        result.Data.ShouldContainKey("circuitBreakerOpen");
        result.Data.ShouldContainKey("degradedThreshold");
        result.Data.ShouldContainKey("unhealthyThreshold");
    }

    #endregion

    #region DatabasePoolHealthCheckOptions

    [Fact]
    public void Options_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DatabasePoolHealthCheckOptions();

        // Assert
        options.DegradedThreshold.ShouldBe(0.8);
        options.UnhealthyThreshold.ShouldBe(0.95);
        options.Name.ShouldBeNull();
        options.Tags.ShouldBeNull();
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        DatabasePoolHealthCheck.DefaultName.ShouldBe("encina-database-pool");
    }

    #endregion

    #region Helpers

    private static IDatabaseHealthMonitor CreateHealthyMonitor(
        double poolUtilization = 0.0,
        int active = 0,
        int idle = 0,
        int total = 0,
        int pending = 0,
        int maxPool = 100)
    {
        // Compute values to achieve desired utilization if only poolUtilization was provided
        if (active == 0 && idle == 0 && total == 0 && poolUtilization > 0.0)
        {
            total = (int)(poolUtilization * maxPool);
            active = total;
        }

        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(false);
        monitor.ProviderName.Returns("test-provider");
        monitor.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(DatabaseHealthResult.Healthy("OK"));
        monitor.GetPoolStatistics()
            .Returns(new ConnectionPoolStats(active, idle, total, pending, maxPool));

        return monitor;
    }

    #endregion
}
