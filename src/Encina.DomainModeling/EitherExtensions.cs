using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling;

/// <summary>
/// Extension methods for the Either monad, providing fluent API for Result pattern operations.
/// </summary>
/// <remarks>
/// These extensions complement LanguageExt's Either type with common patterns
/// used in domain-driven design and CQRS applications.
/// </remarks>
public static class EitherExtensions
{
    #region Combination

    /// <summary>
    /// Combines two Either results into a tuple.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T1">The first success type.</typeparam>
    /// <typeparam name="T2">The second success type.</typeparam>
    /// <param name="first">The first result.</param>
    /// <param name="second">The second result.</param>
    /// <returns>Combined result or the first error encountered.</returns>
    public static Either<TError, (T1, T2)> Combine<TError, T1, T2>(
        this Either<TError, T1> first,
        Either<TError, T2> second)
    {
        return first.Bind(f =>
            second.Map(s => (f, s)));
    }

    /// <summary>
    /// Combines three Either results into a tuple.
    /// </summary>
    public static Either<TError, (T1, T2, T3)> Combine<TError, T1, T2, T3>(
        this Either<TError, T1> first,
        Either<TError, T2> second,
        Either<TError, T3> third)
    {
        return first.Bind(f =>
            second.Bind(s =>
                third.Map(t => (f, s, t))));
    }

    /// <summary>
    /// Combines four Either results into a tuple.
    /// </summary>
    public static Either<TError, (T1, T2, T3, T4)> Combine<TError, T1, T2, T3, T4>(
        this Either<TError, T1> first,
        Either<TError, T2> second,
        Either<TError, T3> third,
        Either<TError, T4> fourth)
    {
        return first.Bind(f =>
            second.Bind(s =>
                third.Bind(t =>
                    fourth.Map(fo => (f, s, t, fo)))));
    }

    /// <summary>
    /// Combines a collection of Either results.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>Combined list or the first error encountered.</returns>
    public static Either<TError, IReadOnlyList<T>> Combine<TError, T>(
        this IEnumerable<Either<TError, T>> results)
    {
        var list = new List<T>();

        foreach (var result in results)
        {
            if (result.IsLeft)
            {
                return result.Match(
                    Right: _ => throw new InvalidOperationException(),
                    Left: error => Left<TError, IReadOnlyList<T>>(error));
            }

            list.Add(result.Match(Right: x => x, Left: _ => default!));
        }

        return list.AsReadOnly();
    }

    #endregion

    #region Conditional Operations

    /// <summary>
    /// Executes an operation only if a condition is true.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="condition">The condition to check.</param>
    /// <param name="operation">The operation to execute if condition is true.</param>
    /// <returns>The result of the operation or the original result.</returns>
    public static Either<TError, T> When<TError, T>(
        this Either<TError, T> result,
        bool condition,
        Func<T, Either<TError, T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return condition
            ? result.Bind(operation)
            : result;
    }

    /// <summary>
    /// Ensures a predicate holds for the value, or returns an error.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="predicate">The predicate that must hold.</param>
    /// <param name="errorFactory">Factory to create the error if predicate fails.</param>
    /// <returns>The original result or an error.</returns>
    public static Either<TError, T> Ensure<TError, T>(
        this Either<TError, T> result,
        Func<T, bool> predicate,
        Func<T, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        return result.Bind(value =>
            predicate(value)
                ? Right<TError, T>(value)
                : Left<TError, T>(errorFactory(value)));
    }

    /// <summary>
    /// Provides a fallback value or operation on error.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="fallback">The fallback to use on error.</param>
    /// <returns>The original result or the fallback.</returns>
    public static Either<TError, T> OrElse<TError, T>(
        this Either<TError, T> result,
        Func<TError, Either<TError, T>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            Right: value => Right<TError, T>(value),
            Left: error => fallback(error));
    }

    /// <summary>
    /// Provides a default value on error.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="defaultValue">The default value to use on error.</param>
    /// <returns>The success value or the default.</returns>
    public static T GetOrDefault<TError, T>(
        this Either<TError, T> result,
        T defaultValue)
    {
        return result.Match(
            Right: value => value,
            Left: _ => defaultValue);
    }

    /// <summary>
    /// Provides a default value using a factory on error.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="defaultFactory">Factory to create the default value.</param>
    /// <returns>The success value or the factory result.</returns>
    public static T GetOrElse<TError, T>(
        this Either<TError, T> result,
        Func<TError, T> defaultFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultFactory);

        return result.Match(
            Right: value => value,
            Left: error => defaultFactory(error));
    }

    #endregion

    #region Side Effects

    /// <summary>
    /// Executes a side effect on success without modifying the result.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="action">The action to execute on success.</param>
    /// <returns>The original result unchanged.</returns>
    public static Either<TError, T> Tap<TError, T>(
        this Either<TError, T> result,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return result.Map(value =>
        {
            action(value);
            return value;
        });
    }

    /// <summary>
    /// Executes a side effect on error without modifying the result.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="action">The action to execute on error.</param>
    /// <returns>The original result unchanged.</returns>
    public static Either<TError, T> TapError<TError, T>(
        this Either<TError, T> result,
        Action<TError> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return result.MapLeft(error =>
        {
            action(error);
            return error;
        });
    }

    #endregion

    #region Async Extensions

    /// <summary>
    /// Async bind operation for Task-wrapped Either.
    /// </summary>
    public static async Task<Either<TError, TResult>> BindAsync<TError, T, TResult>(
        this Task<Either<TError, T>> task,
        Func<T, Task<Either<TError, TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(binder);

        var result = await task.ConfigureAwait(false);

        return await result.Match(
            Right: async value => await binder(value).ConfigureAwait(false),
            Left: error => Task.FromResult(Left<TError, TResult>(error)))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Async map operation for Task-wrapped Either.
    /// </summary>
    public static async Task<Either<TError, TResult>> MapAsync<TError, T, TResult>(
        this Task<Either<TError, T>> task,
        Func<T, Task<TResult>> mapper)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await task.ConfigureAwait(false);

        return await result.Match(
            Right: async value =>
            {
                var mapped = await mapper(value).ConfigureAwait(false);
                return Right<TError, TResult>(mapped);
            },
            Left: error => Task.FromResult(Left<TError, TResult>(error)))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Async tap operation for Task-wrapped Either.
    /// </summary>
    public static async Task<Either<TError, T>> TapAsync<TError, T>(
        this Task<Either<TError, T>> task,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(action);

        var result = await task.ConfigureAwait(false);

        return await result.Match(
            Right: async value =>
            {
                await action(value).ConfigureAwait(false);
                return Right<TError, T>(value);
            },
            Left: error => Task.FromResult(Left<TError, T>(error)))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Async bind operation for Either with async binder.
    /// </summary>
    public static async Task<Either<TError, TResult>> BindAsync<TError, T, TResult>(
        this Either<TError, T> result,
        Func<T, Task<Either<TError, TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return await result.Match(
            Right: binder,
            Left: error => Task.FromResult(Left<TError, TResult>(error)))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Async map operation for Either with async mapper.
    /// </summary>
    public static async Task<Either<TError, TResult>> MapAsync<TError, T, TResult>(
        this Either<TError, T> result,
        Func<T, Task<TResult>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return await result.Match(
            Right: async value =>
            {
                var mapped = await mapper(value).ConfigureAwait(false);
                return Right<TError, TResult>(mapped);
            },
            Left: error => Task.FromResult(Left<TError, TResult>(error)))
            .ConfigureAwait(false);
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Converts an Either to an Option, discarding the error.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>Some(value) on success; None on error.</returns>
    public static Option<T> ToOption<TError, T>(this Either<TError, T> result)
    {
        return result.Match(
            Right: value => Some(value),
            Left: _ => None);
    }

    /// <summary>
    /// Converts an Option to an Either, using a factory for the error case.
    /// </summary>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <param name="errorFactory">Factory to create the error if None.</param>
    /// <returns>Right(value) if Some; Left(error) if None.</returns>
    public static Either<TError, T> ToEither<TError, T>(
        this Option<T> option,
        Func<TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return option.Match(
            Some: value => Right<TError, T>(value),
            None: () => Left<TError, T>(errorFactory()));
    }

    /// <summary>
    /// Extracts the success value or throws if error.
    /// </summary>
    /// <typeparam name="TError">The error type (must be or contain Exception).</typeparam>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="result">The result to unwrap.</param>
    /// <param name="exceptionFactory">Factory to create exception from error.</param>
    /// <returns>The success value.</returns>
    /// <exception cref="Exception">Thrown when result is Left.</exception>
    public static T GetOrThrow<TError, T>(
        this Either<TError, T> result,
        Func<TError, Exception> exceptionFactory)
    {
        ArgumentNullException.ThrowIfNull(exceptionFactory);

        return result.Match(
            Right: value => value,
            Left: error => throw exceptionFactory(error));
    }

    #endregion
}
