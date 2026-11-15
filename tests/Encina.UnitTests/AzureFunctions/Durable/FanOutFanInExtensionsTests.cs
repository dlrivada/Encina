using Encina.AzureFunctions.Durable;
using Encina.UnitTests.AzureFunctions.Fakers;
using Microsoft.DurableTask;

// Alias to avoid conflict with LanguageExt's Partition
using FanOut = Encina.AzureFunctions.Durable.FanOutFanInExtensions;

namespace Encina.UnitTests.AzureFunctions.Durable;

public class FanOutFanInExtensionsTests
{
    private const string Activity = "Activity";

    /// <summary>
    /// A fake orchestration context whose activity calls are completed by the test: each call is routed by its
    /// input to a task source, so the test controls the order in which activities complete.
    /// </summary>
    private sealed class ControlledActivities
    {
        private readonly Dictionary<int, TaskCompletionSource<ActivityResult<string>>> _sources = [];

        public ControlledActivities(params int[] inputs)
        {
            foreach (var input in inputs)
            {
                _sources[input] = new TaskCompletionSource<ActivityResult<string>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            Context = Substitute.For<TaskOrchestrationContext>();
            Context.CallActivityAsync<ActivityResult<string>>(
                    Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
                .Returns(call => _sources[(int)call.ArgAt<object?>(1)!].Task);
        }

        public TaskOrchestrationContext Context { get; }

        public void Succeed(int input, string value) => _sources[input].SetResult(ActivityResult<string>.Success(value));

        public void Fail(int input, string code) => _sources[input].SetResult(ActivityResult<string>.Failure(code, "secret detail"));
    }

    private static string CodeOf(EncinaError error) => error.GetCode().IfNone(string.Empty);

    private static List<(int Index, string Code)> FailuresOf(EncinaError error)
        => ((IReadOnlyList<IReadOnlyDictionary<string, object?>>)error.GetDetails()["failures"]!)
            .Select(f => ((int)f["index"]!, (string)f["code"]!))
            .ToList();

    #region FanOutAsync

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

    [Fact]
    public async Task FanOutAsync_UnwrapsSuccessAndFailureActivityResultsInInputOrder()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        activities.Fail(1, "order.rejected");
        activities.Succeed(2, "two");
        activities.Succeed(3, "three");

        // Act
        var results = await FanOut.FanOutAsync<int, string>(activities.Context, Activity, [1, 2, 3]);

        // Assert
        results.Count.ShouldBe(3);
        results[0].IsLeft.ShouldBeTrue();
        CodeOf(results[0].Match(Right: _ => default!, Left: e => e)).ShouldBe("order.rejected");
        results[1].Match(Right: v => v, Left: _ => "").ShouldBe("two");
        results[2].Match(Right: v => v, Left: _ => "").ShouldBe("three");
    }

    [Fact]
    public async Task FanOutAsync_OutputIsNotAnActivityResult_ReturnsActivityFailedLeft()
    {
        // Arrange: the engine fails to read a plain value as ActivityResult<T> with a JsonException
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CallActivityAsync<ActivityResult<string>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromException<ActivityResult<string>>(new System.Text.Json.JsonException("plain value")));

        // Act
        var results = await FanOut.FanOutAsync<int, string>(context, Activity, [1]);

        // Assert
        var error = results.ShouldHaveSingleItem().Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.activity_failed");
        error.Message.ShouldNotContain("plain value");
    }

    [Fact]
    public async Task FanOutAsync_OutputIsNull_ReturnsActivityFailedLeft()
    {
        // Arrange: a plain activity that returns null deserializes to a null ActivityResult<T>
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CallActivityAsync<ActivityResult<string>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult<ActivityResult<string>>(null!));

        // Act
        var results = await FanOut.FanOutAsync<int, string>(context, Activity, [1]);

        // Assert
        var error = results.ShouldHaveSingleItem().Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.activity_failed");
    }

    #endregion

    #region FanOutAllAsync

    [Fact]
    public async Task FanOutAllAsync_AllSucceed_ReturnsEveryValueInInputOrder()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2);
        activities.Succeed(2, "two");
        activities.Succeed(1, "one");

        // Act
        var result = await FanOut.FanOutAllAsync<int, string>(activities.Context, Activity, [1, 2]);

        // Assert
        result.Match(Right: values => values, Left: _ => []).ShouldBe(["one", "two"]);
    }

    [Fact]
    public async Task FanOutAllAsync_NoInputs_ReturnsEmptyRight()
    {
        // Act
        var result = await FanOut.FanOutAllAsync<int, string>(
            Substitute.For<TaskOrchestrationContext>(), Activity, []);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task FanOutAllAsync_SingleFailure_ReturnsTheAggregateError()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        activities.Succeed(1, "one");
        activities.Fail(2, "order.rejected");
        activities.Succeed(3, "three");

        // Act
        var result = await FanOut.FanOutAllAsync<int, string>(activities.Context, Activity, [1, 2, 3]);

        // Assert
        var error = result.Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.fanout_failed");
        error.GetDetails()["total"].ShouldBe(3);
        error.GetDetails()["failedCount"].ShouldBe(1);
        FailuresOf(error).ShouldBe([(1, "order.rejected")]);
    }

    [Fact]
    public async Task FanOutAllAsync_SeveralFailures_ListsEachCodeAndIndexWithoutMessages()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        activities.Fail(3, "channel.unreachable");
        activities.Succeed(2, "two");
        activities.Fail(1, "consent.withdrawn");

        // Act
        var result = await FanOut.FanOutAllAsync<int, string>(activities.Context, Activity, [1, 2, 3]);

        // Assert
        var error = result.Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.fanout_failed");
        error.GetDetails()["total"].ShouldBe(3);
        error.GetDetails()["failedCount"].ShouldBe(2);
        FailuresOf(error).ShouldBe([(0, "consent.withdrawn"), (2, "channel.unreachable")]);
        error.Message.ShouldNotContain("secret detail");
        error.GetDetails().Values.OfType<string>().ShouldNotContain(s => s.Contains("secret detail"));
    }

    #endregion

    #region FanOutFirstSuccessAsync

    [Fact]
    public async Task FanOutFirstSuccessAsync_FastFailureThenFastSuccess_ReturnsTheSuccessWithoutWaitingForTheSlowOne()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        var pending = FanOut.FanOutFirstSuccessAsync<int, string>(activities.Context, Activity, [1, 2, 3]);
        activities.Fail(1, "a.failed");
        pending.IsCompleted.ShouldBeFalse("a failure alone must not end the wait");
        activities.Succeed(2, "fast");

        // Act
        var result = await pending;

        // Assert: input 3 is still running and the call has returned
        result.Match(Right: v => v, Left: _ => "").ShouldBe("fast");
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_SlowSuccessAfterFailures_ReturnsTheSlowSuccess()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        var pending = FanOut.FanOutFirstSuccessAsync<int, string>(activities.Context, Activity, [1, 2, 3]);
        activities.Fail(1, "a.failed");
        activities.Fail(2, "b.failed");
        pending.IsCompleted.ShouldBeFalse();
        activities.Succeed(3, "slow");

        // Act
        var result = await pending;

        // Assert
        result.Match(Right: v => v, Left: _ => "").ShouldBe("slow");
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_FirstCompletedIsAFailure_DoesNotReturnItAsTheAnswer()
    {
        // Arrange: input 1 completes first with a failure, input 2 later with a success
        var activities = new ControlledActivities(1, 2);
        var pending = FanOut.FanOutFirstSuccessAsync<int, string>(activities.Context, Activity, [1, 2]);
        activities.Fail(1, "a.failed");
        activities.Succeed(2, "later");

        // Act
        var result = await pending;

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_AllFail_ReturnsTheAggregateErrorWithEveryFailure()
    {
        // Arrange
        var activities = new ControlledActivities(1, 2, 3);
        var pending = FanOut.FanOutFirstSuccessAsync<int, string>(activities.Context, Activity, [1, 2, 3]);
        activities.Fail(3, "c.failed");
        activities.Fail(1, "a.failed");
        pending.IsCompleted.ShouldBeFalse();
        activities.Fail(2, "b.failed");

        // Act
        var result = await pending;

        // Assert
        var error = result.Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.fanout_failed");
        error.GetDetails()["total"].ShouldBe(3);
        error.GetDetails()["failedCount"].ShouldBe(3);
        FailuresOf(error).ShouldBe([(0, "a.failed"), (1, "b.failed"), (2, "c.failed")]);
        error.Message.ShouldNotContain("secret detail");
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_NoInputs_ReturnsTheAggregateErrorWithZeroTotals()
    {
        // Act
        var result = await FanOut.FanOutFirstSuccessAsync<int, string>(
            Substitute.For<TaskOrchestrationContext>(), Activity, []);

        // Assert
        var error = result.Match(Right: _ => default!, Left: e => e);
        CodeOf(error).ShouldBe("durable.fanout_failed");
        error.GetDetails()["total"].ShouldBe(0);
        error.GetDetails()["failedCount"].ShouldBe(0);
    }

    #endregion

    #region FanOutMultipleAsync

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

    [Fact]
    public async Task FanOutMultipleAsync_TwoActivities_UnwrapsEachActivityResult()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CallActivityAsync<ActivityResult<string>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(ActivityResult<string>.Success("text")));
        context.CallActivityAsync<ActivityResult<int>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(ActivityResult<int>.Failure("number.rejected", "no")));

        // Act
        var (first, second) = await FanOut.FanOutMultipleAsync<string, int>(
            context, ("Activity1", "input1"), ("Activity2", 42));

        // Assert
        first.Match(Right: v => v, Left: _ => "").ShouldBe("text");
        CodeOf(second.Match(Right: _ => default!, Left: e => e)).ShouldBe("number.rejected");
    }

    [Fact]
    public async Task FanOutMultipleAsync_ThreeActivities_UnwrapsEachActivityResult()
    {
        // Arrange
        var context = Substitute.For<TaskOrchestrationContext>();
        context.CallActivityAsync<ActivityResult<string>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(ActivityResult<string>.Success("text")));
        context.CallActivityAsync<ActivityResult<int>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(ActivityResult<int>.Success(7)));
        context.CallActivityAsync<ActivityResult<bool>>(Arg.Any<TaskName>(), Arg.Any<object?>(), Arg.Any<TaskOptions?>())
            .Returns(Task.FromResult(ActivityResult<bool>.Failure("flag.rejected", "no")));

        // Act
        var (first, second, third) = await FanOut.FanOutMultipleAsync<string, int, bool>(
            context, ("Activity1", "input1"), ("Activity2", 42), ("Activity3", true));

        // Assert
        first.Match(Right: v => v, Left: _ => "").ShouldBe("text");
        second.Match(Right: v => v, Left: _ => -1).ShouldBe(7);
        CodeOf(third.Match(Right: _ => default!, Left: e => e)).ShouldBe("flag.rejected");
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
