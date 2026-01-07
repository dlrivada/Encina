using Encina.Messaging.RoutingSlip;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.Messaging.Tests.RoutingSlip;

/// <summary>
/// Unit tests for <see cref="RoutingSlipRunner"/>.
/// </summary>
public sealed class RoutingSlipRunnerTests
{
    private sealed record TestData
    {
        public int Value { get; init; }
        public List<string> Steps { get; init; } = [];
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new RoutingSlipOptions();
        var logger = NullLogger<RoutingSlipRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        var logger = NullLogger<RoutingSlipRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(requestContext, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        var options = new RoutingSlipOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new RoutingSlipRunner(requestContext, options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var requestContext = Substitute.For<IRequestContext>();
        var options = new RoutingSlipOptions();
        var logger = NullLogger<RoutingSlipRunner>.Instance;

        // Act
        var runner = new RoutingSlipRunner(requestContext, options, logger);

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
        var slipResult = result.RightAsEnumerable().First();
        slipResult.FinalData.Value.ShouldBe(10);
        slipResult.StepsExecuted.ShouldBe(1);
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
        var slipResult = result.RightAsEnumerable().First();
        slipResult.FinalData.Value.ShouldBe(42);
        slipResult.StepsExecuted.ShouldBe(1);
        slipResult.StepsAdded.ShouldBe(0);
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
        var slipResult = result.RightAsEnumerable().First();
        slipResult.FinalData.Value.ShouldBe(3);
        slipResult.StepsExecuted.ShouldBe(3);

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

    [Fact]
    public async Task RunAsync_StepWithoutCompensation_SkipsCompensation()
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
             null), // No compensation
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        // Only Step1 has compensation, Step2 doesn't
        compensated.Count.ShouldBe(1);
        compensated[0].ShouldBe("Compensate1");
    }

    #endregion

    #region RunAsync - Dynamic Step Addition

    [Fact]
    public async Task RunAsync_StepAddsNewStep_ExecutesNewStep()
    {
        // Arrange
        var runner = CreateRunner();
        var executedSteps = new List<string>();

        var dynamicStep = new RoutingSlipStepDefinition<TestData>(
            "DynamicStep",
            execute: (data, _, _) =>
            {
                executedSteps.Add("DynamicStep");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 100 }));
            },
            compensate: null,
            metadata: null);

        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, context, _) =>
            {
                executedSteps.Add("Step1");
                context.AddStep(dynamicStep);
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 }));
            }),
            ("Step2", (data, _, _) =>
            {
                executedSteps.Add("Step2");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 10 }));
            })
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.RightAsEnumerable().First();
        slipResult.StepsExecuted.ShouldBe(3);
        slipResult.StepsAdded.ShouldBe(1);

        executedSteps.Count.ShouldBe(3);
        executedSteps[0].ShouldBe("Step1");
        executedSteps[1].ShouldBe("Step2");
        executedSteps[2].ShouldBe("DynamicStep");
    }

    [Fact]
    public async Task RunAsync_StepAddsNewStepNext_ExecutesNewStepBeforeOthers()
    {
        // Arrange
        var runner = CreateRunner();
        var executedSteps = new List<string>();

        var dynamicStep = new RoutingSlipStepDefinition<TestData>(
            "DynamicStep",
            execute: (data, _, _) =>
            {
                executedSteps.Add("DynamicStep");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 100 }));
            },
            compensate: null,
            metadata: null);

        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, context, _) =>
            {
                executedSteps.Add("Step1");
                context.AddStepNext(dynamicStep);
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 }));
            }),
            ("Step2", (data, _, _) =>
            {
                executedSteps.Add("Step2");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = data.Value + 10 }));
            })
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.RightAsEnumerable().First();
        slipResult.StepsExecuted.ShouldBe(3);
        slipResult.StepsAdded.ShouldBe(1);

        executedSteps.Count.ShouldBe(3);
        executedSteps[0].ShouldBe("Step1");
        executedSteps[1].ShouldBe("DynamicStep");
        executedSteps[2].ShouldBe("Step2");
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
            code => code.ShouldBe(RoutingSlipErrorCodes.HandlerCancelled),
            () => throw new InvalidOperationException("Expected error code"));

        compensated.Count.ShouldBe(1);
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
            code => code.ShouldBe(RoutingSlipErrorCodes.HandlerFailed),
            () => throw new InvalidOperationException("Expected error code"));

        compensated.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_CompensationThrows_WithContinueOnFailure_ContinuesCompensation()
    {
        // Arrange
        var options = new RoutingSlipOptions { ContinueCompensationOnFailure = true };
        var runner = CreateRunner(options);
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
        // Step2 compensation throws but we continue, Step1 compensation runs
        compensated.Count.ShouldBe(1);
        compensated[0].ShouldBe("Compensate1");
    }

    [Fact]
    public async Task RunAsync_CompensationThrows_WithoutContinueOnFailure_Throws()
    {
        // Arrange
        var options = new RoutingSlipOptions { ContinueCompensationOnFailure = false };
        var runner = CreateRunner(options);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => throw new InvalidOperationException("Compensation failed")),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => runner.RunAsync(definition, new TestData()).AsTask());
    }

    #endregion

    #region RunAsync - Completion Handler

    [Fact]
    public async Task RunAsync_WithCompletionHandler_ExecutesHandler()
    {
        // Arrange
        var runner = CreateRunner();
        var completionCalled = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
                .Execute((data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })))
            .OnCompletion((_, _, _) =>
            {
                completionCalled = true;
                return Task.CompletedTask;
            })
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        completionCalled.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static RoutingSlipRunner CreateRunner(RoutingSlipOptions? options = null)
    {
        var requestContext = Substitute.For<IRequestContext>();
        return new RoutingSlipRunner(
            requestContext,
            options ?? new RoutingSlipOptions(),
            NullLogger<RoutingSlipRunner>.Instance);
    }

    private static BuiltRoutingSlipDefinition<TestData> CreateDefinition(
        IReadOnlyList<(string Name, Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>> Execute)>? steps = null,
        IReadOnlyList<(string Name, Func<TestData, RoutingSlipContext<TestData>, CancellationToken, ValueTask<Either<EncinaError, TestData>>> Execute, Func<TestData, RoutingSlipContext<TestData>, CancellationToken, Task>? Compensate)>? stepsWithCompensation = null)
    {
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");
        RoutingSlipStepBuilder<TestData>? currentStepBuilder = null;

        if (steps is not null)
        {
            foreach (var (name, execute) in steps)
            {
                // Use Step() from current step builder to properly chain (and complete previous step)
                if (currentStepBuilder is not null)
                {
                    currentStepBuilder = currentStepBuilder.Step(name).Execute(execute);
                }
                else
                {
                    currentStepBuilder = builder.Step(name).Execute(execute);
                }
            }
        }

        if (stepsWithCompensation is not null)
        {
            foreach (var (name, execute, compensate) in stepsWithCompensation)
            {
                // Use Step() from current step builder to properly chain (and complete previous step)
                if (currentStepBuilder is not null)
                {
                    currentStepBuilder = currentStepBuilder.Step(name).Execute(execute);
                }
                else
                {
                    currentStepBuilder = builder.Step(name).Execute(execute);
                }

                if (compensate is not null)
                {
                    currentStepBuilder.Compensate(compensate);
                }
            }
        }

        // Build via the step builder if we have one, otherwise the parent builder
        return currentStepBuilder?.Build() ?? builder.Build();
    }

    #endregion
}
