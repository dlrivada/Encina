using Encina.Database;

namespace Encina.UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabaseCircuitBreakerOptions"/>.
/// </summary>
public sealed class DatabaseCircuitBreakerOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DatabaseCircuitBreakerOptions();

        // Assert
        options.FailureThreshold.ShouldBe(0.5);
        options.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(10));
        options.BreakDuration.ShouldBe(TimeSpan.FromSeconds(30));
        options.MinimumThroughput.ShouldBe(10);
        options.IncludeTimeouts.ShouldBeTrue();
        options.IncludeConnectionFailures.ShouldBeTrue();
    }

    [Fact]
    public void FailureThreshold_CanBeSet()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.FailureThreshold = 0.3;

        // Assert
        options.FailureThreshold.ShouldBe(0.3);
    }

    [Fact]
    public void SamplingDuration_CanBeSet()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.SamplingDuration = TimeSpan.FromSeconds(15);

        // Assert
        options.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void BreakDuration_CanBeSet()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.BreakDuration = TimeSpan.FromMinutes(2);

        // Assert
        options.BreakDuration.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void MinimumThroughput_CanBeSet()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.MinimumThroughput = 20;

        // Assert
        options.MinimumThroughput.ShouldBe(20);
    }

    [Fact]
    public void IncludeTimeouts_CanBeDisabled()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.IncludeTimeouts = false;

        // Assert
        options.IncludeTimeouts.ShouldBeFalse();
    }

    [Fact]
    public void IncludeConnectionFailures_CanBeDisabled()
    {
        // Arrange
        var options = new DatabaseCircuitBreakerOptions();

        // Act
        options.IncludeConnectionFailures = false;

        // Assert
        options.IncludeConnectionFailures.ShouldBeFalse();
    }
}
