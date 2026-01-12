using LanguageExt;
using Xunit;

namespace Encina.Testing.Handlers;

/// <summary>
/// Wraps the result of a scenario execution and provides assertion methods.
/// </summary>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
/// <remarks>
/// <para>
/// This class encapsulates the result of executing a <see cref="Scenario{TRequest,TResponse}"/>
/// and provides fluent assertion methods for verifying the outcome.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await scenario.WhenAsync(new CreateOrder());
///
/// // Assert success
/// result.ShouldBeSuccess();
///
/// // Or with validation
/// result.ShouldBeSuccess(orderId => Assert.NotEqual(Guid.Empty, orderId.Value));
///
/// // Or assert error
/// result.ShouldBeValidationError("CustomerId");
/// </code>
/// </example>
public sealed class ScenarioResult<TResponse>
{
    private readonly Either<EncinaError, TResponse>? _result;
    private readonly Exception? _exception;
    private readonly object _request;
    private readonly string _description;

    /// <summary>
    /// Initializes a new instance with a successful or error result.
    /// </summary>
    /// <param name="result">The Either result from the handler.</param>
    /// <param name="request">The request that was executed.</param>
    /// <param name="description">The scenario description.</param>
    internal ScenarioResult(Either<EncinaError, TResponse> result, object request, string description)
    {
        _result = result;
        _request = request;
        _description = description;
    }

    /// <summary>
    /// Initializes a new instance with an exception.
    /// </summary>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="request">The request that was executed.</param>
    /// <param name="description">The scenario description.</param>
    internal ScenarioResult(Exception exception, object request, string description)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exception = exception;
        _request = request;
        _description = description;
    }

    /// <summary>
    /// Gets whether the result is a success (Right).
    /// </summary>
    public bool IsSuccess => _result?.IsRight ?? false;

    /// <summary>
    /// Gets whether the result is an error (Left).
    /// </summary>
    public bool IsError => _result?.IsLeft ?? false;

    /// <summary>
    /// Gets whether an exception was thrown during execution.
    /// </summary>
    public bool HasException => _exception is not null;

    /// <summary>
    /// Gets the scenario description.
    /// </summary>
    public string Description => _description;

    /// <summary>
    /// Gets the request that was executed for this scenario.
    /// Exposed for callers who need to inspect the executed request.
    /// </summary>
    public object Request => _request;

    /// <summary>
    /// Gets the exception thrown during execution, if any.
    /// </summary>
    public Exception? Exception => _exception;

    /// <summary>
    /// Gets the raw Either result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when an exception was thrown during execution.</exception>
    public Either<EncinaError, TResponse> Result
    {
        get
        {
            EnsureNoException();
            return _result!.Value;
        }
    }

    #region Success Assertions

    /// <summary>
    /// Asserts that the scenario resulted in success.
    /// </summary>
    /// <param name="validate">Optional action to validate the success value.</param>
    /// <returns>The success value for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an exception was thrown during execution.</exception>
    /// <example>
    /// <code>
    /// result.ShouldBeSuccess(orderId => Assert.NotEqual(Guid.Empty, orderId.Value));
    /// </code>
    /// </example>
    public TResponse ShouldBeSuccess(Action<TResponse>? validate = null)
    {
        EnsureNoException();

        Xunit.Assert.True(
            _result!.Value.IsRight,
            $"Scenario '{_description}' expected success but got error: " +
            $"{_result.Value.Match(Right: _ => "", Left: e => e.ToString())}");

        var value = _result.Value.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"));

        validate?.Invoke(value);
        return value;
    }

    /// <summary>
    /// Asserts that the scenario resulted in success and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the success value.</returns>
    /// <example>
    /// <code>
    /// result.ShouldBeSuccessAnd()
    ///     .ShouldSatisfy(id => Assert.NotEqual(Guid.Empty, id.Value));
    /// </code>
    /// </example>
    public AndConstraint<TResponse> ShouldBeSuccessAnd()
    {
        var value = ShouldBeSuccess();
        return new AndConstraint<TResponse>(value);
    }

    #endregion

    #region Error Assertions

    /// <summary>
    /// Asserts that the scenario resulted in an error.
    /// </summary>
    /// <param name="validate">Optional action to validate the error.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an exception was thrown during execution.</exception>
    /// <example>
    /// <code>
    /// result.ShouldBeError(error => Assert.Contains("not found", error.Message));
    /// </code>
    /// </example>
    public EncinaError ShouldBeError(Action<EncinaError>? validate = null)
    {
        EnsureNoException();

        Xunit.Assert.True(
            _result!.Value.IsLeft,
            $"Scenario '{_description}' expected error but got success: " +
            $"{_result.Value.Match(Right: v => v?.ToString() ?? "null", Left: _ => "")}");

        var error = _result.Value.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e);

        validate?.Invoke(error);
        return error;
    }

    /// <summary>
    /// Asserts that the scenario resulted in an error and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the error.</returns>
    /// <example>
    /// <code>
    /// result.ShouldBeErrorAnd()
    ///     .ShouldSatisfy(e => Assert.StartsWith("encina.validation", e.GetCode().IfNone("")));
    /// </code>
    /// </example>
    public AndConstraint<EncinaError> ShouldBeErrorAnd()
    {
        var error = ShouldBeError();
        return new AndConstraint<EncinaError>(error);
    }

    #endregion

    /// <summary>
    /// Asserts that the scenario resulted in a validation error for specific properties.
    /// </summary>
    /// <param name="expectedPropertyNames">The property names that should have validation errors.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedPropertyNames"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="expectedPropertyNames"/> is empty.</exception>
    /// <example>
    /// <code>
    /// result.ShouldBeValidationError("CustomerId", "Email");
    /// </code>
    /// </example>
    public EncinaError ShouldBeValidationError(params string[] expectedPropertyNames)
    {
        ArgumentNullException.ThrowIfNull(expectedPropertyNames);

        if (expectedPropertyNames.Length == 0)
        {
            throw new ArgumentException("At least one property name must be specified", nameof(expectedPropertyNames));
        }

        var error = ShouldBeError();
        var code = error.GetCode().IfNone(string.Empty);

        Xunit.Assert.True(
            code.StartsWith("encina.validation", StringComparison.OrdinalIgnoreCase),
            $"Scenario '{_description}' expected validation error but got error with code: {code}");

        var metadata = error.GetMetadata();
        var message = error.Message;

        foreach (var propertyName in expectedPropertyNames)
        {
            var containsProperty =
                message.Contains(propertyName, StringComparison.OrdinalIgnoreCase) ||
                metadata.Any(m =>
                    m.Key.Equals("PropertyName", StringComparison.OrdinalIgnoreCase) &&
                    m.Value?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) is true);

            Xunit.Assert.True(
                containsProperty,
                $"Scenario '{_description}' expected validation error for property '{propertyName}' but error was: {message}");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the scenario resulted in an error with a specific error code.
    /// </summary>
    /// <param name="expectedCode">The expected error code.</param>
    /// <returns>The error for further assertions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedCode"/> is null.</exception>
    /// <example>
    /// <code>
    /// result.ShouldBeErrorWithCode("encina.notfound");
    /// </code>
    /// </example>
    public EncinaError ShouldBeErrorWithCode(string expectedCode)
    {
        ArgumentNullException.ThrowIfNull(expectedCode);

        var error = ShouldBeError();
        var actualCode = error.GetCode().IfNone(string.Empty);

        Xunit.Assert.Equal(expectedCode, actualCode);
        return error;
    }


    #region Exception Assertions

    /// <summary>
    /// Asserts that an exception of the specified type was thrown during execution.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>The thrown exception for further assertions.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no exception was thrown.</exception>
    /// <example>
    /// <code>
    /// result.ShouldThrow&lt;ArgumentException&gt;();
    /// </code>
    /// </example>
    public TException ShouldThrow<TException>() where TException : Exception
    {
        if (_exception is null)
        {
            throw new InvalidOperationException(
                $"Scenario '{_description}' expected exception of type {typeof(TException).Name} but no exception was thrown");
        }

        if (_exception is not TException typedException)
        {
            throw new InvalidOperationException(
                $"Scenario '{_description}' expected exception of type {typeof(TException).Name} " +
                $"but got {_exception.GetType().Name}: {_exception.Message}");
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
    /// result.ShouldThrow&lt;InvalidOperationException&gt;(ex =>
    ///     Assert.Contains("not allowed", ex.Message));
    /// </code>
    /// </example>
    public TException ShouldThrow<TException>(Action<TException> validate) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(validate);

        var exception = ShouldThrow<TException>();
        validate(exception);
        return exception;
    }

    /// <summary>
    /// Asserts that an exception was thrown and returns an <see cref="AndConstraint{T}"/>.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the exception.</returns>
    /// <example>
    /// <code>
    /// result.ShouldThrowAnd&lt;ArgumentException&gt;()
    ///     .ShouldSatisfy(e => Assert.Equal("id", e.ParamName));
    /// </code>
    /// </example>
    public AndConstraint<TException> ShouldThrowAnd<TException>() where TException : Exception
    {
        var exception = ShouldThrow<TException>();
        return new AndConstraint<TException>(exception);
    }

    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Implicitly converts the scenario result to an <see cref="Either{EncinaError, TResponse}"/>.
    /// </summary>
    /// <param name="result">The scenario result to convert.</param>
    /// <exception cref="InvalidOperationException">Thrown when an exception was thrown during execution.</exception>
    public static implicit operator Either<EncinaError, TResponse>(ScenarioResult<TResponse> result)
    {
        return result.Result;
    }

    #endregion

    #region Private Helpers

    private void EnsureNoException()
    {
        if (_exception is not null)
        {
            throw new InvalidOperationException(
                $"Scenario '{_description}' threw an exception: {_exception.GetType().Name}: {_exception.Message}. " +
                "Use ShouldThrow<T>() to assert on exceptions.",
                _exception);
        }
    }

    #endregion
}
