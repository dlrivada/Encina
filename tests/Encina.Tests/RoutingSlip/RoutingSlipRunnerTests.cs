using Encina.Messaging.RoutingSlip;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.RoutingSlip;

public sealed class RoutingSlipRunnerTests
{
    private readonly IRequestContext _requestContext;
    private readonly RoutingSlipOptions _options;
    private readonly ILogger<RoutingSlipRunner> _logger;
    private readonly RoutingSlipRunner _sut;

    public RoutingSlipRunnerTests()
    {
        _requestContext = RequestContext.Create();
        _options = new RoutingSlipOptions();
        _logger = Substitute.For<ILogger<RoutingSlipRunner>>();
        _sut = new RoutingSlipRunner(_requestContext, _options, _logger);
    }

    [Fact]
    public void Constructor_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RoutingSlipRunner(null!, _options, _logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RoutingSlipRunner(_requestContext, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RoutingSlipRunner(_requestContext, _options, null!));
    }

    [Fact]
    public async Task RunAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.RunAsync<TestData>(null!));
    }

    [Fact]
    public async Task RunAsync_WithDefaultInitialData_CreatesNewInstance()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_WithSingleSuccessfulStep_ReturnsRight()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) =>
            {
                data.Id = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        var initialData = new TestData();

        // Act
        var result = await _sut.RunAsync(definition, initialData);

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.StepsExecuted.ShouldBe(1);
        slipResult.FinalData.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RunAsync_WithMultipleSuccessfulSteps_ExecutesAllSteps()
    {
        // Arrange
        var step1Executed = false;
        var step2Executed = false;
        var step3Executed = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ct) =>
            {
                step1Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 2")
            .Execute((data, ct) =>
            {
                step2Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 3")
            .Execute((data, ct) =>
            {
                step3Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        step1Executed.ShouldBeTrue();
        step2Executed.ShouldBeTrue();
        step3Executed.ShouldBeTrue();
        result.Match(Right: r => r.StepsExecuted, Left: _ => 0).ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_WhenStepFails_ReturnsLeft()
    {
        // Arrange
        var expectedError = EncinaErrors.Create("test.error", "Step failed");

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(expectedError)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => EncinaErrors.Create("none", "none"), Left: e => e);
        error.GetCode().Match(Some: c => c, None: () => "").ShouldBe("test.error");
    }

    [Fact]
    public async Task RunAsync_WhenStepFails_ExecutesCompensation()
    {
        // Arrange
        var step1Compensated = false;
        var step2Compensated = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ct) =>
            {
                data.ReservationId = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) =>
            {
                step1Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Step 2")
            .Execute((data, ct) =>
            {
                data.PaymentId = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) =>
            {
                step2Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestData());

        // Assert - Both completed steps should be compensated
        step1Compensated.ShouldBeTrue();
        step2Compensated.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_CompensationRunsInReverseOrder()
    {
        // Arrange
        var compensationOrder = new List<int>();

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(1);
                return Task.CompletedTask;
            })
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(2);
                return Task.CompletedTask;
            })
            .Step("Step 3")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(3);
                return Task.CompletedTask;
            })
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestData());

        // Assert - Should be 3, 2, 1 (reverse order)
        compensationOrder.ShouldBe([3, 2, 1]);
    }

    [Fact]
    public async Task RunAsync_StepWithoutCompensation_SkipsCompensation()
    {
        // Arrange
        var step1Compensated = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) =>
            {
                step1Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Step 2 - No Compensation")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestData());

        // Assert
        step1Compensated.ShouldBeTrue(); // Step 1 should still be compensated
    }

    [Fact]
    public async Task RunAsync_CompensationFailure_ContinuesWithOtherCompensations()
    {
        // Arrange
        var step1Compensated = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) =>
            {
                step1Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => throw new InvalidOperationException("Compensation failed"))
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act - Should not throw
        await _sut.RunAsync(definition, new TestData());

        // Assert - Step 1 compensation should still run despite Step 2 compensation failing
        step1Compensated.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_DataFlowsBetweenSteps()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Set Order")
            .Execute((data, ct) =>
            {
                data.Id = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Set Reservation")
            .Execute((data, ct) =>
            {
                data.ReservationId = data.Id;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Set Payment")
            .Execute((data, ct) =>
            {
                data.PaymentId = data.ReservationId;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.FinalData.Id.ShouldNotBe(Guid.Empty);
        slipResult.FinalData.ReservationId.ShouldBe(slipResult.FinalData.Id);
        slipResult.FinalData.PaymentId.ShouldBe(slipResult.FinalData.ReservationId);
    }

    [Fact]
    public async Task RunAsync_ReturnsCorrectRoutingSlipId()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.RoutingSlipId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RunAsync_ExecutesCompletionHandler()
    {
        // Arrange
        var completionExecuted = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .OnCompletion((data, ctx, ct) =>
            {
                completionExecuted = true;
                return Task.CompletedTask;
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        completionExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_RecordsDuration()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Test")
            .Execute(async (data, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, TestData>(data);
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunAsync_RecordsActivityLog()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Step("Step2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.ActivityLog.Count.ShouldBe(2);
        slipResult.ActivityLog[0].StepName.ShouldBe("Step1");
        slipResult.ActivityLog[1].StepName.ShouldBe("Step2");
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_ReturnsError()
    {
        // Arrange
        var cts = new CancellationTokenSource();

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Long Running Step")
            .Execute(async (data, ct) =>
            {
                cts.Cancel();
                await Task.Delay(100, ct);
                return Right<EncinaError, TestData>(data);
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData(), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => EncinaErrors.Create("none", "none"), Left: e => e);
        error.GetCode().Match(Some: c => c, None: () => "").ShouldBe(RoutingSlipErrorCodes.HandlerCancelled);
    }

    [Fact]
    public async Task RunAsync_WhenExceptionThrown_ReturnsError()
    {
        // Arrange
        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Throwing Step")
            .Execute((data, ct) => throw new InvalidOperationException("Test exception"))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.Match(Right: _ => EncinaErrors.Create("none", "none"), Left: e => e);
        error.GetCode().Match(Some: c => c, None: () => "").ShouldBe(RoutingSlipErrorCodes.HandlerFailed);
    }

    // Dynamic route modification tests

    [Fact]
    public async Task RunAsync_WithDynamicStepAddition_ExecutesAddedStep()
    {
        // Arrange
        var dynamicStepExecuted = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                // Dynamically add a new step
                ctx.AddStep(new RoutingSlipStepDefinition<TestData>(
                    "Dynamic Step",
                    (d, c, t) =>
                    {
                        dynamicStepExecuted = true;
                        return ValueTask.FromResult(Right<EncinaError, TestData>(d));
                    }));
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        dynamicStepExecuted.ShouldBeTrue();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.StepsExecuted.ShouldBe(2);
        slipResult.StepsAdded.ShouldBe(1);
    }

    [Fact]
    public async Task RunAsync_WithAddStepNext_InsertedStepExecutesImmediately()
    {
        // Arrange
        var executionOrder = new List<string>();

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                executionOrder.Add("Step 1");
                ctx.AddStepNext(new RoutingSlipStepDefinition<TestData>(
                    "Inserted Step",
                    (d, c, t) =>
                    {
                        executionOrder.Add("Inserted Step");
                        return ValueTask.FromResult(Right<EncinaError, TestData>(d));
                    }));
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Step("Step 2")
            .Execute((data, ctx, ct) =>
            {
                executionOrder.Add("Step 2");
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        executionOrder.ShouldBe(["Step 1", "Inserted Step", "Step 2"]);
    }

    [Fact]
    public async Task RunAsync_WithClearRemainingSteps_TerminatesEarly()
    {
        // Arrange
        var step2Executed = false;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                ctx.ClearRemainingSteps();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Step("Step 2")
            .Execute((data, ct) =>
            {
                step2Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
        step2Executed.ShouldBeFalse();
        var slipResult = result.Match(Right: r => r, Left: _ => null!);
        slipResult.StepsExecuted.ShouldBe(1);
        slipResult.StepsRemoved.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task RunAsync_ContextProvidesRequestContext()
    {
        // Arrange
        IRequestContext? capturedContext = null;

        var definition = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                capturedContext = ctx.RequestContext;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestData());

        // Assert
        capturedContext.ShouldNotBeNull();
        capturedContext.ShouldBe(_requestContext);
    }

    private sealed class TestData
    {
        public Guid Id { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? PaymentId { get; set; }
    }
}
