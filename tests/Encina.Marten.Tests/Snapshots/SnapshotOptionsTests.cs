using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new SnapshotOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.SnapshotEvery.Should().Be(100);
        options.KeepSnapshots.Should().Be(3);
        options.AsyncSnapshotCreation.Should().BeTrue();
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        options.Enabled = true;

        // Assert
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public void SnapshotEvery_CanBeSet()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        options.SnapshotEvery = 50;

        // Assert
        options.SnapshotEvery.Should().Be(50);
    }

    [Fact]
    public void KeepSnapshots_CanBeSet()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        options.KeepSnapshots = 5;

        // Assert
        options.KeepSnapshots.Should().Be(5);
    }

    [Fact]
    public void AsyncSnapshotCreation_CanBeDisabled()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        options.AsyncSnapshotCreation = false;

        // Assert
        options.AsyncSnapshotCreation.Should().BeFalse();
    }

    [Fact]
    public void ConfigureAggregate_DoesNotThrowForValidParameters()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: 25,
            keepSnapshots: 10);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAggregate_WithOnlySnapshotEvery_DoesNotThrow()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 50);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAggregate_ReturnsSelf_ForChaining()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var result = options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 50);

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void ConfigureAggregate_ThrowsForZeroSnapshotEvery()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConfigureAggregate_ThrowsForNegativeSnapshotEvery()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: -5);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConfigureAggregate_ThrowsForNegativeKeepSnapshots()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: 50,
            keepSnapshots: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ConfigureAggregate_AllowsZeroKeepSnapshots()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(
            snapshotEvery: 50,
            keepSnapshots: 0);

        // Assert - Zero means keep all, which is valid
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAggregate_CanBeCalledMultipleTimes()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act - configure twice (second overrides)
        options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 25);
        var act = () => options.ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 50);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ConfigureAggregate_SupportsFluentChaining()
    {
        // Arrange
        var options = new SnapshotOptions();

        // Act - This should compile and work
        var result = options
            .ConfigureAggregate<TestSnapshotableAggregate>(snapshotEvery: 25);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(options);
    }
}
