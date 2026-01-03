using LanguageExt;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace Encina.Testing.TUnit;

/// <summary>
/// TUnit assertion extensions for <see cref="Either{L,R}"/> types.
/// Provides async-first assertions compatible with TUnit's assertion model.
/// </summary>
/// <remarks>
/// <para>
/// TUnit uses an async assertion model with <c>Assert.That()</c>. These extensions
/// provide ergonomic methods for testing Railway Oriented Programming results
/// while maintaining TUnit's assertion patterns.
/// </para>
/// <para>
/// All assertions are async by design, following TUnit's philosophy of async-first testing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// public async Task CreateOrder_ShouldSucceed()
/// {
///     var result = await encina.Send(new CreateOrderCommand());
///
///     // Using extension methods
///     await result.ShouldBeSuccessAsync();
///
///     // Or using AndReturn pattern
///     var order = await result.AndReturnAsync();
///     await Assert.That(order.Id).IsNotEqualTo(Guid.Empty);
/// }
/// </code>
/// </example>
public static class TUnitEitherAssertions
{
    #region Success Assertions

    /// <summary>
    /// Asserts that the result is a success (Right) and returns the success value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the success value.</returns>
    /// <exception cref="Exception">
    /// Thrown when the result is an error (Left).
    /// </exception>
    public static async Task<TRight> ShouldBeSuccessAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either)
    {
        var errorMessage = either.Match(
            Right: _ => string.Empty,
            Left: e => e is null ? "null" : e.ToString());

        await Assert.That(either.IsRight)
            .IsTrue()
            .Because($"Expected success (Right) but got error (Left): {errorMessage}");

        return (TRight)either;
    }

    /// <summary>
    /// Asserts that the result is a success (Right) with the expected value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="expected">The expected success value.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldBeSuccessAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        TRight expected)
    {
        var actual = await either.ShouldBeSuccessAsync();
        await Assert.That(actual).IsEqualTo(expected);
    }

    /// <summary>
    /// Asserts that the result is a success (Right) and validates using a custom assertion.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="validator">An async action to validate the success value.</param>
    /// <returns>A task that completes with the success value after validation.</returns>
    public static async Task<TRight> ShouldBeSuccessAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Func<TRight, Task> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var value = await either.ShouldBeSuccessAsync();
        await validator(value);
        return value;
    }

    /// <summary>
    /// Returns the success value for further assertions. Alias for <see cref="ShouldBeSuccessAsync{TLeft,TRight}(Either{TLeft,TRight})"/>.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the success value.</returns>
    public static Task<TRight> AndReturnAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either)
        => either.ShouldBeSuccessAsync();

    #endregion

    #region Error Assertions

    /// <summary>
    /// Asserts that the result is an error (Left) and returns the error value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error value.</returns>
    /// <exception cref="Exception">
    /// Thrown when the result is a success (Right).
    /// </exception>
    public static async Task<TLeft> ShouldBeErrorAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either)
    {
        var successMessage = either.Match(
            Right: v => v?.ToString() ?? "null",
            Left: _ => string.Empty);

        await Assert.That(either.IsLeft)
            .IsTrue()
            .Because($"Expected error (Left) but got success (Right): {successMessage}");

        return either.Match(
            Right: _ => throw new InvalidOperationException("Unreachable"),
            Left: error => error);
    }

    /// <summary>
    /// Asserts that the result is an error (Left) and validates using a custom assertion.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="validator">An async action to validate the error value.</param>
    /// <returns>A task that completes with the error value after validation.</returns>
    public static async Task<TLeft> ShouldBeErrorAsync<TLeft, TRight>(
        this Either<TLeft, TRight> either,
        Func<TLeft, Task> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var error = await either.ShouldBeErrorAsync();
        await validator(error);
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
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeErrorWithCodeAsync<TRight>(
        this Either<EncinaError, TRight> either,
        string expectedCode)
    {
        var error = await either.ShouldBeErrorAsync();
        var actualCode = error.GetCode().IfNone(string.Empty);

        await Assert.That(actualCode)
            .IsEqualTo(expectedCode)
            .Because($"Expected error code '{expectedCode}' but got '{actualCode}'");

        return error;
    }

    /// <summary>
    /// Asserts that the result is an error with a message containing the specified text.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <param name="expectedMessagePart">The text that should be contained in the error message.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeErrorContainingAsync<TRight>(
        this Either<EncinaError, TRight> either,
        string expectedMessagePart)
    {
        var error = await either.ShouldBeErrorAsync();

        await Assert.That(error.Message)
            .Contains(expectedMessagePart)
            .Because($"Expected error message to contain '{expectedMessagePart}'");

        return error;
    }

    /// <summary>
    /// Asserts that the result is a validation error (code starts with "encina.validation").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static Task<EncinaError> ShouldBeValidationErrorAsync<TRight>(
        this Either<EncinaError, TRight> either)
        => ShouldBeErrorWithCodePrefixAsync(either, "encina.validation", "validation");

    /// <summary>
    /// Asserts that the result is an authorization error (code starts with "encina.authorization").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static Task<EncinaError> ShouldBeAuthorizationErrorAsync<TRight>(
        this Either<EncinaError, TRight> either)
        => ShouldBeErrorWithCodePrefixAsync(either, "encina.authorization", "authorization");

    /// <summary>
    /// Asserts that the result is a not found error (code starts with "encina.notfound").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static Task<EncinaError> ShouldBeNotFoundErrorAsync<TRight>(
        this Either<EncinaError, TRight> either)
        => ShouldBeErrorWithCodePrefixAsync(either, "encina.notfound", "not found");

    /// <summary>
    /// Asserts that the result is a conflict error (code starts with "encina.conflict").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static Task<EncinaError> ShouldBeConflictErrorAsync<TRight>(
        this Either<EncinaError, TRight> either)
        => ShouldBeErrorWithCodePrefixAsync(either, "encina.conflict", "conflict");

    /// <summary>
    /// Asserts that the result is an internal error (code starts with "encina.internal").
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="either">The Either result to assert.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static Task<EncinaError> ShouldBeInternalErrorAsync<TRight>(
        this Either<EncinaError, TRight> either)
        => ShouldBeErrorWithCodePrefixAsync(either, "encina.internal", "internal");

    private static async Task<EncinaError> ShouldBeErrorWithCodePrefixAsync<TRight>(
        Either<EncinaError, TRight> either,
        string codePrefix,
        string errorTypeName)
    {
        var error = await either.ShouldBeErrorAsync();
        var code = error.GetCode().IfNone(string.Empty);

        await Assert.That(code.StartsWith(codePrefix, StringComparison.OrdinalIgnoreCase))
            .IsTrue()
            .Because($"Expected {errorTypeName} error but got code '{code}'");

        return error;
    }

    #endregion

    #region Task Extensions

    /// <summary>
    /// Awaits the task and asserts that the result is a success (Right).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the success value.</returns>
    public static async Task<TRight> ShouldBeSuccessAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeSuccessAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is a success (Right) with the expected value.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <param name="expected">The expected success value.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task ShouldBeSuccessAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task,
        TRight expected)
    {
        var result = await task.ConfigureAwait(false);
        await result.ShouldBeSuccessAsync(expected);
    }

    /// <summary>
    /// Awaits the task and asserts that the result is an error (Left).
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error value.</returns>
    public static async Task<TLeft> ShouldBeErrorAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeErrorAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is an error with the specified code.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <param name="expectedCode">The expected error code.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeErrorWithCodeAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task,
        string expectedCode)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeErrorWithCodeAsync(expectedCode);
    }

    /// <summary>
    /// Awaits the task and asserts that the result is a validation error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeValidationErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeValidationErrorAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is an authorization error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeAuthorizationErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeAuthorizationErrorAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is a not found error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeNotFoundErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeNotFoundErrorAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is a conflict error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeConflictErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeConflictErrorAsync();
    }

    /// <summary>
    /// Awaits the task and asserts that the result is an internal error.
    /// </summary>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the error for further assertions.</returns>
    public static async Task<EncinaError> ShouldBeInternalErrorAsync<TRight>(
        this Task<Either<EncinaError, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.ShouldBeInternalErrorAsync();
    }

    /// <summary>
    /// Awaits the task and returns the success value for further assertions.
    /// </summary>
    /// <typeparam name="TLeft">The error type.</typeparam>
    /// <typeparam name="TRight">The success type.</typeparam>
    /// <param name="task">The task containing the Either result.</param>
    /// <returns>A task that completes with the success value.</returns>
    public static async Task<TRight> AndReturnAsync<TLeft, TRight>(
        this Task<Either<TLeft, TRight>> task)
    {
        var result = await task.ConfigureAwait(false);
        return await result.AndReturnAsync();
    }

    #endregion
}
