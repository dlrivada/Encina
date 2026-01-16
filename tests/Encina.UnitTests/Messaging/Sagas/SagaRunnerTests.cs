using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaRunner"/>.
/// </summary>
public sealed class SagaRunnerTests
{
    private sealed record TestData
    {
        public int Value { get; init; }
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        var logger = NullLogger<SagaRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaRunner(null!, requestContext, logger));
    }

    [Fact]
    public void Constructor_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var logger = NullLogger<SagaRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaRunner(orchestrator, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var requestContext = Substitute.For<IRequestContext>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaRunner(orchestrator, requestContext, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var requestContext = Substitute.For<IRequestContext>();
        var logger = NullLogger<SagaRunner>.Instance;

        // Act
        var runner = new SagaRunner(orchestrator, requestContext, logger);

        // Assert
        runner.ShouldNotBeNull();
    }

    #endregion

    #region RunAsync - Basic Success

    [Fact]
    public async Task RunAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => runner.RunAsync<TestData>(null!).AsTask());
    }

    [Fact]
    public async Task RunAsync_WithNullInitialData_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
        ]);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => runner.RunAsync(definition, (TestData)null!).AsTask());
    }

    [Fact]
    public async Task RunAsync_WithDefaultData_ExecutesSuccessfully()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 10 })))
        ]);

        // Act
        var result = await runner.RunAsync(definition);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sagaResult = result.RightAsEnumerable().First();
        sagaResult.Data.Value.ShouldBe(10);
        sagaResult.StepsExecuted.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_SingleStep_Success_ReturnsResult()
    {
        // Arrange
        var runner = CreateRunner();
        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 42 })))
        ]);
        var initialData = new TestData { Value = 0 };

        // Act
        var result = await runner.RunAsync(definition, initialData);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sagaResult = result.RightAsEnumerable().First();
        sagaResult.Data.Value.ShouldBe(42);
        sagaResult.StepsExecuted.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_MultipleSteps_ExecutesInOrder()
    {
        // Arrange
        var runner = CreateRunner();
        var executedSteps = new List<string>();

        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, _, _) =>
            {
                executedSteps.Add("Step1");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 }));
            }),
            ("Step2", (data, _, _) =>
            {
                executedSteps.Add("Step2");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 1 }));
            }),
            ("Step3", (data, _, _) =>
            {
                executedSteps.Add("Step3");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 1 }));
            })
        ]);
        var initialData = new TestData { Value = 0 };

        // Act
        var result = await runner.RunAsync(definition, initialData);

        // Assert
        result.IsRight.ShouldBeTrue();
        var sagaResult = result.RightAsEnumerable().First();
        sagaResult.Data.Value.ShouldBe(3);
        sagaResult.StepsExecuted.ShouldBe(3);

        executedSteps.Count.ShouldBe(3);
        executedSteps[0].ShouldBe("Step1");
        executedSteps[1].ShouldBe("Step2");
        executedSteps[2].ShouldBe("Step3");
    }

    #endregion

    #region RunAsync - Step Failure and Compensation

    [Fact]
    public async Task RunAsync_StepFails_ReturnsError()
    {
        // Arrange
        var runner = CreateRunner();
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 }))),
            ("Step2", (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error))),
            ("Step3", (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 3 })))
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe("STEP_FAILED"),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task RunAsync_StepFails_RunsCompensation()
    {
        // Arrange
        var runner = CreateRunner();
        var compensated = new List<string>();
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        compensated.Count.ShouldBe(1);
        compensated[0].ShouldBe("Compensate1");
    }

    [Fact]
    public async Task RunAsync_MultipleStepsFail_RunsCompensationInReverseOrder()
    {
        // Arrange
        var runner = CreateRunner();
        var compensated = new List<string>();
        var error = EncinaErrors.Create("STEP_FAILED", "Step 3 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 2 })),
             (_, _, _) => { compensated.Add("Compensate2"); return Task.CompletedTask; }),
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        compensated.Count.ShouldBe(2);
        compensated[0].ShouldBe("Compensate2");
        compensated[1].ShouldBe("Compensate1");
    }

    #endregion

    #region RunAsync - Exception Handling

    [Fact]
    public async Task RunAsync_StepThrowsException_ReturnsErrorAndCompensates()
    {
        // Arrange
        var runner = CreateRunner();
        var compensated = new List<string>();

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             (_, _, _) => throw new InvalidOperationException("Unexpected error"),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(SagaErrorCodes.HandlerFailed),
            () => throw new InvalidOperationException("Expected error code"));

        compensated.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_CompensationThrows_ContinuesWithOtherCompensations()
    {
        // Arrange
        var runner = CreateRunner();
        var compensated = new List<string>();
        var error = EncinaErrors.Create("STEP_FAILED", "Step 3 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 2 })),
             (_, _, _) => throw new InvalidOperationException("Compensation failed")),
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        // SagaRunner continues with other compensations even when one fails
        compensated.Count.ShouldBe(1);
        compensated[0].ShouldBe("Compensate1");
    }

    #endregion

    #region RunAsync - Cancellation

    [Fact]
    public async Task RunAsync_WhenCancelled_ReturnsErrorAndCompensates()
    {
        // Arrange
        var runner = CreateRunner();
        var compensated = new List<string>();
        var cts = new CancellationTokenSource();

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             async (data, _, ct) =>
             {
                 await cts.CancelAsync();
                 ct.ThrowIfCancellationRequested();
                 return Right<EncinaError, TestData>(data with { Value = 2 });
             },
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData(), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            code => code.ShouldBe(SagaErrorCodes.HandlerCancelled),
            () => throw new InvalidOperationException("Expected error code"));

        compensated.Count.ShouldBe(1);
    }

    #endregion

    #region Helper Methods

    private static SagaOrchestrator CreateOrchestrator()
    {
        var sagaStore = Substitute.For<ISagaStore>();
        var options = new SagaOptions();
        var logger = NullLogger<SagaOrchestrator>.Instance;
        var stateFactory = Substitute.For<ISagaStateFactory>();

        // Setup the mock to return a proper saga state
        var mockState = Substitute.For<ISagaState>();
        mockState.SagaId.Returns(Guid.NewGuid());
        mockState.Status.Returns(SagaStatus.Running);
        mockState.Data.Returns("{}");
        mockState.CurrentStep.Returns(0);

        stateFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>())
            .Returns(mockState);

        sagaStore.AddAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        sagaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(mockState);

        sagaStore.UpdateAsync(Arg.Any<ISagaState>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new SagaOrchestrator(sagaStore, options, logger, stateFactory);
    }

    private static SagaRunner CreateRunner()
    {
        var orchestrator = CreateOrchestrator();
        var requestContext = Substitute.For<IRequestContext>();
        var logger = NullLogger<SagaRunner>.Instance;
        return new SagaRunner(orchestrator, requestContext, logger);
    }

    private static BuiltSagaDefinition<TestData> CreateDefinition(
        IReadOnlyList<(string Name, Func<TestData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TestData>>> Execute)>? steps = null,
        IReadOnlyList<(string Name, Func<TestData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TestData>>> Execute, Func<TestData, IRequestContext, CancellationToken, Task>? Compensate)>? stepsWithCompensation = null)
    {
        var definition = SagaDefinition.Create<TestData>("TestSaga");
        SagaStepBuilder<TestData>? lastStepBuilder = null;

        if (steps is not null)
        {
            foreach (var (name, execute) in steps)
            {
                if (lastStepBuilder is not null)
                {
                    // Chain from previous step builder
                    lastStepBuilder = lastStepBuilder.Step(name).Execute(execute);
                }
                else
                {
                    // Start from definition
                    lastStepBuilder = definition.Step(name).Execute(execute);
                }
            }
        }

        if (stepsWithCompensation is not null)
        {
            foreach (var (name, execute, compensate) in stepsWithCompensation)
            {
                SagaStepBuilder<TestData> stepBuilder;

                if (lastStepBuilder is not null)
                {
                    // Chain from previous step builder
                    stepBuilder = lastStepBuilder.Step(name).Execute(execute);
                }
                else
                {
                    // Start from definition
                    stepBuilder = definition.Step(name).Execute(execute);
                }

                if (compensate is not null)
                {
                    // Compensate returns SagaDefinition, so we lose the step builder chain
                    // We need to use the definition for the next step
                    stepBuilder.Compensate(compensate);
                    lastStepBuilder = null; // Reset - next step must start from definition
                }
                else
                {
                    // No compensation - keep the step builder for chaining
                    lastStepBuilder = stepBuilder;
                }
            }
        }

        return lastStepBuilder?.Build() ?? definition.Build();
    }

    #endregion
}
