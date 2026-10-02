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
    public void Degraded_WithData_SetsData()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["k"] = 1 };

        // Act
        var result = DatabaseHealthResult.Degraded("slow", data);

        // Assert
        result.Data["k"].ShouldBe(1);
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
    public void Unhealthy_WithDescriptionAndData_SetsBoth()
    {
        // Arrange
        var data = new Dictionary<string, object> { ["provider"] = "x" };

        // Act
        var result = DatabaseHealthResult.Unhealthy("Connection failed", data);

        // Assert
        result.Description.ShouldBe("Connection failed");
        result.Data["provider"].ShouldBe("x");
    }

    [Fact]
    public void DatabaseHealthResult_DoesNotExposeAnException()
    {
        // A health result can reach a health endpoint: it never carries an exception object.
        typeof(DatabaseHealthResult).GetProperty("Exception").ShouldBeNull();
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
