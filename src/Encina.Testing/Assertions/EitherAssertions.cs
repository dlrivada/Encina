using LanguageExt;
using Xunit;

namespace Encina.Testing;

/// <summary>
/// Fluent assertion extensions for Railway Oriented Programming results.
/// Provides expressive assertions for <see cref="Either{L,R}"/> and <see cref="Either{EncinaError,T}"/>.
/// </summary>
public static class EitherAssertions
{
    #region Success Assertions

    /// <summary>
    /// Asserts that the result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="result">The Either result to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The success value for further assertions.</returns>
    public static TRight ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        Assert.True(
            result.IsRight,
            message ?? $"Expected success (Right) but got error (Left): {result.Match(Right: _ => "", Left: e => e?.ToString() ?? "null")}");

        return result.Match(
            Right: value => value,
            Left: _ => throw new InvalidOperationException("Unreachable"));
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="result">The Either result to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the success value for chained assertions.</returns>
    /// <example>
    /// <code>
    /// result.ShouldBeSuccessAnd()
    ///     .ShouldSatisfy(order => order.Id.ShouldBePositive())
    ///     .And.ShouldSatisfy(order => order.Items.ShouldNotBeEmpty());
    /// </code>
    /// </example>
    public static AndConstraint<TRight> ShouldBeSuccessAnd<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        var value = result.ShouldBeSuccess(message);
        return new AndConstraint<TRight>(value);
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// Alias for <see cref="ShouldBeSuccessAnd{TLeft,TRight}(Either{TLeft,TRight}, string?)"/>.
    /// </summary>
    public static AndConstraint<TRight> ShouldBeRightAnd<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
        => result.ShouldBeSuccessAnd(message);

    /// <summary>
    /// Asserts that the result is a success (Right) and returns the value.
    /// Alias for <see cref="ShouldBeSuccess{TLeft,TRight}(Either{TLeft,TRight}, string?)"/>.
    /// </summary>
    public static TRight ShouldBeRight<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
        => result.ShouldBeSuccess(message);

    /// <summary>
    /// Asserts that the result is a success (Right) with a specific value.
    /// </summary>
    public static void ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        TRight expectedValue,
        string? message = null)
    {
        var actualValue = result.ShouldBeSuccess(message);
        Assert.Equal(expectedValue, actualValue);
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and validates the value.
    /// </summary>
    public static TRight ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        Action<TRight> validator,
        string? message = null)
    {
        var value = result.ShouldBeSuccess(message);
        validator(value);
        return value;
    }

    #endregion

    #region Error Assertions

    /// <summary>
    /// Asserts that the result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="result">The Either result to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The error value for further assertions.</returns>
    public static TLeft ShouldBeError<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        Assert.True(
            result.IsLeft,
            message ?? $"Expected error (Left) but got success (Right): {result.Match(Right: v => v?.ToString() ?? "null", Left: _ => "")}");

        return result.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: error => error);
    }

    /// <summary>
    /// Asserts that the result is an error (Left) and returns the error.
    /// Alias for <see cref="ShouldBeError{TLeft,TRight}(Either{TLeft,TRight}, string?)"/>.
    /// </summary>
    public static TLeft ShouldBeLeft<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
        => result.ShouldBeError(message);

    /// <summary>
    /// Asserts that the result is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="result">The Either result to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>An <see cref="AndConstraint{T}"/> wrapping the error for chained assertions.</returns>
    /// <example>
    /// <code>
    /// result.ShouldBeErrorAnd()
    ///     .ShouldSatisfy(error => error.Code.ShouldContain("validation"))
    ///     .And.ShouldSatisfy(error => error.Message.ShouldNotBeEmpty());
    /// </code>
    /// </example>
    public static AndConstraint<TLeft> ShouldBeErrorAnd<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        return new AndConstraint<TLeft>(error);
    }

    /// <summary>
    /// Asserts that the result is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// Alias for <see cref="ShouldBeErrorAnd{TLeft,TRight}(Either{TLeft,TRight}, string?)"/>.
    /// </summary>
    public static AndConstraint<TLeft> ShouldBeLeftAnd<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
        => result.ShouldBeErrorAnd(message);

    /// <summary>
    /// Asserts that the result is an error (Left) and validates the error.
    /// </summary>
    public static TLeft ShouldBeError<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        Action<TLeft> validator,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        validator(error);
        return error;
    }

    #endregion

    #region EncinaError Specific Assertions

    /// <summary>
    /// Asserts that the result is an error with a specific error code.
    /// </summary>
    public static EncinaError ShouldBeErrorWithCode<TRight>(
        this Either<EncinaError, TRight> result,
        string expectedCode,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        var actualCode = error.GetCode().IfNone(string.Empty);

        Assert.Equal(expectedCode, actualCode);
        return error;
    }

    /// <summary>
    /// Asserts that the result is an error with a message containing the specified text.
    /// </summary>
    public static EncinaError ShouldBeErrorContaining<TRight>(
        this Either<EncinaError, TRight> result,
        string expectedMessagePart,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        Assert.Contains(expectedMessagePart, error.Message);
        return error;
    }

    /// <summary>
    /// Asserts that the result is a validation error.
    /// </summary>
    public static EncinaError ShouldBeValidationError<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        var code = error.GetCode().IfNone(string.Empty);

        Assert.StartsWith("encina.validation", code, StringComparison.OrdinalIgnoreCase);
        return error;
    }

    /// <summary>
    /// Asserts that the result is an authorization error.
    /// </summary>
    public static EncinaError ShouldBeAuthorizationError<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        var code = error.GetCode().IfNone(string.Empty);

        Assert.StartsWith("encina.authorization", code, StringComparison.OrdinalIgnoreCase);
        return error;
    }

    /// <summary>
    /// Asserts that the result is a not found error.
    /// </summary>
    public static EncinaError ShouldBeNotFoundError<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        var code = error.GetCode().IfNone(string.Empty);

        Assert.StartsWith("encina.notfound", code, StringComparison.OrdinalIgnoreCase);
        return error;
    }

    /// <summary>
    /// Asserts that the result is an error with a specific error code and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeErrorWithCodeAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string expectedCode,
        string? message = null)
    {
        var error = result.ShouldBeErrorWithCode(expectedCode, message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the result is a validation error and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeValidationErrorAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeValidationError(message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the result is an authorization error and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeAuthorizationErrorAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeAuthorizationError(message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the result is a not found error and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeNotFoundErrorAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string? message = null)
    {
        var error = result.ShouldBeNotFoundError(message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the result is an error containing the specified message and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeErrorContainingAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string expectedMessagePart,
        string? message = null)
    {
        var error = result.ShouldBeErrorContaining(expectedMessagePart, message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the result is a validation error for a specific property.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="result">The Either result to assert.</param>
    /// <param name="propertyName">The property name that should have the validation error.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The error for further assertions.</returns>
    public static EncinaError ShouldBeValidationErrorForProperty<TRight>(
        this Either<EncinaError, TRight> result,
        string propertyName,
        string? message = null)
    {
        var error = result.ShouldBeValidationError(message);
        var metadata = error.GetMetadata();

        // Check if property name is in the error message or metadata
        var containsProperty = error.Message.Contains(propertyName, StringComparison.OrdinalIgnoreCase) ||
                               metadata.Any(m => m.Key.Equals("PropertyName", StringComparison.OrdinalIgnoreCase) &&
                                                 m.Value?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) is true);

        Assert.True(
            containsProperty,
            message ?? $"Expected validation error for property '{propertyName}' but error was: {error.Message}");

        return error;
    }

    /// <summary>
    /// Asserts that the result is a validation error for a specific property and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldBeValidationErrorForPropertyAnd<TRight>(
        this Either<EncinaError, TRight> result,
        string propertyName,
        string? message = null)
    {
        var error = result.ShouldBeValidationErrorForProperty(propertyName, message);
        return new AndConstraint<EncinaError>(error);
    }

    #endregion

    #region Async Assertions

    /// <summary>
    /// Asserts that the async result is a success (Right).
    /// </summary>
    public static async Task<TRight> ShouldBeSuccessAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeSuccess(message);
    }

    /// <summary>
    /// Asserts that the async result is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TRight>> ShouldBeSuccessAndAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeSuccessAnd(message);
    }

    /// <summary>
    /// Asserts that the async result is an error (Left).
    /// </summary>
    public static async Task<TLeft> ShouldBeErrorAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeError(message);
    }

    /// <summary>
    /// Asserts that the async result is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TLeft>> ShouldBeErrorAndAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeErrorAnd(message);
    }

    /// <summary>
    /// Asserts that the async result is an error with a specific code.
    /// </summary>
    public static async Task<EncinaError> ShouldBeErrorWithCodeAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string expectedCode,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeErrorWithCode(expectedCode, message);
    }

    /// <summary>
    /// Asserts that the async result is an error with a specific code and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<EncinaError>> ShouldBeErrorWithCodeAndAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string expectedCode,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeErrorWithCodeAnd(expectedCode, message);
    }

    /// <summary>
    /// Asserts that the async result is a validation error.
    /// </summary>
    public static async Task<EncinaError> ShouldBeValidationErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeValidationError(message);
    }

    /// <summary>
    /// Asserts that the async result is a validation error and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<EncinaError>> ShouldBeValidationErrorAndAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeValidationErrorAnd(message);
    }

    /// <summary>
    /// Asserts that the async result is a validation error for a specific property.
    /// </summary>
    public static async Task<EncinaError> ShouldBeValidationErrorForPropertyAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string propertyName,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeValidationErrorForProperty(propertyName, message);
    }

    /// <summary>
    /// Asserts that the async result is a validation error for a specific property and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<EncinaError>> ShouldBeValidationErrorForPropertyAndAsync<TRight>(
        this Task<Either<EncinaError, TRight>> resultTask,
        string propertyName,
        string? message = null)
    {
        var result = await resultTask;
        return result.ShouldBeValidationErrorForPropertyAnd(propertyName, message);
    }

    #endregion
}
