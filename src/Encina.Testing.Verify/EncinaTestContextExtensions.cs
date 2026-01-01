using LanguageExt;

namespace Encina.Testing.Verify;

/// <summary>
/// Extension methods for preparing test context results for Verify snapshot testing.
/// </summary>
/// <remarks>
/// <para>
/// These extensions allow direct integration between <c>EncinaTestContext</c> results
/// and Verify snapshot testing without requiring explicit extraction.
/// </para>
/// <para>
/// <b>Important</b>: Call <see cref="EncinaVerifySettings.Initialize"/> once in your
/// test project before using these methods.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = await fixture.SendAsync(new CreateOrderCommand(...));
/// await Verify(context.ForVerify());
/// </code>
/// </example>
public static class EncinaTestContextExtensions
{
    /// <summary>
    /// Prepares an Either result for Verify snapshot testing.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result to prepare.</param>
    /// <returns>An object suitable for snapshot verification.</returns>
    /// <remarks>
    /// This is equivalent to calling <see cref="EncinaVerify.PrepareEither{TLeft, TRight}"/>
    /// but provides a more convenient extension method syntax.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(command);
    /// await Verify(result.ForVerify());
    /// </code>
    /// </example>
    public static object ForVerify<TResponse>(this Either<EncinaError, TResponse> result)
    {
        return EncinaVerify.PrepareEither(result);
    }

    /// <summary>
    /// Prepares a success value for Verify snapshot testing.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result.</param>
    /// <returns>The success value for snapshot verification.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is an error.</exception>
    /// <remarks>
    /// Use this when you expect the result to be successful and want to verify the response value.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(command);
    /// await Verify(result.SuccessForVerify());
    /// </code>
    /// </example>
    public static TResponse SuccessForVerify<TResponse>(this Either<EncinaError, TResponse> result)
    {
        return EncinaVerify.ExtractSuccess(result);
    }

    /// <summary>
    /// Prepares an error value for Verify snapshot testing.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="result">The Either result.</param>
    /// <returns>The error for snapshot verification.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is a success.</exception>
    /// <remarks>
    /// Use this when you expect the result to be an error and want to verify the error details.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await handler.Handle(invalidCommand);
    /// await Verify(result.ErrorForVerify());
    /// </code>
    /// </example>
    public static EncinaError ErrorForVerify<TResponse>(this Either<EncinaError, TResponse> result)
    {
        return EncinaVerify.ExtractError(result);
    }
}
