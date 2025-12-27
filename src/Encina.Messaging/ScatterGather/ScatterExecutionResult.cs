using LanguageExt;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Factory methods for creating <see cref="ScatterExecutionResult{TResponse}"/> instances.
/// </summary>
public static class ScatterExecutionResult
{
    /// <summary>
    /// Creates a successful scatter execution result.
    /// </summary>
    /// <typeparam name="TResponse">The type of response from the scatter handler.</typeparam>
    /// <param name="handlerName">The name of the scatter handler.</param>
    /// <param name="response">The successful response.</param>
    /// <param name="duration">The duration of the execution.</param>
    /// <param name="startedAtUtc">The UTC timestamp when execution started.</param>
    /// <param name="completedAtUtc">The UTC timestamp when execution completed.</param>
    /// <returns>A successful scatter execution result.</returns>
    public static ScatterExecutionResult<TResponse> Success<TResponse>(
        string handlerName,
        TResponse response,
        TimeSpan duration,
        DateTime startedAtUtc,
        DateTime completedAtUtc)
        => new(handlerName, response, duration, startedAtUtc, completedAtUtc);

    /// <summary>
    /// Creates a failed scatter execution result.
    /// </summary>
    /// <typeparam name="TResponse">The type of response from the scatter handler.</typeparam>
    /// <param name="handlerName">The name of the scatter handler.</param>
    /// <param name="error">The error that occurred.</param>
    /// <param name="duration">The duration of the execution.</param>
    /// <param name="startedAtUtc">The UTC timestamp when execution started.</param>
    /// <param name="completedAtUtc">The UTC timestamp when execution completed.</param>
    /// <returns>A failed scatter execution result.</returns>
    public static ScatterExecutionResult<TResponse> Failure<TResponse>(
        string handlerName,
        EncinaError error,
        TimeSpan duration,
        DateTime startedAtUtc,
        DateTime completedAtUtc)
        => new(handlerName, error, duration, startedAtUtc, completedAtUtc);
}

/// <summary>
/// Represents the result of executing a single scatter handler.
/// </summary>
/// <typeparam name="TResponse">The type of response from the scatter handler.</typeparam>
public sealed class ScatterExecutionResult<TResponse>
{
    /// <summary>
    /// Gets the name of the scatter handler.
    /// </summary>
    public string HandlerName { get; }

    /// <summary>
    /// Gets the result of the scatter handler execution.
    /// </summary>
    /// <remarks>
    /// Left contains the error if the handler failed.
    /// Right contains the successful response.
    /// </remarks>
    public Either<EncinaError, TResponse> Result { get; }

    /// <summary>
    /// Gets the duration of the scatter handler execution.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the UTC timestamp when the handler started execution.
    /// </summary>
    public DateTime StartedAtUtc { get; }

    /// <summary>
    /// Gets the UTC timestamp when the handler completed execution.
    /// </summary>
    public DateTime CompletedAtUtc { get; }

    /// <summary>
    /// Gets whether the scatter handler executed successfully.
    /// </summary>
    public bool IsSuccess => Result.IsRight;

    /// <summary>
    /// Gets whether the scatter handler failed.
    /// </summary>
    public bool IsFailure => Result.IsLeft;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterExecutionResult{TResponse}"/> class.
    /// </summary>
    /// <param name="handlerName">The name of the scatter handler.</param>
    /// <param name="result">The result of the handler execution.</param>
    /// <param name="duration">The duration of the execution.</param>
    /// <param name="startedAtUtc">The UTC timestamp when execution started.</param>
    /// <param name="completedAtUtc">The UTC timestamp when execution completed.</param>
    public ScatterExecutionResult(
        string handlerName,
        Either<EncinaError, TResponse> result,
        TimeSpan duration,
        DateTime startedAtUtc,
        DateTime completedAtUtc)
    {
        HandlerName = handlerName;
        Result = result;
        Duration = duration;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
    }
}
