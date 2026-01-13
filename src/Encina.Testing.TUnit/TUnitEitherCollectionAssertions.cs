using LanguageExt;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Encina.Testing.TUnit;

/// <summary>
/// TUnit assertion extensions for collections of <see cref="Either{L,R}"/> types.
/// </summary>
/// <remarks>
/// Provides assertions for verifying multiple Either results in batch operations.
/// </remarks>
public static class TUnitEitherCollectionAssertions
{
    private const string UnreachableMessage = "Unreachable";

    /// <summary>
    /// Asserts that all results in the collection are successes (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>A task that completes with the list of success values.</returns>
    public static async Task<IReadOnlyList<TRight>> ShouldAllBeSuccessAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var resultList = results.ToList();
        var errors = resultList
            .Select((r, i) => (Result: r, Index: i))
            .Where(x => x.Result.IsLeft)
            .Select(x => $"[{x.Index}]: {x.Result.Match(Right: _ => string.Empty, Left: e => e is null ? "null" : e.ToString())}")
            .ToList();

        await Assert.That(errors.Count)
            .IsEqualTo(0)
            .Because($"Expected all results to be success, but found {errors.Count} errors:\n{string.Join("\n", errors)}");

        return resultList
            .Select(r => r.Match(Right: v => v, Left: _ => throw new InvalidOperationException(UnreachableMessage)))
            .ToList();
    }

    /// <summary>
    /// Asserts that all results in the collection are errors (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>A task that completes with the list of error values.</returns>
    public static async Task<IReadOnlyList<TLeft>> ShouldAllBeErrorAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var resultList = results.ToList();
        var successes = resultList
            .Select((r, i) => (Result: r, Index: i))
            .Where(x => x.Result.IsRight)
            .Select(x => $"[{x.Index}]: {x.Result.Match(Right: v => v is null ? "null" : v.ToString(), Left: _ => string.Empty)}")
            .ToList();

        await Assert.That(successes.Count)
            .IsEqualTo(0)
            .Because($"Expected all results to be errors, but found {successes.Count} successes:\n{string.Join("\n", successes)}");

        return resultList
            .Select(r => r.Match(Right: _ => throw new InvalidOperationException(UnreachableMessage), Left: e => e))
            .ToList();
    }

    /// <summary>
    /// Asserts that at least one result in the collection is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldContainSuccessAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var hasSuccess = results.Any(r => r.IsRight);

        await Assert.That(hasSuccess)
            .IsTrue()
            .Because("Expected at least one success in the collection");
    }

    /// <summary>
    /// Asserts that at least one result in the collection is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldContainErrorAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var hasError = results.Any(r => r.IsLeft);

        await Assert.That(hasError)
            .IsTrue()
            .Because("Expected at least one error in the collection");
    }

    /// <summary>
    /// Asserts the exact count of successes in the collection.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <param name="expectedCount">The expected number of successes.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldHaveSuccessCountAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(results);

        var successCount = results.Count(r => r.IsRight);

        await Assert.That(successCount)
            .IsEqualTo(expectedCount)
            .Because($"Expected {expectedCount} successes but found {successCount}");
    }

    /// <summary>
    /// Asserts the exact count of errors in the collection.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <param name="expectedCount">The expected number of errors.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldHaveErrorCountAsync<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(results);

        var errorCount = results.Count(r => r.IsLeft);

        await Assert.That(errorCount)
            .IsEqualTo(expectedCount)
            .Because($"Expected {expectedCount} errors but found {errorCount}");
    }

    /// <summary>
    /// Filters and returns only the success values from the collection.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>The success values from the collection.</returns>
    public static IReadOnlyList<TRight> GetSuccesses<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results
            .Where(r => r.IsRight)
            .Select(r => r.Match(Right: v => v, Left: _ => throw new InvalidOperationException(UnreachableMessage)))
            .ToList();
    }

    /// <summary>
    /// Filters and returns only the error values from the collection.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="results">The collection of Either results.</param>
    /// <returns>The error values from the collection.</returns>
    public static IReadOnlyList<TLeft> GetErrors<TLeft, TRight>(
        this IEnumerable<Either<TLeft, TRight>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return results
            .Where(r => r.IsLeft)
            .Select(r => r.Match(Right: _ => throw new InvalidOperationException(UnreachableMessage), Left: e => e))
            .ToList();
    }
}
