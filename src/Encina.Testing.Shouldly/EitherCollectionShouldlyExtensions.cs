using LanguageExt;
using Shouldly;

namespace Encina.Testing.Shouldly;

/// <summary>
/// Shouldly assertion extensions for collections of <see cref="Either{L,R}"/> types.
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
public static class EitherCollectionShouldlyExtensions
{
    /// <summary>
    /// Asserts that all results in the collection are successes (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static IReadOnlyList<TRight> ShouldAllBeSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? customMessage = null)
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

            throw new ShouldAssertException(
                customMessage ?? $"Expected all results to be success but {errors.Count} of {resultList.Count} were errors: {errorDetails}");
        }

        return resultList.Select(r => r.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"))).ToList();
    }

    /// <summary>
    /// Asserts that all results in the collection are errors (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static IReadOnlyList<TLeft> ShouldAllBeError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? customMessage = null)
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

            throw new ShouldAssertException(
                customMessage ?? $"Expected all results to be errors but {successes.Count} of {resultList.Count} were successes: {successDetails}");
        }

        return resultList.Select(r => r.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e)).ToList();
    }

    /// <summary>
    /// Asserts that at least one result in the collection is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The first success value found.</returns>
    public static TRight ShouldContainSuccess<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? customMessage = null)
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

        throw new ShouldAssertException(
            customMessage ?? $"Expected at least one success but all {resultList.Count} results were errors");
    }

    /// <summary>
    /// Asserts that at least one result in the collection is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The first error value found.</returns>
    public static TLeft ShouldContainError<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        string? customMessage = null)
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

        throw new ShouldAssertException(
            customMessage ?? $"Expected at least one error but all {resultList.Count} results were successes");
    }

    /// <summary>
    /// Asserts that exactly the specified number of results are successes.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of successful results.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The collection of success values for further assertions.</returns>
    public static IReadOnlyList<TRight> ShouldHaveSuccessCount<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? customMessage = null)
    {
        var resultList = results.ToList();
        var successResults = resultList.Where(r => r.IsRight).ToList();
        var actualCount = successResults.Count;

        actualCount.ShouldBe(expectedCount,
            customMessage ?? $"Expected {expectedCount} successes but found {actualCount} (of {resultList.Count} total)");

        return successResults.Select(r => r.Match(
            Right: v => v,
            Left: _ => throw new InvalidOperationException("Unreachable"))).ToList();
    }

    /// <summary>
    /// Asserts that exactly the specified number of results are errors.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results to assert.</param>
    /// <param name="expectedCount">The expected number of error results.</param>
    /// <param name="customMessage">Optional custom failure message.</param>
    /// <returns>The collection of error values for further assertions.</returns>
    public static IReadOnlyList<TLeft> ShouldHaveErrorCount<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount,
        string? customMessage = null)
    {
        var resultList = results.ToList();
        var errorResults = resultList.Where(r => r.IsLeft).ToList();
        var actualCount = errorResults.Count;

        actualCount.ShouldBe(expectedCount,
            customMessage ?? $"Expected {expectedCount} errors but found {actualCount} (of {resultList.Count} total)");

        return errorResults.Select(r => r.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: e => e)).ToList();
    }

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
}
