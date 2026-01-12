using LanguageExt;
using Shouldly;

namespace Encina.Testing.Shouldly;

// For streaming-first variants (infinite/large streams), see GitHub Issue #529.

/// <summary>
/// Shouldly assertion extensions for <see cref="IAsyncEnumerable{T}"/> of <see cref="Either{L,R}"/> types.
/// Provides assertions for streaming operations that yield multiple results over time.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Important:</strong> Most methods in this class fully materialize the <see cref="IAsyncEnumerable{T}"/>
/// by calling <see cref="CollectAsync{T}"/>, which incurs O(n) memory usage where n is the number of items
/// in the stream. This means the entire stream is consumed before assertions are applied.
/// </para>
/// <para>
/// <strong>Not suitable for infinite or very large streams:</strong> Because the stream is fully consumed,
/// these methods will hang indefinitely on infinite streams and may cause <see cref="OutOfMemoryException"/>
/// on very large streams. For such scenarios, consider using <see cref="FirstShouldBeSuccessAsync{TLeft,TRight}"/>
/// or <see cref="FirstShouldBeErrorAsync{TLeft,TRight}"/> which only consume the first item.
/// </para>
/// <para>
/// All async methods respect the provided <see cref="CancellationToken"/>, allowing cancellation during
/// stream enumeration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Assert all streaming results succeed (materializes entire stream)
/// var stream = ProcessItemsAsync(items);
/// await stream.ShouldAllBeSuccessAsync();
///
/// // Collect and assert specific count
/// var results = await stream.CollectAsync();
/// results.ShouldHaveSuccessCount(5);
///
/// // For potentially large streams, assert only the first item
/// await stream.FirstShouldBeSuccessAsync();
/// </code>
/// </example>
public static class StreamingShouldlyExtensions
{
    #region Collection Helpers

    /// <summary>
    /// Collects all items from an async enumerable into a list.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The async enumerable to collect.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration. The token is checked before each iteration.</param>
    /// <returns>A list containing all items from the async enumerable.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream, consuming all items and storing them in memory (O(n) space).
    /// Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<List<T>> CollectAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
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
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration. The token is checked before each iteration.</param>
    /// <returns>A list containing all Either results from the async enumerable.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream, consuming all items and storing them in memory (O(n) space).
    /// Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
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
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before asserting.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<TRight>> ShouldAllBeSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldAllBeSuccess(customMessage);
    }

    /// <summary>
    /// Asserts that all streaming results are errors (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before asserting.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<TLeft>> ShouldAllBeErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldAllBeError(customMessage);
    }

    #endregion

    #region Contains Assertions

    /// <summary>
    /// Asserts that at least one streaming result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The first success value found.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before searching.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// <para>
    /// For large streams where early termination is desired, consider using a streaming-first variant
    /// (not yet implemented) that iterates until the first match is found.
    /// </para>
    /// </remarks>
    public static async Task<TRight> ShouldContainSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainSuccess(customMessage);
    }

    /// <summary>
    /// Asserts that at least one streaming result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The first error value found.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before searching.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// <para>
    /// For large streams where early termination is desired, consider using a streaming-first variant
    /// (not yet implemented) that iterates until the first match is found.
    /// </para>
    /// </remarks>
    public static async Task<TLeft> ShouldContainErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldContainError(customMessage);
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
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before counting.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<TRight>> ShouldHaveSuccessCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldHaveSuccessCount(expectedCount, customMessage);
    }

    /// <summary>
    /// Asserts that exactly the specified number of streaming results are errors.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of error results.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before counting.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<TLeft>> ShouldHaveErrorCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        return results.ShouldHaveErrorCount(expectedCount, customMessage);
    }

    /// <summary>
    /// Asserts that the streaming source yields exactly the specified total number of items.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCount">The expected total number of items.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of all results for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before counting.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<Either<TLeft, TRight>>> ShouldHaveCountAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        int expectedCount,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        results.Count.ShouldBe(expectedCount,
            customMessage ?? $"Expected {expectedCount} items but found {results.Count}");

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
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The matching error for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before searching.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<EncinaError> ShouldContainValidationErrorForAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string propertyName,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
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
                                                     m.Value?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) is true);

            if (containsProperty)
            {
                return error;
            }
        }

        throw new ShouldAssertException(
            customMessage ?? $"Expected to find a validation error for property '{propertyName}' but none was found");
    }

    /// <summary>
    /// Asserts that the streaming source does not contain any authorization errors.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before checking.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task ShouldNotContainAuthorizationErrorsAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        var errors = results.GetErrors();
        var authErrors = errors
            .Where(e => e.GetCode().IfNone(string.Empty).StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (authErrors.Count > 0)
        {
            var errorDetails = string.Join(", ", authErrors.Select(e => e.Message));
            throw new ShouldAssertException(
                customMessage ?? $"Expected no authorization errors but found {authErrors.Count}: {errorDetails}");
        }
    }

    /// <summary>
    /// Asserts that the streaming source contains at least one authorization error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The first authorization error found.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before searching.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<EncinaError> ShouldContainAuthorizationErrorAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        var errors = results.GetErrors();

        foreach (var error in errors)
        {
            var code = error.GetCode().IfNone(string.Empty);
            if (code.StartsWith("encina.authorization", StringComparison.OrdinalIgnoreCase))
            {
                return error;
            }
        }

        throw new ShouldAssertException(
            customMessage ?? "Expected to find an authorization error but none was found");
    }

    /// <summary>
    /// Asserts that all streaming errors have the specified error code.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="expectedCode">The expected error code.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of errors with the expected code.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/> before checking.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<EncinaError>> ShouldAllHaveErrorCodeAsync<TRight>(
        this IAsyncEnumerable<Either<EncinaError, TRight>> source,
        string expectedCode,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);
        var errors = results.ShouldAllBeError(customMessage);

        foreach (var error in errors)
        {
            var actualCode = error.GetCode().IfNone(string.Empty);
            actualCode.ShouldBe(expectedCode,
                customMessage ?? $"Expected error code '{expectedCode}' but got '{actualCode}'");
        }

        return errors;
    }

    #endregion

    #region First Item Assertions

    /// <summary>
    /// Asserts that the first streaming result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The first success value.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Streaming-friendly:</strong> This method only consumes the first item from the stream,
    /// making it suitable for large or potentially infinite streams. O(1) memory usage.
    /// </para>
    /// </remarks>
    public static async Task<TRight> FirstShouldBeSuccessAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current.ShouldBeSuccess(customMessage);
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        throw new ShouldAssertException(customMessage ?? "Expected at least one result but the stream was empty");
    }

    /// <summary>
    /// Asserts that the first streaming result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The first error value.</returns>
    /// <remarks>
    /// <para>
    /// <strong>Streaming-friendly:</strong> This method only consumes the first item from the stream,
    /// making it suitable for large or potentially infinite streams. O(1) memory usage.
    /// </para>
    /// </remarks>
    public static async Task<TLeft> FirstShouldBeErrorAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current.ShouldBeError(customMessage);
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        throw new ShouldAssertException(customMessage ?? "Expected at least one result but the stream was empty");
    }

    #endregion

    #region Empty/Non-Empty Assertions

    /// <summary>
    /// Asserts that the streaming source yields no items.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/>.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task ShouldBeEmptyAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        if (results.Count > 0)
        {
            throw new ShouldAssertException(customMessage ?? $"Expected empty stream but found {results.Count} items");
        }
    }

    /// <summary>
    /// Asserts that the streaming source yields at least one item.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="source">The async enumerable of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the enumeration.</param>
    /// <returns>The collection of all results for further assertions.</returns>
    /// <remarks>
    /// <para>
    /// This method fully materializes the stream via <see cref="CollectAsync{T}"/>.
    /// O(n) memory usage. Not suitable for infinite or very large streams.
    /// </para>
    /// </remarks>
    public static async Task<IReadOnlyList<Either<TLeft, TRight>>> ShouldNotBeEmptyAsync<TLeft, TRight>(
        this IAsyncEnumerable<Either<TLeft, TRight>> source,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var results = await source.CollectAsync(cancellationToken);

        if (results.Count == 0)
        {
            throw new ShouldAssertException(customMessage ?? "Expected non-empty stream but it was empty");
        }

        return results;
    }

    #endregion
}
