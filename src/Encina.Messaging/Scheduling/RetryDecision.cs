namespace Encina.Messaging.Scheduling;

/// <summary>
/// Outcome of a single call to <see cref="IScheduledMessageRetryPolicy.Compute"/>.
/// </summary>
/// <param name="NextRetryAtUtc">
/// When the next retry should occur, expressed in UTC. <see langword="null"/> if and
/// only if <paramref name="IsDeadLettered"/> is <see langword="true"/>.
/// </param>
/// <param name="IsDeadLettered">
/// <see langword="true"/> when the message has exhausted its retry budget and must not
/// be retried again. <see langword="false"/> when the orchestrator should reschedule
/// the next attempt at <paramref name="NextRetryAtUtc"/>.
/// </param>
/// <remarks>
/// <para>
/// <b>Invariant</b>: a well-formed decision satisfies
/// <c>(IsDeadLettered == true) ⇔ (NextRetryAtUtc == null)</c>. Use the
/// <see cref="RetryAt"/> and <see cref="DeadLetter"/> factory helpers to guarantee
/// the invariant by construction; the constructor itself does not validate the
/// combination so that <c>IScheduledMessageRetryPolicy</c> implementations remain
/// allocation-free.
/// </para>
/// <para>
/// The <see cref="IScheduledMessageStore.MarkAsFailedAsync"/> contract treats a
/// <see langword="null"/> <c>nextRetryAtUtc</c> as the dead-letter signal, so the
/// orchestrator passes <see cref="NextRetryAtUtc"/> through unchanged.
/// </para>
/// </remarks>
public sealed record RetryDecision(DateTime? NextRetryAtUtc, bool IsDeadLettered)
{
    /// <summary>
    /// Creates a decision that schedules the next retry at the given UTC time.
    /// </summary>
    /// <param name="nextUtc">When the next retry should occur (UTC).</param>
    /// <returns>A non-dead-lettered <see cref="RetryDecision"/>.</returns>
    public static RetryDecision RetryAt(DateTime nextUtc) => new(nextUtc, IsDeadLettered: false);

    /// <summary>
    /// Creates a decision that marks the message as dead-lettered (no further retries).
    /// </summary>
    /// <returns>A dead-lettered <see cref="RetryDecision"/> with a null next retry time.</returns>
    public static RetryDecision DeadLetter() => new(NextRetryAtUtc: null, IsDeadLettered: true);
}
