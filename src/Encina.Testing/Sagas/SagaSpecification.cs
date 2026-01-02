using LanguageExt;
using Xunit;

namespace Encina.Testing.Sagas;

/// <summary>
/// Base class for testing sagas using the Given/When/Then BDD pattern.
/// Provides a structured approach to saga testing with explicit phases.
/// </summary>
/// <typeparam name="TSaga">The type of saga being tested.</typeparam>
/// <typeparam name="TSagaData">The type of data the saga accumulates.</typeparam>
/// <remarks>
/// <para>
/// This class provides a BDD-style testing pattern for sagas:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up initial saga data and dependencies</description></item>
/// <item><description><b>When</b>: Execute saga steps or compensation</description></item>
/// <item><description><b>Then</b>: Verify the saga state and data</description></item>
/// </list>
/// <para>
/// <b>Note</b>: This abstract class is designed to work with saga implementations
/// that follow the <c>Saga&lt;TSagaData&gt;</c> pattern with ExecuteAsync/CompensateAsync methods.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderProcessingSagaSpecs : SagaSpecification&lt;OrderProcessingSaga, OrderProcessingSagaData&gt;
/// {
///     protected override OrderProcessingSaga CreateSaga() =>
///         new(_mockEncina.Object, _mockInventory.Object, _mockPayment.Object);
///
///     protected override OrderProcessingSagaData CreateSagaData() => new()
///     {
///         OrderId = Guid.NewGuid()
///     };
///
///     [Fact]
///     public async Task Should_complete_order_processing()
///     {
///         GivenData(data => data.OrderId = _orderId);
///
///         await WhenComplete();
///
///         ThenSuccess(data =>
///         {
///             Assert.NotNull(data.ReservationId);
///             Assert.NotNull(data.PaymentId);
///             Assert.NotNull(data.ShipmentId);
///         });
///     }
///
///     [Fact]
///     public async Task Should_compensate_on_payment_failure()
///     {
///         _mockPayment.Setup(x => x.Charge(...)).Returns(Left(...));
///         GivenData(data => data.OrderId = _orderId);
///
///         await WhenComplete();
///
///         ThenError(error => Assert.Contains("payment", error.Message));
///     }
/// }
/// </code>
/// </example>
public abstract class SagaSpecification<TSaga, TSagaData>
    where TSagaData : class, new()
{
    private TSaga? _saga;
    private TSagaData? _sagaData;
    private Either<EncinaError, TSagaData>? _result;
    private Exception? _exception;
    private bool _whenExecuted;
    private int _executedFromStep;
    private int _compensatedFromStep;
    private bool _compensationExecuted;
    private readonly List<Action<TSagaData>> _givenDataActions = [];

    /// <summary>
    /// Gets the result of the saga execution.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling a When method.</exception>
    protected Either<EncinaError, TSagaData> Result
    {
        get
        {
            EnsureWhenExecuted();
            return _result!.Value;
        }
    }

    /// <summary>
    /// Gets the saga data used in the execution.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling a When method.</exception>
    protected TSagaData SagaData
    {
        get
        {
            EnsureWhenExecuted();
            return _sagaData!;
        }
    }

    /// <summary>
    /// Gets the saga instance used in the test.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling a When method.</exception>
    protected TSaga Saga
    {
        get
        {
            EnsureWhenExecuted();
            return _saga!;
        }
    }

    /// <summary>
    /// Gets the step index from which execution started.
    /// </summary>
    protected int ExecutedFromStep => _executedFromStep;

    /// <summary>
    /// Gets whether compensation was executed.
    /// </summary>
    protected bool CompensationExecuted => _compensationExecuted;

    /// <summary>
    /// Gets the step index from which compensation started.
    /// </summary>
    protected int CompensatedFromStep => _compensatedFromStep;

    /// <summary>
    /// Creates the saga instance to test.
    /// Override this to instantiate your saga with mocked dependencies.
    /// </summary>
    /// <returns>The saga instance.</returns>
    protected abstract TSaga CreateSaga();

    /// <summary>
    /// Creates the default saga data for testing.
    /// Override this to provide a baseline data that tests can modify.
    /// </summary>
    /// <returns>A new instance of the saga data.</returns>
    protected virtual TSagaData CreateSagaData() => new();

    /// <summary>
    /// Creates the request context for saga execution.
    /// Override this to provide custom context.
    /// </summary>
    /// <returns>A request context for testing.</returns>
    protected virtual IRequestContext CreateContext() =>
        RequestContext.CreateForTest(correlationId: $"saga-test-{Guid.NewGuid():N}");

    /// <summary>
    /// Executes the saga from a specific step.
    /// Override this to provide the saga-specific execution logic.
    /// </summary>
    /// <param name="saga">The saga instance.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="fromStep">The step to start execution from.</param>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either an error or the updated saga data.</returns>
    protected abstract ValueTask<Either<EncinaError, TSagaData>> ExecuteSagaAsync(
        TSaga saga,
        TSagaData data,
        int fromStep,
        IRequestContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Compensates the saga from a specific step.
    /// Override this to provide the saga-specific compensation logic.
    /// </summary>
    /// <param name="saga">The saga instance.</param>
    /// <param name="data">The saga data.</param>
    /// <param name="fromStep">The step to start compensation from (going backwards).</param>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the compensation operation.</returns>
    protected abstract Task CompensateSagaAsync(
        TSaga saga,
        TSagaData data,
        int fromStep,
        IRequestContext context,
        CancellationToken cancellationToken);

    #region Given

    /// <summary>
    /// Configures the saga data with custom modifications.
    /// Multiple calls accumulate modifications.
    /// </summary>
    /// <param name="configure">Action to configure the saga data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// GivenData(data => data.OrderId = _orderId);
    /// GivenData(data => data.CustomerId = _customerId);
    /// </code>
    /// </example>
    protected void GivenData(Action<TSagaData> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _givenDataActions.Add(configure);
    }

    /// <summary>
    /// Sets an explicit saga data instance instead of using <see cref="CreateSagaData"/>.
    /// This replaces any previously accumulated GivenData actions.
    /// </summary>
    /// <param name="data">The saga data to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <example>
    /// <code>
    /// GivenSagaData(new OrderProcessingSagaData
    /// {
    ///     OrderId = _orderId,
    ///     CustomerId = _customerId
    /// });
    /// </code>
    /// </example>
    protected void GivenSagaData(TSagaData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _sagaData = data;
        _givenDataActions.Clear();
    }

    #endregion

    #region When

    /// <summary>
    /// Executes the saga from the beginning (step 0).
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// await WhenComplete();
    /// </code>
    /// </example>
    protected Task WhenComplete(CancellationToken cancellationToken = default)
        => WhenStep(0, cancellationToken);

    /// <summary>
    /// Executes the saga from a specific step index.
    /// </summary>
    /// <param name="fromStep">The step index to start from (0-based).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// // Resume from step 2
    /// await WhenStep(2);
    /// </code>
    /// </example>
    protected async Task WhenStep(int fromStep, CancellationToken cancellationToken = default)
    {
        try
        {
            _saga = CreateSaga();
            _sagaData ??= CreateSagaData();

            foreach (var action in _givenDataActions)
            {
                action(_sagaData);
            }

            var context = CreateContext();
            _executedFromStep = fromStep;
            _result = await ExecuteSagaAsync(_saga, _sagaData, fromStep, context, cancellationToken);
            _exception = null;
        }
        catch (Exception ex)
        {
            _exception = ex;
            _result = null;
        }

        _whenExecuted = true;
    }

    /// <summary>
    /// Executes compensation from a specific step.
    /// This is typically used to test compensation logic in isolation.
    /// </summary>
    /// <param name="fromStep">The step index to start compensation from (going backwards).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// // Test compensation from step 2 (will compensate steps 2, 1, 0)
    /// await WhenCompensate(2);
    /// </code>
    /// </example>
    protected async Task WhenCompensate(int fromStep, CancellationToken cancellationToken = default)
    {
        try
        {
            _saga = CreateSaga();
            _sagaData ??= CreateSagaData();

            foreach (var action in _givenDataActions)
            {
                action(_sagaData);
            }

            var context = CreateContext();
            _compensatedFromStep = fromStep;
            _compensationExecuted = true;
            await CompensateSagaAsync(_saga, _sagaData, fromStep, context, cancellationToken);

            // For compensation-only tests, return the saga data as success
            // since compensation itself doesn't have a ROP result
            _result = _sagaData;
            _exception = null;
        }
        catch (Exception ex)
        {
            _exception = ex;
            _result = null;
        }

        _whenExecuted = true;
    }

    #endregion

    #region Then - Success

    /// <summary>
    /// Asserts that the saga completed successfully.
    /// </summary>
    /// <param name="validate">Optional action to validate the final saga data.</param>
    /// <returns>The final saga data for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a When method was not called or an exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenSuccess(data =>
    /// {
    ///     Assert.NotNull(data.ReservationId);
    ///     Assert.NotNull(data.PaymentId);
    /// });
    /// </code>
    /// </example>
    protected TSagaData ThenSuccess(Action<TSagaData>? validate = null)
    {
        EnsureWhenExecuted();
        EnsureNoException();

        Assert.True(
            _result!.Value.IsRight,
            $"Expected saga success but got error: {_result.Value.Match(Right: _ => "", Left: e => e.ToString())}");

        var data = _result.Value.Match(
            Right: d => d,
            Left: _ => throw new InvalidOperationException("Unreachable"));

        validate?.Invoke(data);
        return data;
    }

    /// <summary>
    /// Asserts that the saga completed successfully and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the saga data.</returns>
    /// <example>
    /// <code>
    /// ThenSuccessAnd()
    ///     .ShouldSatisfy(data => Assert.NotNull(data.ReservationId))
    ///     .And.ShouldSatisfy(data => Assert.NotNull(data.PaymentId));
    /// </code>
    /// </example>
    protected AndConstraint<TSagaData> ThenSuccessAnd()
    {
        var data = ThenSuccess();
        return new AndConstraint<TSagaData>(data);
    }

    /// <summary>
    /// Validates the final saga data for completed outcomes (either a successful
    /// completion or an error outcome that the saga returned/handled).
    /// Note: this method will not run if an unhandled exception occurred during
    /// the When/Execute phase — <see cref="EnsureNoException"/> prevents the
    /// validator from running in that case.
    /// Useful for verifying side effects when the saga completed or returned an error.
    /// </summary>
    /// <param name="validate">Action to validate the saga data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a When method was not called or when an unhandled exception was thrown during execution; in the latter case the validator is not executed because <see cref="EnsureNoException"/> enforces that behavior.</exception>
    /// <example>
    /// <code>
    /// ThenData(data =>
    /// {
    ///     // Verify partial progress even after failure
    ///     Assert.NotNull(data.ReservationId);
    ///     Assert.Null(data.PaymentId); // Failed before this step
    /// });
    /// </code>
    /// </example>
    protected void ThenData(Action<TSagaData> validate)
    {
        ArgumentNullException.ThrowIfNull(validate);
        EnsureWhenExecuted();
        EnsureNoException();

        validate(_sagaData!);
    }

    #endregion

    #region Then - Error

    /// <summary>
    /// Asserts that the saga failed with an error.
    /// </summary>
    /// <param name="validate">Optional action to validate the error.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a When method was not called or an exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenError(error => Assert.Contains("payment failed", error.Message));
    /// </code>
    /// </example>
    protected EncinaError ThenError(Action<EncinaError>? validate = null)
    {
        EnsureWhenExecuted();
        EnsureNoException();

        Assert.True(
            _result!.Value.IsLeft,
            $"Expected saga error but got success: {_result.Value.Match(Right: d => d?.ToString() ?? "null", Left: _ => "")}");

        var error = _result.Value.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e);

        validate?.Invoke(error);
        return error;
    }

    /// <summary>
    /// Asserts that the saga failed with an error and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the error.</returns>
    /// <example>
    /// <code>
    /// ThenErrorAnd()
    ///     .ShouldSatisfy(e => Assert.StartsWith("encina", e.GetCode().IfNone("")))
    ///     .And.ShouldSatisfy(e => Assert.Contains("failed", e.Message));
    /// </code>
    /// </example>
    protected AndConstraint<EncinaError> ThenErrorAnd()
    {
        var error = ThenError();
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the saga failed with a specific error code.
    /// </summary>
    /// <param name="expectedCode">The expected error code.</param>
    /// <returns>The error for further assertions.</returns>
    /// <example>
    /// <code>
    /// ThenErrorWithCode("encina.saga.step.failed");
    /// </code>
    /// </example>
    protected EncinaError ThenErrorWithCode(string expectedCode)
    {
        ArgumentNullException.ThrowIfNull(expectedCode);

        var error = ThenError();
        var actualCode = error.GetCode().IfNone(string.Empty);

        Assert.Equal(expectedCode, actualCode);
        return error;
    }

    #endregion

    #region Then - Exception

    /// <summary>
    /// Asserts that an exception of the specified type was thrown during saga execution.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The thrown exception for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a When method was not called or no exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenThrows&lt;InvalidOperationException&gt;();
    /// </code>
    /// </example>
    protected TException ThenThrows<TException>() where TException : Exception
    {
        EnsureWhenExecuted();

        if (_exception is null)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but no exception was thrown");
        }

        if (_exception is not TException typedException)
        {
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but got {_exception.GetType().Name}: {_exception.Message}");
        }

        return typedException;
    }

    /// <summary>
    /// Asserts that an exception of the specified type was thrown and validates it.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="validate">Action to validate the exception.</param>
    /// <returns>The thrown exception.</returns>
    /// <example>
    /// <code>
    /// ThenThrows&lt;InvalidOperationException&gt;(ex =>
    ///     Assert.Contains("saga not configured", ex.Message));
    /// </code>
    /// </example>
    protected TException ThenThrows<TException>(Action<TException> validate) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(validate);

        var exception = ThenThrows<TException>();
        validate(exception);
        return exception;
    }

    /// <summary>
    /// Asserts that an exception was thrown and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the exception.</returns>
    protected AndConstraint<TException> ThenThrowsAnd<TException>() where TException : Exception
    {
        var exception = ThenThrows<TException>();
        return new AndConstraint<TException>(exception);
    }

    #endregion

    #region Then - Saga State

    /// <summary>
    /// Asserts that the saga completed all steps successfully and that
    /// execution started from the initial step (index 0).
    /// </summary>
    /// <returns>This specification for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a When method was not called or an unhandled exception was thrown.</exception>
    /// <exception cref="Xunit.Sdk.TrueException">Thrown when the saga did not start execution from step 0.</exception>
    /// <example>
    /// <code>
    /// await WhenComplete();
    /// ThenCompleted(); // asserts success and that execution began at step 0
    /// </code>
    /// </example>
    protected SagaSpecification<TSaga, TSagaData> ThenCompleted()
    {
        // Reuse ThenSuccess to validate success and absence of exceptions
        ThenSuccess();

        // Additionally verify that execution started from step 0 (full run)
        Assert.True(
            _executedFromStep == 0,
            $"Expected saga to execute from step 0 (full run) but execution started from step {_executedFromStep}");

        return this;
    }

    /// <summary>
    /// Asserts that compensation was executed.
    /// </summary>
    /// <returns>This specification for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when compensation was not executed.</exception>
    /// <example>
    /// <code>
    /// await WhenCompensate(2);
    /// ThenCompensated();
    /// </code>
    /// </example>
    protected SagaSpecification<TSaga, TSagaData> ThenCompensated()
    {
        EnsureWhenExecuted();
        EnsureNoException();

        Assert.True(
            _compensationExecuted,
            "Expected compensation to be executed but WhenCompensate was not called");

        return this;
    }

    /// <summary>
    /// Asserts that the saga failed (returned an error).
    /// </summary>
    /// <param name="expectedMessagePart">Optional part of the error message to verify.</param>
    /// <returns>This specification for method chaining.</returns>
    /// <example>
    /// <code>
    /// await WhenComplete();
    /// ThenFailed("payment");
    /// </code>
    /// </example>
    protected SagaSpecification<TSaga, TSagaData> ThenFailed(string? expectedMessagePart = null)
    {
        var error = ThenError();

        if (expectedMessagePart is not null)
        {
            Assert.Contains(expectedMessagePart, error.Message, StringComparison.OrdinalIgnoreCase);
        }

        return this;
    }

    #endregion

    #region Private Helpers

    private void EnsureWhenExecuted()
    {
        if (!_whenExecuted)
        {
            throw new InvalidOperationException(
                "A When method (WhenComplete, WhenStep, WhenCompensate) must be called before Then assertions");
        }
    }

    private void EnsureNoException()
    {
        if (_exception is not null)
        {
            throw new InvalidOperationException(
                $"An exception was thrown during saga execution: {_exception.GetType().Name}: {_exception.Message}. " +
                "Use ThenThrows<T>() to assert on exceptions.",
                _exception);
        }
    }

    #endregion
}
