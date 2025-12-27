using System.Text.Json.Serialization;
using LanguageExt;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// A serializable wrapper for Either results used in Durable Functions activities.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
/// <remarks>
/// <para>
/// Since Durable Functions serialize activity results, we need a serializable representation
/// of Either&lt;EncinaError, T&gt;. This class provides that wrapper with proper JSON serialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Function("ProcessPayment")]
/// public async Task&lt;ActivityResult&lt;PaymentResult&gt;&gt; ProcessPayment(
///     [ActivityTrigger] ProcessPayment command,
///     IEncina encina)
/// {
///     var result = await encina.Send(command);
///     return result.ToActivityResult();
/// }
/// </code>
/// </example>
public sealed class ActivityResult<T>
{
    /// <summary>
    /// Gets or sets whether the activity succeeded.
    /// </summary>
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the success value (null if failed).
    /// </summary>
    [JsonPropertyName("value")]
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the error code (null if succeeded).
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error message (null if succeeded).
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful activity result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful activity result.</returns>
    public static ActivityResult<T> Success(T value)
    {
        return new ActivityResult<T>
        {
            IsSuccess = true,
            Value = value
        };
    }

    /// <summary>
    /// Creates a failed activity result from an EncinaError.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>A failed activity result.</returns>
    public static ActivityResult<T> Failure(EncinaError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ActivityResult<T>
        {
            IsSuccess = false,
            ErrorCode = error.GetCode().IfNone(string.Empty),
            ErrorMessage = error.Message
        };
    }

    /// <summary>
    /// Creates a failed activity result from an error code and message.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed activity result.</returns>
    public static ActivityResult<T> Failure(string errorCode, string errorMessage)
    {
        return new ActivityResult<T>
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Converts this activity result back to an Either.
    /// </summary>
    /// <returns>The Either representation.</returns>
    public Either<EncinaError, T> ToEither()
    {
        if (IsSuccess)
        {
            return Value!;
        }

        var error = EncinaErrors.Create(
            ErrorCode ?? "durable.activity_failed",
            ErrorMessage ?? "Activity failed");

        return error;
    }
}

/// <summary>
/// Extension methods for converting Either results to ActivityResult.
/// </summary>
public static class ActivityResultExtensions
{
    /// <summary>
    /// Converts an Either result to an ActivityResult for serialization in Durable Functions.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="either">The Either result.</param>
    /// <returns>The serializable activity result.</returns>
    /// <example>
    /// <code>
    /// [Function("ProcessPayment")]
    /// public async Task&lt;ActivityResult&lt;PaymentResult&gt;&gt; ProcessPayment(
    ///     [ActivityTrigger] ProcessPayment command,
    ///     IEncina encina)
    /// {
    ///     var result = await encina.Send(command);
    ///     return result.ToActivityResult();
    /// }
    /// </code>
    /// </example>
    public static ActivityResult<T> ToActivityResult<T>(this Either<EncinaError, T> either)
    {
        return either.Match(
            Right: value => ActivityResult<T>.Success(value),
            Left: error => ActivityResult<T>.Failure(error));
    }

    /// <summary>
    /// Converts an Either Unit result to an ActivityResult for serialization.
    /// </summary>
    /// <param name="either">The Either result.</param>
    /// <returns>The serializable activity result.</returns>
    public static ActivityResult<Unit> ToActivityResult(this Either<EncinaError, Unit> either)
    {
        return either.Match(
            Right: _ => ActivityResult<Unit>.Success(Unit.Default),
            Left: error => ActivityResult<Unit>.Failure(error));
    }
}
