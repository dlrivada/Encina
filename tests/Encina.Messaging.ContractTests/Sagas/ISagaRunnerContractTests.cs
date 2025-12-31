using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Messaging.ContractTests.Sagas;

/// <summary>
/// Contract tests that verify ISagaRunner implementations follow the expected behavioral contract.
/// </summary>
public abstract class ISagaRunnerContractTests
{
    protected abstract ISagaRunner CreateSagaRunner();

    #region RunAsync Contract

    [Fact]
    public async Task RunAsync_WithValidDefinition_ReturnsEither()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var inputData = new TestData();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, inputData);

        // Assert
        var sagaResult = result.ShouldBeRight();
        sagaResult.Data.ShouldBe(inputData);
    }

    [Fact]
    public async Task RunAsync_WithSuccessfulSteps_ReturnsRight()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_WithFailingStep_ReturnsLeft()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var error = EncinaErrors.Create("test.error", "Test failure");
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Failing")
            .Execute((data, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(error)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_Success_ContainsSagaId()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var sagaResult = result.ShouldBeRight();
        sagaResult.SagaId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RunAsync_Success_ContainsCorrectStepsExecuted()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 2")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Step 3")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var sagaResult = result.ShouldBeRight();
        sagaResult.StepsExecuted.ShouldBe(3);
    }

    [Fact]
    public async Task RunAsync_WithDefaultData_CreatesNewInstance()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) =>
            {
                // Data should be new instance with default values
                data.Value.ShouldBe(0);
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await runner.RunAsync(definition);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Data Flow Contract

    [Fact]
    public async Task RunAsync_DataFlowsBetweenSteps()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Set Value")
            .Execute((data, ct) =>
            {
                data.Value = 42;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Compensate((data, ct) => Task.CompletedTask)
            .Step("Verify Value")
            .Execute((data, ct) =>
            {
                data.Value.ShouldBe(42);
                data.Verified = true;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var sagaResult = result.ShouldBeRight();
        sagaResult.Data.Value.ShouldBe(42);
        sagaResult.Data.Verified.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsync_FinalDataIsReturned()
    {
        // Arrange
        var runner = CreateSagaRunner();
        var expectedGuid = Guid.NewGuid();
        var definition = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Set Id")
            .Execute((data, ct) =>
            {
                data.Id = expectedGuid;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
            .Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var sagaResult = result.ShouldBeRight();
        sagaResult.Data.Id.ShouldBe(expectedGuid);
    }

    #endregion

    protected sealed class TestData
    {
        public Guid Id { get; set; }
        public int Value { get; set; }
        public bool Verified { get; set; }
    }
}

/// <summary>
/// Contract tests for the default SagaRunner implementation.
/// </summary>
public sealed class SagaRunnerContractTests : ISagaRunnerContractTests
{
    private readonly ISagaStore _sagaStore;
    private readonly ISagaStateFactory _sagaStateFactory;

    public SagaRunnerContractTests()
    {
        _sagaStore = Substitute.For<ISagaStore>();
        _sagaStateFactory = Substitute.For<ISagaStateFactory>();

        // Setup factory to return mock saga states
        _sagaStateFactory.Create(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<DateTime>(),
            Arg.Any<DateTime?>())
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

    protected override ISagaRunner CreateSagaRunner()
    {
        var orchestratorLogger = Substitute.For<ILogger<SagaOrchestrator>>();
        var runnerLogger = Substitute.For<ILogger<SagaRunner>>();
        var options = new SagaOptions();
        var requestContext = RequestContext.Create();

        var orchestrator = new SagaOrchestrator(_sagaStore, options, orchestratorLogger, _sagaStateFactory);
        return new SagaRunner(orchestrator, requestContext, runnerLogger);
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
