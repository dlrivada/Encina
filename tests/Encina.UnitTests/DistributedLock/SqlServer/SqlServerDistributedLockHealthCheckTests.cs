using Encina.DistributedLock.SqlServer.Health;
using Encina.Messaging.Health;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.SqlServer;

/// <summary>
/// Unit tests for <see cref="SqlServerDistributedLockHealthCheck"/>.
/// </summary>
public sealed class SqlServerDistributedLockHealthCheckTests
{
    [Fact]
    public void Constructor_WithNullConnectionString_ThrowsArgumentException()
    {
        var options = new ProviderHealthCheckOptions();
        Should.Throw<ArgumentException>(() =>
            new SqlServerDistributedLockHealthCheck(null!, options));
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ThrowsArgumentException()
    {
        var options = new ProviderHealthCheckOptions();
        Should.Throw<ArgumentException>(() =>
            new SqlServerDistributedLockHealthCheck("", options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new SqlServerDistributedLockHealthCheck("Server=.;", null!));
    }

    [Fact]
    public void Name_WithDefaultOptions_ReturnsDefaultName()
    {
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new SqlServerDistributedLockHealthCheck("Server=.;", options);

        healthCheck.Name.ShouldBe("sqlserver-distributed-lock");
    }

    [Fact]
    public void Name_WithCustomName_ReturnsCustomName()
    {
        var options = new ProviderHealthCheckOptions { Name = "custom-lock-check" };
        var healthCheck = new SqlServerDistributedLockHealthCheck("Server=.;", options);

        healthCheck.Name.ShouldBe("custom-lock-check");
    }

    [Fact]
    public void Tags_WithDefaultOptions_ReturnsDefaultTags()
    {
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new SqlServerDistributedLockHealthCheck("Server=.;", options);

        healthCheck.Tags.ShouldContain("sqlserver");
        healthCheck.Tags.ShouldContain("distributed-lock");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void Tags_WithCustomTags_ReturnsCustomTags()
    {
        var options = new ProviderHealthCheckOptions
        {
            Tags = ["custom-tag", "my-lock"]
        };
        var healthCheck = new SqlServerDistributedLockHealthCheck("Server=.;", options);

        healthCheck.Tags.ShouldContain("custom-tag");
        healthCheck.Tags.ShouldContain("my-lock");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions();
        var healthCheck = new SqlServerDistributedLockHealthCheck(
            "Server=nonexistent-server-that-should-fail;Database=Test;Connection Timeout=1;",
            options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Should be unhealthy because connection fails
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
