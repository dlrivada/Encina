namespace Encina.Hangfire;

/// <summary>
/// Exception thrown by <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> and
/// <see cref="HangfireNotificationJobAdapter{TNotification}"/> when the underlying Encina
/// request or notification handler reports a domain failure via
/// <see cref="LanguageExt.Either{L, R}"/>'s <c>Left</c> case.
/// </summary>
/// <remarks>
/// <para>
/// Hangfire only marks a job as failed (and applies its configured retry policy) when the job
/// method throws. Without this exception, a handler-level <c>Left</c> result would be returned
/// as a normal (non-exceptional) value, so Hangfire would record the job as succeeded and never
/// retry it, mirroring how <see cref="Encina.Quartz.QuartzRequestJob{TRequest, TResponse}"/>
/// throws a <c>JobExecutionException</c> on <c>Left</c> to trigger Quartz's retry mechanism.
/// </para>
/// </remarks>
public sealed class EncinaJobFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class from the
    /// <see cref="EncinaError"/> returned by the failed handler.
    /// </summary>
    /// <param name="error">The Encina error describing the handler failure.</param>
    public EncinaJobFailedException(EncinaError error)
        : base(BuildMessage(error))
    {
        ErrorCode = error.GetCode().IfNone(() => "Encina.unknown");

        foreach (var entry in error.GetDetails())
        {
            Data[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class.
    /// </summary>
    public EncinaJobFailedException()
        : base("The Encina job failed.")
    {
        ErrorCode = "Encina.unknown";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class with a
    /// specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EncinaJobFailedException(string message)
        : base(message)
    {
        ErrorCode = "Encina.unknown";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class with a
    /// specified error message and a reference to the inner exception that is the cause of
    /// this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EncinaJobFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "Encina.unknown";
    }

    /// <summary>
    /// Gets the <see cref="EncinaErrors"/> code that caused the job to fail.
    /// </summary>
    public string ErrorCode { get; }

    private static string BuildMessage(EncinaError error)
    {
        var code = error.GetCode().IfNone(() => "Encina.unknown");
        return $"Encina job failed with error code '{code}': {error.Message}";
    }
}
