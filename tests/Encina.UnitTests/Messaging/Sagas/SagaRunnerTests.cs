using Encina.Messaging.Sagas;
using Encina.Messaging.Sagas.LowCeremony;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

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
        var requestContextAccessor = CreateAccessor();
        var logger = NullLogger<SagaRunner>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaRunner(null!, requestContextAccessor, logger));
    }

    [Fact]
    public void Constructor_WithNullRequestContextAccessor_ThrowsArgumentNullException()
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
        var requestContextAccessor = CreateAccessor();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaRunner(orchestrator, requestContextAccessor, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var orchestrator = CreateOrchestrator();
        var requestContextAccessor = CreateAccessor();
        var logger = NullLogger<SagaRunner>.Instance;

        // Act
        var runner = new SagaRunner(orchestrator, requestContextAccessor, logger);

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

    #region RunAsync - Request Context Propagation

    [Fact]
    public async Task RunAsync_OutsideDispatch_StepsReceiveTenantLessContext()
    {
        // Arrange: a real accessor with no ambient context set, as when a background job
        // invokes the saga directly instead of going through IEncina.Send/Publish/Stream.
        var accessor = new RequestContextAccessor();
        var orchestrator = CreateOrchestrator();
        var logger = NullLogger<SagaRunner>.Instance;
        var runner = new SagaRunner(orchestrator, accessor, logger);

        IRequestContext? observedContext = null;
        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, context, _) =>
            {
                observedContext = context;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
        ]);

        // Act
        var result = await runner.RunAsync(definition);

        // Assert
        result.IsRight.ShouldBeTrue();
        observedContext.ShouldNotBeNull();
        observedContext.TenantId.ShouldBeNull();
        observedContext.UserId.ShouldBeNull();
    }

    [Fact]
    public async Task RunAsync_WithTenantSetOnAccessor_StepsReceiveThatTenant()
    {
        // Arrange: the ambient context carries a tenant, as when a dispatch set it before
        // invoking the saga.
        var accessor = new RequestContextAccessor
        {
            RequestContext = RequestContext.CreateForTest(tenantId: "tenant-42")
        };
        var orchestrator = CreateOrchestrator();
        var logger = NullLogger<SagaRunner>.Instance;
        var runner = new SagaRunner(orchestrator, accessor, logger);

        IRequestContext? observedContext = null;
        var definition = CreateDefinition(steps:
        [
            ("Step1", (data, context, _) =>
            {
                observedContext = context;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            })
        ]);

        // Act
        var result = await runner.RunAsync(definition);

        // Assert
        result.IsRight.ShouldBeTrue();
        observedContext.ShouldNotBeNull();
        observedContext.TenantId.ShouldBe("tenant-42");
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
    public async Task RunAsync_StepFailsAndAllCompensationsSucceed_EndsCompensatedWithCompletedAt()
    {
        // Arrange
        var store = new FakeSagaStore();
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => Task.CompletedTask),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert: the caller still gets the original step error
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STEP_FAILED");

        var saga = store.GetSagas().Single();
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.CompletedAtUtc.ShouldNotBeNull();
        saga.ErrorMessage.ShouldBe("STEP_FAILED");
    }

    [Fact]
    public async Task RunAsync_ThirdStepFailsAfterTwoExecuted_EndsCompensated()
    {
        // Arrange
        var store = new FakeSagaStore();
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 3 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => Task.CompletedTask),
            ("Step2",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 2 })),
             (_, _, _) => Task.CompletedTask),
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert: every executed step is counted down, not just the first
        result.IsLeft.ShouldBeTrue();
        var saga = store.GetSagas().Single();
        saga.Status.ShouldBe(SagaStatus.Compensated);
        saga.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task RunAsync_SecondCompensationStepPersistFails_ReturnsOrchestratorError()
    {
        // Arrange: Update calls: 1-2 = AdvanceAsync, 3 = StartCompensationAsync,
        // 4 = first CompensateStepAsync, 5 = second CompensateStepAsync.
        var store = new FailingUpdateStore(failOnUpdate: 5);
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 3 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => Task.CompletedTask),
            ("Step2",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 2 })),
             (_, _, _) => Task.CompletedTask),
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STORE_DOWN");
    }

    [Fact]
    public async Task RunAsync_CompensationThrowsAndFailPersistFails_ReturnsOrchestratorError()
    {
        // Arrange: 1 = AdvanceAsync, 2 = StartCompensationAsync, 3 = FailAsync.
        var store = new FailingUpdateStore(failOnUpdate: 3);
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => throw new InvalidOperationException("boom")),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STORE_DOWN");
    }

    [Fact]
    public async Task RunAsync_FirstStepFails_EndsCompensated()
    {
        // Arrange
        var store = new FakeSagaStore();
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 1 failed");

        var definition = CreateDefinition(steps:
        [
            ("Step1", (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)))
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        store.GetSagas().Single().Status.ShouldBe(SagaStatus.Compensated);
    }

    [Fact]
    public async Task RunAsync_StepFailsAndCompensationThrows_EndsFailedWithCodeAndExceptionType()
    {
        // Arrange
        var store = new FakeSagaStore();
        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 3 failed");
        var compensated = new List<string>();

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated.Add("Compensate1"); return Task.CompletedTask; }),
            ("Step2",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 2 })),
             (_, _, _) => throw new InvalidOperationException("secret compensation detail")),
            ("Step3",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert: original error returned, the other compensation still ran, final state Failed
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STEP_FAILED");
        compensated.ShouldBe(["Compensate1"]);

        var saga = store.GetSagas().Single();
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.CompletedAtUtc.ShouldNotBeNull();
        var persisted = saga.ErrorMessage.ShouldNotBeNull();
        persisted.ShouldBe($"{SagaErrorCodes.CompensationFailed}:{nameof(InvalidOperationException)}");
        persisted.ShouldNotContain("secret");
    }

    [Fact]
    public async Task RunAsync_StepFails_PersistsCompensatingBeforeCompensationsRun()
    {
        // Arrange
        var store = new FakeSagaStore();
        var runner = CreateRunner(store);
        string? statusDuringCompensation = null;
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { statusDuringCompensation = store.GetSagas().Single().Status; return Task.CompletedTask; }),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        await runner.RunAsync(definition, new TestData());

        // Assert
        statusDuringCompensation.ShouldBe(SagaStatus.Compensating);
    }

    [Fact]
    public async Task RunAsync_StepFailsAndStartCompensationFails_ReturnsOrchestratorError()
    {
        // Arrange
        // Update calls: 1 = AdvanceAsync of Step1, 2 = StartCompensationAsync.
        var store = new FailingUpdateStore(failOnUpdate: 2);

        var runner = CreateRunner(store);
        var compensated = false;
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => { compensated = true; return Task.CompletedTask; }),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert: the orchestrator failure is returned, never reported as success or as the step error
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STORE_DOWN");
        compensated.ShouldBeFalse();
    }

    [Fact]
    public async Task RunAsync_CompensationsSucceedButTerminalPersistFails_ReturnsOrchestratorError()
    {
        // Arrange: Update calls: 1 = AdvanceAsync of Step1, 2 = StartCompensationAsync, 3 = CompensateStepAsync.
        var store = new FailingUpdateStore(failOnUpdate: 3);

        var runner = CreateRunner(store);
        var error = EncinaErrors.Create("STEP_FAILED", "Step 2 failed");

        var definition = CreateDefinition(stepsWithCompensation:
        [
            ("Step1",
             (data, _, _) => ValueTask.FromResult(Right<EncinaError, TestData>(data with { Value = 1 })),
             (_, _, _) => Task.CompletedTask),
            ("Step2",
             (_, _, _) => ValueTask.FromResult(Left<EncinaError, TestData>(error)),
             null)
        ]);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.LeftAsEnumerable().First().GetCode().IfNone(string.Empty).ShouldBe("STORE_DOWN");
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
        resultError.Message.ShouldNotContain("Unexpected error");

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

    /// <summary>Wraps a <see cref="FakeSagaStore"/> and fails the Nth UpdateAsync call with a "STORE_DOWN" error.</summary>
    private sealed class FailingUpdateStore(int failOnUpdate) : ISagaStore
    {
        private readonly FakeSagaStore _inner = new();
        private int _updates;

        public Task<Either<EncinaError, Option<ISagaState>>> GetAsync(Guid sagaId, CancellationToken cancellationToken = default)
            => _inner.GetAsync(sagaId, cancellationToken);

        public Task<Either<EncinaError, LanguageExt.Unit>> AddAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
            => _inner.AddAsync(sagaState, cancellationToken);

        public Task<Either<EncinaError, LanguageExt.Unit>> UpdateAsync(ISagaState sagaState, CancellationToken cancellationToken = default)
        {
            if (++_updates == failOnUpdate)
            {
                return Task.FromResult<Either<EncinaError, LanguageExt.Unit>>(
                    EncinaErrors.Create("STORE_DOWN", "store unavailable"));
            }

            return _inner.UpdateAsync(sagaState, cancellationToken);
        }

        public Task<Either<EncinaError, IEnumerable<ISagaState>>> GetStuckSagasAsync(TimeSpan olderThan, int batchSize, CancellationToken cancellationToken = default)
            => _inner.GetStuckSagasAsync(olderThan, batchSize, cancellationToken);

        public Task<Either<EncinaError, IEnumerable<ISagaState>>> GetExpiredSagasAsync(int batchSize, CancellationToken cancellationToken = default)
            => _inner.GetExpiredSagasAsync(batchSize, cancellationToken);

        public Task<Either<EncinaError, LanguageExt.Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _inner.SaveChangesAsync(cancellationToken);
    }

    private sealed class FakeStateFactory : ISagaStateFactory
    {
        public ISagaState Create(
            Guid sagaId,
            string sagaType,
            string data,
            string status,
            int currentStep,
            DateTime startedAtUtc,
            DateTime? timeoutAtUtc = null) => new FakeSagaState
            {
                SagaId = sagaId,
                SagaType = sagaType,
                Data = data,
                Status = status,
                CurrentStep = currentStep,
                StartedAtUtc = startedAtUtc,
                LastUpdatedAtUtc = startedAtUtc,
                TimeoutAtUtc = timeoutAtUtc
            };
    }

    private static SagaOrchestrator CreateOrchestrator(ISagaStore? store = null)
    {
        // A stateful in-memory store: the orchestrator's status transitions are real, so the
        // tests observe the persisted final state of the saga.
        return new SagaOrchestrator(
            store ?? new FakeSagaStore(),
            new SagaOptions(),
            NullLogger<SagaOrchestrator>.Instance,
            new FakeStateFactory(),
            new JsonMessageSerializer());
    }

    private static SagaRunner CreateRunner(ISagaStore? store = null)
    {
        var orchestrator = CreateOrchestrator(store);
        var requestContextAccessor = CreateAccessor();
        var logger = NullLogger<SagaRunner>.Instance;
        return new SagaRunner(orchestrator, requestContextAccessor, logger);
    }

    private static IRequestContextAccessor CreateAccessor()
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(Substitute.For<IRequestContext>());
        return accessor;
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
