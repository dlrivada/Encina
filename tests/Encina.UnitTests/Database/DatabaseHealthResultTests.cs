using System.Collections.Immutable;

using Encina.Database;

namespace Encina.UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabaseHealthResult"/> and <see cref="DatabaseHealthStatus"/>.
/// </summary>
public sealed class DatabaseHealthResultTests
{
    #region Factory Methods

    [Fact]
    public void Healthy_ReturnsCorrectStatus()
    {
        // Act
        var result = DatabaseHealthResult.Healthy();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
    }

    [Fact]
    public void Healthy_WithDescription_SetsDescription()
    {
        // Act
        var result = DatabaseHealthResult.Healthy("All good");

        // Assert
        result.Description.ShouldBe("All good");
    }

    [Fact]
    public void Healthy_WithData_SetsData()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var result = DatabaseHealthResult.Healthy(data: data);

        // Assert
        result.Data.ShouldContainKey("key");
        result.Data["key"].ShouldBe("value");
    }

    [Fact]
    public void Degraded_ReturnsCorrectStatus()
    {
        // Act
        var result = DatabaseHealthResult.Degraded();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Degraded);
    }

    [Fact]
    public void Degraded_WithException_SetsException()
    {
        // Arrange
        var ex = new InvalidOperationException("test");

        // Act
        var result = DatabaseHealthResult.Degraded(exception: ex);

        // Assert
        result.Exception.ShouldBe(ex);
    }

    [Fact]
    public void Unhealthy_ReturnsCorrectStatus()
    {
        // Act
        var result = DatabaseHealthResult.Unhealthy();

        // Assert
        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
    }

    [Fact]
    public void Unhealthy_WithDescriptionAndException_SetsBoth()
    {
        // Arrange
        var ex = new TimeoutException("timed out");

        // Act
        var result = DatabaseHealthResult.Unhealthy("Connection failed", ex);

        // Assert
        result.Description.ShouldBe("Connection failed");
        result.Exception.ShouldBe(ex);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_WithNullData_UsesEmptyDictionary()
    {
        // Act
        var result = new DatabaseHealthResult(DatabaseHealthStatus.Healthy);

        // Assert
        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithNullDescription_SetsNull()
    {
        // Act
        var result = new DatabaseHealthResult(DatabaseHealthStatus.Healthy);

        // Assert
        result.Description.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithNullException_SetsNull()
    {
        // Act
        var result = new DatabaseHealthResult(DatabaseHealthStatus.Healthy);

        // Assert
        result.Exception.ShouldBeNull();
    }

    #endregion

    #region DatabaseHealthStatus

    [Fact]
    public void DatabaseHealthStatus_HasThreeValues()
    {
        // Assert
        Enum.GetValues<DatabaseHealthStatus>().Length.ShouldBe(3);
    }

    [Fact]
    public void DatabaseHealthStatus_UnhealthyIsZero()
    {
        // Assert
        ((int)DatabaseHealthStatus.Unhealthy).ShouldBe(0);
    }

    [Fact]
    public void DatabaseHealthStatus_DegradedIsOne()
    {
        // Assert
        ((int)DatabaseHealthStatus.Degraded).ShouldBe(1);
    }

    [Fact]
    public void DatabaseHealthStatus_HealthyIsTwo()
    {
        // Assert
        ((int)DatabaseHealthStatus.Healthy).ShouldBe(2);
    }

    #endregion
}
