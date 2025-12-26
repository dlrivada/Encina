using Encina.Messaging.Sagas.LowCeremony;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Sagas.LowCeremony;

public sealed class SagaDefinitionTests
{
    [Fact]
    public void Create_WithValidSagaType_ReturnsDefinition()
    {
        // Act
        var definition = SagaDefinition.Create<TestSagaData>("OrderProcessing");

        // Assert
        definition.ShouldNotBeNull();
        definition.SagaType.ShouldBe("OrderProcessing");
    }

    [Fact]
    public void Create_WithNullSagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>(null!));
    }

    [Fact]
    public void Create_WithEmptySagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>(string.Empty));
    }

    [Fact]
    public void Create_WithWhitespaceSagaType_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SagaDefinition.Create<TestSagaData>("   "));
    }

    [Fact]
    public void Step_ReturnsStepBuilder()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var stepBuilder = definition.Step("First Step");

        // Assert
        stepBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void Step_WithNoName_GeneratesDefaultName()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var builtSaga = definition
            .Step()
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.Steps[0].Name.ShouldBe("Step 1");
    }

    [Fact]
    public void WithTimeout_SetsTimeout()
    {
        // Arrange
        var timeout = TimeSpan.FromMinutes(5);
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var builtSaga = definition
            .WithTimeout(timeout)
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.Timeout.ShouldBe(timeout);
    }

    [Fact]
    public void WithTimeout_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => definition.WithTimeout(TimeSpan.Zero));
    }

    [Fact]
    public void WithTimeout_WithNegativeTimeout_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => definition.WithTimeout(TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => definition.Build());
        exception.Message.ShouldContain("At least one step");
    }

    [Fact]
    public void Build_WithSingleStep_ReturnsBuiltDefinition()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Only Step")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.ShouldNotBeNull();
        builtSaga.SagaType.ShouldBe("TestSaga");
        builtSaga.StepCount.ShouldBe(1);
        builtSaga.Steps.Count.ShouldBe(1);
        builtSaga.Steps[0].Name.ShouldBe("Only Step");
        builtSaga.Timeout.ShouldBeNull();
    }

    [Fact]
    public void Build_WithMultipleSteps_ReturnsBuiltDefinitionWithAllSteps()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 3")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.StepCount.ShouldBe(3);
        builtSaga.Steps[0].Name.ShouldBe("Step 1");
        builtSaga.Steps[1].Name.ShouldBe("Step 2");
        builtSaga.Steps[2].Name.ShouldBe("Step 3");
    }

    [Fact]
    public void Build_StepWithCompensation_HasCompensateFunction()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("With Compensation")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Build();

        // Assert
        builtSaga.Steps[0].Compensate.ShouldNotBeNull();
    }

    [Fact]
    public void Build_StepWithoutCompensation_HasNullCompensateFunction()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Without Compensation")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.Steps[0].Compensate.ShouldBeNull();
    }

    [Fact]
    public async Task Build_PreservesExecuteFunction()
    {
        // Arrange
        var executed = false;

        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test Step")
            .Execute((data, ct) =>
            {
                executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        // Assert - Execute the function to verify it was preserved
        var context = RequestContext.Create();
        await builtSaga.Steps[0].Execute(new TestSagaData(), context, CancellationToken.None);
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Build_PreservesCompensateFunction()
    {
        // Arrange
        var compensated = false;

        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test Step")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                compensated = true;
                return Task.CompletedTask;
            })
            .Build();

        // Assert - Execute the function to verify it was preserved
        var context = RequestContext.Create();
        await builtSaga.Steps[0].Compensate!(new TestSagaData(), context, CancellationToken.None);
        compensated.ShouldBeTrue();
    }

    private sealed class TestSagaData
    {
        public Guid OrderId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? PaymentId { get; set; }
    }
}
