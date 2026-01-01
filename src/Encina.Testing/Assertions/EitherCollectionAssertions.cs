using LanguageExt;
using Xunit;

namespace Encina.Testing;

/// <summary>
/// Fluent assertion extensions for collections of <see cref="Either{L,R}"/> types.
/// Provides assertions for batch operations that return multiple results.
/// </summary>
/// <remarks>
/// <para>
/// Use these extensions when testing batch operations that return collections of Either results.
/// They provide convenient assertions for verifying all or some results succeeded or failed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Assert all batch operations succeeded
/// var results = await Task.WhenAll(
///     encina.Send(new ProcessItem(1)),
///     encina.Send(new ProcessItem(2)),
///     encina.Send(new ProcessItem(3))
/// );
/// results.ShouldAllBeSuccess();
///
/// // Assert at least one succeeded
/// results.ShouldContainSuccess();
/// </code>
/// </example>
public static class EitherCollectionAssertions
{
    #region All Success/Error Assertions

    /// <summary>
    /// Asserts that all results in the collection are successes (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static IReadOnlyList<TRight> ShouldAllBeSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();
        var errors = resultList
            .Select((r, i) => (Index: i, Result: r))
            .Where(x => x.Result.IsLeft)
            .ToList();

        if (errors.Count > 0)
        {
            var errorDetails = string.Join(", ", errors.Select(e =>
                $"[{e.Index}]: {e.Result.Match(Right: _ => "", Left: err => err?.ToString() ?? "null")}"));

            Assert.Fail(message ?? $"Expected all results to be success but {errors.Count} of {resultList.Count} were errors: {errorDetails}");
        }

        return resultList.Select(r => r.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"))).ToList();
    }

    /// <summary>
    /// Asserts that all results in the collection are successes (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<IReadOnlyList<TRight>> ShouldAllBeSuccessAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var values = results.ShouldAllBeSuccess(message);
        return new AndConstraint<IReadOnlyList<TRight>>(values);
    }

    /// <summary>
    /// Asserts that all results in the collection are errors (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static IReadOnlyList<TLeft> ShouldAllBeError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();
        var successes = resultList
            .Select((r, i) => (Index: i, Result: r))
            .Where(x => x.Result.IsRight)
            .ToList();

        if (successes.Count > 0)
        {
            var successDetails = string.Join(", ", successes.Select(s =>
                $"[{s.Index}]: {s.Result.Match(Right: v => v?.ToString() ?? "null", Left: _ => "")}"));

            Assert.Fail(message ?? $"Expected all results to be errors but {successes.Count} of {resultList.Count} were successes: {successDetails}");
        }

        return resultList.Select(r => r.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e)).ToList();
    }

    /// <summary>
    /// Asserts that all results in the collection are errors (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<IReadOnlyList<TLeft>> ShouldAllBeErrorAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var errors = results.ShouldAllBeError(message);
        return new AndConstraint<IReadOnlyList<TLeft>>(errors);
    }

    #endregion

    #region Contains Assertions

    /// <summary>
    /// Asserts that at least one result in the collection is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The first success value found.</returns>
    public static TRight ShouldContainSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();

        foreach (var result in resultList)
        {
            if (result.IsRight)
            {
                return result.Match(
                    Right: v => v,
                    Left: _ => throw new InvalidOperationException("Unreachable"));
            }
        }

        Assert.Fail(message ?? $"Expected at least one success but all {resultList.Count} results were errors");
        return default!; // Unreachable
    }

    /// <summary>
    /// Asserts that at least one result in the collection is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<TRight> ShouldContainSuccessAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var value = results.ShouldContainSuccess(message);
        return new AndConstraint<TRight>(value);
    }

    /// <summary>
    /// Asserts that at least one result in the collection is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The first error value found.</returns>
    public static TLeft ShouldContainError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var resultList = results.ToList();

        foreach (var result in resultList)
        {
            if (result.IsLeft)
            {
                return result.Match(
                    Right: _ => throw new InvalidOperationException("Unreachable"),
                    Left: e => e);
            }
        }

        Assert.Fail(message ?? $"Expected at least one error but all {resultList.Count} results were successes");
        return default!; // Unreachable
    }

    /// <summary>
    /// Asserts that at least one result in the collection is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<TLeft> ShouldContainErrorAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? message = null)
    {
        var error = results.ShouldContainError(message);
        return new AndConstraint<TLeft>(error);
    }

    #endregion

    #region Count Assertions

    /// <summary>
    /// Asserts that exactly the specified number of results are successes.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of successful results.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static IReadOnlyList<TRight> ShouldHaveSuccessCount<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? message = null)
    {
        var resultList = results.ToList();
        var successResults = resultList.Where(r => r.IsRight).ToList();
        var actualCount = successResults.Count;

        Assert.True(
            expectedCount == actualCount,
            message ?? $"Expected {expectedCount} success result(s) but found {actualCount}");

        return successResults.Select(r => r.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"))).ToList();
    }

    /// <summary>
    /// Asserts that exactly the specified number of results are successes and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<IReadOnlyList<TRight>> ShouldHaveSuccessCountAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? message = null)
    {
        var values = results.ShouldHaveSuccessCount(expectedCount, message);
        return new AndConstraint<IReadOnlyList<TRight>>(values);
    }

    /// <summary>
    /// Asserts that exactly the specified number of results are errors.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of error results.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static IReadOnlyList<TLeft> ShouldHaveErrorCount<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? message = null)
    {
        var resultList = results.ToList();
        var errorResults = resultList.Where(r => r.IsLeft).ToList();
        var actualCount = errorResults.Count;

        Assert.True(
            expectedCount == actualCount,
            message ?? $"Expected {expectedCount} error result(s) but found {actualCount}");

        return errorResults.Select(r => r.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e)).ToList();
    }

    /// <summary>
    /// Asserts that exactly the specified number of results are errors and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<IReadOnlyList<TLeft>> ShouldHaveErrorCountAnd<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? message = null)
    {
        var errors = results.ShouldHaveErrorCount(expectedCount, message);
        return new AndConstraint<IReadOnlyList<TLeft>>(errors);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the success values from the collection, ignoring errors.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>The collection of success values.</returns>
    public static IReadOnlyList<TRight> GetSuccesses<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        return results
            .Where(r => r.IsRight)
            .Select(r => r.Match(
                Right: v => v,
                Left: _ => throw new InvalidOperationException("Unreachable")))
            .ToList();
    }

    /// <summary>
    /// Gets the error values from the collection, ignoring successes.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>The collection of error values.</returns>
    public static IReadOnlyList<TLeft> GetErrors<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        return results
            .Where(r => r.IsLeft)
            .Select(r => r.Match(
                Right: _ => throw new InvalidOperationException("Unreachable"),
                Left: e => e))
            .ToList();
    }

    #endregion

    #region EncinaError Specific Collection Assertions

    /// <summary>
    /// Asserts that the collection contains at least one validation error for a specific property.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="propertyName">The property name that should have the validation error.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The matching error for further assertions.</returns>
    public static EncinaError ShouldContainValidationErrorFor<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string propertyName,
        string? message = null)
    {
        var errors = results.GetErrors();

        foreach (var error in errors)
        {
            var code = error.GetCode().IfNone(string.Empty);
            if (!code.StartsWith("encina.validation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var metadata = error.GetMetadata();
            var containsProperty = error.Message.Contains(propertyName, StringComparison.OrdinalIgnoreCase) ||
                                   metadata.Any(m => m.Key.Equals("PropertyName", StringComparison.OrdinalIgnoreCase) &&
                                                     m.Value?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true);

            if (containsProperty)
            {
                return error;
            }
        }

        Assert.Fail(message ?? $"Expected to find a validation error for property '{propertyName}' but none was found");
        return default!; // Unreachable
    }

    /// <summary>
    /// Asserts that the collection contains at least one validation error for a specific property and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldContainValidationErrorForAnd<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string propertyName,
        string? message = null)
    {
        var error = results.ShouldContainValidationErrorFor(propertyName, message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that the collection does not contain any authorization errors.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    public static void ShouldNotContainAuthorizationErrors<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string? message = null)
    {
        var errors = results.GetErrors();
        var authErrors = errors
            .Where(e => e.GetCode().IfNone(string.Empty).StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (authErrors.Count > 0)
        {
            var errorDetails = string.Join(", ", authErrors.Select(e => e.Message));
            Assert.Fail(message ?? $"Expected no authorization errors but found {authErrors.Count}: {errorDetails}");
        }
    }

    /// <summary>
    /// Asserts that the collection contains at least one authorization error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The first authorization error found.</returns>
    public static EncinaError ShouldContainAuthorizationError<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string? message = null)
    {
        var errors = results.GetErrors();

        foreach (var error in errors)
        {
            var code = error.GetCode().IfNone(string.Empty);
            if (code.StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase))
            {
                return error;
            }
        }

        Assert.Fail(message ?? "Expected to find an authorization error but none was found");
        return default!; // Unreachable
    }

    /// <summary>
    /// Asserts that the collection contains at least one authorization error and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<EncinaError> ShouldContainAuthorizationErrorAnd<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string? message = null)
    {
        var error = results.ShouldContainAuthorizationError(message);
        return new AndConstraint<EncinaError>(error);
    }

    /// <summary>
    /// Asserts that all errors in the collection have the specified error code.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="expectedCode">The expected error code.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <returns>The collection of errors with the expected code.</returns>
    public static IReadOnlyList<EncinaError> ShouldAllHaveErrorCode<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string expectedCode,
        string? message = null)
    {
        var errors = results.ShouldAllBeError(message);

        foreach (var error in errors)
        {
            var actualCode = error.GetCode().IfNone(string.Empty);
            Assert.Equal(expectedCode, actualCode);
        }

        return errors;
    }

    /// <summary>
    /// Asserts that all errors in the collection have the specified error code and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static AndConstraint<IReadOnlyList<EncinaError>> ShouldAllHaveErrorCodeAnd<TRight>(
        this IEnumerable<Either<EncinaError, TRight>> results,
        string expectedCode,
        string? message = null)
    {
        var errors = results.ShouldAllHaveErrorCode(expectedCode, message);
        return new AndConstraint<IReadOnlyList<EncinaError>>(errors);
    }

    #endregion

    #region Async Collection Assertions

    /// <summary>
    /// Asserts that all async results in the collection are successes (Right).
    /// </summary>
    public static async Task<IReadOnlyList<TRight>> ShouldAllBeSuccessAsync<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldAllBeSuccess(message);
    }

    /// <summary>
    /// Asserts that all async results in the collection are successes (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<IReadOnlyList<TRight>>> ShouldAllBeSuccessAsyncAnd<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var values = await resultsTask.ConfigureAwait(false);
        return values.ShouldAllBeSuccessAnd(message);
    }

    /// <summary>
    /// Asserts that all async results in the collection are errors (Left).
    /// </summary>
    public static async Task<IReadOnlyList<TLeft>> ShouldAllBeErrorAsync<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldAllBeError(message);
    }

    /// <summary>
    /// Asserts that all async results in the collection are errors (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<IReadOnlyList<TLeft>>> ShouldAllBeErrorAsyncAnd<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var errors = await resultsTask.ConfigureAwait(false);
        return errors.ShouldAllBeErrorAnd(message);
    }

    /// <summary>
    /// Asserts that at least one async result in the collection is a success (Right).
    /// </summary>
    public static async Task<TRight> ShouldContainSuccessAsync<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldContainSuccess(message);
    }

    /// <summary>
    /// Asserts that at least one async result in the collection is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TRight>> ShouldContainSuccessAsyncAnd<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldContainSuccessAnd(message);
    }

    /// <summary>
    /// Asserts that at least one async result in the collection is an error (Left).
    /// </summary>
    public static async Task<TLeft> ShouldContainErrorAsync<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldContainError(message);
    }

    /// <summary>
    /// Asserts that at least one async result in the collection is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TLeft>> ShouldContainErrorAsyncAnd<TLeft, TRight>(
        this Task<IEnumerable<Either<TLeft, TRight>>> resultsTask,
        string? message = null)
    {
        var results = await resultsTask.ConfigureAwait(false);
        return results.ShouldContainErrorAnd(message);
    }

    #endregion
}
