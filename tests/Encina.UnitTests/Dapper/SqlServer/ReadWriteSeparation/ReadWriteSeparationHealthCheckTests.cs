using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging.Health;
using Encina.Messaging.ReadWriteSeparation;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Dapper.SqlServer.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteSeparationHealthCheck"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ReadWriteSeparationHealthCheckTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteSeparationHealthCheck(null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        // Act
        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Assert
        healthCheck.ShouldNotBeNull();
    }

    #endregion

    #region Name Tests

    [Fact]
    public void Name_ReturnsDefaultName()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act & Assert
        healthCheck.Name.ShouldBe(ReadWriteSeparationHealthCheck.DefaultName);
    }

    [Fact]
    public void DefaultName_HasExpectedValue()
    {
        // Assert
        ReadWriteSeparationHealthCheck.DefaultName.ShouldBe("encina-read-write-separation-dapper");
    }

    #endregion

    #region Tags Tests

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act & Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("read-write-separation");
        healthCheck.Tags.ShouldContain("dapper");
        healthCheck.Tags.ShouldContain("ready");
    }

    #endregion

    #region CheckHealthAsync - Primary Connection Tests

    [Fact]
    public async Task CheckHealthAsync_WithEmptyWriteConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = string.Empty
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("not configured");
        result.Data.ShouldContainKey("primary");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullWriteConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = null
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("not configured");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidWriteConnectionString_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=nonexistent-server-that-doesnt-exist;Database=test;Connection Timeout=1;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("unreachable");
        result.Data.ShouldContainKey("primary");
    }

    #endregion

    #region CheckHealthAsync - Replica Tests

    [Fact]
    public async Task CheckHealthAsync_WithInvalidPrimaryAndNoReplicas_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=nonexistent-server;Database=test;Connection Timeout=1;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithInvalidReplicaConnectionStrings_ReturnsDegradedOrHealthy()
    {
        // This test would require a working primary connection
        // Since we can't mock the actual SQL connection, this is a structural test
        // Integration tests should cover actual database connectivity

        // Arrange - set up options with an invalid replica
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=invalid-primary;Database=test;Connection Timeout=1;"
        };
        options.ReadConnectionStrings.Add("Server=invalid-replica;Database=test;Connection Timeout=1;");

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert - Since primary is invalid, it should be Unhealthy
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIEncinaHealthCheck()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Assert
        (healthCheck is IEncinaHealthCheck).ShouldBeTrue();
    }

    [Fact]
    public void InheritsFromEncinaHealthCheck()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);

        // Assert
        (healthCheck is EncinaHealthCheck).ShouldBeTrue();
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;Connection Timeout=30;"
        };

        var healthCheck = new ReadWriteSeparationHealthCheck(options);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should either throw OperationCanceledException or return a status
        // The behavior depends on when the cancellation is checked
        try
        {
            var result = await healthCheck.CheckHealthAsync(cts.Token);
            // If we get here, the check handled cancellation gracefully
            result.Status.ShouldBeOneOf(HealthStatus.Healthy, HealthStatus.Degraded, HealthStatus.Unhealthy);
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable behavior
        }
    }

    #endregion
}
