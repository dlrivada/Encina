using LanguageExt;
using Shouldly;

namespace Encina.Testing.Shouldly;

/// <summary>
/// Shouldly assertion extensions for <see cref="Either{L,R}"/> types.
/// Provides Shouldly-style assertions for Railway Oriented Programming results.
/// </summary>
/// <remarks>
/// <para>
/// This package provides an open-source alternative to FluentAssertions for testing
/// Encina's Either-based return types. All assertions follow Shouldly's naming conventions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic success assertion
/// var result = await encina.Send(new CreateOrder { CustomerId = "CUST-001" });
/// var orderId = result.ShouldBeSuccess();
/// orderId.Value.ShouldNotBe(Guid.Empty);
///
/// // Error assertion with code check
/// var result = await encina.Send(new CreateOrder { CustomerId = "" });
/// result.ShouldBeValidationError();
/// </code>
/// </example>
public static class EitherShouldlyExtensions
{
    #region Success Assertions

    /// <summary>
    /// Asserts that the result is a success (Right) and returns the success value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The success value for further assertions.</returns>
    /// <exception cref="ShouldAssertException">Thrown when the result is an error.</exception>
    public static TRight ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        string? customMessage = null)
    {
        var errorMessage = either.Match(
            Right: _ => string.Empty,
            Left: e => e?.ToString() ?? "null");

        either.IsRight.ShouldBeTrue(customMessage ?? $"Expected success (Right) but got error (Left): {errorMessage}");

        return either.Match(
            Right: value => value,
            Left: _ => throw new InvalidOperationException("Unreachable"));
    }

    /// <summary>
    /// Asserts that the result is a success (Right) with the expected value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="expected">The expected success value.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <exception cref="ShouldAssertException">Thrown when the result is an error or values don't match.</exception>
    public static void ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        TRight expected,
        string? customMessage = null)
    {
        var actual = either.ShouldBeSuccess(customMessage);
        actual.ShouldBe(expected, customMessage);
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and validates the value using a custom validator.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="validator">A function to validate the success value.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The success value for further assertions.</returns>
    public static TRight ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Action<TRight> validator,
        string? customMessage = null)
    {
        var value = either.ShouldBeSuccess(customMessage);
        validator(value);
        return value;
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and returns the success value.
    /// Alias for <see cref="ShouldBeSuccess{TLeft,TRight}(Either{TLeft,TRight},string?)"/>.
    /// </summary>
    public static TRight ShouldBeRight<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        string? customMessage = null)
        => either.ShouldBeSuccess(customMessage);

    #endregion

    #region Error Assertions

    /// <summary>
    /// Asserts that the result is an error (Left) and returns the error value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error value for further assertions.</returns>
    /// <exception cref="ShouldAssertException">Thrown when the result is a success.</exception>
    public static TLeft ShouldBeError<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        string? customMessage = null)
    {
        var successMessage = either.Match(
            Right: v => v?.ToString() ?? "null",
            Left: _ => string.Empty);

        either.IsLeft.ShouldBeTrue(customMessage ?? $"Expected error (Left) but got success (Right): {successMessage}");

        return either.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: error => error);
    }

    /// <summary>
    /// Asserts that the result is an error (Left) and returns the error value.
    /// Alias for <see cref="ShouldBeError{TLeft,TRight}(Either{TLeft,TRight},string?)"/>.
    /// </summary>
    public static TLeft ShouldBeLeft<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        string? customMessage = null)
        => either.ShouldBeError(customMessage);

    /// <summary>
    /// Asserts that the result is an error (Left) and validates the error using a custom validator.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="validator">A function to validate the error value.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error value for further assertions.</returns>
    public static TLeft ShouldBeError<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Action<TLeft> validator,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        validator(error);
        return error;
    }

    #endregion

    #region EncinaError Specific Assertions

    /// <summary>
    /// Asserts that the result is an error with the specified error code.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="expectedCode">The expected error code.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeErrorWithCode<TRight>(
        this Either<EncinaError, TRight> either,
        string expectedCode,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var actualCode = error.GetCode().IfNone(string.Empty);

        actualCode.ShouldBe(expectedCode, customMessage ?? $"Expected error code '{expectedCode}' but got '{actualCode}'");
        return error;
    }

    /// <summary>
    /// Asserts that the result is an error with a message containing the specified text.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="expectedMessagePart">The text that should be contained in the error message.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeErrorContaining<TRight>(
        this Either<EncinaError, TRight> either,
        string expectedMessagePart,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        error.Message.ShouldContain(expectedMessagePart, Case.Sensitive, customMessage);
        return error;
    }

    /// <summary>
    /// Asserts that the result is a validation error (code starts with "encina.validation").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeValidationError<TRight>(
        this Either<EncinaError, TRight> either,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var code = error.GetCode().IfNone(string.Empty);

        if (!code.StartsWith("encina.validation", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected validation error but got code '{code}'");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is an authorization error (code starts with "encina.authorization").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeAuthorizationError<TRight>(
        this Either<EncinaError, TRight> either,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var code = error.GetCode().IfNone(string.Empty);

        if (!code.StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected authorization error but got code '{code}'");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is a not found error (code starts with "encina.notfound").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeNotFoundError<TRight>(
        this Either<EncinaError, TRight> either,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var code = error.GetCode().IfNone(string.Empty);

        if (!code.StartsWith("encina.notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected not found error but got code '{code}'");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is a conflict error (code starts with "encina.conflict").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeConflictError<TRight>(
        this Either<EncinaError, TRight> either,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var code = error.GetCode().IfNone(string.Empty);

        if (!code.StartsWith("encina.conflict", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected conflict error but got code '{code}'");
        }

        return error;
    }

    /// <summary>
    /// Asserts that the result is an internal error (code starts with "encina.internal").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeInternalError<TRight>(
        this Either<EncinaError, TRight> either,
        string? customMessage = null)
    {
        var error = either.ShouldBeError(customMessage);
        var code = error.GetCode().IfNone(string.Empty);

        if (!code.StartsWith("encina.internal", StringComparison.OrdinalIgnoreCase))
        {
            throw new ShouldAssertException(
                customMessage ?? $"Expected internal error but got code '{code}'");
        }

        return error;
    }

    #endregion

    #region Async Assertions

    /// <summary>
    /// Asserts that the async result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The success value for further assertions.</returns>
    public static async Task<TRight> ShouldBeSuccessAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeSuccess(customMessage);
    }

    /// <summary>
    /// Asserts that the async result is a success (Right) with the expected value.
    /// </summary>
    public static async Task ShouldBeSuccessAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task,
        TRight expected,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        result.ShouldBeSuccess(expected, customMessage);
    }

    /// <summary>
    /// Asserts that the async result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The error value for further assertions.</returns>
    public static async Task<TLeft> ShouldBeErrorAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeError(customMessage);
    }

    /// <summary>
    /// Asserts that the async result is an error with the specified error code.
    /// </summary>
    public static async Task<EncinaError> ShouldBeErrorWithCodeAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task,
        string expectedCode,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeErrorWithCode(expectedCode, customMessage);
    }

    /// <summary>
    /// Asserts that the async result is a validation error.
    /// </summary>
    public static async Task<EncinaError> ShouldBeValidationErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeValidationError(customMessage);
    }

    /// <summary>
    /// Asserts that the async result is an authorization error.
    /// </summary>
    public static async Task<EncinaError> ShouldBeAuthorizationErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeAuthorizationError(customMessage);
    }

    /// <summary>
    /// Asserts that the async result is a not found error.
    /// </summary>
    public static async Task<EncinaError> ShouldBeNotFoundErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task,
        string? customMessage = null)
    {
        var result = await task.ConfigureAwait(false);
        return result.ShouldBeNotFoundError(customMessage);
    }

    #endregion
}
