using LanguageExt;
using Xunit;

namespace Encina.Testing;

/// <summary>
/// Fluent assertion extensions for <see cref="IAsyncEnumerable{T}"/> of <see cref="Either{L,R}"/> types.
/// Provides assertions for streaming operations that yield multiple results over time.
/// </summary>
/// <remarks>
/// <para>
/// Use these extensions when testing streaming operations that yield Either results asynchronously.
/// They collect all results and then apply assertions similar to collection assertions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Assert all streaming results succeed
/// var stream = ProcessItemsAsync(items);
/// await stream.ShouldAllBeSuccessAsync();
///
/// // Collect and assert specific count
/// var results = await stream.CollectAsync();
/// results.ShouldHaveSuccessCount(5);
/// </code>
/// </example>
public static class StreamingAssertions
{
    #region Collection Helpers

    /// <summary>
    /// Collects all items from an async enumerable into a list.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The async enumerable to collect.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list containing all items from the async enumerable.</returns>
    public static async Task<List<T>> CollectAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results;
    }

    /// <summary>
    /// Collects all Either items from an async enumerable into a list.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable to collect.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A list containing all Either results from the async enumerable.</returns>
    public static async Task<List<Either<TLeft, TRight>>> CollectEithersAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        CancellationToken cancellationToken = default)
    {
        return await source.CollectAsync(cancellationToken);
    }

    #endregion

    #region All Success/Error Assertions

    /// <summary>
    /// Asserts that all streaming results are successes (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static async Task<IReadOnlyList<TRight>> ShouldAllBeSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldAllBeSuccess(message);
    }

    /// <summary>
    /// Asserts that all streaming results are successes (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<IReadOnlyList<TRight>>> ShouldAllBeSuccessAndAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var values = await source.ShouldAllBeSuccessAsync(message, cancellationToken);
        return new AndConstraint<IReadOnlyList<TRight>>(values);
    }

    /// <summary>
    /// Asserts that all streaming results are errors (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static async Task<IReadOnlyList<TLeft>> ShouldAllBeErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldAllBeError(message);
    }

    /// <summary>
    /// Asserts that all streaming results are errors (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<IReadOnlyList<TLeft>>> ShouldAllBeErrorAndAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var errors = await source.ShouldAllBeErrorAsync(message, cancellationToken);
        return new AndConstraint<IReadOnlyList<TLeft>>(errors);
    }

    #endregion

    #region Contains Assertions

    /// <summary>
    /// Asserts that at least one streaming result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The first success value found.</returns>
    public static async Task<TRight> ShouldContainSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainSuccess(message);
    }

    /// <summary>
    /// Asserts that at least one streaming result is a success (Right) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TRight>> ShouldContainSuccessAndAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var value = await source.ShouldContainSuccessAsync(message, cancellationToken);
        return new AndConstraint<TRight>(value);
    }

    /// <summary>
    /// Asserts that at least one streaming result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The first error value found.</returns>
    public static async Task<TLeft> ShouldContainErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainError(message);
    }

    /// <summary>
    /// Asserts that at least one streaming result is an error (Left) and returns an <see cref="AndConstraint{T}"/> for fluent chaining.
    /// </summary>
    public static async Task<AndConstraint<TLeft>> ShouldContainErrorAndAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var error = await source.ShouldContainErrorAsync(message, cancellationToken);
        return new AndConstraint<TLeft>(error);
    }

    #endregion

    #region Count Assertions

    /// <summary>
    /// Asserts that exactly the specified number of streaming results are successes.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of successful results.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static async Task<IReadOnlyList<TRight>> ShouldHaveSuccessCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldHaveSuccessCount(expectedCount, message);
    }

    /// <summary>
    /// Asserts that exactly the specified number of streaming results are errors.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of error results.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static async Task<IReadOnlyList<TLeft>> ShouldHaveErrorCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldHaveErrorCount(expectedCount, message);
    }

    /// <summary>
    /// Asserts that the streaming source yields exactly the specified total number of items.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCount">The expected total number of items.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of all results for further assertions.</returns>
    public static async Task<IReadOnlyList<Either<TLeft, TRight>>> ShouldHaveCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        Assert.Equal(expectedCount, results.Count);

        return results;
    }

    #endregion

    #region EncinaError Specific Streaming Assertions

    /// <summary>
    /// Asserts that the streaming source contains at least one validation error for a specific property.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="propertyName">The property name that should have the validation error.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The matching error for further assertions.</returns>
    public static async Task<EncinaError> ShouldContainValidationErrorForAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string propertyName,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainValidationErrorFor(propertyName, message);
    }

    /// <summary>
    /// Asserts that the streaming source does not contain any authorization errors.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task ShouldNotContainAuthorizationErrorsAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        results.ShouldNotContainAuthorizationErrors(message);
    }

    /// <summary>
    /// Asserts that the streaming source contains at least one authorization error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The first authorization error found.</returns>
    public static async Task<EncinaError> ShouldContainAuthorizationErrorAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainAuthorizationError(message);
    }

    /// <summary>
    /// Asserts that all streaming errors have the specified error code.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCode">The expected error code.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of errors with the expected code.</returns>
    public static async Task<IReadOnlyList<EncinaError>> ShouldAllHaveErrorCodeAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string expectedCode,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldAllHaveErrorCode(expectedCode, message);
    }

    #endregion

    #region First Item Assertions

    /// <summary>
    /// Asserts that the first streaming result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The first success value.</returns>
    public static async Task<TRight> FirstShouldBeSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        if (await enumerator.MoveNextAsync())
        {
            return enumerator.Current.ShouldBeSuccess(message);
        }

        Assert.Fail(message ?? "Expected at least one result but the stream was empty");
        return default!; // Unreachable
    }

    /// <summary>
    /// Asserts that the first streaming result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The first error value.</returns>
    public static async Task<TLeft> FirstShouldBeErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        await using var enumerator = source.GetAsyncEnumerator(cancellationToken);
        if (await enumerator.MoveNextAsync())
        {
            return enumerator.Current.ShouldBeError(message);
        }

        Assert.Fail(message ?? "Expected at least one result but the stream was empty");
        return default!; // Unreachable
    }

    #endregion

    #region Empty/Non-Empty Assertions

    /// <summary>
    /// Asserts that the streaming source yields no items.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public static async Task ShouldBeEmptyAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        if (results.Count > 0)
        {
            Assert.Fail(message ?? $"Expected empty stream but found {results.Count} items");
        }
    }

    /// <summary>
    /// Asserts that the streaming source yields at least one item.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="message">Optional custom failure message.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The collection of all results for further assertions.</returns>
    public static async Task<IReadOnlyList<Either<TLeft, TRight>>> ShouldNotBeEmptyAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        if (results.Count == 0)
        {
            Assert.Fail(message ?? "Expected non-empty stream but it was empty");
        }

        return results;
    }

    #endregion
}
