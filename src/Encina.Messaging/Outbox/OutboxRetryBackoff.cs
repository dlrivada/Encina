namespace Encina.Messaging.Outbox;

/// <summary>
/// Computes the delay before a failed outbox message is retried: exponential backoff, capped,
/// with jitter.
/// </summary>
/// <remarks>
/// <para>
/// The delay is <c>min(baseDelay * 2^retryCount, maxDelay) * (1 - jitterRatio * jitterSample)</c>.
/// The result therefore never exceeds the maximum delay, never decreases when <c>retryCount</c>
/// grows (for the same jitter sample), and is never shorter than <c>(1 - jitterRatio)</c> times
/// the capped delay.
/// </para>
/// <para>
/// <c>retryCount</c> is the number of failures recorded before the current one, so the first
/// failure waits <c>baseDelay</c>, the second <c>2 * baseDelay</c>, and so on.
/// </para>
/// </remarks>
public static class OutboxRetryBackoff
{
    /// <summary>
    /// Computes the retry delay for a message that has already failed <paramref name="retryCount"/> times.
    /// </summary>
    /// <param name="retryCount">The number of failures recorded before the current one.</param>
    /// <param name="baseDelay">The delay after the first failure.</param>
    /// <param name="maxDelay">The upper bound of the delay.</param>
    /// <param name="jitterRatio">The fraction of the delay that is randomised, between 0 and 1.</param>
    /// <param name="jitterSample">A uniformly distributed sample in <c>[0, 1)</c>.</param>
    /// <returns>The delay to wait before the next attempt.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount"/> is negative, a delay is negative,
    /// <paramref name="jitterRatio"/> is not between 0 and 1, or <paramref name="jitterSample"/>
    /// is not in <c>[0, 1)</c>.
    /// </exception>
    public static TimeSpan ComputeDelay(
        int retryCount,
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        double jitterRatio,
        double jitterSample)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(baseDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, TimeSpan.Zero);

        if (double.IsNaN(jitterRatio) || jitterRatio < 0 || jitterRatio > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterRatio), jitterRatio, "The jitter ratio must be between 0 and 1.");
        }

        if (double.IsNaN(jitterSample) || jitterSample < 0 || jitterSample >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(jitterSample), jitterSample, "The jitter sample must be in [0, 1).");
        }

        if (baseDelay == TimeSpan.Zero || maxDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        // Math.Pow overflows to +Infinity for large retry counts; Math.Min then returns the cap.
        var exponentialTicks = baseDelay.Ticks * Math.Pow(2, retryCount);
        var cappedTicks = Math.Min(exponentialTicks, maxDelay.Ticks);
        var jitteredTicks = cappedTicks * (1 - (jitterRatio * jitterSample));

        // A double cannot represent every tick count exactly (TimeSpan.MaxValue rounds up past
        // long.MaxValue), so a value at or above the cap returns the cap itself.
        if (jitteredTicks >= maxDelay.Ticks)
        {
            return maxDelay;
        }

        return TimeSpan.FromTicks((long)Math.Round(jitteredTicks));
    }
}
