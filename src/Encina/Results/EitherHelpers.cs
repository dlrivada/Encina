using LanguageExt;

namespace Encina;

/// <summary>
/// Helper methods for wrapping exception-throwing operations into <see cref="Either{L,R}"/> results.
/// </summary>
/// <remarks>
/// <para>
/// These helpers are designed for infrastructure boundaries where external libraries or database
/// drivers throw exceptions that need to be captured as <see cref="EncinaError"/> values.
/// </para>
/// <para>
/// <see cref="OperationCanceledException"/> is always re-thrown (cancellation is not an error).
/// Guard clause exceptions (<see cref="ArgumentNullException"/>, etc.) are also re-thrown
/// (programming errors should not be captured as business errors).
/// </para>
/// </remarks>
public static class EitherHelpers
{
    /// <summary>
    /// Wraps an async void operation in an <see cref="Either{EncinaError, Unit}"/> result.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="errorCode">The error code to use if the operation fails.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> wrapping the caught exception.
    /// </returns>
    public static async Task<Either<EncinaError, Unit>> TryAsync(
        Func<Task> operation,
        string errorCode)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return Unit.Default;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(errorCode, ex);
        }
    }

    /// <summary>
    /// Wraps an async void operation in an <see cref="Either{EncinaError, Unit}"/> result
    /// with a contextual message.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="errorCode">The error code to use if the operation fails.</param>
    /// <param name="message">Contextual message describing the failed operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> wrapping the caught exception.
    /// </returns>
    public static async Task<Either<EncinaError, Unit>> TryAsync(
        Func<Task> operation,
        string errorCode,
        string message)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return Unit.Default;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(errorCode, ex, message);
        }
    }

    /// <summary>
    /// Wraps an async operation returning <typeparamref name="T"/> in an <see cref="Either{EncinaError, T}"/> result.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="errorCode">The error code to use if the operation fails.</param>
    /// <returns>
    /// The operation result on success, or an <see cref="EncinaError"/> wrapping the caught exception.
    /// </returns>
    public static async Task<Either<EncinaError, T>> TryAsync<T>(
        Func<Task<T>> operation,
        string errorCode)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(errorCode, ex);
        }
    }

    /// <summary>
    /// Wraps an async operation returning <typeparamref name="T"/> in an <see cref="Either{EncinaError, T}"/> result
    /// with a contextual message.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="errorCode">The error code to use if the operation fails.</param>
    /// <param name="message">Contextual message describing the failed operation.</param>
    /// <returns>
    /// The operation result on success, or an <see cref="EncinaError"/> wrapping the caught exception.
    /// </returns>
    public static async Task<Either<EncinaError, T>> TryAsync<T>(
        Func<Task<T>> operation,
        string errorCode,
        string message)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return EncinaErrors.FromException(errorCode, ex, message);
        }
    }
}
