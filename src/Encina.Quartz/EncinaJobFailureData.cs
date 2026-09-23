namespace Encina.Quartz;

/// <summary>
/// Keys of the <see cref="Exception.Data"/> entries that <see cref="QuartzRequestJob{TRequest, TResponse}"/>
/// and <see cref="QuartzNotificationJob{TNotification}"/> set on the <c>JobExecutionException</c> they
/// throw when the Encina handler returns a <c>Left</c> result.
/// </summary>
/// <remarks>
/// Quartz has no retry policy of its own (a failed job is not refired, because
/// <c>RefireImmediately</c> is <c>false</c>). An <c>IJobListener</c> can read these entries in
/// <c>JobWasExecuted</c> to reschedule transient failures and alert on permanent ones.
/// </remarks>
/// <example>
/// <code>
/// public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken ct)
/// {
///     if (jobException?.Data[EncinaJobFailureData.ErrorClassificationKey] is "Transient")
///     {
///         // reschedule the job with a back-off trigger
///     }
///     return Task.CompletedTask;
/// }
/// </code>
/// </example>
public static class EncinaJobFailureData
{
    /// <summary>
    /// Key of the Encina error code (a <see cref="string"/>, <c>encina.unknown</c> when the error has no code).
    /// </summary>
    public const string ErrorCodeKey = "Encina.ErrorCode";

    /// <summary>
    /// Key of the failure classification: <c>"Permanent"</c> or <c>"Transient"</c> (unclassified failures
    /// are reported as transient), as decided by <c>Encina.Messaging.Recoverability.IErrorClassifier</c>.
    /// </summary>
    public const string ErrorClassificationKey = "Encina.ErrorClassification";
}
