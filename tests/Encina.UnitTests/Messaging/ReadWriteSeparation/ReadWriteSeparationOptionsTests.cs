using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="ReadWriteSeparationOptions"/>.
/// </summary>
public sealed class ReadWriteSeparationOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ReadWriteSeparationOptions();

        // Assert
        options.WriteConnectionString.ShouldBeNull();
        options.ReadConnectionStrings.ShouldNotBeNull();
        options.ReadConnectionStrings.ShouldBeEmpty();
        options.ReplicaStrategy.ShouldBe(ReplicaStrategy.RoundRobin);
        options.ValidateOnStartup.ShouldBeFalse();
    }

    [Fact]
    public void WriteConnectionString_CanBeSet()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();
        const string connectionString = "Server=primary;Database=test;";

        // Act
        options.WriteConnectionString = connectionString;

        // Assert
        options.WriteConnectionString.ShouldBe(connectionString);
    }

    [Fact]
    public void ReadConnectionStrings_CanBeSetWithMultipleValues()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();
        var replicas = new List<string>
        {
            "Server=replica1;Database=test;",
            "Server=replica2;Database=test;",
            "Server=replica3;Database=test;"
        };

        // Act
        options.ReadConnectionStrings = replicas;

        // Assert
        options.ReadConnectionStrings.Count.ShouldBe(3);
        options.ReadConnectionStrings[0].ShouldBe("Server=replica1;Database=test;");
        options.ReadConnectionStrings[1].ShouldBe("Server=replica2;Database=test;");
        options.ReadConnectionStrings[2].ShouldBe("Server=replica3;Database=test;");
    }

    [Theory]
    [InlineData(ReplicaStrategy.RoundRobin)]
    [InlineData(ReplicaStrategy.Random)]
    [InlineData(ReplicaStrategy.LeastConnections)]
    public void ReplicaStrategy_CanBeSet(ReplicaStrategy strategy)
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();

        // Act
        options.ReplicaStrategy = strategy;

        // Assert
        options.ReplicaStrategy.ShouldBe(strategy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateOnStartup_CanBeSet(bool validateOnStartup)
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();

        // Act
        options.ValidateOnStartup = validateOnStartup;

        // Assert
        options.ValidateOnStartup.ShouldBe(validateOnStartup);
    }

    [Fact]
    public void ReadConnectionStrings_CanBeAddedIndividually()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions();

        // Act
        options.ReadConnectionStrings.Add("Server=replica1;");
        options.ReadConnectionStrings.Add("Server=replica2;");

        // Assert
        options.ReadConnectionStrings.Count.ShouldBe(2);
    }

    [Fact]
    public void FullConfiguration_AllPropertiesSet()
    {
        // Arrange & Act
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;Database=test;",
            ReadConnectionStrings = ["Server=replica1;", "Server=replica2;"],
            ReplicaStrategy = ReplicaStrategy.LeastConnections,
            ValidateOnStartup = true
        };

        // Assert
        options.WriteConnectionString.ShouldBe("Server=primary;Database=test;");
        options.ReadConnectionStrings.Count.ShouldBe(2);
        options.ReplicaStrategy.ShouldBe(ReplicaStrategy.LeastConnections);
        options.ValidateOnStartup.ShouldBeTrue();
    }
}
