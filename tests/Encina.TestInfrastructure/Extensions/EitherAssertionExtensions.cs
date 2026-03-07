using LanguageExt;
using Xunit;

namespace Encina.TestInfrastructure.Extensions;

/// <summary>
/// Collection assertion extensions for Railway Oriented Programming results.
/// Provides assertions for collections of <see cref="Either{L,R}"/> results.
/// </summary>
/// <remarks>
/// Individual Either assertions (ShouldBeSuccess, ShouldBeError, ShouldBeRight, ShouldBeLeft, etc.)
/// are provided by <c>Encina.Testing.Shouldly.EitherShouldlyExtensions</c>.
/// This class only provides collection-level assertions.
/// </remarks>
public static class EitherAssertionExtensions
{
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
