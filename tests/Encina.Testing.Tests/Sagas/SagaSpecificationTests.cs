using LanguageExt;
using Shouldly;
using Encina.Testing.Sagas;
using static LanguageExt.Prelude;

namespace Encina.Testing.Tests.Sagas;

public sealed class SagaSpecificationTests
{
    #region Test Infrastructure

    public sealed class TestSagaData
    {
        public string? Step1Result { get; set; }
        public string? Step2Result { get; set; }
        public string? Step3Result { get; set; }
        public bool Step1Compensated { get; set; }
        public bool Step2Compensated { get; set; }
        // Custom fields for testing GivenData accumulation (not modified by saga execution)
        public string? CustomField1 { get; set; }
        public string? CustomField2 { get; set; }
    }

    public sealed class TestSaga
    {
        private readonly bool _failAtStep;
        private readonly int _failStepIndex;

        public TestSaga(bool failAtStep = false, int failStepIndex = 0)
        {
            _failAtStep = failAtStep;
            _failStepIndex = failStepIndex;
        }

        public async ValueTask<Either<EncinaError, TestSagaData>> ExecuteAsync(
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            for (var step = fromStep; step < 3; step++)
            {
                if (_failAtStep && step == _failStepIndex)
                {
                    return EncinaErrors.Create("saga.step.failed", $"Step {step} failed");
                }

                switch (step)
                {
                    case 0:
                        data.Step1Result = "Step1 completed";
                        break;
                    case 1:
                        data.Step2Result = "Step2 completed";
                        break;
                    case 2:
                        data.Step3Result = "Step3 completed";
                        break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Task.CompletedTask;
            }

            return data;
        }

#pragma warning disable CA1822 // Method doesn't access instance data but matches saga interface pattern
        public async Task CompensateAsync(
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            for (var step = fromStep; step >= 0; step--)
            {
                switch (step)
                {
                    case 0:
                        data.Step1Compensated = true;
                        break;
                    case 1:
                        data.Step2Compensated = true;
                        break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await Task.CompletedTask;
            }
        }
#pragma warning restore CA1822
    }

    public sealed class ThrowingSaga
    {
#pragma warning disable CA1822 // Method doesn't access instance data but is used polymorphically in tests
        public ValueTask<Either<EncinaError, TestSagaData>> ExecuteAsync(
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Saga execution crashed");
        }

        public Task CompensateAsync(
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Saga compensation crashed");
        }
#pragma warning restore CA1822
    }

    // Base class for success scenarios
    private sealed class SuccessSagaSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.ExecuteAsync(data, fromStep, context, cancellationToken);

        protected override Task CompensateSagaAsync(
            TestSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.CompensateAsync(data, fromStep, context, cancellationToken);
    }

    // Base class for failure scenarios
    private sealed class FailingSagaSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        private readonly int _failAtStep;

        public FailingSagaSpec(int failAtStep = 1)
        {
            _failAtStep = failAtStep;
        }

        protected override TestSaga CreateSaga() => new(failAtStep: true, failStepIndex: _failAtStep);

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.ExecuteAsync(data, fromStep, context, cancellationToken);

        protected override Task CompensateSagaAsync(
            TestSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.CompensateAsync(data, fromStep, context, cancellationToken);
    }

    // Base class for throwing scenarios
    private sealed class ThrowingSagaSpec : SagaSpecification<ThrowingSaga, TestSagaData>
    {
        protected override ThrowingSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            ThrowingSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.ExecuteAsync(data, fromStep, context, cancellationToken);

        protected override Task CompensateSagaAsync(
            ThrowingSaga saga,
            TestSagaData data,
            int fromStep,
            IRequestContext context,
            CancellationToken cancellationToken)
            => saga.CompensateAsync(data, fromStep, context, cancellationToken);
    }

    #endregion

    #region GivenData Tests

    [Fact]
    public async Task GivenData_AccumulatesModifications()
    {
        // Arrange
        var spec = new GivenDataTestSpec();

        // Act - set custom values that saga execution will NOT overwrite
        spec.CallGivenData(data => data.CustomField1 = "first");
        spec.CallGivenData(data => data.CustomField2 = "second");
        await spec.CallWhenComplete();

        // Assert - verify GivenData modifications were accumulated and persist after execution
        spec.CallThenData(data =>
        {
            // Custom fields should retain their GivenData values
            data.CustomField1.ShouldBe("first");
            data.CustomField2.ShouldBe("second");
            // Step fields are modified by saga execution
            data.Step1Result.ShouldBe("Step1 completed");
        });
    }

    [Fact]
    public void GivenData_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new GivenDataTestSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => spec.CallGivenData(null!));
    }

    [Fact]
    public void GivenSagaData_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new GivenDataTestSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => spec.CallGivenSagaData(null!));
    }

    private sealed class GivenDataTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public void CallGivenData(Action<TestSagaData> configure) => GivenData(configure);
        public void CallGivenSagaData(TestSagaData data) => GivenSagaData(data);
        public Task CallWhenComplete() => WhenComplete();
        public void CallThenData(Action<TestSagaData> validate) => ThenData(validate);
    }

    #endregion

    #region WhenComplete Tests

    [Fact]
    public async Task WhenComplete_ExecutesAllSteps()
    {
        // Arrange
        var spec = new WhenCompleteTestSpec();

        // Act
        await spec.CallWhenComplete();

        // Assert
        spec.CallThenSuccess(data =>
        {
            data.Step1Result.ShouldBe("Step1 completed");
            data.Step2Result.ShouldBe("Step2 completed");
            data.Step3Result.ShouldBe("Step3 completed");
        });
    }

    [Fact]
    public async Task WhenComplete_StartsFromStep0()
    {
        // Arrange
        var spec = new WhenCompleteTestSpec();

        // Act
        await spec.CallWhenComplete();

        // Assert
        spec.GetExecutedFromStep().ShouldBe(0);
    }

    private sealed class WhenCompleteTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public TestSagaData CallThenSuccess(Action<TestSagaData>? validate = null) => ThenSuccess(validate);
        public int GetExecutedFromStep() => ExecutedFromStep;
    }

    #endregion

    #region WhenStep Tests

    [Fact]
    public async Task WhenStep_ExecutesFromSpecificStep()
    {
        // Arrange
        var spec = new WhenStepTestSpec();

        // Act
        await spec.CallWhenStep(1);

        // Assert
        spec.CallThenSuccess(data =>
        {
            data.Step1Result.ShouldBeNull(); // Step 0 was skipped
            data.Step2Result.ShouldBe("Step2 completed");
            data.Step3Result.ShouldBe("Step3 completed");
        });
    }

    [Fact]
    public async Task WhenStep_SetsExecutedFromStep()
    {
        // Arrange
        var spec = new WhenStepTestSpec();

        // Act
        await spec.CallWhenStep(2);

        // Assert
        spec.GetExecutedFromStep().ShouldBe(2);
    }

    private sealed class WhenStepTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenStep(int step) => WhenStep(step);
        public TestSagaData CallThenSuccess(Action<TestSagaData>? validate = null) => ThenSuccess(validate);
        public int GetExecutedFromStep() => ExecutedFromStep;
    }

    #endregion

    #region WhenCompensate Tests

    [Fact]
    public async Task WhenCompensate_ExecutesCompensation()
    {
        // Arrange
        var spec = new WhenCompensateTestSpec();

        // Act
        await spec.CallWhenCompensate(1);

        // Assert
        spec.CallThenCompensated();
        spec.CallThenData(data =>
        {
            data.Step1Compensated.ShouldBeTrue();
            data.Step2Compensated.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task WhenCompensate_SetsCompensatedFromStep()
    {
        // Arrange
        var spec = new WhenCompensateTestSpec();

        // Act
        await spec.CallWhenCompensate(2);

        // Assert
        spec.GetCompensatedFromStep().ShouldBe(2);
    }

    private sealed class WhenCompensateTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenCompensate(int step) => WhenCompensate(step);
        public void CallThenCompensated() => ThenCompensated();
        public void CallThenData(Action<TestSagaData> validate) => ThenData(validate);
        public int GetCompensatedFromStep() => CompensatedFromStep;
    }

    #endregion

    #region ThenSuccess Tests

    [Fact]
    public async Task ThenSuccess_WhenSagaSucceeds_ReturnsData()
    {
        // Arrange
        var spec = new ThenSuccessTestSpec();
        await spec.CallWhenComplete();

        // Act
        var data = spec.CallThenSuccess();

        // Assert
        data.Step1Result.ShouldNotBeNull();
    }

    [Fact]
    public async Task ThenSuccess_WithValidator_ExecutesValidator()
    {
        // Arrange
        var spec = new ThenSuccessTestSpec();
        await spec.CallWhenComplete();
        var validated = false;

        // Act
        spec.CallThenSuccess(data =>
        {
            data.Step1Result.ShouldBe("Step1 completed");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenSuccess_WhenSagaFails_Throws()
    {
        // Arrange
        var spec = new ThenSuccessFailingSpec();
        await spec.CallWhenComplete();

        // Act & Assert: capture any thrown exception and assert on message to avoid
        // coupling to xUnit internal exception types.
        var ex = Should.Throw<Exception>(() => spec.CallThenSuccess());
        ex.Message.ShouldContain("Expected saga success");
    }

    [Fact]
    public void ThenSuccess_BeforeWhen_ThrowsInvalidOperationException()
    {
        // Arrange
        var spec = new ThenSuccessTestSpec();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => spec.CallThenSuccess());
        ex.Message.ShouldContain("When");
    }

    [Fact]
    public async Task ThenSuccessAnd_ReturnsAndConstraint()
    {
        // Arrange
        var spec = new ThenSuccessTestSpec();
        await spec.CallWhenComplete();

        // Act
        var constraint = spec.CallThenSuccessAnd();

        // Assert
        constraint.Value.Step1Result.ShouldNotBeNull();
    }

    private sealed class ThenSuccessTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public TestSagaData CallThenSuccess(Action<TestSagaData>? validate = null) => ThenSuccess(validate);
        public AndConstraint<TestSagaData> CallThenSuccessAnd() => ThenSuccessAnd();
    }

    private sealed class ThenSuccessFailingSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new(failAtStep: true, failStepIndex: 1);

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public TestSagaData CallThenSuccess() => ThenSuccess();
    }

    #endregion

    #region ThenError Tests

    [Fact]
    public async Task ThenError_WhenSagaFails_ReturnsError()
    {
        // Arrange
        var spec = new ThenErrorTestSpec();
        await spec.CallWhenComplete();

        // Act
        var error = spec.CallThenError();

        // Assert
        error.Message.ShouldContain("Step 1 failed");
    }

    [Fact]
    public async Task ThenError_WithValidator_ExecutesValidator()
    {
        // Arrange
        var spec = new ThenErrorTestSpec();
        await spec.CallWhenComplete();
        var validated = false;

        // Act
        spec.CallThenError(e =>
        {
            e.Message.ShouldContain("failed");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenError_WhenSagaSucceeds_Throws()
    {
        // Arrange
        var spec = new ThenErrorSuccessSpec();
        await spec.CallWhenComplete();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenError());
    }

    [Fact]
    public async Task ThenErrorAnd_ReturnsAndConstraint()
    {
        // Arrange
        var spec = new ThenErrorTestSpec();
        await spec.CallWhenComplete();

        // Act
        var constraint = spec.CallThenErrorAnd();

        // Assert
        constraint.Value.Message.ShouldContain("failed");
    }

    [Fact]
    public async Task ThenErrorWithCode_MatchingCode_ReturnsError()
    {
        // Arrange
        var spec = new ThenErrorTestSpec();
        await spec.CallWhenComplete();

        // Act
        var error = spec.CallThenErrorWithCode("saga.step.failed");

        // Assert
        error.Message.ShouldNotBeNullOrEmpty();
    }

    private sealed class ThenErrorTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new(failAtStep: true, failStepIndex: 1);

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public EncinaError CallThenError(Action<EncinaError>? validate = null) => ThenError(validate);
        public AndConstraint<EncinaError> CallThenErrorAnd() => ThenErrorAnd();
        public EncinaError CallThenErrorWithCode(string code) => ThenErrorWithCode(code);
    }

    private sealed class ThenErrorSuccessSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public EncinaError CallThenError() => ThenError();
    }

    #endregion

    #region ThenThrows Tests

    [Fact]
    public async Task ThenThrows_WhenExceptionThrown_ReturnsException()
    {
        // Arrange
        var spec = new ThenThrowsTestSpec();
        await spec.CallWhenComplete();

        // Act
        var exception = spec.CallThenThrows<InvalidOperationException>();

        // Assert
        exception.Message.ShouldContain("crashed");
    }

    [Fact]
    public async Task ThenThrows_WrongExceptionType_Throws()
    {
        // Arrange
        var spec = new ThenThrowsTestSpec();
        await spec.CallWhenComplete();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            spec.CallThenThrows<ArgumentException>());
        ex.Message.ShouldContain("InvalidOperationException");
    }

    [Fact]
    public async Task ThenThrows_NoException_Throws()
    {
        // Arrange
        var spec = new ThenThrowsNoExceptionSpec();
        await spec.CallWhenComplete();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            spec.CallThenThrows<Exception>());
        ex.Message.ShouldContain("no exception was thrown");
    }

    private sealed class ThenThrowsTestSpec : SagaSpecification<ThrowingSaga, TestSagaData>
    {
        protected override ThrowingSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            ThrowingSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            ThrowingSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public TException CallThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
    }

    private sealed class ThenThrowsNoExceptionSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public TException CallThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
    }

    #endregion

    #region Saga State Assertions

    [Fact]
    public async Task ThenCompleted_WhenSuccess_DoesNotThrow()
    {
        // Arrange
        var spec = new SagaStateTestSpec();
        await spec.CallWhenComplete();

        // Act & Assert (should not throw)
        spec.CallThenCompleted();
    }

    [Fact]
    public async Task ThenCompleted_WhenFailed_Throws()
    {
        // Arrange
        var spec = new SagaStateFailingSpec();
        await spec.CallWhenComplete();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenCompleted());
    }

    [Fact]
    public async Task ThenFailed_WhenFailed_DoesNotThrow()
    {
        // Arrange
        var spec = new SagaStateFailingSpec();
        await spec.CallWhenComplete();

        // Act & Assert (should not throw)
        spec.CallThenFailed();
    }

    [Fact]
    public async Task ThenFailed_WithMessagePart_ValidatesMessage()
    {
        // Arrange
        var spec = new SagaStateFailingSpec();
        await spec.CallWhenComplete();

        // Act & Assert (should not throw)
        spec.CallThenFailed("Step 1");
    }

    [Fact]
    public async Task ThenCompensated_AfterWhenCompensate_DoesNotThrow()
    {
        // Arrange
        var spec = new SagaStateTestSpec();
        await spec.CallWhenCompensate(1);

        // Act & Assert (should not throw)
        spec.CallThenCompensated();
    }

    [Fact]
    public async Task ThenCompensated_WithoutWhenCompensate_Throws()
    {
        // Arrange
        var spec = new SagaStateTestSpec();
        await spec.CallWhenComplete();

        // Act & Assert
        Should.Throw<Xunit.Sdk.TrueException>(() => spec.CallThenCompensated());
    }

    private sealed class SagaStateTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public Task CallWhenCompensate(int step) => WhenCompensate(step);
        public void CallThenCompleted() => ThenCompleted();
        public void CallThenCompensated() => ThenCompensated();
        public void CallThenFailed(string? message = null) => ThenFailed(message);
    }

    private sealed class SagaStateFailingSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new(failAtStep: true, failStepIndex: 1);

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public Task CallWhenComplete() => WhenComplete();
        public void CallThenCompleted() => ThenCompleted();
        public void CallThenFailed(string? message = null) => ThenFailed(message);
    }

    #endregion

    #region ThenData Tests

    [Fact]
    public async Task ThenData_ValidatesDataState()
    {
        // Arrange
        var spec = new ThenDataTestSpec();
        spec.CallGivenData(data => data.Step1Result = "pre-set");
        await spec.CallWhenComplete();

        var validated = false;

        // Act
        spec.CallThenData(data =>
        {
            // The saga will have overwritten Step1Result
            data.Step1Result.ShouldBe("Step1 completed");
            validated = true;
        });

        // Assert
        validated.ShouldBeTrue();
    }

    [Fact]
    public void ThenData_NullValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var spec = new ThenDataTestSpec();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => spec.CallThenData(null!));
    }

    private sealed class ThenDataTestSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.ExecuteAsync(data, fromStep, context, ct);

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
            => saga.CompensateAsync(data, fromStep, context, ct);

        public void CallGivenData(Action<TestSagaData> configure) => GivenData(configure);
        public Task CallWhenComplete() => WhenComplete();
        public void CallThenData(Action<TestSagaData> validate) => ThenData(validate);
    }

    #endregion
}
