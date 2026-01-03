using Encina.Messaging.Health;

namespace Encina.Messaging.ContractTests.Health;

/// <summary>
/// Contract tests that verify all IEncinaHealthCheck implementations follow the same behavioral contract.
/// These tests ensure consistency across all health check implementations (database, messaging, caching, etc.).
/// </summary>
public abstract class IEncinaHealthCheckContractTests
{
    protected abstract IEncinaHealthCheck CreateHealthCheck();
    protected abstract IEncinaHealthCheck CreateHealthCheckWithCustomName(string name);
    protected abstract IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags);

    #region Name Contract

    [Fact]
    public void Name_ShouldNotBeNull()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Name.ShouldNotBeNull();
    }

    [Fact]
    public void Name_ShouldNotBeEmpty()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Name.ShouldNotBeEmpty();
    }

    [Fact]
    public void Name_WithCustomName_ShouldUseCustomName()
    {
        // Arrange
        const string customName = "my-custom-health-check";
        var healthCheck = CreateHealthCheckWithCustomName(customName);

        // Assert
        healthCheck.Name.ShouldBe(customName);
    }

    #endregion

    #region Tags Contract

    [Fact]
    public void Tags_ShouldNotBeNull()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Tags.ShouldNotBeNull();
    }

    [Fact]
    public void Tags_ShouldContainEncinaTag()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Assert
        healthCheck.Tags.ShouldContain("encina");
    }

    [Fact]
    public void Tags_WithCustomTags_ShouldContainAllCustomTags()
    {
        // Arrange
        var customTags = new[] { "custom", "tags", "test" };
        var healthCheck = CreateHealthCheckWithCustomTags(customTags);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        foreach (var tag in customTags)
        {
            healthCheck.Tags.ShouldContain(tag);
        }
    }

    #endregion

    #region CheckHealthAsync Contract

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnValidResult()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Degraded);
        result.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnValidStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancelledToken_ShouldNotThrow()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await healthCheck.CheckHealthAsync(cts.Token);

        // Assert - Cancelled health checks should return a valid result rather than throwing
        result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy, HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleCalls_ShouldBeIdempotent()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result1 = await healthCheck.CheckHealthAsync();
        var result2 = await healthCheck.CheckHealthAsync();
        var result3 = await healthCheck.CheckHealthAsync();

        // Assert - All calls should return the same status
        result2.Status.ShouldBe(result1.Status, "Second call should return the same status as first call");
        result3.Status.ShouldBe(result1.Status, "Third call should return the same status as first call");
    }

    #endregion
}

/// <summary>
/// Contract tests for a mock healthy health check.
/// </summary>
public sealed class MockHealthyHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        return new MockHealthyHealthCheck("test-healthy", ["encina", "test", "ready"]);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        return new MockHealthyHealthCheck(name, ["encina", "test", "ready"]);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        return new MockHealthyHealthCheck("test-healthy", tags);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthy_ReturnsHealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }
}

/// <summary>
/// Contract tests for a mock unhealthy health check.
/// </summary>
public sealed class MockUnhealthyHealthCheckContractTests : IEncinaHealthCheckContractTests
{
    protected override IEncinaHealthCheck CreateHealthCheck()
    {
        return new MockUnhealthyHealthCheck("test-unhealthy", ["encina", "test", "ready"]);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomName(string name)
    {
        return new MockUnhealthyHealthCheck(name, ["encina", "test", "ready"]);
    }

    protected override IEncinaHealthCheck CreateHealthCheckWithCustomTags(IReadOnlyCollection<string> tags)
    {
        return new MockUnhealthyHealthCheck("test-unhealthy", tags);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnhealthy_ReturnsUnhealthyStatus()
    {
        // Arrange
        var healthCheck = CreateHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}

/// <summary>
/// Mock implementation of EncinaHealthCheck that always returns healthy.
/// </summary>
internal sealed class MockHealthyHealthCheck : EncinaHealthCheck
{
    public MockHealthyHealthCheck(string name, IReadOnlyCollection<string> tags)
        : base(name, tags)
    {
    }

    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Mock healthy"));
    }
}

/// <summary>
/// Mock implementation of EncinaHealthCheck that always returns unhealthy.
/// </summary>
internal sealed class MockUnhealthyHealthCheck : EncinaHealthCheck
{
    public MockUnhealthyHealthCheck(string name, IReadOnlyCollection<string> tags)
        : base(name, tags)
    {
    }

    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(HealthCheckResult.Unhealthy("Mock unhealthy"));
    }
}
