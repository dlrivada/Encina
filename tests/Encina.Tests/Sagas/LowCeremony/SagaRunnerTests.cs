using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Sagas.LowCeremony;

public sealed class SagaRunnerTests
{
    private readonly ISagaStore _sagaStore;
    private readonly ISagaStateFactory _sagaStateFactory;
    private readonly SagaOrchestrator _orchestrator;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<SagaRunner> _logger;
    private readonly SagaRunner _sut;

    public SagaRunnerTests()
    {
        _sagaStore = Substitute.For<ISagaStore>();
        _sagaStateFactory = Substitute.For<ISagaStateFactory>();
        _requestContext = RequestContext.Create();
        _logger = Substitute.For<ILogger<SagaRunner>>();

        var orchestratorLogger = Substitute.For<ILogger<SagaOrchestrator>>();
        var options = new SagaOptions();

        // Setup factory to return mock saga states
        _sagaStateFactory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime?>())
            .Returns(callInfo => new TestSagaState
            {
                SagaId = callInfo.ArgAt<Guid>(0),
                SagaType = callInfo.ArgAt<string>(1),
                Data = callInfo.ArgAt<string>(2),
                Status = callInfo.ArgAt<string>(3),
                CurrentStep = callInfo.ArgAt<int>(4),
                StartedAtUtc = callInfo.ArgAt<DateTime>(5),
                TimeoutAtUtc = callInfo.ArgAt<DateTime?>(6),
                LastUpdatedAtUtc = DateTime.UtcNow
            });

        _orchestrator = new SagaOrchestrator(_sagaStore, options, orchestratorLogger, _sagaStateFactory);
        _sut = new SagaRunner(_orchestrator, _requestContext, _logger);
    }

    [Fact]
    public void Constructor_WithNullOrchestrator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SagaRunner(null!, _requestContext, _logger));
    }

    [Fact]
    public void Constructor_WithNullRequestContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SagaRunner(_orchestrator, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SagaRunner(_orchestrator, _requestContext, null!));
    }

    [Fact]
    public async Task RunAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.RunAsync<TestSagaData>(null!));
    }

    [Fact]
    public async Task RunAsync_WithDefaultInitialData_CreatesNewInstance()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task RunAsync_WithSingleSuccessfulStep_ReturnsRight()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test Step")
            .Execute((data, ct) =>
            {
                data.OrderId = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        var initialData = new TestSagaData();

        // Act
        var result = await _sut.RunAsync(definition, initialData);

        // Assert
        var sagaResult = result.ShouldBeSuccess();
        sagaResult.StepsExecuted.ShouldBe(1);
        sagaResult.Data.OrderId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RunAsync_WithMultipleSuccessfulSteps_ExecutesAllSteps()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var step1Executed = false;
        var step2Executed = false;
        var step3Executed = false;

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) =>
            {
                step1Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 2")
            .Execute((data, ct) =>
            {
                step2Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 3")
            .Execute((data, ct) =>
            {
                step3Executed = true;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        var sagaResult = result.ShouldBeSuccess();
        step1Executed.ShouldBeTrue();
        step2Executed.ShouldBeTrue();
        step3Executed.ShouldBeTrue();
        sagaResult.StepsExecuted.ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_WhenStepFails_ReturnsLeft()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var expectedError = EncinaErrors.Create("test.error", "Step failed");

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestSagaData>(expectedError)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        var error = result.ShouldBeError();
        error.GetCode().Match(Some: c => c, None: () => "").ShouldBe("test.error");
    }

    [Fact]
    public async Task RunAsync_WhenStepFails_ExecutesCompensation()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var step1Compensated = false;
        var step2Compensated = false;

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) =>
            {
                data.ReservationId = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
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
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Compensate((data, ct) =>
            {
                step2Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestSagaData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestSagaData());

        // Assert - Both completed steps should be compensated in reverse order
        step1Compensated.ShouldBeTrue();
        step2Compensated.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_CompensationRunsInReverseOrder()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var compensationOrder = new List<int>();

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(1);
                return Task.CompletedTask;
            })
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(2);
                return Task.CompletedTask;
            })
            .Step("Step 3")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                compensationOrder.Add(3);
                return Task.CompletedTask;
            })
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestSagaData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestSagaData());

        // Assert - Should be 3, 2, 1 (reverse order)
        compensationOrder.ShouldBe([3, 2, 1]);
    }

    [Fact]
    public async Task RunAsync_StepWithoutCompensation_SkipsCompensation()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var step1Compensated = false;

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                step1Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Step 2 - No Compensation")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Step("Failing Step") // Chained without compensation
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestSagaData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        step1Compensated.ShouldBeTrue(); // Step 1 should still be compensated
    }

    [Fact]
    public async Task RunAsync_CompensationFailure_ContinuesWithOtherCompensations()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var step1Compensated = false;
        var step2ThrowsException = true;

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                step1Compensated = true;
                return Task.CompletedTask;
            })
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Compensate((data, ct) =>
            {
                if (step2ThrowsException)
                    throw new InvalidOperationException("Compensation failed");
                return Task.CompletedTask;
            })
            .Step("Failing Step")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestSagaData>(EncinaErrors.Create("fail", "Failed"))))
            .Build();

        // Act - Should not throw
        await _sut.RunAsync(definition, new TestSagaData());

        // Assert - Step 1 compensation should still run despite Step 2 compensation failing
        step1Compensated.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_DataFlowsBetweenSteps()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Set Order")
            .Execute((data, ct) =>
            {
                data.OrderId = Guid.NewGuid();
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Set Reservation")
            .Execute((data, ct) =>
            {
                // Should have OrderId from previous step
                data.ReservationId = data.OrderId; // Use OrderId to verify data flow
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Set Payment")
            .Execute((data, ct) =>
            {
                // Should have ReservationId from previous step
                data.PaymentId = data.ReservationId;
                return ValueTask.FromResult(Right<EncinaError, TestSagaData>(data));
            })
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        var sagaResult = result.ShouldBeSuccess();
        sagaResult.Data.OrderId.ShouldNotBe(Guid.Empty);
        sagaResult.Data.ReservationId.ShouldBe(sagaResult.Data.OrderId);
        sagaResult.Data.PaymentId.ShouldBe(sagaResult.Data.ReservationId);
    }

    [Fact]
    public async Task RunAsync_ReturnsCorrectSagaId()
    {
        // Arrange
        SetupSuccessfulSagaExecution();

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .Build();

        // Act
        var result = await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        var sagaResult = result.ShouldBeSuccess();
        sagaResult.SagaId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_PassesTimeoutToOrchestrator()
    {
        // Arrange
        var capturedTimeout = (DateTime?)null;
        _sagaStateFactory.Create(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime?>())
            .Returns(callInfo =>
            {
                capturedTimeout = callInfo.ArgAt<DateTime?>(6);
                return new TestSagaState
                {
                    SagaId = callInfo.ArgAt<Guid>(0),
                    SagaType = callInfo.ArgAt<string>(1),
                    Data = callInfo.ArgAt<string>(2),
                    Status = callInfo.ArgAt<string>(3),
                    CurrentStep = callInfo.ArgAt<int>(4),
                    StartedAtUtc = callInfo.ArgAt<DateTime>(5),
                    TimeoutAtUtc = callInfo.ArgAt<DateTime?>(6),
                    LastUpdatedAtUtc = DateTime.UtcNow
                };
            });

        var definition = SagaDefinition.Create<TestSagaData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestSagaData>(data)))
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build();

        // Act
        await _sut.RunAsync(definition, new TestSagaData());

        // Assert
        capturedTimeout.ShouldNotBeNull();
    }

    private void SetupSuccessfulSagaExecution()
    {
        // Setup store to return saga state for updates
        _sagaStore.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new TestSagaState
            {
                SagaId = callInfo.ArgAt<Guid>(0),
                SagaType = "TestSaga",
                Data = "{}",
                Status = SagaStatus.Running,
                CurrentStep = 0,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            });
    }

    private sealed class TestSagaData
    {
        public Guid OrderId { get; set; }
        public Guid? ReservationId { get; set; }
        public Guid? PaymentId { get; set; }
    }

    private sealed class TestSagaState : ISagaState
    {
        public Guid SagaId { get; set; }
        public string SagaType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Status { get; set; } = SagaStatus.Running;
        public int CurrentStep { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; }
        public DateTime? TimeoutAtUtc { get; set; }
    }
}
