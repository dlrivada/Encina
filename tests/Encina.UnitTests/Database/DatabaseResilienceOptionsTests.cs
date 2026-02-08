using Encina.Database;

namespace Encina.UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabaseResilienceOptions"/>.
/// </summary>
public sealed class DatabaseResilienceOptionsTests
{
    [Fact]
    public void DefaultValues_AllFeaturesDisabled()
    {
        // Arrange & Act
        var options = new DatabaseResilienceOptions();

        // Assert
        options.EnablePoolMonitoring.ShouldBeFalse();
        options.EnableCircuitBreaker.ShouldBeFalse();
        options.WarmUpConnections.ShouldBe(0);
        options.HealthCheckInterval.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void CircuitBreaker_ReturnsNonNullOptions()
    {
        // Arrange & Act
        var options = new DatabaseResilienceOptions();

        // Assert
        options.CircuitBreaker.ShouldNotBeNull();
    }

    [Fact]
    public void CircuitBreaker_ReturnsSameInstance()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        var cb1 = options.CircuitBreaker;
        var cb2 = options.CircuitBreaker;

        // Assert
        ReferenceEquals(cb1, cb2).ShouldBeTrue();
    }

    [Fact]
    public void EnablePoolMonitoring_CanBeEnabled()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        options.EnablePoolMonitoring = true;

        // Assert
        options.EnablePoolMonitoring.ShouldBeTrue();
    }

    [Fact]
    public void EnableCircuitBreaker_CanBeEnabled()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        options.EnableCircuitBreaker = true;

        // Assert
        options.EnableCircuitBreaker.ShouldBeTrue();
    }

    [Fact]
    public void WarmUpConnections_CanBeSet()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        options.WarmUpConnections = 10;

        // Assert
        options.WarmUpConnections.ShouldBe(10);
    }

    [Fact]
    public void HealthCheckInterval_CanBeSet()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        options.HealthCheckInterval = TimeSpan.FromSeconds(30);

        // Assert
        options.HealthCheckInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CircuitBreaker_CanConfigureSubOptions()
    {
        // Arrange
        var options = new DatabaseResilienceOptions();

        // Act
        options.CircuitBreaker.FailureThreshold = 0.3;
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);

        // Assert
        options.CircuitBreaker.FailureThreshold.ShouldBe(0.3);
        options.CircuitBreaker.BreakDuration.ShouldBe(TimeSpan.FromMinutes(1));
    }
}
