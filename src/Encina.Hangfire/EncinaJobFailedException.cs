namespace Encina.Hangfire;

/// <summary>
/// Exception thrown by <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> and
/// <see cref="HangfireNotificationJobAdapter{TNotification}"/> when the Encina handler reports a
/// <em>transient</em> failure through the <c>Left</c> case of <see cref="LanguageExt.Either{L, R}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Hangfire only marks a job as failed, and applies its retry policy, when the job method throws. The
/// adapters classify every <c>Left</c> with <see cref="Encina.Messaging.Recoverability.IErrorClassifier"/>:
/// transient (and unclassified) failures throw this exception so the job is retried; permanent failures
/// throw <see cref="EncinaJobPermanentFailureException"/> instead, and an Encina cancellation error while the
/// job's token is cancelled throws <see cref="OperationCanceledException"/>. The Quartz integration
/// (<c>Encina.Quartz</c>) applies the same semantics with Quartz's <c>JobExecutionException</c>.
/// </para>
/// <para>
/// Hangfire persists the exception type, message and details (including the full
/// <see cref="Exception.InnerException"/> chain) of every failed attempt in its storage. The
/// <see cref="Exception.Message"/> therefore contains only the error code and a generic text, never
/// <see cref="EncinaError.Message"/>, which may contain data-subject identifiers or other personal data.
/// <see cref="Exception.Data"/> carries only the error code (<see cref="ErrorCodeDataKey"/>). When the
/// error carries a cause, <see cref="Exception.InnerException"/> is a sanitized exception whose message
/// is only the cause's type name — never the cause's own <see cref="Exception.Message"/>, which may also
/// carry personal data.
/// </para>
/// </remarks>
public sealed class EncinaJobFailedException : Exception
{
    /// <summary>
    /// The <see cref="Exception.Data"/> key under which the Encina error code is stored.
    /// </summary>
    public const string ErrorCodeDataKey = "Encina.ErrorCode";

    private const string UnknownCode = "encina.unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class from the
    /// <see cref="EncinaError"/> returned by the failed handler.
    /// </summary>
    /// <param name="error">The Encina error describing the handler failure.</param>
    public EncinaJobFailedException(EncinaError error)
        : base(BuildMessage(ResolveCode(error)), ResolveInnerException(error))
    {
        ErrorCode = ResolveCode(error);
        Data[ErrorCodeDataKey] = ErrorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class.
    /// </summary>
    public EncinaJobFailedException()
        : base("The Encina job failed with a transient error.")
    {
        ErrorCode = UnknownCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobFailedException"/> class with a
    /// specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EncinaJobFailedException(string message)
        : base(message)
    {
        ErrorCode = UnknownCode;
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
        ErrorCode = UnknownCode;
    }

    /// <summary>
    /// Gets the Encina error code (<see cref="EncinaErrorExtensions.GetCode(EncinaError)"/>) that caused
    /// the job to fail, or <c>encina.unknown</c> when the error carries no code.
    /// </summary>
    public string ErrorCode { get; }

    private static string ResolveCode(EncinaError error) => error.GetCode().IfNone(UnknownCode);

    // Only the cause's type travels: Hangfire's FailedState persists the full exception chain
    // (including every inner exception's Message) in its own storage as ExceptionDetails, and
    // the cause's Message may carry personal data (#1259 review).
    private static Exception? ResolveInnerException(EncinaError error) =>
        error.GetCause().MatchUnsafe(
            cause => new InvalidOperationException($"Cause type: {cause.GetType().FullName ?? cause.GetType().Name}"),
            () => (Exception?)null);

    private static string BuildMessage(string code) =>
        $"The Encina job failed with transient error code '{code}'. The job can be retried.";
}
