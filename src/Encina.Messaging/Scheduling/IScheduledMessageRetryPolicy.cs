namespace Encina.Messaging.Scheduling;

/// <summary>
/// Pluggable retry strategy for failed scheduled message dispatches.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction is queried by <see cref="SchedulerOrchestrator"/> whenever a scheduled
/// message dispatch fails (either because the dispatch callback returned a <c>Left</c>, or
/// because an unexpected exception escaped the safety net). The orchestrator delegates the
/// "when to retry next" decision entirely to the policy — there is no inline math in the
/// orchestrator itself.
/// </para>
/// <para>
/// <b>Determinism</b>: <see cref="Compute"/> is synchronous and pure. The orchestrator
/// owns the <see cref="TimeProvider"/> and passes <c>nowUtc</c> explicitly so the policy
/// is trivially testable against arbitrary <c>(retryCount, maxRetries, now)</c> tuples
/// without any time injection of its own.
/// </para>
/// <para>
/// <b>Default implementation</b>: <see cref="ExponentialBackoffRetryPolicy"/> computes
/// <c>delay = SchedulingOptions.BaseRetryDelay * 2^retryCount</c> and dead-letters once
/// <c>retryCount + 1 &gt;= maxRetries</c>.
/// </para>
/// <para>
/// <b>Custom policies</b>: register your own implementation in DI before calling
/// <c>AddEncina*()</c>; the messaging core uses <c>TryAddSingleton</c> so user
/// registrations win.
/// </para>
/// </remarks>
public interface IScheduledMessageRetryPolicy
{
    /// <summary>
    /// Computes the retry decision for a scheduled message that just failed dispatch.
    /// </summary>
    /// <param name="retryCount">
    /// The number of times the message has already been retried (i.e. its current
    /// <see cref="IScheduledMessage.RetryCount"/> value <em>before</em> this failure
    /// is recorded). A value of <c>0</c> means this is the first failure.
    /// </param>
    /// <param name="maxRetries">
    /// The maximum number of attempts allowed for the message, sourced from
    /// <see cref="SchedulingOptions.MaxRetries"/>.
    /// </param>
    /// <param name="nowUtc">
    /// The current UTC time as observed by the orchestrator's
    /// <see cref="TimeProvider"/>. Passed in so the policy is deterministic and does
    /// not need its own time source.
    /// </param>
    /// <returns>
    /// A <see cref="RetryDecision"/> describing whether to schedule another retry
    /// (with the next attempt time) or to dead-letter the message permanently.
    /// </returns>
    /// <remarks>
    /// Implementations MUST be deterministic and side-effect free for the same input
    /// triple. Logging, metrics and store updates are the orchestrator's responsibility.
    /// </remarks>
    RetryDecision Compute(int retryCount, int maxRetries, DateTime nowUtc);
}
