using System.Collections.Immutable;
using Encina.Messaging.Health;
using Shouldly;

namespace Encina.UnitTests.Messaging.Health;

/// <summary>
/// Unit tests for <see cref="HealthCheckResult"/> and <see cref="HealthStatus"/>.
/// </summary>
public sealed class HealthCheckResultTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var result = new HealthCheckResult(
            HealthStatus.Unhealthy,
            "Test description",
            data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Test description");
        result.Data.ShouldContainKey("key");
        result.Data["key"].ShouldBe("value");
    }

    [Fact]
    public void Constructor_WithNullData_UsesEmptyDictionary()
    {
        // Arrange & Act
        var result = new HealthCheckResult(HealthStatus.Healthy);

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new HealthCheckResult(HealthStatus.Healthy);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBeNull();
        result.Data.ShouldBeEmpty();
    }

    #endregion

    #region Static Factory Methods

    [Fact]
    public void Healthy_WithNoParameters_ReturnsHealthyResult()
    {
        // Act
        var result = HealthCheckResult.Healthy();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBeNull();
        result.Data.ShouldBeEmpty();
    }

    [Fact]
    public void Healthy_WithDescription_ReturnsHealthyResultWithDescription()
    {
        // Act
        var result = HealthCheckResult.Healthy("All systems operational");

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("All systems operational");
    }

    [Fact]
    public void Healthy_WithData_ReturnsHealthyResultWithData()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["pendingCount"] = 5 };

        // Act
        var result = HealthCheckResult.Healthy("OK", data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("pendingCount");
        result.Data["pendingCount"].ShouldBe(5);
    }

    [Fact]
    public void Degraded_WithNoParameters_ReturnsDegradedResult()
    {
        // Act
        var result = HealthCheckResult.Degraded();

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBeNull();
    }

    [Fact]
    public void Degraded_WithDescription_ReturnsDegradedResultWithDescription()
    {
        // Act
        var result = HealthCheckResult.Degraded("High latency detected");

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe("High latency detected");
    }

    [Fact]
    public void Degraded_WithData_ReturnsDegradedResultWithData()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["latency"] = 500 };

        // Act
        var result = HealthCheckResult.Degraded("Slow", data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["latency"].ShouldBe(500);
    }

    [Fact]
    public void Unhealthy_WithNoParameters_ReturnsUnhealthyResult()
    {
        // Act
        var result = HealthCheckResult.Unhealthy();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBeNull();
    }

    [Fact]
    public void Unhealthy_WithDescription_ReturnsUnhealthyResultWithDescription()
    {
        // Act
        var result = HealthCheckResult.Unhealthy("Database connection failed");

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Database connection failed");
    }

    [Fact]
    public void HealthCheckResult_DoesNotExposeAnException()
    {
        // A health result can reach a health endpoint: it never carries an exception object.
        typeof(HealthCheckResult).GetProperty("Exception").ShouldBeNull();
    }

    [Fact]
    public void Unhealthy_WithAllParameters_ReturnsUnhealthyResultWithAllProperties()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["errorCount"] = 10 };

        // Act
        var result = HealthCheckResult.Unhealthy("Critical failure", data);

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Critical failure");
        result.Data["errorCount"].ShouldBe(10);
    }

    #endregion

    #region HealthStatus Enum

    [Fact]
    public void HealthStatus_Values_AreCorrect()
    {
        // Assert
        ((int)HealthStatus.Unhealthy).ShouldBe(0);
        ((int)HealthStatus.Degraded).ShouldBe(1);
        ((int)HealthStatus.Healthy).ShouldBe(2);
    }

    [Fact]
    public void HealthStatus_Ordering_UnhealthyIsLowest()
    {
        // Assert
        (HealthStatus.Unhealthy < HealthStatus.Degraded).ShouldBeTrue();
        (HealthStatus.Degraded < HealthStatus.Healthy).ShouldBeTrue();
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var result1 = HealthCheckResult.Healthy("OK");
        var result2 = HealthCheckResult.Healthy("OK");

        // Assert
        result1.ShouldBe(result2);
    }

    [Fact]
    public void Equality_DifferentStatus_AreNotEqual()
    {
        // Arrange
        var result1 = HealthCheckResult.Healthy();
        var result2 = HealthCheckResult.Unhealthy();

        // Assert
        result1.ShouldNotBe(result2);
    }

    #endregion
}
