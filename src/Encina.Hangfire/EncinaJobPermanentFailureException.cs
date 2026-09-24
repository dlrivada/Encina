namespace Encina.Hangfire;

/// <summary>
/// Exception thrown by <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> and
/// <see cref="HangfireNotificationJobAdapter{TNotification}"/> when the Encina handler reports a
/// <em>permanent</em> failure — one that retrying the same job cannot fix, such as a validation
/// failure, missing consent, an active processing restriction or a missing handler.
/// </summary>
/// <remarks>
/// <para>
/// The adapters classify every <c>Left</c> result with
/// <see cref="Encina.Messaging.Recoverability.IErrorClassifier"/> (the registered one, or
/// <see cref="Encina.Messaging.Recoverability.DefaultErrorClassifier"/>). A
/// <see cref="Encina.Messaging.Recoverability.ErrorClassification.Permanent"/> classification throws this
/// exception; transient and unclassified failures throw <see cref="EncinaJobFailedException"/>.
/// </para>
/// <para>
/// Hangfire's default <c>AutomaticRetryAttribute</c> retries every exception, including this one. To
/// stop retrying permanent failures, exclude this type from retries — for example with
/// <see cref="EncinaAutomaticRetry.UseEncinaAutomaticRetry(global::Hangfire.Common.JobFilterCollection, int)"/>,
/// which replaces the global retry filter with one whose <c>ExceptOn</c> contains this exception type.
/// </para>
/// <para>
/// As with <see cref="EncinaJobFailedException"/>, the <see cref="Exception.Message"/> contains only the
/// error code and a generic text (never <see cref="EncinaError.Message"/>, which may contain personal
/// data), <see cref="Exception.Data"/> carries only the error code, and, when the error carries a cause,
/// <see cref="Exception.InnerException"/> is a sanitized exception whose message is only the cause's type
/// name — never the cause's own <see cref="Exception.Message"/>, which Hangfire would otherwise persist
/// in full in its <c>ExceptionDetails</c>.
/// </para>
/// </remarks>
public sealed class EncinaJobPermanentFailureException : Exception
{
    /// <summary>
    /// The <see cref="Exception.Data"/> key under which the Encina error code is stored.
    /// </summary>
    public const string ErrorCodeDataKey = EncinaJobFailedException.ErrorCodeDataKey;

    private const string UnknownCode = "encina.unknown";

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobPermanentFailureException"/> class from
    /// the <see cref="EncinaError"/> returned by the failed handler.
    /// </summary>
    /// <param name="error">The Encina error describing the handler failure.</param>
    public EncinaJobPermanentFailureException(EncinaError error)
        : base(BuildMessage(ResolveCode(error)), ResolveInnerException(error))
    {
        ErrorCode = ResolveCode(error);
        Data[ErrorCodeDataKey] = ErrorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobPermanentFailureException"/> class.
    /// </summary>
    public EncinaJobPermanentFailureException()
        : base("The Encina job failed with a permanent error.")
    {
        ErrorCode = UnknownCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobPermanentFailureException"/> class with a
    /// specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public EncinaJobPermanentFailureException(string message)
        : base(message)
    {
        ErrorCode = UnknownCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaJobPermanentFailureException"/> class with a
    /// specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EncinaJobPermanentFailureException(string message, Exception innerException)
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
        $"The Encina job failed with permanent error code '{code}'. Retrying the job will not succeed.";
}
