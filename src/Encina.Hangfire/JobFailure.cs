using Encina.Messaging.Recoverability;

namespace Encina.Hangfire;

/// <summary>
/// Maps a handler <c>Left</c> result to the exception a Hangfire job adapter throws.
/// </summary>
internal static class JobFailure
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="error"/> is one of Encina's cancellation errors (the dispatcher's,
    /// the handler's, a behavior's or a pre/post-processor's <c>*.cancelled</c> code) and the
    /// job's own token was cancelled, i.e. the job was cancelled rather than failed.
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
            error.GetCause().MatchUnsafe(ex => ex, () => (Exception?)null),
            cancellationToken);

    /// <summary>
    /// Classifies <paramref name="error"/>; unknown classifications are treated as transient.
    /// </summary>
    internal static ErrorClassification Classify(EncinaError error, IErrorClassifier classifier) =>
        classifier.Classify(error, null) == ErrorClassification.Permanent
            ? ErrorClassification.Permanent
            : ErrorClassification.Transient;

    /// <summary>
    /// Creates the exception for a failed job: <see cref="EncinaJobPermanentFailureException"/> for a
    /// permanent failure, otherwise <see cref="EncinaJobFailedException"/>.
    /// </summary>
    internal static Exception Failed(EncinaError error, ErrorClassification classification) =>
        classification == ErrorClassification.Permanent
            ? new EncinaJobPermanentFailureException(error)
            : new EncinaJobFailedException(error);
}
