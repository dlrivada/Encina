using Encina.AzureFunctions.Durable;
using Encina.AzureFunctions.Tests.Fakers;
using LanguageExt;
using Microsoft.DurableTask;
using NSubstitute;
using Shouldly;
using Xunit;

// Alias to avoid conflict with LanguageExt's Partition
using FanOut = Encina.AzureFunctions.Durable.FanOutFanInExtensions;

namespace Encina.AzureFunctions.Tests.Durable;

public class FanOutFanInExtensionsTests
{
    #region FanOutAsync Guard Clauses

    [Fact]
    public async Task FanOutAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutAsync<int, string>(context!, "ActivityName", inputs));
    }

    [Fact]
    public async Task FanOutAsync_WithNullActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await FanOut.FanOutAsync<int, string>(context, null!, inputs));
    }

    [Fact]
    public async Task FanOutAsync_WithEmptyActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await FanOut.FanOutAsync<int, string>(context, string.Empty, inputs));
    }

    [Fact]
    public async Task FanOutAsync_WithNullInputs_ThrowsArgumentNullException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        IEnumerable<int>? inputs = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutAsync<int, string>(context, "ActivityName", inputs!));
    }

    [Fact]
    public async Task FanOutAsync_WithEmptyInputs_ReturnsEmptyList()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = Array.Empty<int>();

        // Act
        var result = await FanOut.FanOutAsync<int, string>(context, "ActivityName", inputs);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region FanOutWithResultAsync Guard Clauses

    [Fact]
    public async Task FanOutWithResultAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutWithResultAsync<int, string>(context!, "ActivityName", inputs));
    }

    [Fact]
    public async Task FanOutWithResultAsync_WithNullActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await FanOut.FanOutWithResultAsync<int, string>(context, null!, inputs));
    }

    [Fact]
    public async Task FanOutWithResultAsync_WithEmptyActivityName_ThrowsArgumentException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = new[] { 1, 2, 3 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await FanOut.FanOutWithResultAsync<int, string>(context, string.Empty, inputs));
    }

    [Fact]
    public async Task FanOutWithResultAsync_WithNullInputs_ThrowsArgumentNullException()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        IEnumerable<int>? inputs = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutWithResultAsync<int, string>(context, "ActivityName", inputs!));
    }

    [Fact]
    public async Task FanOutWithResultAsync_WithEmptyInputs_ReturnsEmptyList()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        var inputs = Array.Empty<int>();

        // Act
        var result = await FanOut.FanOutWithResultAsync<int, string>(context, "ActivityName", inputs);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region FanOutMultipleAsync Guard Clauses

    [Fact]
    public async Task FanOutMultipleAsync_TwoActivities_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutMultipleAsync<string, int>(
                context!,
                ("Activity1", "input1"),
                ("Activity2", 42)));
    }

    [Fact]
    public async Task FanOutMultipleAsync_ThreeActivities_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        TaskOrchestrationContext? context = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await FanOut.FanOutMultipleAsync<string, int, bool>(
                context!,
                ("Activity1", "input1"),
                ("Activity2", 42),
                ("Activity3", true)));
    }

    #endregion

    #region Partition Tests

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
        successes.ShouldBe([1, 2, 3]);
        failures.ShouldBeEmpty();
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
        successes.ShouldBeEmpty();
        failures.Count.ShouldBe(2);
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
        successes.ShouldBe([1, 3]);
        failures.Count.ShouldBe(2);
    }

    [Fact]
    public void Partition_WithEmptyList_ReturnsBothEmpty()
    {
        // Arrange
        var results = new List<Either<EncinaError, int>>();

        // Act
        var (successes, failures) = FanOut.Partition(results);

        // Assert
        successes.ShouldBeEmpty();
        failures.ShouldBeEmpty();
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
        successes.ShouldBe([10, 20, 30]);
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
        successes.ShouldBe(["hello", "world"], ignoreOrder: true);
        failures.ShouldHaveSingleItem();
    }

    [Fact]
    public void Partition_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var faker = new TestItemFaker();
        var item1 = faker.WithId(1).WithName("Item 1").Generate();
        var item2 = faker.WithId(2).WithName("Item 2").Generate();
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
        successes.Select(x => x.Id).ShouldBe([1, 2], ignoreOrder: true);
        failures.ShouldHaveSingleItem();
    }

    #endregion
}
