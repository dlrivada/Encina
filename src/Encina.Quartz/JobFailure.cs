using Encina.Messaging.Recoverability;
using Quartz;

namespace Encina.Quartz;

/// <summary>
/// Maps a handler <c>Left</c> result to the exception a Quartz job throws.
/// </summary>
internal static class JobFailure
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="error"/> is one of Encina's cancellation errors (the dispatcher's,
    /// the handler's, a behavior's or a pre/post-processor's <c>*.cancelled</c> code) and the
    /// job's own token was cancelled, i.e. the job was cancelled (for example on scheduler shutdown)
    /// rather than failed.
    /// </summary>
    internal static bool IsJobCancellation(EncinaError error, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var code = error.GetCode().IfNone(string.Empty);
        return code is EncinaErrorCodes.RequestCancelled
            or EncinaErrorCodes.NotificationCancelled
            or EncinaErrorCodes.HandlerCancelled
            or EncinaErrorCodes.BehaviorCancelled
            or EncinaErrorCodes.PreProcessorCancelled
            or EncinaErrorCodes.PostProcessorCancelled;
    }

    /// <summary>
    /// Creates the <see cref="OperationCanceledException"/> thrown for a cancelled job.
    /// </summary>
    internal static OperationCanceledException Cancelled(EncinaError error, CancellationToken cancellationToken) =>
        new(
            "The Encina job was cancelled.",
            InnerException(error),
            cancellationToken);

    /// <summary>
    /// Classifies <paramref name="error"/>, passing the exception that caused it (if any) to the
    /// classifier; unknown classifications are treated as transient.
    /// </summary>
    internal static ErrorClassification Classify(EncinaError error, IErrorClassifier classifier) =>
        classifier.Classify(error, InnerException(error)) == ErrorClassification.Permanent
            ? ErrorClassification.Permanent
            : ErrorClassification.Transient;

    /// <summary>
    /// Creates the <see cref="JobExecutionException"/> thrown for a failed job.
    /// </summary>
    /// <remarks>
    /// The message contains only the error code and the classification, never
    /// <see cref="EncinaError.Message"/> (which may contain personal data). <see cref="Exception.Data"/>
    /// carries the error code and the classification so an <c>IJobListener</c> can decide what to do;
    /// <see cref="JobExecutionException.RefireImmediately"/> is always <c>false</c>.
    /// </remarks>
    internal static JobExecutionException Failed(EncinaError error, ErrorClassification classification)
    {
        var code = error.GetCode().IfNone(UnknownCode);
        var message = classification == ErrorClassification.Permanent
            ? $"The Encina job failed with permanent error code '{code}'. Retrying the job will not succeed."
            : $"The Encina job failed with transient error code '{code}'. The job can be retried.";

        var inner = InnerException(error);
        var exception = inner is null
            ? new JobExecutionException(message)
            : new JobExecutionException(message, inner);

        exception.RefireImmediately = false;
        exception.Data[EncinaJobFailureData.ErrorCodeKey] = code;
        exception.Data[EncinaJobFailureData.ErrorClassificationKey] = classification.ToString();
        return exception;
    }

    private const string UnknownCode = "encina.unknown";

    private static Exception? InnerException(EncinaError error) =>
        error.GetCause().MatchUnsafe(ex => ex, () => (Exception?)null);
}
