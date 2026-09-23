using Encina.Messaging.Outbox;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging.Outbox;

/// <summary>
/// Property-based tests for the invariants of <see cref="OutboxRetryBackoff.ComputeDelay"/> (#1150).
/// </summary>
[Trait("Category", "Property")]
public sealed class OutboxRetryBackoffPropertyTests
{
    [Property(MaxTest = 200)]
    public bool Property_Delay_NeverExceedsTheCap(NonNegativeInt retryCount, PositiveInt baseSeconds, PositiveInt maxSeconds, int ratioPercent, int samplePermille)
    {
        var (ratio, sample) = Jitter(ratioPercent, samplePermille);
        var max = TimeSpan.FromSeconds(maxSeconds.Get % 86_400 + 1);

        var delay = OutboxRetryBackoff.ComputeDelay(
            retryCount.Get, TimeSpan.FromSeconds(baseSeconds.Get % 3_600 + 1), max, ratio, sample);

        return delay >= TimeSpan.Zero && delay <= max;
    }

    [Property(MaxTest = 200)]
    public bool Property_Delay_NeverDecreasesAsRetriesGrow(NonNegativeInt retryCount, PositiveInt baseSeconds, int ratioPercent, int samplePermille)
    {
        var (ratio, sample) = Jitter(ratioPercent, samplePermille);
        var baseDelay = TimeSpan.FromSeconds(baseSeconds.Get % 3_600 + 1);
        var max = TimeSpan.FromDays(1);
        var count = retryCount.Get % 64;

        var current = OutboxRetryBackoff.ComputeDelay(count, baseDelay, max, ratio, sample);
        var next = OutboxRetryBackoff.ComputeDelay(count + 1, baseDelay, max, ratio, sample);

        return next >= current;
    }

    [Property(MaxTest = 200)]
    public bool Property_Jitter_NeverShortensTheDelayBeyondTheRatio(NonNegativeInt retryCount, PositiveInt baseSeconds, int ratioPercent, int samplePermille)
    {
        var (ratio, sample) = Jitter(ratioPercent, samplePermille);
        var baseDelay = TimeSpan.FromSeconds(baseSeconds.Get % 3_600 + 1);
        var max = TimeSpan.FromDays(1);
        var count = retryCount.Get % 64;

        var withoutJitter = OutboxRetryBackoff.ComputeDelay(count, baseDelay, max, 0, 0);
        var withJitter = OutboxRetryBackoff.ComputeDelay(count, baseDelay, max, ratio, sample);

        // One tick of tolerance for rounding to whole ticks.
        var lowerBound = (long)(withoutJitter.Ticks * (1 - ratio)) - 1;
        return withJitter <= withoutJitter && withJitter.Ticks >= lowerBound;
    }

    [Property(MaxTest = 100)]
    public bool Property_WithoutJitter_DelayIsBaseTimesPowerOfTwoUntilTheCap(PositiveInt baseSeconds)
    {
        var baseDelay = TimeSpan.FromSeconds(baseSeconds.Get % 60 + 1);
        var max = TimeSpan.FromDays(365);

        for (var retry = 0; retry < 16; retry++)
        {
            var expected = TimeSpan.FromTicks(baseDelay.Ticks * (1L << retry));
            if (OutboxRetryBackoff.ComputeDelay(retry, baseDelay, max, 0, 0) != expected)
            {
                return false;
            }
        }

        return true;
    }

    private static (double Ratio, double Sample) Jitter(int ratioPercent, int samplePermille)
        => (Math.Abs(ratioPercent % 101) / 100.0, Math.Abs(samplePermille % 1_000) / 1_000.0);
}
