using LanguageExt;
using Xunit;

namespace Encina.Testing.Handlers;

/// <summary>
/// Base class for testing request handlers using the Given/When/Then BDD pattern.
/// Provides a structured approach to handler testing with explicit phases.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// This class provides a BDD-style testing pattern for request handlers:
/// </para>
/// <list type="bullet">
/// <item><description><b>Given</b>: Set up the request state and dependencies</description></item>
/// <item><description><b>When</b>: Execute the handler</description></item>
/// <item><description><b>Then</b>: Verify the results (success, error, or exception)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CreateOrderHandlerSpecs : HandlerSpecification&lt;CreateOrder, OrderId&gt;
/// {
///     protected override CreateOrder CreateRequest() => new()
///     {
///         CustomerId = "CUST-001",
///         Items = [new OrderItem("PROD-001", 1, 99.99m)]
///     };
///
///     protected override IRequestHandler&lt;CreateOrder, OrderId&gt; CreateHandler() =>
///         new CreateOrderHandler(_mockOrderRepository.Object);
///
///     [Fact]
///     public async Task Should_create_order_with_valid_data()
///     {
///         Given(r => r.CustomerId = "PREMIUM-CUSTOMER");
///
///         await When();
///
///         ThenSuccess(orderId => Assert.NotEqual(Guid.Empty, orderId.Value));
///     }
/// }
/// </code>
/// </example>
public abstract class HandlerSpecification<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private TRequest? _request;
    private Either<EncinaError, TResponse>? _result;
    private Exception? _exception;
    private bool _whenExecuted;
    private readonly List<Action<TRequest>> _givenActions = [];

    /// <summary>
    /// Gets the result of the handler execution.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling When.</exception>
    protected Either<EncinaError, TResponse> Result
    {
        get
        {
            EnsureWhenExecuted();
            return _result!.Value;
        }
    }

    /// <summary>
    /// Gets the request that was used in the handler execution.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before calling When.</exception>
    protected TRequest Request
    {
        get
        {
            EnsureWhenExecuted();
            return _request!;
        }
    }

    /// <summary>
    /// Creates the default request for testing.
    /// Override this to provide a baseline request that tests can modify.
    /// </summary>
    /// <returns>A new instance of the request.</returns>
    protected abstract TRequest CreateRequest();

    /// <summary>
    /// Creates the handler instance to test.
    /// Override this to instantiate your handler with mocked dependencies.
    /// </summary>
    /// <returns>The handler instance.</returns>
    protected abstract IRequestHandler<TRequest, TResponse> CreateHandler();



    #region Given

    /// <summary>
    /// Configures the request with custom modifications.
    /// Multiple calls accumulate modifications.
    /// </summary>
    /// <param name="configure">Action to configure the request.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// Given(r => r.CustomerId = "PREMIUM");
    /// Given(r => r.DiscountCode = "SAVE20");
    /// </code>
    /// </example>
    protected void Given(Action<TRequest> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _givenActions.Add(configure);
    }

    /// <summary>
    /// Sets an explicit request instance instead of using <see cref="CreateRequest"/>.
    /// This replaces any previously accumulated Given actions.
    /// </summary>
    /// <param name="request">The request to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <example>
    /// <code>
    /// GivenRequest(new CreateOrder
    /// {
    ///     CustomerId = "CUSTOM-001",
    ///     Items = myCustomItems
    /// });
    /// </code>
    /// </example>
    protected void GivenRequest(TRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _request = request;
        _givenActions.Clear();
    }

    #endregion

    #region When

    /// <summary>
    /// Executes the handler with the configured request.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// await When();
    /// </code>
    /// </example>
    protected async Task When(CancellationToken cancellationToken = default)
    {
        try
        {
            _request ??= CreateRequest();

            foreach (var action in _givenActions)
            {
                action(_request);
            }

            var handler = CreateHandler();
            _result = await handler.Handle(_request, cancellationToken);
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
    /// Applies additional modifications to the request and executes the handler.
    /// Convenience method combining Given and When in one call.
    /// </summary>
    /// <param name="modify">Action to modify the request before execution.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <example>
    /// <code>
    /// await When(r => r.CustomerId = "INVALID");
    /// </code>
    /// </example>
    protected async Task When(Action<TRequest> modify, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modify);
        Given(modify);
        await When(cancellationToken);
    }

    #endregion

    #region Then - Success

    /// <summary>
    /// Asserts that the handler returned a successful result.
    /// </summary>
    /// <param name="validate">Optional action to validate the success value.</param>
    /// <returns>The success value for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when When was not called or an exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenSuccess(orderId => Assert.NotEqual(Guid.Empty, orderId.Value));
    /// </code>
    /// </example>
    protected TResponse ThenSuccess(Action<TResponse>? validate = null)
    {
        EnsureWhenExecuted();
        EnsureNoException();

        Assert.True(
            _result!.Value.IsRight,
            $"Expected success but got error: {_result.Value.Match(Right: _ => "", Left: e => e.ToString())}");

        var value = _result.Value.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"));

        validate?.Invoke(value);
        return value;
    }

    /// <summary>
    /// Asserts that the handler returned a successful result and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the success value.</returns>
    /// <example>
    /// <code>
    /// ThenSuccessAnd()
    ///     .ShouldSatisfy(id => Assert.NotEqual(Guid.Empty, id.Value))
    ///     .And.ShouldSatisfy(id => Assert.True(id.IsValid));
    /// </code>
    /// </example>
    protected AndConstraint<TResponse> ThenSuccessAnd()
    {
        var value = ThenSuccess();
        return new AndConstraint<TResponse>(value);
    }

    #endregion

    #region Then - Error

    /// <summary>
    /// Asserts that the handler returned an error result.
    /// </summary>
    /// <param name="validate">Optional action to validate the error.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when When was not called or an exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenError(error => Assert.Contains("validation", error.Message));
    /// </code>
    /// </example>
    protected EncinaError ThenError(Action<EncinaError>? validate = null)
    {
        EnsureWhenExecuted();
        EnsureNoException();

        Assert.True(
            _result!.Value.IsLeft,
            $"Expected error but got success: {_result.Value.Match(Right: v => v?.ToString() ?? "null", Left: _ => "")}");

        var error = _result.Value.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e);

        validate?.Invoke(error);
        return error;
    }

    /// <summary>
    /// Asserts that the handler returned an error result and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the error.</returns>
    /// <example>
    /// <code>
    /// ThenErrorAnd()
    ///     .ShouldSatisfy(e => Assert.StartsWith("encina.validation", e.GetCode().IfNone("")))
    ///     .And.ShouldSatisfy(e => Assert.Contains("required", e.Message));
    /// </code>
    /// </example>
    protected AndConstraint<EncinaError> ThenErrorAnd()
    {
        var error = ThenError();
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the handler returned a validation error for specific properties.
    /// </summary>
    /// <param name="expectedPropertyNames">The property names that should have validation errors.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedPropertyNames"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expectedPropertyNames"/> is empty.</exception>
    /// <example>
    /// <code>
    /// ThenValidationError("CustomerId", "Email");
    /// </code>
    /// </example>
    protected EncinaError ThenValidationError(params string[] expectedPropertyNames)
    {
        ArgumentNullException.ThrowIfNull(expectedPropertyNames);

        if (expectedPropertyNames.Length == 0)
        {
            throw new ArgumentException("At least one property name must be specified", nameof(expectedPropertyNames));
        }

        var error = ThenError();
        var code = error.GetCode().IfNone(string.Empty);

        Assert.True(
            code.StartsWith("encina.validation", StringComparison.OrdinalIgnoreCase),
            $"Expected validation error but got error with code: {code}");

        var metadata = error.GetMetadata();
        var message = error.Message;

        foreach (var propertyName in expectedPropertyNames)
        {
            var containsProperty = ErrorContainsProperty(error, propertyName);

            Assert.True(
                containsProperty,
                $"Expected validation error for property '{propertyName}' but error was: {message}");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the handler returned an error with a specific error code.
    /// </summary>
    /// <param name="expectedCode">The expected error code.</param>
    /// <returns>The error for further assertions.</returns>
    /// <example>
    /// <code>
    /// ThenErrorWithCode("encina.notfound");
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
    /// Asserts that an exception of the specified type was thrown during handler execution.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The thrown exception for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when When was not called or no exception was thrown.</exception>
    /// <example>
    /// <code>
    /// ThenThrows&lt;ArgumentException&gt;();
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validate"/> is null.</exception>
    /// <example>
    /// <code>
    /// ThenThrows&lt;InvalidOperationException&gt;(ex =>
    ///     Assert.Contains("not allowed", ex.Message));
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
    /// Asserts that an exception was thrown and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the exception.</returns>
    /// <example>
    /// <code>
    /// ThenThrowsAnd&lt;ArgumentException&gt;()
    ///     .ShouldSatisfy(e => Assert.Equal("id", e.ParamName));
    /// </code>
    /// </example>
    protected AndConstraint<TException> ThenThrowsAnd<TException>() where TException : Exception
    {
        var exception = ThenThrows<TException>();
        return new AndConstraint<TException>(exception);
    }

    #endregion

    #region Private Helpers

    private void EnsureWhenExecuted()
    {
        if (!_whenExecuted)
        {
            throw new InvalidOperationException(
                "When() must be called before Then assertions");
        }
    }

    private void EnsureNoException()
    {
        if (_exception is not null)
        {
            throw new InvalidOperationException(
                $"An exception was thrown during When(): {_exception.GetType().Name}: {_exception.Message}. " +
                "Use ThenThrows<T>() to assert on exceptions.",
                _exception);
        }
    }

    private static bool ErrorContainsProperty(EncinaError error, string propertyName)
    {
        var message = error.Message ?? string.Empty;

        if (message.Contains(propertyName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var metadata = error.GetMetadata();
        return metadata.Any(m =>
            m.Key.Equals("PropertyName", StringComparison.OrdinalIgnoreCase) &&
            m.Value?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true);
    }

    #endregion
}
