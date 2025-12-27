using Encina.AzureFunctions.Durable;
using FluentAssertions;
using LanguageExt;
using Xunit;

// Alias to avoid conflict with LanguageExt's Partition
using FanOut = Encina.AzureFunctions.Durable.FanOutFanInExtensions;

namespace Encina.AzureFunctions.Tests.Durable;

public class FanOutFanInExtensionsTests
{
    [Fact]
    public void Partition_WithAllSuccesses_ReturnsAllInSuccessList()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>
        {
            Either<EncinaError, int>.Right(1),
            Either<EncinaError, int>.Right(2),
            Either<EncinaError, int>.Right(3)
        };

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().HaveCount(3);
        successes.Should().BeEquivalentTo([1, 2, 3]);
        failures.Should().BeEmpty();
    }

    [Fact]
    public void Partition_WithAllFailures_ReturnsAllInFailuresList()
    {
        // Arrange
        var error1 = EncinaErrors.Create("error1", "Error 1");
        var error2 = EncinaErrors.Create("error2", "Error 2");
        var results = new List<Either<EncinaError, int>>
        {
            Either<EncinaError, int>.Left(error1),
            Either<EncinaError, int>.Left(error2)
        };

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().BeEmpty();
        failures.Should().HaveCount(2);
    }

    [Fact]
    public void Partition_WithMixedResults_SeparatesCorrectly()
    {
        // Arrange
        var error = EncinaErrors.Create("error", "Error message");
        var results = new List<Either<EncinaError, int>>
        {
            Either<EncinaError, int>.Right(1),
            Either<EncinaError, int>.Left(error),
            Either<EncinaError, int>.Right(3),
            Either<EncinaError, int>.Left(error)
        };

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().HaveCount(2);
        successes.Should().BeEquivalentTo([1, 3]);
        failures.Should().HaveCount(2);
    }

    [Fact]
    public void Partition_WithEmptyList_ReturnsBothEmpty()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>();

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().BeEmpty();
        failures.Should().BeEmpty();
    }

    [Fact]
    public void Partition_PreservesOrderOfSuccesses()
    {
        // Arrange
        var error = EncinaErrors.Create("error", "Error");
        var results = new List<Either<EncinaError, int>>
        {
            Either<EncinaError, int>.Right(10),
            Either<EncinaError, int>.Left(error),
            Either<EncinaError, int>.Right(20),
            Either<EncinaError, int>.Right(30)
        };

        // Act
        var (successes, _) = FanOut.Partition(results);

        // Assert
        successes.Should().BeEquivalentTo([10, 20, 30], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Partition_WithStringResults_WorksCorrectly()
    {
        // Arrange
        var error = EncinaErrors.Create("error", "Error");
        var results = new List<Either<EncinaError, string>>
        {
            Either<EncinaError, string>.Right("hello"),
            Either<EncinaError, string>.Left(error),
            Either<EncinaError, string>.Right("world")
        };

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().HaveCount(2);
        successes.Should().Contain("hello");
        successes.Should().Contain("world");
        failures.Should().HaveCount(1);
    }

    [Fact]
    public void Partition_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var item1 = new TestItem { Id = 1, Name = "Item 1" };
        var item2 = new TestItem { Id = 2, Name = "Item 2" };
        var error = EncinaErrors.Create("error", "Error");
        var results = new List<Either<EncinaError, TestItem>>
        {
            Either<EncinaError, TestItem>.Right(item1),
            Either<EncinaError, TestItem>.Left(error),
            Either<EncinaError, TestItem>.Right(item2)
        };

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.Should().HaveCount(2);
        successes.Select(x => x.Id).Should().Contain(1);
        successes.Select(x => x.Id).Should().Contain(2);
        failures.Should().HaveCount(1);
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
