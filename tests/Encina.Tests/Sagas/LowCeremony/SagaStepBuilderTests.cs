using Encina.Messaging.Sagas.LowCeremony;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Sagas.LowCeremony;

public sealed class SagaStepBuilderTests
{
    [Fact]
    public void Execute_WithContextOverload_ReturnsStepBuilder()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var stepBuilder = definition
            .Step("Test")
            .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)));

        // Assert
        stepBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithSimplifiedOverload_ReturnsStepBuilder()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var stepBuilder = definition
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)));

        // Assert
        stepBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void Execute_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            definition.Step("Test").Execute((Func<TestSagaData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TestSagaData>>>)null!));
    }

    [Fact]
    public void Compensate_WithContextOverload_ReturnsParentDefinition()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var returnedDefinition = definition
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ctx, ct) => Task.CompletedTask);

        // Assert
        returnedDefinition.ShouldBe(definition);
    }

    [Fact]
    public void Compensate_WithSimplifiedOverload_ReturnsParentDefinition()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act
        var returnedDefinition = definition
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) => Task.CompletedTask);

        // Assert
        returnedDefinition.ShouldBe(definition);
    }

    [Fact]
    public void Compensate_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            definition
                .Step("Test")
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
                .Compensate((Func<TestSagaData, IRequestContext, CancellationToken, Task>)null!));
    }

    [Fact]
    public void Step_ChainedFromStepBuilder_AddsStepWithoutCompensation()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("First")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Step("Second") // Chained without Compensate
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Assert
        builtSaga.StepCount.ShouldBe(2);
        builtSaga.Steps[0].Name.ShouldBe("First");
        builtSaga.Steps[0].Compensate.ShouldBeNull();
        builtSaga.Steps[1].Name.ShouldBe("Second");
    }

    [Fact]
    public void WithTimeout_ChainedFromStepBuilder_SetsTimeout()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .WithTimeout(TimeSpan.FromMinutes(10))
            .Build();

        // Assert
        builtSaga.Timeout.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void Build_ChainedFromStepBuilder_ReturnsBuiltDefinition()
    {
        // Act
        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build(); // Directly from step builder

        // Assert
        builtSaga.ShouldNotBeNull();
        builtSaga.StepCount.ShouldBe(1);
    }

    [Fact]
    public void Build_WithoutExecute_ThrowsInvalidOperationException()
    {
        // Arrange
        var definition = SagaDefinition.Create<TestSagaData>("TestSaga");
        var stepBuilder = definition.Step("Test");

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => stepBuilder.Build());
        exception.Message.ShouldContain("Execute action");
    }

    [Fact]
    public async Task Execute_FunctionReceivesCorrectContext()
    {
        // Arrange
        IRequestContext? capturedContext = null;
        var expectedContext = RequestContext.Create().WithUserId("test-user");

        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ctx, ct) =>
            {
                capturedContext = ctx;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        // Act
        await builtSaga.Steps[0].Execute(new TestSagaData(), expectedContext, CancellationToken.None);

        // Assert
        capturedContext.ShouldBe(expectedContext);
    }

    [Fact]
    public async Task Compensate_FunctionReceivesCorrectContext()
    {
        // Arrange
        IRequestContext? capturedContext = null;
        var expectedContext = RequestContext.Create().WithUserId("test-user");

        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ctx, ct) =>
            {
                capturedContext = ctx;
                return Task.CompletedTask;
            })
            .Build();

        // Act
        await builtSaga.Steps[0].Compensate!(new TestSagaData(), expectedContext, CancellationToken.None);

        // Assert
        capturedContext.ShouldBe(expectedContext);
    }

    [Fact]
    public async Task Execute_SimplifiedOverload_WorksWithoutContext()
    {
        // Arrange
        var executed = false;

        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) =>
            {
                executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        // Act
        var context = RequestContext.Create();
        await builtSaga.Steps[0].Execute(new TestSagaData(), context, CancellationToken.None);

        // Assert
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Compensate_SimplifiedOverload_WorksWithoutContext()
    {
        // Arrange
        var compensated = false;

        var builtSaga = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                compensated = true;
                return Task.CompletedTask;
            })
            .Build();

        // Act
        var context = RequestContext.Create();
        await builtSaga.Steps[0].Compensate!(new TestSagaData(), context, CancellationToken.None);

        // Assert
        compensated.ShouldBeTrue();
    }

    private sealed class TestSagaData
    {
        public Guid OrderId { get; set; }
    }
}
