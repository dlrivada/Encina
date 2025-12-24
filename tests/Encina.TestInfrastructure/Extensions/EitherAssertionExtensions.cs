using LanguageExt;
using Xunit;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Fluent assertion extensions for Railway Oriented Programming results.
/// Provides expressive assertions for <see cref="Either{L,R}"/> and <see cref="Either{EncinaError,T}"/>.
/// </summary>
public static class EitherAssertionExtensions
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
    /// Asserts that the result is a success (Right) and returns the value.
    /// Alias for <see cref="ShouldBeSuccess{TLeft,TRight}"/>.
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
    public static void ShouldBeSuccess<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        Action<TRight> validator,
        string? message = null)
    {
        var value = result.ShouldBeSuccess(message);
        validator(value);
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
    /// Alias for <see cref="ShouldBeError{TLeft,TRight}"/>.
    /// </summary>
    public static TLeft ShouldBeLeft<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
        => result.ShouldBeError(message);

    /// <summary>
    /// Asserts that the result is an error (Left) and validates the error.
    /// </summary>
    public static void ShouldBeError<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        Action<TLeft> validator,
        string? message = null)
    {
        var error = result.ShouldBeError(message);
        validator(error);
    }

    #endregion

    #region Bottom State Assertions

    /// <summary>
    /// Asserts that the Either is in its default/bottom state (neither Left nor Right).
    /// This is useful when testing guard methods that output a default Either on success.
    /// </summary>
    public static void ShouldBeBottom<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        Assert.False(result.IsLeft, message ?? "Expected bottom state but got Left");
        Assert.False(result.IsRight, message ?? "Expected bottom state but got Right");
    }

    /// <summary>
    /// Asserts that the Either is NOT in its default/bottom state (must be Left or Right).
    /// This is useful when verifying that an operation completed and returned a valid result.
    /// </summary>
    public static void ShouldNotBeBottom<TLeft, TRight>(
        this Either<TLeft, TRight> result,
        string? message = null)
    {
        Assert.True(
            result.IsLeft || result.IsRight,
            message ?? "Expected a valid result (Left or Right) but got bottom state");
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

    #endregion

    #region Collection Assertions

    /// <summary>
    /// Asserts that all results in the collection are successful (Right).
    /// </summary>
    public static void AllShouldBeSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();
        var errors = resultList
            .Select((r, i) => (Result: r, Index: i))
            .Where(x => x.Result.IsLeft)
            .ToList();

        if (errors.Count > 0)
        {
            var errorMessages = errors
                .Select(x => $"[{x.Index}]: {x.Result.Match(Right: _ => "", Left: e => e?.ToString() ?? "null")}")
                .ToList();

            Assert.Fail(message ?? $"Expected all results to be successful, but {errors.Count} failed:\n{string.Join("\n", errorMessages)}");
        }
    }

    /// <summary>
    /// Asserts that all results in the collection are errors (Left).
    /// </summary>
    public static void AllShouldBeError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();
        var successes = resultList
            .Select((r, i) => (Result: r, Index: i))
            .Where(x => x.Result.IsRight)
            .ToList();

        if (successes.Count > 0)
        {
            var successMessages = successes
                .Select(x => $"[{x.Index}]: {x.Result.Match(Right: v => v?.ToString() ?? "null", Left: _ => "")}")
                .ToList();

            Assert.Fail(message ?? $"Expected all results to be errors, but {successes.Count} succeeded:\n{string.Join("\n", successMessages)}");
        }
    }

    /// <summary>
    /// Asserts that the collection contains at least one successful result (Right).
    /// </summary>
    public static void ShouldContainSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var hasSuccess = results.Any(r => r.IsRight);
        Assert.True(hasSuccess, message ?? "Expected collection to contain at least one successful result");
    }

    /// <summary>
    /// Asserts that the collection contains at least one error result (Left).
    /// </summary>
    public static void ShouldContainError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var hasError = results.Any(r => r.IsLeft);
        Assert.True(hasError, message ?? "Expected collection to contain at least one error result");
    }

    #endregion
}
