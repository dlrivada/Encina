using Encina.Sharding.Configuration;

namespace Encina.UnitTests.Core.Sharding.Configuration;

/// <summary>
/// Unit tests for <see cref="ScatterGatherOptions"/>.
/// </summary>
public sealed class ScatterGatherOptionsTests
{
    // ────────────────────────────────────────────────────────────
    //  Defaults
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxParallelism_DefaultIsUnlimited()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions();

        // Assert
        options.MaxParallelism.ShouldBe(-1);
    }

    [Fact]
    public void Timeout_DefaultIs30Seconds()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions();

        // Assert
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AllowPartialResults_DefaultIsTrue()
    {
        // Arrange & Act
        var options = new ScatterGatherOptions();

        // Assert
        options.AllowPartialResults.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Property setters
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxParallelism_CanBeSet()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act
        options.MaxParallelism = 4;

        // Assert
        options.MaxParallelism.ShouldBe(4);
    }

    [Fact]
    public void Timeout_CanBeSet()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act
        options.Timeout = TimeSpan.FromMinutes(2);

        // Assert
        options.Timeout.ShouldBe(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void AllowPartialResults_CanBeSetToFalse()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act
        options.AllowPartialResults = false;

        // Assert
        options.AllowPartialResults.ShouldBeFalse();
    }

    // ────────────────────────────────────────────────────────────
    //  Boundary values
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void MaxParallelism_ZeroIsAccepted()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act
        options.MaxParallelism = 0;

        // Assert
        options.MaxParallelism.ShouldBe(0);
    }

    [Fact]
    public void Timeout_ZeroIsAccepted()
    {
        // Arrange
        var options = new ScatterGatherOptions();

        // Act
        options.Timeout = TimeSpan.Zero;

        // Assert
        options.Timeout.ShouldBe(TimeSpan.Zero);
    }
}
