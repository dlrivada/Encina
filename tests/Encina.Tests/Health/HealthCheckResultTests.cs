using Encina.Messaging.Health;
using Shouldly;

namespace Encina.Tests.Health;

public sealed class HealthCheckResultTests
{
    [Fact]
    public void Healthy_CreatesHealthyResult()
    {
        // Act
        var result = HealthCheckResult.Healthy("All good");

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("All good");
        result.Exception.ShouldBeNull();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public void Healthy_WithData_IncludesData()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var result = HealthCheckResult.Healthy("All good", data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("key");
        result.Data["key"].ShouldBe("value");
    }

    [Fact]
    public void Degraded_CreatesDegradedResult()
    {
        // Act
        var result = HealthCheckResult.Degraded("Performance reduced");

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe("Performance reduced");
    }

    [Fact]
    public void Degraded_WithException_IncludesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong");

        // Act
        var result = HealthCheckResult.Degraded("Partial failure", exception);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Exception.ShouldBe(exception);
    }

    [Fact]
    public void Unhealthy_CreatesUnhealthyResult()
    {
        // Act
        var result = HealthCheckResult.Unhealthy("Service down");

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Service down");
    }

    [Fact]
    public void Unhealthy_WithExceptionAndData_IncludesBoth()
    {
        // Arrange
        var exception = new TimeoutException("Connection timed out");
        var data = new Dictionary<string, object> { ["timeout"] = 30 };

        // Act
        var result = HealthCheckResult.Unhealthy("Database unavailable", exception, data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBe(exception);
        result.Data.ShouldContainKey("timeout");
        result.Data["timeout"].ShouldBe(30);
    }

    [Fact]
    public void Constructor_WithNullData_UsesEmptyDictionary()
    {
        // Act
        var result = new HealthCheckResult(HealthStatus.Healthy, "test", null, null);

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultResult_HasUnhealthyStatus()
    {
        // Act
        var result = default(HealthCheckResult);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBeNull();
    }
}
